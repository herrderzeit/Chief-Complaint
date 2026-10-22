using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Core;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChiefComplaint.Tests.EditMode
{
    /// <summary>
    /// Covers Red Tape handling in <see cref="DeckManager"/>: <see cref="DeckManager.CleanseRedTape"/> counts every
    /// Red Tape card removed from the draw pile, hand and discard pile and pays the per-card Reputation bonus;
    /// <see cref="DeckManager.SpawnRandomRedTape"/> adds exactly one card to the hand or discard pile; and every
    /// entry of the MainBoard scene's Red Tape pool is a valid, free Red Tape card.
    /// </summary>
    [Category("Deck")]
    public class DeckManagerRedTapeTests
    {
        private const string ScenePath = "Assets/Scenes/MainBoard.unity";

        private readonly List<Object> created = new List<Object>();
        private readonly Dictionary<PropertyInfo, object> previousInstances = new Dictionary<PropertyInfo, object>();

        private GameManager gm;
        private DeckManager deck;
        private Random.State previousRandomState;

        [SetUp]
        public void Setup()
        {
            previousRandomState = Random.state;
            Random.InitState(1234);

            gm = NewComponent<GameManager>("RedTapeTestsGameManager");
            gm.maxReputation = 0; // the cap is not under test here; keep the bonus maths exact
            Override(gm);
            Override<HandView>(null); // no hand visuals: the deck's data lists are the whole story

            deck = NewComponent<DeckManager>("RedTapeTestsDeckManager");
            deck.redTapePool = new List<ActionCardSO>();
        }

        [TearDown]
        public void Teardown()
        {
            // Restore singletons before destroying ours, so their OnDestroy cannot clear a real instance.
            foreach (var kv in previousInstances) kv.Key.GetSetMethod(true).Invoke(null, new[] { kv.Value });
            previousInstances.Clear();
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
            Random.state = previousRandomState;
        }

        private T NewComponent<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            created.Add(go);
            return go.AddComponent<T>();
        }

        private void Override<T>(T instance) where T : Singleton<T>
        {
            PropertyInfo prop = typeof(Singleton<T>).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (!previousInstances.ContainsKey(prop)) previousInstances[prop] = prop.GetValue(null);
            prop.GetSetMethod(true).Invoke(null, new object[] { instance });
        }

        private ActionCardSO NewCard(string name, ActionCardType type)
        {
            var card = ScriptableObject.CreateInstance<ActionCardSO>();
            card.name = name;
            card.cardName = name;
            card.cardType = type;
            created.Add(card);
            return card;
        }

        private ActionCardSO RedTape(string name = "Test Red Tape") => NewCard(name, ActionCardType.RedTape);

        private static void AddCopies(List<ActionCardSO> pile, ActionCardSO card, int count)
        {
            for (int i = 0; i < count; i++) pile.Add(card);
        }

        // ------------------------------------------------------------------ CleanseRedTape

        // Red Tape in draw, hand, discard, bonus per card -> expected count removed
        [TestCase(1, 1, 1, 1, 3, Description = "One in each pile, bonus 1: three removed, +3 Reputation")]
        [TestCase(0, 0, 0, 1, 0, Description = "No Red Tape anywhere: nothing removed, no Reputation")]
        [TestCase(0, 3, 0, 1, 3, Description = "All three in the hand: three removed")]
        [TestCase(2, 0, 1, 2, 3, Description = "Bonus 2 per card: +6 Reputation")]
        [TestCase(1, 1, 1, 0, 3, Description = "Bonus 0: cards still removed, no Reputation")]
        public void CleanseRedTape_RemovesEveryRedTapeCardAndPaysTheBonus(int inDraw, int inHand, int inDiscard, int bonus, int expected)
        {
            ActionCardSO redTape = RedTape();
            ActionCardSO keepDraw = NewCard("Keep Draw", ActionCardType.Treatment);
            ActionCardSO keepHand = NewCard("Keep Hand", ActionCardType.Staff);
            ActionCardSO keepDiscard = NewCard("Keep Discard", ActionCardType.Diagnostic);

            deck.drawPile.Add(keepDraw);
            deck.handData.Add(keepHand);
            deck.discardPile.Add(keepDiscard);
            AddCopies(deck.drawPile, redTape, inDraw);
            AddCopies(deck.handData, redTape, inHand);
            AddCopies(deck.discardPile, redTape, inDiscard);

            int reputationBefore = gm.CurrentReputation;

            int removed = deck.CleanseRedTape(bonus);

            Assert.AreEqual(expected, removed, "CleanseRedTape should return the total removed across draw, hand and discard.");
            Assert.AreEqual(reputationBefore + bonus * expected, gm.CurrentReputation,
                $"Expected +{bonus * expected} Reputation ({bonus} per card x {expected}).");

            Assert.IsFalse(deck.drawPile.Any(c => c.cardType == ActionCardType.RedTape), "Red Tape left in the draw pile.");
            Assert.IsFalse(deck.handData.Any(c => c.cardType == ActionCardType.RedTape), "Red Tape left in the hand.");
            Assert.IsFalse(deck.discardPile.Any(c => c.cardType == ActionCardType.RedTape), "Red Tape left in the discard pile.");

            CollectionAssert.AreEqual(new[] { keepDraw }, deck.drawPile, "Non-Red-Tape cards in the draw pile must be untouched.");
            CollectionAssert.AreEqual(new[] { keepHand }, deck.handData, "Non-Red-Tape cards in the hand must be untouched.");
            CollectionAssert.AreEqual(new[] { keepDiscard }, deck.discardPile, "Non-Red-Tape cards in the discard pile must be untouched.");
        }

        [Test]
        public void CleanseRedTape_NothingToRemove_DoesNotRaiseReputationEvent()
        {
            deck.drawPile.Add(NewCard("Keep", ActionCardType.Treatment));
            bool raised = false;
            gm.OnReputationChanged += _ => raised = true;

            int removed = deck.CleanseRedTape(5);

            Assert.AreEqual(0, removed);
            Assert.IsFalse(raised, "With no Red Tape to remove, no Reputation bonus should be paid.");
        }

        // ------------------------------------------------------------------ SpawnRandomRedTape

        [TestCase(true, TestName = "SpawnRandomRedTape_ToHand_AddsExactlyOneToTheHand")]
        [TestCase(false, TestName = "SpawnRandomRedTape_ToDiscard_AddsExactlyOneToTheDiscardPile")]
        public void SpawnRandomRedTape_AddsExactlyOneCard(bool toHand)
        {
            deck.redTapePool = new List<ActionCardSO> { RedTape("Red Tape A"), RedTape("Red Tape B") };
            ActionCardSO existingHand = NewCard("Existing Hand", ActionCardType.Staff);
            ActionCardSO existingDiscard = NewCard("Existing Discard", ActionCardType.Treatment);
            deck.handData.Add(existingHand);
            deck.discardPile.Add(existingDiscard);
            int drawBefore = deck.drawPile.Count;

            deck.SpawnRandomRedTape(toHand);

            List<ActionCardSO> target = toHand ? deck.handData : deck.discardPile;
            List<ActionCardSO> other = toHand ? deck.discardPile : deck.handData;
            Assert.AreEqual(2, target.Count, $"Exactly one card should be added to the {(toHand ? "hand" : "discard pile")}.");
            Assert.AreEqual(1, other.Count, $"The {(toHand ? "discard pile" : "hand")} must not change.");
            Assert.AreEqual(drawBefore, deck.drawPile.Count, "The draw pile must not change.");

            ActionCardSO added = target.Last();
            Assert.AreEqual(ActionCardType.RedTape, added.cardType, "The added card should be Red Tape.");
            CollectionAssert.Contains(deck.redTapePool, added, "The added card should come from the Red Tape pool.");
        }

        [Test]
        public void SpawnRandomRedTape_EmptyPool_AddsNothing()
        {
            deck.redTapePool = new List<ActionCardSO>();
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, "No Red Tape cards assigned in DeckManager!");

            deck.SpawnRandomRedTape(true);
            deck.SpawnRandomRedTape(false);

            Assert.IsEmpty(deck.handData);
            Assert.IsEmpty(deck.discardPile);
        }

        // ------------------------------------------------------------------ MainBoard Red Tape pool

        [Test]
        public void MainBoardRedTapePool_EveryEntryIsAFreeRedTapeCard()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var sceneDeck = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DeckManager>(true)).FirstOrDefault();
                Assert.IsNotNull(sceneDeck, "MainBoard should contain a DeckManager.");
                Assert.IsNotEmpty(sceneDeck.redTapePool, "MainBoard's DeckManager.redTapePool should not be empty.");

                for (int i = 0; i < sceneDeck.redTapePool.Count; i++)
                {
                    ActionCardSO card = sceneDeck.redTapePool[i];
                    Assert.IsNotNull(card, $"redTapePool[{i}] is empty (missing reference).");
                    Assert.AreEqual(ActionCardType.RedTape, card.cardType, $"redTapePool[{i}] '{card.cardName}' should be cardType RedTape.");
                    Assert.AreEqual(0, card.CalculateDraftValue(), $"redTapePool[{i}] '{card.cardName}' should have a draft value of 0.");
                }
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
