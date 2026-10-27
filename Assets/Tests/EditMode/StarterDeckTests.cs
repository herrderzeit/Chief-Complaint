using System.Collections.Generic;
using System.Linq;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Effects;
using ChiefComplaint.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChiefComplaint.Tests.EditMode
{
    /// <summary>Protects the 10-card starter deck: its make-up in the scene, each card's stats and effects, and opening-hand odds.</summary>
    [Category("StarterDeck")]
    public class StarterDeckTests
    {
        private const string ScenePath = "Assets/Scenes/MainBoard.unity";
        private const string Dir = "Assets/Data/Actions/Starter/";

        private static ActionCardSO Load(string file) => AssetDatabase.LoadAssetAtPath<ActionCardSO>(Dir + file + ".asset");

        private static List<ActionCardSO> SceneStarterDeck(out int handSize)
        {
            // Read the serialised deck from the scene without disturbing what is open in the Editor.
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var deck = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DeckManager>(true)).First();
                handSize = deck.handSize;
                return new List<ActionCardSO>(deck.startingDeck);
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void StarterDeck_HasTenCardsAndHandSizeFive()
        {
            var deck = SceneStarterDeck(out int handSize);
            Assert.AreEqual(10, deck.Count);
            Assert.IsFalse(deck.Any(c => c == null), "The starter deck must not contain empty slots.");
            Assert.AreEqual(5, handSize);
        }

        [TestCase("Medical Student", 2)]
        [TestCase("Initial Assessment", 3)]
        [TestCase("Standard Care", 3)]
        [TestCase("Nursing Student", 1)]
        [TestCase("IV Saline", 1)]
        public void StarterDeck_ContainsExpectedCopies(string cardName, int expected)
        {
            var deck = SceneStarterDeck(out _);
            Assert.AreEqual(expected, deck.Count(c => c != null && c.cardName == cardName));
        }

        [TestCase("Card_MedicalStudent", ActionCardType.Staff, 0)]
        [TestCase("Card_InitialAssessment", ActionCardType.Diagnostic, 1)]
        [TestCase("Card_StandardCare", ActionCardType.Treatment, 1)]
        [TestCase("Card_NursingStudent", ActionCardType.Staff, 0)]
        [TestCase("Card_IVSaline", ActionCardType.Treatment, 1)]
        public void StarterCard_StatsAndEffects(string file, ActionCardType type, int apCost)
        {
            var card = Load(file);
            Assert.IsNotNull(card, file);
            Assert.AreEqual(type, card.cardType);
            Assert.AreEqual(apCost, card.apCost);
            Assert.AreEqual(1, card.powerTier);
            Assert.IsTrue(card.isPlayable);
            Assert.That(card.effects, Is.Not.Empty.And.All.Not.Null, "Every starter card needs at least one working effect.");
            Assert.IsNotNull(card.upgradesTo, "Every starter card should have an upgrade path.");
        }

        [Test]
        public void StarterCards_EffectsMatchRulesText()
        {
            Assert.AreEqual(1, ((GainAPEffectSO)Load("Card_MedicalStudent").effects[0]).apGenerated);
            Assert.AreEqual(SymptomTier.Tier1, ((DiagnoseEffectSO)Load("Card_InitialAssessment").effects[0]).diagnosesSymptomTier);
            Assert.AreEqual(SymptomTier.Tier1, ((CureSymptomEffectSO)Load("Card_StandardCare").effects[0]).curesSymptomTier);

            var choice = (ChoiceEffectSO)Load("Card_NursingStudent").effects[0];
            Assert.AreEqual(2, ((StabilisationHealEffectSO)choice.optionA).healAmount);
            Assert.AreEqual(1, ((DrawCardsEffectSO)choice.optionB).cardsToDraw);
            Assert.IsFalse(choice.RequiresPatientTarget, "The draw option must be playable on the play zone.");

            var saline = Load("Card_IVSaline").effects;
            Assert.IsTrue(saline.Any(e => e is PauseStabilityEffectSO));
            Assert.AreEqual(1, saline.OfType<StabilisationHealEffectSO>().Single().healAmount);
        }

        [Test]
        public void StarterDeck_MostOpeningHandsCanDiagnoseAndTreat()
        {
            var deck = SceneStarterDeck(out int handSize);
            int total = 0, playable = 0;
            foreach (var hand in Combinations(deck.Count, handSize))
            {
                total++;
                bool diag = hand.Any(i => deck[i].cardType == ActionCardType.Diagnostic);
                bool treat = hand.Any(i => deck[i].cardType == ActionCardType.Treatment && deck[i].cardName != "IV Saline");
                if (diag && treat) playable++;
            }
            float odds = (float)playable / total;
            Assert.GreaterOrEqual(odds, 0.80f, $"Only {odds:P0} of opening hands can both diagnose and treat.");
        }

        private static IEnumerable<int[]> Combinations(int n, int k)
        {
            var idx = Enumerable.Range(0, k).ToArray();
            while (true)
            {
                yield return (int[])idx.Clone();
                int i = k - 1;
                while (i >= 0 && idx[i] == n - k + i) i--;
                if (i < 0) yield break;
                idx[i]++;
                for (int j = i + 1; j < k; j++) idx[j] = idx[j - 1] + 1;
            }
        }
    }
}
