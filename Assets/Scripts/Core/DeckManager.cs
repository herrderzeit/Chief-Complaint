using System.Collections.Generic;
using UnityEngine;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Core
{
    /// <summary>
    /// Manages the player's action-card deck: draw pile, hand, and discard pile.
    /// Visuals have been moved to HandView.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class DeckManager : Singleton<DeckManager>
    {
        [Header("Card Setup")]
        public List<ActionCardSO> startingDeck = new List<ActionCardSO>(10);
        [Tooltip("Number of cards drawn at the start of a run and at the start of every shift.")]
        public int handSize = 5;

        [Header("Red Tape Setup")]
        public List<ActionCardSO> redTapePool = new List<ActionCardSO>();
        private List<ActionCardSO> redTapeDrawPile = new List<ActionCardSO>();

        // Runtime Piles Events
        public event System.Action<int> OnDeckCountChanged;
        public event System.Action<int> OnDiscardCountChanged;

        [Header("Runtime Piles")]
        public List<ActionCardSO> drawPile = new List<ActionCardSO>();
        public List<ActionCardSO> handData = new List<ActionCardSO>();
        public List<ActionCardSO> discardPile = new List<ActionCardSO>();

        /// <summary>
        /// Builds and shuffles the draw pile and draws the opening hand.
        /// </summary>
        public void StartRun()
        {
            InitializeDeck();
            DrawCards(handSize);
        }

        public void InitializeDeck()
        {
            drawPile.Clear();
            InitializeRedTapeDeck();
            drawPile.AddRange(startingDeck);
            ShufflePile(drawPile);
            NotifyDeckCounts();
        }

        private void InitializeRedTapeDeck()
        {
            redTapeDrawPile.Clear();
            redTapeDrawPile.AddRange(redTapePool);

            for (int i = 0; i < redTapeDrawPile.Count; i++)
            {
                ActionCardSO temp = redTapeDrawPile[i];
                int randomIndex = Random.Range(i, redTapeDrawPile.Count);
                redTapeDrawPile[i] = redTapeDrawPile[randomIndex];
                redTapeDrawPile[randomIndex] = temp;
            }
        }

        public void SpawnRandomRedTape(bool toHand = false)
        {
            if (redTapePool.Count == 0)
            {
                Debug.LogWarning("No Red Tape cards assigned in DeckManager!");
                return;
            }

            if (redTapeDrawPile.Count == 0)
            {
                InitializeRedTapeDeck();
            }

            ActionCardSO drawnRedTape = redTapeDrawPile[0];
            redTapeDrawPile.RemoveAt(0);

            if (toHand)
            {
                AddCardToHand(drawnRedTape);
                GameLog.Info($"Added Red Tape to Hand: {drawnRedTape.cardName}");
            }
            else
            {
                AddCardToDiscardPile(drawnRedTape);
                GameLog.Info($"Added Red Tape to Discard: {drawnRedTape.cardName}");
            }
        }

        public void CleanseRedTape(int repBonusPerCard)
        {
            int totalCleansed = 0;

            for (int i = discardPile.Count - 1; i >= 0; i--)
            {
                if (discardPile[i].cardType == ActionCardType.RedTape)
                {
                    discardPile.RemoveAt(i);
                    totalCleansed++;
                }
            }

            for (int i = drawPile.Count - 1; i >= 0; i--)
            {
                if (drawPile[i].cardType == ActionCardType.RedTape)
                {
                    drawPile.RemoveAt(i);
                    totalCleansed++;
                }
            }

            for (int i = handData.Count - 1; i >= 0; i--)
            {
                if (handData[i].cardType == ActionCardType.RedTape)
                {
                    ActionCardSO card = handData[i];
                    handData.RemoveAt(i);
                    // Remove the matching visual by card, not index: the hand visuals can be ordered differently.
                    if (HandView.Instance != null) HandView.Instance.RemoveCardVisualFor(card);
                    totalCleansed++;
                }
            }

            if (totalCleansed > 0)
            {
                GameLog.Info($"Cleansed {totalCleansed} Red Tape cards!");
                if (repBonusPerCard > 0 && GameManager.Instance != null)
                {
                    GameManager.Instance.AddReputation(repBonusPerCard * totalCleansed);
                }
                NotifyDeckCounts();
            }
        }

        public void DrawCards(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                if (drawPile.Count == 0)
                {
                    if (discardPile.Count == 0) return;

                    drawPile.AddRange(discardPile);
                    discardPile.Clear();
                    ShufflePile(drawPile);
                }

                ActionCardSO drawnCard = drawPile[0];
                drawPile.RemoveAt(0);
                handData.Add(drawnCard);

                if (HandView.Instance != null) HandView.Instance.SpawnCardVisual(drawnCard);
            }

            NotifyDeckCounts();
        }

        public void AddCardToHand(ActionCardSO newCard)
        {
            handData.Add(newCard);
            if (HandView.Instance != null) HandView.Instance.SpawnCardVisual(newCard);
        }

        public void DiscardHand()
        {
            foreach (ActionCardSO card in handData)
            {
                if (card.exhaustsAtShiftEnd)
                {
                    GameLog.Info($"{card.cardName} exhausted at the end of the shift.");
                }
                else
                {
                    discardPile.Add(card);
                }
            }
            handData.Clear();
            if (HandView.Instance != null) HandView.Instance.ClearHandVisuals();
            NotifyDeckCounts();
        }

        public void AddCardToDiscardPile(ActionCardSO purchasedCard)
        {
            discardPile.Add(purchasedCard);
            GameLog.Info(purchasedCard.cardName + " was added to your deck!");
            NotifyDeckCounts();
        }

        public void StartNewShift()
        {
            DrawCards(handSize);
            GameLog.Info($"New shift started! Drew {handSize} cards.");
        }

        public void RemoveCardFromHand(ActionCardSO cardData, bool sendToDiscard = true)
        {
            // Only a card that was actually in the hand can move to the discard pile.
            bool removed = handData.Remove(cardData);

            if (removed && sendToDiscard)
            {
                discardPile.Add(cardData);
            }

            NotifyDeckCounts();
        }

        private void ShufflePile(List<ActionCardSO> pile)
        {
            for (int i = 0; i < pile.Count; i++)
            {
                ActionCardSO temp = pile[i];
                int randomIndex = Random.Range(i, pile.Count);
                pile[i] = pile[randomIndex];
                pile[randomIndex] = temp;
            }
        }

        private void NotifyDeckCounts()
        {
            OnDeckCountChanged?.Invoke(drawPile.Count);
            OnDiscardCountChanged?.Invoke(discardPile.Count);
        }
    }
}
