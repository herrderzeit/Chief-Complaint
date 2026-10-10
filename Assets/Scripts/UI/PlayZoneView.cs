using UnityEngine;
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

        [Header("Fade Settings")]
        public float fadeSpeed = 5f;
        private float targetAlpha = 0f;

        private bool isVisible = false;
        private bool isHighlighted = false;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            canvasGroup = GetComponent<CanvasGroup>();

            // Initialise to completely invisible
            if (spriteRenderer != null)
            {
                Color c = normalColor;
                c.a = 0f;
                spriteRenderer.color = c;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
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
                UpdateVisuals();
            }
        }

        private void HandleDragEnded()
        {
            isVisible = false;
            isHighlighted = false;
            UpdateVisuals();
        }

        public void SetHighlight(bool highlight)
        {
            isHighlighted = highlight;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            targetAlpha = isVisible ? (isHighlighted ? highlightColor.a : normalColor.a) : 0f;
        }
    }
}
