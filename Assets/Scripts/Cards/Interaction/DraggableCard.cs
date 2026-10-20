using UnityEngine;
using UnityEngine.Rendering;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Cards.Interaction
{
    /// <summary>
    /// Handles mouse-driven drag-and-drop for action cards in the player's hand.
    /// On drop, validates AP cost and resolves one of four effect categories in priority order:
    /// <list type="number">
    ///   <item><description>Targeted patient effects (diagnose, stabilise, pause clock, cure symptom).</description></item>
    ///   <item><description>Targeted card effects (upgrade or trash another hand card).</description></item>
    ///   <item><description>Untargeted staff/utility effects (AP gain, Reputation gain, card draw).</description></item>
    ///   <item><description>Snap back to original position if no valid target was found.</description></item>
    /// </list>
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class DraggableCard : MonoBehaviour
    {
        public static event System.Action<ActionCardSO> OnCardDragStarted;
        public static event System.Action OnCardDragEnded;

        private bool isDragging = false;
        private Vector3 offset;
        private Vector3 originalLocalPosition;
        private SortingGroup sortingGroup;
        private int originalSortingOrder;

        private PatientCardView hoveredPatient;
        private ActionCardView hoveredCard;
        private PlayZoneView hoveredPlayZone;

        [Header("Hover Settings")]
        public float scaleSpeed = 15f;
        private Vector3 originalScale;
        private Vector3 targetScale;
        private bool isHovering = false;

        private BoxCollider2D boxCol;
        private Vector2 origColSize;
        private Vector2 origColOffset;

        void Start()
        {
            sortingGroup = GetComponent<SortingGroup>();
            if (sortingGroup != null) originalSortingOrder = sortingGroup.sortingOrder;
            originalScale = transform.localScale;
            targetScale = originalScale;

            boxCol = GetComponent<BoxCollider2D>();
            if (boxCol != null)
            {
                origColSize = boxCol.size;
                origColOffset = boxCol.offset;
            }
        }

        void OnEnable()
        {
            isDragging = false;
            isHovering = false;
            hoveredPatient = null;
            hoveredCard = null;
            hoveredPlayZone = null;
            targetScale = originalScale == Vector3.zero ? Vector3.one : originalScale;
        }

        void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
        }

        private void ApplyHoverState()
        {
            if (sortingGroup != null) sortingGroup.sortingOrder = originalSortingOrder + 100;
            float zoom = HandView.Instance != null ? HandView.Instance.hoverZoom : 1.5f;
            targetScale = originalScale * zoom;
            transform.localPosition += new Vector3(0, 0, -2.0f);
        }

        private void RemoveHoverState()
        {
            if (sortingGroup != null) sortingGroup.sortingOrder = originalSortingOrder;
            targetScale = originalScale;
            transform.localPosition -= new Vector3(0, 0, -2.0f);
        }

        void OnMouseEnter()
        {
            isHovering = true;
            if (!isDragging) ApplyHoverState();
        }

        void OnMouseExit()
        {
            isHovering = false;
            if (!isDragging) RemoveHoverState();
        }

        void OnMouseDown()
        {
            if (GameManager.Instance != null && !GameManager.Instance.CanInteract) return;

            ActionCardView actionView = GetComponent<ActionCardView>();
            if (actionView != null && actionView.currentCardData != null && !actionView.currentCardData.isPlayable) return;

            // Force cleanup of hover modifications BEFORE capturing originalLocalPosition
            if (isHovering) RemoveHoverState();

            isDragging = true;
            if (HandView.Instance != null) HandView.Instance.isDraggingCard = true;
            originalLocalPosition = transform.localPosition;
            offset = Vector3.zero; // Center on mouse cursor
            targetScale = originalScale; // Shrink back to original size
            if (sortingGroup != null) sortingGroup.sortingOrder = originalSortingOrder + 100;

            if (actionView != null && actionView.currentCardData != null)
            {
                OnCardDragStarted?.Invoke(actionView.currentCardData);
            }
        }

        void OnMouseDrag()
        {
            if (isDragging)
            {
                transform.position = GetMouseWorldPosition() + offset;
                UpdateDragTarget();
            }
        }

        /// <summary>
        /// Performs an overlap test at the current mouse position and updates hover highlights
        /// for the nearest valid patient and card targets.
        /// </summary>
        private void UpdateDragTarget()
        {
            Collider2D[] hits = Physics2D.OverlapPointAll(GetMouseWorldPosition());
            PatientCardView foundPatient = null;
            ActionCardView foundCard = null;
            PlayZoneView foundPlayZone = null;

            foreach (Collider2D hit in hits)
            {
                if (foundPlayZone == null)
                {
                    foundPlayZone = hit.GetComponentInParent<PlayZoneView>();
                }
                // Patient target: check the hit collider and its parent for a PatientCardView,
                // or walk up through a SymptomSubCardView to find the owning patient.
                if (foundPatient == null)
                {
                    foundPatient = hit.GetComponentInParent<PatientCardView>();
                    if (foundPatient != null && !foundPatient.enabled) foundPatient = null;

                    if (foundPatient == null)
                    {
                        SymptomSubCardView symptom = hit.GetComponentInParent<SymptomSubCardView>();
                        if (symptom != null) 
                        {
                            foundPatient = symptom.parentPatient;
                            if (foundPatient != null && !foundPatient.enabled) foundPatient = null;
                        }
                    }
                }

                // Card target: find an ActionCardView, but ignore this card itself.
                if (foundCard == null)
                {
                    ActionCardView hitCard = hit.GetComponentInParent<ActionCardView>();
                    if (hitCard != null && hitCard != this.GetComponent<ActionCardView>())
                    {
                        foundCard = hitCard;
                    }
                }
            }

            if (foundPatient != hoveredPatient)
            {
                if (hoveredPatient != null) hoveredPatient.SetHighlight(false);
                hoveredPatient = foundPatient;
                if (hoveredPatient != null) hoveredPatient.SetHighlight(true);
            }

            if (foundCard != hoveredCard)
            {
                if (hoveredCard != null) hoveredCard.SetHighlight(false);
                hoveredCard = foundCard;
                if (hoveredCard != null) hoveredCard.SetHighlight(true);
            }

            if (foundPlayZone != hoveredPlayZone)
            {
                if (hoveredPlayZone != null) hoveredPlayZone.SetHighlight(false);
                hoveredPlayZone = foundPlayZone;
                if (hoveredPlayZone != null) hoveredPlayZone.SetHighlight(true);
            }
        }

        void OnMouseUp()
        {
            if (!isDragging) return;
            isDragging = false;
            OnCardDragEnded?.Invoke();
            
            if (HandView.Instance != null) HandView.Instance.isDraggingCard = false;
            if (sortingGroup != null) sortingGroup.sortingOrder = isHovering ? (originalSortingOrder + 100) : originalSortingOrder;

            float dragDistance = Vector3.Distance(transform.localPosition, originalLocalPosition);
            if (dragDistance < 0.5f)
            {
                ClearHighlights();
                transform.localPosition = originalLocalPosition;
                if (isHovering) ApplyHoverState();
                return;
            }

            ActionCardView actionView = GetComponent<ActionCardView>();
            if (actionView != null && actionView.currentCardData != null)
            {
                ActionCardSO cardData = actionView.currentCardData;

                // AP gate: abort and snap back if the player cannot afford this card.
                if (GameManager.Instance.CurrentAP < cardData.apCost)
                {
                    GameFeedback.WarnAt("NOT ENOUGH AP!", transform.position);
                    ClearHighlights();
                    transform.localPosition = originalLocalPosition;
                    if (isHovering) ApplyHoverState();
                    return;
                }

                bool effectApplied = false;
                bool rngFailed = UnityEngine.Random.value > cardData.successChance;

                // Validate targeting requirements
                bool requiresPatient = false;
                bool requiresCard = false;
                bool canTargetPatient = false;
                foreach (var effect in cardData.effects)
                {
                    if (effect.RequiresPatientTarget) requiresPatient = true;
                    if (effect.RequiresCardTarget) requiresCard = true;
                    if (effect.CanTargetPatient) canTargetPatient = true;
                }

                // Untargeted cards go on the play zone; cards with an optional patient effect may also go on a patient.
                bool untargetedDropOk = hoveredPlayZone != null || (canTargetPatient && hoveredPatient != null);

                if ((requiresPatient && hoveredPatient == null) || 
                    (requiresCard && hoveredCard == null) || 
                    (!requiresPatient && !requiresCard && !untargetedDropOk))
                {
                    ClearHighlights();
                    transform.localPosition = originalLocalPosition;
                    if (isHovering) ApplyHoverState();
                    return;
                }

                // Apply all strategy effects
                foreach (var effect in cardData.effects)
                {
                    if (effect.CanApply(hoveredPatient, hoveredCard))
                    {
                        if (effect.Apply(hoveredPatient, hoveredCard, rngFailed, GetMouseWorldPosition()))
                        {
                            effectApplied = true;
                        }
                    }
                }

                // A playable card with no effects (e.g. Red Tape such as Prior Authorization) is
                // resolved by paying its AP on the play zone, which lets it exhaust as its text says.
                if (cardData.effects.Count == 0 && hoveredPlayZone != null)
                {
                    effectApplied = true;
                }

                ClearHighlights();

                // --- 4. Finalise ---
                if (effectApplied)
                {
                    if (!rngFailed && cardData.cardsToDraw > 0)
                    {
                        DeckManager.Instance.DrawCards(cardData.cardsToDraw);
                    }

                    GameManager.Instance.SpendAP(cardData.apCost);
                    
                    if (HandView.Instance != null) HandView.Instance.RemoveCardVisual(gameObject);
                    if (DeckManager.Instance != null) DeckManager.Instance.RemoveCardFromHand(cardData, !cardData.exhaustsOnUse);
                    
                    return;
                }
            }

            // No valid effect resolved — snap back to hand position.
            ClearHighlights();
            transform.localPosition = originalLocalPosition;
            if (isHovering) ApplyHoverState();
        }

        /// <summary>Clears the highlight state from the currently hovered patient and card, if any.</summary>
        private void ClearHighlights()
        {
            if (hoveredPatient != null)
            {
                hoveredPatient.SetHighlight(false);
                hoveredPatient = null;
            }
            if (hoveredCard != null)
            {
                hoveredCard.SetHighlight(false);
                hoveredCard = null;
            }
            if (hoveredPlayZone != null)
            {
                hoveredPlayZone.SetHighlight(false);
                hoveredPlayZone = null;
            }
        }

        /// <summary>
        /// Converts the current mouse screen position to a world-space point at the same
        /// Z-depth as the camera, suitable for 2D drag operations.
        /// </summary>
        /// <returns>Mouse position in world space.</returns>
        private Vector3 GetMouseWorldPosition()
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            Camera cam = CameraUtil.Main;
            if (mouse == null || cam == null) return transform.position;
            Vector3 mousePos = mouse.position.ReadValue();
            mousePos.z = Mathf.Abs(cam.transform.position.z);
            return cam.ScreenToWorldPoint(mousePos);
        }
    }
}
