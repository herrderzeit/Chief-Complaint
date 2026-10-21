using TMPro;
using UnityEngine;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Cards.Views
{
    /// <summary>
    /// Shows the card type name in a small tooltip after the mouse rests on the type badge icon.
    /// Uses the icon's bounds rather than a collider so it never competes with the card's drag collider.
    /// </summary>
    public class CardTypeTooltip : MonoBehaviour
    {
        [Tooltip("The type icon the mouse must rest on.")]
        public SpriteRenderer icon;
        [Tooltip("Root object of the tooltip (background, border and label); toggled on and off.")]
        public GameObject tooltipRoot;
        public TextMeshPro label;

        private float hoverTime;

        private void OnEnable()
        {
            hoverTime = 0f;
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
        }

        /// <summary>Sets the text the tooltip displays.</summary>
        public void SetText(string text)
        {
            if (label != null) label.text = text;
        }

        private void Update()
        {
            if (icon == null || tooltipRoot == null) return;

            bool dragging = HandView.Instance != null && HandView.Instance.isDraggingCard;
            bool over = !dragging && IsMouseOverIcon();

            hoverTime = over ? hoverTime + Time.deltaTime : 0f;

            // The delay is set once for the whole hand on HandView.
            float delay = HandView.Instance != null ? HandView.Instance.typeTooltipDelay : 2f;
            bool show = over && hoverTime >= delay;
            if (tooltipRoot.activeSelf != show) tooltipRoot.SetActive(show);
        }

        private bool IsMouseOverIcon()
        {
            Camera cam = CameraUtil.Main;
            if (cam == null || icon.sprite == null) return false;
            return GameInput.IsPointerOver(icon.bounds, cam);
        }
    }
}
