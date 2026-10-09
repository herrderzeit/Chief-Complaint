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
    }
}
