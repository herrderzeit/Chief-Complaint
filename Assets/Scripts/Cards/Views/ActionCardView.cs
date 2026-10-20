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
    /// Presentation layer for a single action card in the player's hand.
    /// Binds an <see cref="ActionCardSO"/> to the card's TextMeshPro labels and
    /// manages the highlight outline used during drag-and-drop targeting.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class ActionCardView : MonoBehaviour
    {
        public ActionCardSO currentCardData;

        [Header("Visual References")]
        public TextMeshPro nameText;
        public TextMeshPro rulesText;
        public TextMeshPro flavorText;
        public TextMeshPro costText;

        [Header("UI Feedback")]
        /// <summary>
        /// A child GameObject containing the neon outline sprite.
        /// Toggled active/inactive to indicate a valid drag-and-drop target.
        /// </summary>
        public GameObject highlightOutline;

        [Header("Type Badge")]
        /// <summary>Lookup of icon sprites per card type, shared with the UI card.</summary>
        public CardTypeIconsSO typeIcons;
        /// <summary>The icon inside the dark type badge; tinted with the card type colour.</summary>
        public SpriteRenderer typeIcon;
        /// <summary>Hover tooltip on the type icon that names the card type.</summary>
        public CardTypeTooltip typeTooltip;

        /// <summary>
        /// Binds <paramref name="cardData"/> to this view and immediately refreshes all visuals.
        /// Call this once after instantiating the card prefab.
        /// </summary>
        /// <param name="cardData">The card asset to display.</param>
        public void InitialiseCard(ActionCardSO cardData)
        {
            currentCardData = cardData;
            UpdateVisuals();
        }

        /// <summary>
        /// Activates or deactivates the highlight outline to indicate targeting status.
        /// </summary>
        /// <param name="isHighlighted"><c>true</c> to show the outline; <c>false</c> to hide it.</param>
        public void SetHighlight(bool isHighlighted)
        {
            if (highlightOutline != null)
            {
                highlightOutline.SetActive(isHighlighted);
            }
        }

        /// <summary>
        /// Pushes the latest values from <see cref="currentCardData"/> to all label components.
        /// Card type is shown by the type badge (with a hover tooltip); flavour text is rendered in italics with a dimmed colour.
        /// </summary>
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

            SpriteRenderer bg = transform.Find("Card_Front")?.GetComponent<SpriteRenderer>();
            if (bg != null) bg.color = CardTypeIconsSO.GetTypeColor(currentCardData.cardType);

            if (typeIcon != null && typeIcons != null)
            {
                typeIcon.sprite = typeIcons.GetIcon(currentCardData.cardType);
                typeIcon.color = CardTypeIconsSO.GetTypeColor(currentCardData.cardType);
            }

            if (typeTooltip != null)
                typeTooltip.SetText(CardTypeIconsSO.GetDisplayName(currentCardData.cardType));
        }
    }
}
