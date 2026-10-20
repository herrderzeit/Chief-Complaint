using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;

namespace ChiefComplaint.Drafting
{
    /// <summary>
    /// One offer in the draft market: the card (drawn with the shared UI action card), its Funding price,
    /// and an "In cart" overlay. Clicking it buys the card, and clicking again refunds it.
    /// </summary>
    public class DraftMarketCardView : MonoBehaviour
    {
        [Header("References")]
        public UI_ActionCardView card;
        public Button button;
        public TextMeshProUGUI priceText;
        public GameObject inCartOverlay;
        public CanvasGroup canvasGroup;

        [Header("Styling")]
        public Color affordablePriceColour = new Color(0.30f, 0.90f, 0.45f);
        public Color unaffordablePriceColour = new Color(0.95f, 0.35f, 0.30f);
        [Range(0f, 1f)] public float unaffordableAlpha = 0.5f;

        public ActionCardSO CardData { get; private set; }
        public int Cost { get; private set; }

        /// <summary>Shows <paramref name="data"/> at <paramref name="cost"/> Funding and calls <paramref name="onClick"/> when clicked.</summary>
        public void Setup(ActionCardSO data, int cost, Action onClick)
        {
            CardData = data;
            Cost = cost;
            if (card != null) card.InitialiseCard(data);
            if (priceText != null) priceText.text = $"{cost} Funding";

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
            SetState(false, true);
        }

        /// <summary>Updates the visuals for being in the cart, or for being (un)affordable.</summary>
        public void SetState(bool inCart, bool affordable)
        {
            if (inCartOverlay != null) inCartOverlay.SetActive(inCart);
            bool available = inCart || affordable;
            button.interactable = available; // In the cart, a click refunds it.
            if (canvasGroup != null) canvasGroup.alpha = available ? 1f : unaffordableAlpha;
            if (priceText != null) priceText.color = affordable || inCart ? affordablePriceColour : unaffordablePriceColour;
        }
    }
}
