using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;

namespace ChiefComplaint.StaffRoom
{
    /// <summary>
    /// One pile in the staff room's supply room: the card (drawn with the shared UI action card), its Funding price,
    /// how many copies are left and how many are in the cart. Clicking the card buys a copy; the undo button
    /// returns one. A locked pile shows the escalation stage that opens it.
    /// </summary>
    public class MarketCardView : MonoBehaviour
    {
        [Header("References")]
        public UI_ActionCardView card;
        [Tooltip("Clicking the card buys one copy.")]
        public Button button;
        public TextMeshProUGUI priceText;
        public CanvasGroup canvasGroup;

        [Header("Stock and cart")]
        [Tooltip("Shows copies left, e.g. \"2 / 3 left\".")]
        public TextMeshProUGUI stockText;
        [Tooltip("Shown while at least one copy is in the cart.")]
        public GameObject inCartOverlay;
        [Tooltip("Optional: shows \"In cart ×N\" inside the in-cart badge.")]
        public TextMeshProUGUI inCartText;
        [Tooltip("Returns one copy from the cart (refund). Shown while a copy is in the cart.")]
        public Button undoButton;

        [Header("Locked")]
        [Tooltip("Shown while the pile is locked.")]
        public GameObject lockedOverlay;
        public TextMeshProUGUI lockedText;

        [Header("Styling")]
        public Color affordablePriceColour = new Color(0.30f, 0.90f, 0.45f);
        public Color unaffordablePriceColour = new Color(0.95f, 0.35f, 0.30f);
        [Range(0f, 1f)] public float unaffordableAlpha = 0.5f;
        [Range(0f, 1f)] public float lockedAlpha = 0.35f;

        public ActionCardSO CardData { get; private set; }
        public int Cost { get; private set; }

        /// <summary>Shows <paramref name="data"/> at <paramref name="cost"/> Funding; <paramref name="onBuy"/> and <paramref name="onUndo"/> run on click.</summary>
        public void Setup(ActionCardSO data, int cost, Action onBuy, Action onUndo)
        {
            CardData = data;
            Cost = cost;
            if (card != null) card.InitialiseCard(data);
            if (priceText != null) priceText.text = $"{cost} Funding";

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onBuy?.Invoke());
            if (undoButton != null)
            {
                undoButton.onClick.RemoveAllListeners();
                undoButton.onClick.AddListener(() => onUndo?.Invoke());
            }
        }

        /// <summary>Updates the pile's visuals for its lock state, stock, cart count and affordability.</summary>
        public void SetPileState(bool unlocked, string unlockLabel, int stock, int maxStock, int inCart, bool affordable)
        {
            if (lockedOverlay != null) lockedOverlay.SetActive(!unlocked);
            if (lockedText != null) lockedText.text = unlocked ? "" : $"Unlocks with {unlockLabel}";

            bool canBuy = unlocked && stock > 0 && affordable;
            button.interactable = canBuy;

            if (stockText != null)
                stockText.text = !unlocked ? "" : stock > 0 ? $"{stock} / {maxStock} left" : "Out of stock";

            if (inCartOverlay != null) inCartOverlay.SetActive(inCart > 0);
            if (inCartText != null) inCartText.text = inCart > 1 ? $"In cart x{inCart}" : "In cart";
            if (undoButton != null) undoButton.gameObject.SetActive(inCart > 0);

            if (canvasGroup != null)
                canvasGroup.alpha = !unlocked ? lockedAlpha : (canBuy || inCart > 0) ? 1f : unaffordableAlpha;
            if (priceText != null)
            {
                // A locked pile shows only its unlock stage, not a price the player can't act on yet.
                priceText.text = unlocked ? $"{Cost} Funding" : "";
                priceText.color = affordable ? affordablePriceColour : unaffordablePriceColour;
            }
        }
    }
}
