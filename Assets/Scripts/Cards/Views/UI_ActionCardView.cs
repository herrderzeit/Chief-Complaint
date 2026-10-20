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
                rulesText.text = $"<b>[{currentCardData.cardType.ToString().ToUpper()}]</b>\n\n{currentCardData.rulesText}";

            if (flavorText != null)
                flavorText.text = $"<i><color=#D3D3D3>{currentCardData.flavorText}</color></i>";

            if (costText != null)
                costText.text = currentCardData.apCost.ToString();

            UnityEngine.UI.Image bg = transform.Find("Card_Front")?.GetComponent<UnityEngine.UI.Image>();
            if (bg != null) bg.color = GetTypeColor(currentCardData.cardType);

            if (typeIcon != null && typeIcons != null)
            {
                typeIcon.sprite = typeIcons.GetIcon(currentCardData.cardType);
                typeIcon.color = CardTypeIconsSO.GetTypeColor(currentCardData.cardType);
                typeIcon.enabled = typeIcon.sprite != null;
            }
        }

        private Color GetTypeColor(ActionCardType type)
        {
            return type switch
            {
                ActionCardType.Staff          => new Color(0.00f, 0.68f, 0.94f), // Neon Blue
                ActionCardType.Diagnostic     => new Color(0.51f, 0.75f, 0.51f), // Neon Green
                ActionCardType.Treatment      => new Color(1.00f, 0.60f, 0.00f), // Amber
                ActionCardType.Utility        => new Color(0.57f, 0.15f, 0.56f), // Purple
                ActionCardType.Administration => new Color(0.40f, 0.40f, 0.40f), // Grey
                ActionCardType.Supply         => new Color(0.99f, 0.93f, 0.13f), // Yellow
                ActionCardType.RedTape        => new Color(0.93f, 0.11f, 0.14f), // Red
                _ => Color.white
            };
        }
    }

}
