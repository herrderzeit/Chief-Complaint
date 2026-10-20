using UnityEngine;
using TMPro;
using ChiefComplaint.Core;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;

namespace ChiefComplaint.UI
{
    /// <summary>
    /// A generic drop zone for non-targeting cards (e.g. Utility, Red Tape).
    /// Appears when dragging a card that doesn't require a specific target,
    /// providing a clear area to drop the card and confirm the player's intention to play it.
    /// </summary>
    public class PlayZoneView : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private CanvasGroup canvasGroup;

        [Header("Colors")]
        public Color normalColor = new Color(1f, 1f, 1f, 0.3f);
        public Color highlightColor = new Color(0.8f, 1f, 0.5f, 0.6f);
        [Range(0f, 1f)]
        [Tooltip("Alpha while no card is being dragged, so players can see where untargeted cards go.")]
        public float idleAlpha = 0.12f;

        [Header("Drag Preview")]
        [Tooltip("World-space label showing what the dragged card gives when played here.")]
        [SerializeField] private TextMeshPro previewText;
        [SerializeField] private Color previewColor = Color.white;
        [SerializeField] private Color warningColor = new Color(1f, 0.45f, 0.4f, 1f);

        [Header("Fade Settings")]
        public float fadeSpeed = 5f;
        private float targetAlpha = 0f;

        private bool isVisible = false;
        private bool isHighlighted = false;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            canvasGroup = GetComponent<CanvasGroup>();

            // Start at the faint idle level; dragging a playable card fades it up.
            targetAlpha = idleAlpha;
            if (spriteRenderer != null)
            {
                Color c = normalColor;
                c.a = idleAlpha;
                spriteRenderer.color = c;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = idleAlpha;
            }
            ClearPreview();
        }

        private void OnEnable()
        {
            DraggableCard.OnCardDragStarted += HandleDragStarted;
            DraggableCard.OnCardDragEnded += HandleDragEnded;
        }

        private void OnDisable()
        {
            DraggableCard.OnCardDragStarted -= HandleDragStarted;
            DraggableCard.OnCardDragEnded -= HandleDragEnded;
        }

        private void Update()
        {
            float currentAlpha = 0f;
            if (spriteRenderer != null) currentAlpha = spriteRenderer.color.a;
            else if (canvasGroup != null) currentAlpha = canvasGroup.alpha;

            if (Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
            {
                float newAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
                
                Color targetColor = isHighlighted ? highlightColor : normalColor;
                targetColor.a = newAlpha;

                if (spriteRenderer != null)
                {
                    spriteRenderer.color = targetColor;
                }
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = newAlpha;
                }
                SetPreviewAlpha(newAlpha);
            }
            else
            {
                // Snap color if alpha is stable to ensure highlight color changes instantly
                if (spriteRenderer != null && spriteRenderer.color.a > 0.05f)
                {
                    Color targetColor = isHighlighted ? highlightColor : normalColor;
                    targetColor.a = currentAlpha;
                    spriteRenderer.color = targetColor;
                }
                SetPreviewAlpha(targetAlpha);
            }
        }

        private void HandleDragStarted(ActionCardSO card)
        {
            if (card == null) return;

            bool requiresTarget = false;
            foreach (var effect in card.effects)
            {
                if (effect.RequiresPatientTarget || effect.RequiresCardTarget)
                {
                    requiresTarget = true;
                    break;
                }
            }

            // Only show the play zone if the card DOES NOT require a target
            if (!requiresTarget)
            {
                isVisible = true;
                SetPreview(card);
                UpdateVisuals();
            }
        }

        private void HandleDragEnded()
        {
            isVisible = false;
            isHighlighted = false;
            ClearPreview();
            UpdateVisuals();
        }

        /// <summary>
        /// Fills the label with the card's play-zone results, or an affordability warning if it can't be played.
        /// </summary>
        private void SetPreview(ActionCardSO card)
        {
            if (previewText == null) return;

            string text;
            Color colour = previewColor;
            var gm = GameManager.Instance;
            if (gm != null && gm.CurrentAP < card.apCost)
            {
                text = "Not enough AP";
                colour = warningColor;
            }
            else if (gm != null && !DraggableCard.CanAffordReputation(card.reputationCost, gm.CurrentReputation))
            {
                text = "Not enough Reputation";
                colour = warningColor;
            }
            else
            {
                text = card.PlayZonePreview();
            }

            colour.a = previewText.color.a; // keep the current fade; Update drives the alpha
            previewText.color = colour;
            previewText.text = text;
        }

        private void ClearPreview()
        {
            if (previewText == null) return;
            previewText.text = string.Empty;
            SetPreviewAlpha(0f);
        }

        /// <summary>
        /// The label fades with the zone but reads at full strength once the zone is at its normal alpha.
        /// </summary>
        private void SetPreviewAlpha(float zoneAlpha)
        {
            if (previewText == null) return;
            float a = normalColor.a > 0f ? Mathf.Clamp01(zoneAlpha / normalColor.a) : (zoneAlpha > 0f ? 1f : 0f);
            Color c = previewText.color;
            c.a = a;
            previewText.color = c;
        }

        public void SetHighlight(bool highlight)
        {
            isHighlighted = highlight;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            targetAlpha = isVisible ? (isHighlighted ? highlightColor.a : normalColor.a) : idleAlpha;
        }
    }
}
