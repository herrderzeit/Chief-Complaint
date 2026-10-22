using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using ChiefComplaint.Core;
using ChiefComplaint.Cards.Data;

namespace ChiefComplaint.Tests.EditMode
{
    public class DeckManagerTests
    {
        private GameObject go;
        private DeckManager deckManager;

        [SetUp]
        public void Setup()
        {
            go = new GameObject();
            deckManager = go.AddComponent<DeckManager>();

            // Setup mock data
            deckManager.startingDeck = new List<ActionCardSO>();
            for (int i = 0; i < 10; i++)
            {
                var card = ScriptableObject.CreateInstance<ActionCardSO>();
                card.name = "Test Card " + i;
                deckManager.startingDeck.Add(card);
            }
            deckManager.handSize = 5;
            
            deckManager.redTapePool = new List<ActionCardSO>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(go);
        }

        [Test]
        public void InitializeDeck_PopulatesDrawPile()
        {
            deckManager.InitializeDeck();

            Assert.AreEqual(10, deckManager.drawPile.Count, "Draw pile should contain all starting cards.");
            Assert.AreEqual(0, deckManager.handData.Count, "Hand should be empty after initialization.");
            Assert.AreEqual(0, deckManager.discardPile.Count, "Discard pile should be empty after initialization.");
        }

        [Test]
        public void DrawCards_MovesCardsFromDrawPileToHand()
        {
            deckManager.InitializeDeck();
            deckManager.DrawCards(5);

            Assert.AreEqual(5, deckManager.drawPile.Count, "Draw pile should have 5 cards remaining.");
            Assert.AreEqual(5, deckManager.handData.Count, "Hand should have 5 cards.");
        }

        [Test]
        public void DrawCards_RecyclesDiscardPileWhenDrawPileEmpty()
        {
            deckManager.InitializeDeck();
            
            // Draw all 10 cards
            deckManager.DrawCards(10);
            Assert.AreEqual(0, deckManager.drawPile.Count);
            
            // Discard hand
            deckManager.DiscardHand();
            Assert.AreEqual(10, deckManager.discardPile.Count);
            Assert.AreEqual(0, deckManager.handData.Count);

            // Draw 3 cards, should trigger recycle
            deckManager.DrawCards(3);

            Assert.AreEqual(3, deckManager.handData.Count, "Hand should have drawn 3 cards after recycle.");
            Assert.AreEqual(7, deckManager.drawPile.Count, "Draw pile should contain 7 recycled cards.");
            Assert.AreEqual(0, deckManager.discardPile.Count, "Discard pile should be empty after recycle.");
        }

        /// <summary>Empties the draw pile into the hand and the hand into the discard pile, then draws 2 back.</summary>
        private void LeaveEmptyDrawPileAndFullDiscard()
        {
            deckManager.InitializeDeck();
            deckManager.DrawCards(10);
            deckManager.DiscardHand();
            Assert.AreEqual(0, deckManager.drawPile.Count, "Fixture error: the draw pile should be empty.");
            Assert.AreEqual(10, deckManager.discardPile.Count, "Fixture error: the discard pile should hold the deck.");
        }

        [Test]
        public void DrawCards_WithoutReshuffle_DrawsNothingFromAnEmptyDrawPile()
        {
            LeaveEmptyDrawPileAndFullDiscard();

            deckManager.DrawCards(1, reshuffleDiscard: false);

            Assert.AreEqual(0, deckManager.handData.Count, "A mid-shift draw must not pull the discard pile back in.");
            Assert.AreEqual(10, deckManager.discardPile.Count, "The discard pile must be left untouched.");
            Assert.AreEqual(0, deckManager.drawPile.Count);
        }

        [Test]
        public void DrawCards_DefaultReshufflesTheDiscardIntoAnEmptyDrawPile()
        {
            LeaveEmptyDrawPileAndFullDiscard();

            deckManager.DrawCards(1);

            Assert.AreEqual(1, deckManager.handData.Count, "The start-of-shift draw reshuffles the discard pile.");
            Assert.AreEqual(9, deckManager.drawPile.Count);
            Assert.AreEqual(0, deckManager.discardPile.Count);
        }

        [Test]
        public void DrawCards_WithoutReshuffle_StillDrawsWhatIsLeftInTheDrawPile()
        {
            deckManager.InitializeDeck();
            deckManager.DrawCards(8);
            deckManager.DiscardHand();

            deckManager.DrawCards(5, reshuffleDiscard: false);

            Assert.AreEqual(2, deckManager.handData.Count, "Only the 2 cards left in the draw pile can be drawn.");
            Assert.AreEqual(8, deckManager.discardPile.Count);
        }

        [Test]
        public void RemoveCardFromHand_CardNotInHand_DoesNotAddToDiscard()
        {
            deckManager.InitializeDeck();
            var stray = ScriptableObject.CreateInstance<ActionCardSO>();

            deckManager.RemoveCardFromHand(stray);

            Assert.AreEqual(0, deckManager.discardPile.Count, "A card that was not in the hand must not be duplicated into the discard pile.");
            Object.DestroyImmediate(stray);
        }

        [Test]
        public void RemoveCardFromHand_CardInHand_MovesToDiscard()
        {
            deckManager.InitializeDeck();
            deckManager.DrawCards(1);
            var card = deckManager.handData[0];

            deckManager.RemoveCardFromHand(card);

            Assert.AreEqual(0, deckManager.handData.Count);
            Assert.AreEqual(1, deckManager.discardPile.Count);
        }

        [Test]
        public void CleanseRedTape_RemovesFromDrawPileHandAndDiscard()
        {
            deckManager.InitializeDeck();
            var tape = ScriptableObject.CreateInstance<ActionCardSO>();
            tape.cardType = ActionCardType.RedTape;
            deckManager.drawPile.Add(tape);
            deckManager.discardPile.Add(tape);
            deckManager.handData.Add(tape);

            deckManager.CleanseRedTape(0);

            Assert.IsFalse(deckManager.drawPile.Contains(tape), "Red Tape in the draw pile must be cleansed.");
            Assert.IsFalse(deckManager.discardPile.Contains(tape));
            Assert.IsFalse(deckManager.handData.Contains(tape));
            Assert.AreEqual(10, deckManager.drawPile.Count, "Normal cards stay in the draw pile.");
            Object.DestroyImmediate(tape);
        }
    }
}
