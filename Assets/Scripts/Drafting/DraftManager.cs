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
        [Tooltip("One market offer; an instance is spawned into the market container for each card on sale.")]
        public DraftMarketCardView marketCardPrefab;
        public Transform marketContainer;
        public GameObject endShiftButton;

        [Header("Market Data")]
        public List<ActionCardSO> draftableCardsPool;

        // Single source of truth for how many cards appear in the market each draft.
        private const int MARKET_SIZE = 5;

        private List<ActionCardSO> currentOfferedCards = new List<ActionCardSO>();
        private int[] currentCosts = new int[MARKET_SIZE];
        private bool[] isSlotPurchased = new bool[MARKET_SIZE];
        private readonly List<DraftMarketCardView> marketCards = new List<DraftMarketCardView>();
        private List<ActionCardSO> pendingPurchases = new List<ActionCardSO>();

        // -------------------------------------------------------------------------
        // Event Listeners
        // -------------------------------------------------------------------------

        private void Start()
        {
            // The panel stays visible in the scene for editing; a run always begins in the shift phase.
            if (draftPanel != null) draftPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnFundingChanged += OnFundingChanged;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnFundingChanged -= OnFundingChanged;
        }

        private void OnFundingChanged(int newFunding)
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
        /// Rolls five random cards for the market, prices them via <see cref="ActionCardSO.CalculateDraftValue"/>
        /// and spawns a <see cref="DraftMarketCardView"/> for each.
        /// </summary>
        private void RollMarketCards()
        {
            currentOfferedCards.Clear();
            foreach (var old in marketCards) if (old != null) Destroy(old.gameObject);
            marketCards.Clear();

            for (int i = 0; i < MARKET_SIZE; i++)
            {
                ActionCardSO rolledCard = draftableCardsPool[Random.Range(0, draftableCardsPool.Count)];
                currentOfferedCards.Add(rolledCard);
                isSlotPurchased[i] = false;
                currentCosts[i] = rolledCard.CalculateDraftValue();

                if (marketCardPrefab == null || marketContainer == null) continue;
                DraftMarketCardView view = Instantiate(marketCardPrefab, marketContainer);
                int slotIndex = i; // captured for the click callback
                view.Setup(rolledCard, currentCosts[i], () => TogglePurchase(slotIndex));
                marketCards.Add(view);
            }

            RefreshAffordability();
        }

        // -------------------------------------------------------------------------
        // Purchase Logic
        // -------------------------------------------------------------------------

        /// <summary>
        /// Toggles the purchase state of a market slot.
        /// On first click: spends Activity Funding and adds the card to the pending cart (greyed button).
        /// On second click: refunds the Funding and removes the card from the cart.
        /// </summary>
        /// <param name="slotIndex">Zero-based index of the market slot (0–4).</param>
        public void TogglePurchase(int slotIndex)
        {
            ActionCardSO cardToBuy = currentOfferedCards[slotIndex];
            int cost = currentCosts[slotIndex];

            if (!isSlotPurchased[slotIndex])
            {
                if (GameManager.Instance.TrySpendFunding(cost))
                {
                    pendingPurchases.Add(cardToBuy);
                    isSlotPurchased[slotIndex] = true;

                    Debug.Log($"Reserved {cardToBuy.cardName} for {cost} Funding!");
                }
                else
                {
                    Debug.Log("Not enough Funding!");
                }
            }
            else
            {
                GameManager.Instance.AddFunding(cost);
                pendingPurchases.Remove(cardToBuy);
                isSlotPurchased[slotIndex] = false;

                Debug.Log($"Refunded {cardToBuy.cardName}, gained {cost} Funding back!");
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
        /// Updates each market card for its purchase status and the player's current Activity Funding.
        /// </summary>
        private void RefreshAffordability()
        {
            int funding = GameManager.Instance != null ? GameManager.Instance.CurrentFunding : 0;
            for (int i = 0; i < marketCards.Count; i++)
            {
                if (marketCards[i] == null) continue;
                // Mirror the purchase guard: affordable if the Funding balance covers the cost.
                marketCards[i].SetState(isSlotPurchased[i], funding >= currentCosts[i]);
            }
        }
    }

}
