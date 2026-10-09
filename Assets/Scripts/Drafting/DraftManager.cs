using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Drafting
{
    /// <summary>
    /// Controls the inter-shift draft/market phase. Rolls three random cards from the
    /// draftable pool, prices them dynamically via <see cref="ActionCardSO.CalculateDraftValue"/>,
    /// and manages a shopping-cart of pending purchases that are committed to the deck
    /// when the player closes the panel.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class DraftManager : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject draftPanel;
        public Button[] cardOptionButtons;
        public TextMeshProUGUI[] cardNameTexts;
        public TextMeshProUGUI[] cardCostTexts;
        public TextMeshProUGUI[] cardDescriptionTexts;
        public GameObject endShiftButton;

        [Header("Market Data")]
        public List<ActionCardSO> draftableCardsPool;

        // Single source of truth for how many cards appear in the market each draft.
        private const int MARKET_SIZE = 5;

        private List<ActionCardSO> currentOfferedCards = new List<ActionCardSO>();
        private int[] currentCosts = new int[MARKET_SIZE];
        private bool[] isSlotPurchased = new bool[MARKET_SIZE];
        private List<ActionCardSO> pendingPurchases = new List<ActionCardSO>();

        // -------------------------------------------------------------------------
        // Event Listeners
        // -------------------------------------------------------------------------

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnReputationChanged += OnReputationChanged;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnReputationChanged -= OnReputationChanged;
        }

        private void OnReputationChanged(int newRep)
        {
            RefreshAffordability();
        }

        // -------------------------------------------------------------------------
        // Phase Control
        // -------------------------------------------------------------------------

        /// <summary>
        /// Opens the draft panel and rolls a fresh market of five cards.
        /// Clears any leftover pending purchases from a previous draft.
        /// </summary>
        public void OpenDraftPhase()
        {
            if (endShiftButton != null)
            {
                endShiftButton.SetActive(false);
            }
            draftPanel.SetActive(true);
            pendingPurchases.Clear();
            RollMarketCards();
        }

        /// <summary>
        /// Populates the five market slots with randomly chosen cards, calculates their
        /// dynamic Reputation costs, and rebuilds all UI text and button listeners.
        /// </summary>
        private void RollMarketCards()
        {
            currentOfferedCards.Clear();

            for (int i = 0; i < MARKET_SIZE; i++)
            {
                ActionCardSO rolledCard = draftableCardsPool[Random.Range(0, draftableCardsPool.Count)];
                currentOfferedCards.Add(rolledCard);
                isSlotPurchased[i] = false;

                int dynamicCost = rolledCard.CalculateDraftValue();
                currentCosts[i] = dynamicCost;

                cardNameTexts[i].text = rolledCard.cardName;
                cardCostTexts[i].text = $"Cost: {dynamicCost} Rep";

                string apText = rolledCard.apCost == 0 ? "FREE" : $"{rolledCard.apCost} AP";

                cardDescriptionTexts[i].text =
                    $"<b>[{rolledCard.cardType.ToString().ToUpper()}]</b>   <b>{apText}</b>\n\n" +
                    $"{rolledCard.rulesText}\n\n" +
                    $"<i><color=#D3D3D3>{rolledCard.flavorText}</color></i>";

                // Capture the loop index for the closure before assigning the listener.
                int slotIndex = i;
                cardOptionButtons[i].onClick.RemoveAllListeners();
                cardOptionButtons[i].onClick.AddListener(() => TogglePurchase(slotIndex));
            }

            RefreshAffordability();
        }

        // -------------------------------------------------------------------------
        // Purchase Logic
        // -------------------------------------------------------------------------

        /// <summary>
        /// Toggles the purchase state of a market slot.
        /// On first click: spends Reputation and adds the card to the pending cart (greyed button).
        /// On second click: refunds Reputation and removes the card from the cart.
        /// </summary>
        /// <param name="slotIndex">Zero-based index of the market slot (0–2).</param>
        public void TogglePurchase(int slotIndex)
        {
            ActionCardSO cardToBuy = currentOfferedCards[slotIndex];
            int cost = currentCosts[slotIndex];
            Image buttonImage = cardOptionButtons[slotIndex].GetComponent<Image>();

            if (!isSlotPurchased[slotIndex])
            {
                // Reserve 1 Rep as a floor — a draft purchase must never trigger Game Over.
                if (GameManager.Instance.CurrentReputation - cost > 0)
                {
                    GameManager.Instance.SpendReputation(cost);
                    pendingPurchases.Add(cardToBuy);
                    isSlotPurchased[slotIndex] = true;

                    Debug.Log($"Reserved {cardToBuy.cardName} for {cost} Rep!");
                }
                else
                {
                    Debug.Log("Not enough Reputation!");
                }
            }
            else
            {
                GameManager.Instance.AddReputation(cost);
                pendingPurchases.Remove(cardToBuy);
                isSlotPurchased[slotIndex] = false;

                Debug.Log($"Refunded {cardToBuy.cardName}, gained {cost} Rep back!");
            }

            RefreshAffordability();
        }

        /// <summary>
        /// Commits all pending purchases to the player's discard pile, closes the draft panel,
        /// and triggers the next shift via <see cref="GameManager.StartNewShift"/>.
        /// </summary>
        public void CloseDraftAndStartShift()
        {
            foreach (ActionCardSO card in pendingPurchases)
            {
                DeckManager.Instance.AddCardToDiscardPile(card);
                Debug.Log($"COMMITTED: Added {card.cardName} to deck.");
            }
            pendingPurchases.Clear();

            draftPanel.SetActive(false);
            if (endShiftButton != null)
            {
                endShiftButton.SetActive(true);
            }
            GameManager.Instance.StartNewShift();
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Updates the colour and interactability of all market slots based on
        /// their purchase status and the player's current Reputation.
        /// </summary>
        private void RefreshAffordability()
        {
            for (int i = 0; i < MARKET_SIZE; i++)
            {
                if (currentOfferedCards.Count <= i) continue;

                Image buttonImage = cardOptionButtons[i].GetComponent<Image>();
                if (buttonImage == null) continue;

                if (isSlotPurchased[i])
                {
                    // In cart (reserved)
                    buttonImage.color = Color.gray;
                    cardOptionButtons[i].interactable = true; // Click to refund
                }
                else
                {
                    // Mirror the purchase guard: card is only affordable if buying it
                    // would still leave the player with at least 1 Reputation.
                    if (GameManager.Instance.CurrentReputation - currentCosts[i] > 0)
                    {
                        // Affordable
                        buttonImage.color = GetCardTypeColor(currentOfferedCards[i].cardType);
                        cardOptionButtons[i].interactable = true;
                    }
                    else
                    {
                        // Too Expensive
                        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark Grey / Dimmed
                        cardOptionButtons[i].interactable = false;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a thematic tint colour for a card's button based on its <see cref="ActionCardType"/>.
        /// </summary>
        /// <param name="type">The card type to look up.</param>
        /// <returns>A <see cref="Color"/> representing the card type's visual theme.</returns>
        private Color GetCardTypeColor(ActionCardType type)
        {
            return type switch
            {
                ActionCardType.Staff      => new Color(0.85f, 0.65f, 0.2f),   // Gold
                ActionCardType.Diagnostic => new Color(0.2f,  0.6f,  0.85f),  // Blue
                ActionCardType.Treatment  => new Color(0.2f,  0.75f, 0.3f),   // Green
                ActionCardType.Utility    => new Color(0.6f,  0.4f,  0.8f),   // Purple
                ActionCardType.RedTape    => new Color(0.85f, 0.2f,  0.2f),   // Red
                _ => Color.white
            };
        }
    }

}
