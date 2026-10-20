using UnityEngine;
using TMPro;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Cards.Views
{
    /// <summary>
    /// Presentation layer for a single action card in the UI Canvas (e.g. Draft Panel).
    /// Uses TextMeshProUGUI instead of World Space TextMeshPro.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class UI_ActionCardView : MonoBehaviour
    {
        public ActionCardSO currentCardData;

        [Header("Visual References")]
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI rulesText;
        public TextMeshProUGUI flavorText;
        public TextMeshProUGUI costText;

        [Header("UI Feedback")]
        public GameObject highlightOutline;

        [Header("Type Badge")]
        public CardTypeIconsSO typeIcons;
        /// <summary>The icon inside the dark type badge; tinted with the card type colour.</summary>
        public UnityEngine.UI.Image typeIcon;

        public void InitialiseCard(ActionCardSO cardData)
        {
            currentCardData = cardData;
            UpdateVisuals();
        }

        public void SetHighlight(bool isHighlighted)
        {
            if (highlightOutline != null)
            {
                highlightOutline.SetActive(isHighlighted);
            }
        }

        public void UpdateVisuals()
        {
            if (currentCardData == null) return;

            if (nameText != null)
                nameText.text = currentCardData.cardName;

            if (rulesText != null)
                rulesText.text = currentCardData.rulesText;

            if (flavorText != null)
                flavorText.text = $"<i><color=#D3D3D3>{currentCardData.flavorText}</color></i>";

            if (costText != null)
                costText.text = currentCardData.apCost.ToString();

            // The UI card's frame image is named "Background" (the world-space card uses "Card_Front").
            UnityEngine.UI.Image bg = transform.Find("Background")?.GetComponent<UnityEngine.UI.Image>();
            if (bg != null) bg.color = CardTypeIconsSO.GetTypeColor(currentCardData.cardType);

            if (typeIcon != null && typeIcons != null)
            {
                typeIcon.sprite = typeIcons.GetIcon(currentCardData.cardType);
                typeIcon.color = CardTypeIconsSO.GetTypeColor(currentCardData.cardType);
                typeIcon.enabled = typeIcon.sprite != null;
            }
        }
    }

}
