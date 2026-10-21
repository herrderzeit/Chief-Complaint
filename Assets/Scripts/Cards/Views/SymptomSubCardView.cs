using UnityEngine;
using UnityEngine.Rendering;
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
    /// Presentation and interaction layer for a single symptom sub-card attached to a patient.
    /// Handles face-down/face-up state, category colour tinting, and a click-to-zoom mechanic
    /// that temporarily unparents the card and renders it over everything else on screen.
    /// Smoothly animates both scale and world position each frame.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class SymptomSubCardView : MonoBehaviour, IBoardPointerHandler
    {
        [Header("Visual References")]
        public TextMeshPro symptomNameText;
        public TextMeshPro symptomDescriptionText;
        public SpriteRenderer cardBackgroundSprite;
        public GameObject cardBackMask;

        [Header("Tier UI")]
        public TextMeshPro tierText;

        [Header("State")]
        /// <summary>
        /// The patient card that owns this symptom. Assigned by <see cref="PatientCardView"/>
        /// at spawn time so the sub-card can be found via the parent during drag detection.
        /// </summary>
        public PatientCardView parentPatient;
        public bool isRevealed = false;
        private bool isZoomed = false;

        [Header("Click Zoom Behaviour")]
        public float zoomMultiplier = 1.5f;
        public float zoomSpeed = 12f;

        private Vector3 originalScale;
        private Vector3 targetScale;
        private SortingGroup sortingGroup;
        private int originalSortingOrder;
        private int originalSortingLayerID;

        [Tooltip("Sorting layer used while zoomed, so the card draws over the PlayZone and the rest of the board.")]
        [SerializeField] private string zoomSortingLayer = "Player Hand";

        /// <summary>The symptom card currently zoomed in, if any. Only one may be zoomed at a time.</summary>
        private static SymptomSubCardView currentlyZoomed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => currentlyZoomed = null;

        void OnDisable()
        {
            // A pooled or destroyed card must not keep blocking other zooms.
            if (currentlyZoomed == this) currentlyZoomed = null;
        }

        /// <summary>The transform this sub-card was parented to before a zoom unparents it.</summary>
        private Transform originalParent;
        private Vector3 originalLocalPosition;
        private Vector3 targetWorldPosition;

        void Start()
        {
            originalScale = transform.localScale;
            targetScale = originalScale;

            originalParent = transform.parent;
            originalLocalPosition = transform.localPosition;

            if (originalParent != null)
            {
                targetWorldPosition = originalParent.TransformPoint(originalLocalPosition);
            }

            sortingGroup = GetComponent<SortingGroup>();
            if (sortingGroup != null)
            {
                originalSortingOrder = sortingGroup.sortingOrder;
                originalSortingLayerID = sortingGroup.sortingLayerID;
            }
        }

        void Update()
        {
            // Safety: if the owning patient was destroyed while this card was zoomed, clean up.
            if (originalParent == null)
            {
                Destroy(gameObject);
                return;
            }

            // When not zoomed, track the parent's position so the sub-card follows the moving patient.
            if (!isZoomed)
            {
                targetWorldPosition = originalParent.TransformPoint(originalLocalPosition);
            }

            if (transform.localScale != targetScale)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * zoomSpeed);
            }

            if (transform.position != targetWorldPosition)
            {
                transform.position = Vector3.Lerp(transform.position, targetWorldPosition, Time.deltaTime * zoomSpeed);
            }
        }

        // -------------------------------------------------------------------------
        // Setup & State
        // -------------------------------------------------------------------------

        /// <summary>
        /// Configures the sub-card for the given symptom and tier. When <paramref name="revealed"/>
        /// is <c>true</c>, the card face is shown with name, description, and tier indicator.
        /// When <c>false</c>, the card back mask is shown and text is hidden.
        /// </summary>
        /// <param name="symptomData">The symptom asset to display.</param>
        /// <param name="tier">The severity tier to look up on the symptom asset.</param>
        /// <param name="revealed"><c>true</c> to show the card face; <c>false</c> to show the card back.</param>
        public void SetupSubCard(SymptomCardSO symptomData, SymptomTier tier, bool revealed)
        {
            isRevealed = revealed;

            originalParent = transform.parent;
            originalLocalPosition = transform.localPosition;
            if (originalParent != null)
            {
                targetWorldPosition = originalParent.TransformPoint(originalLocalPosition);
            }

            // Always reset zoom when re-initializing from pool
            if (isZoomed)
            {
                ResetZoom();
            }

            if (symptomData != null && cardBackgroundSprite != null)
            {
                cardBackgroundSprite.color = GetCategoryColor(symptomData.specialtyCategory);
            }

            // The tier text should always be visible, even when unrevealed, so the player knows what diagnostic card is needed.
            if (tierText != null)
            {
                tierText.gameObject.SetActive(true);
                tierText.text = tier switch
                {
                    SymptomTier.Tier1 => "I",
                    SymptomTier.Tier2 => "II",
                    SymptomTier.Tier3 => "III",
                    _ => ""
                };
            }

            if (isRevealed && symptomData != null)
            {
                if (symptomNameText != null)
                {
                    symptomNameText.gameObject.SetActive(true);
                    symptomNameText.text = symptomData.GetNameByTier(tier);
                }

                if (symptomDescriptionText != null)
                {
                    symptomDescriptionText.gameObject.SetActive(true);
                    symptomDescriptionText.text = symptomData.GetDescriptionByTier(tier);
                }

                if (cardBackMask != null) cardBackMask.SetActive(false);
            }
            else
            {
                if (symptomNameText != null)        symptomNameText.gameObject.SetActive(false);
                if (symptomDescriptionText != null) symptomDescriptionText.gameObject.SetActive(false);
                
                if (cardBackMask != null)
                {
                    cardBackMask.SetActive(true);
                    
                    TextMeshPro questionMark = cardBackMask.GetComponentInChildren<TextMeshPro>();
                    if (questionMark != null)
                    {
                        questionMark.text = tier switch
                        {
                            SymptomTier.Tier1 => "I",
                            SymptomTier.Tier2 => "II",
                            SymptomTier.Tier3 => "III",
                            _ => "?"
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Returns a thematic colour for the card background based on the symptom's
        /// <see cref="SymptomCategory"/>.
        /// </summary>
        /// <param name="category">The specialty category to look up.</param>
        /// <returns>A <see cref="Color"/> representing the category's visual theme.</returns>
        private Color GetCategoryColor(SymptomCategory category)
        {
            return category switch
            {
                SymptomCategory.Trauma          => new Color(0.93f, 0.11f, 0.14f), // Neon Red (Matches Trauma Bay)
                SymptomCategory.Cardiopulmonary => new Color(0.00f, 0.68f, 0.94f), // Neon Blue (Matches Cardiology)
                SymptomCategory.Infectious      => new Color(0.51f, 0.75f, 0.51f), // Neon Green (Matches Isolation Ward)
                SymptomCategory.Internal        => new Color(1.00f, 0.60f, 0.00f), // Amber/Orange (Matches Internal Medicine)
                _ => Color.gray
            };
        }

        // -------------------------------------------------------------------------
        // Click-to-Zoom
        // -------------------------------------------------------------------------

        private Vector3 mouseDownPos;
        private bool isDraggingParent = false;

        /// <summary>The owning patient's drag handler, or null (e.g. on a ghost, whose drag script is removed).</summary>
        private DraggablePatient ParentDrag()
        {
            if (parentPatient == null) return null;
            var drag = parentPatient.GetComponent<DraggablePatient>();
            return drag != null && drag.isActiveAndEnabled ? drag : null;
        }

        public void PointerDown()
        {
            mouseDownPos = GameInput.PointerScreenPosition;
            isDraggingParent = false;
        }

        public void PointerDrag()
        {
            if (isZoomed) return;

            // If the mouse has moved enough, consider it a drag rather than a click
            if (!isDraggingParent && Vector3.Distance(mouseDownPos, GameInput.PointerScreenPosition) > 5f)
            {
                isDraggingParent = true;
                
                // Now that we confirmed it's a drag, tell the parent to start dragging
                ParentDrag()?.PointerDown();
            }

            // Forward drag to the patient card
            if (isDraggingParent) ParentDrag()?.PointerDrag();
        }

        public void PointerUp()
        {
            // If we didn't drag the card, it's a click!
            if (!isDraggingParent)
            {
                // While a patient is being picked (Team Assessment), clicking the symptom card picks its patient;
                // otherwise the symptom card, which sits over the middle of the patient card, swallows the click.
                if (PatientPicker.IsPicking && parentPatient != null)
                {
                    PatientPicker.TryPick(parentPatient);
                    return;
                }

                if (!isRevealed) return;

                // Another symptom card is already zoomed: close that one first.
                if (!isZoomed && currentlyZoomed != null && currentlyZoomed != this) return;

                isZoomed = !isZoomed;

                if (isZoomed)
                {
                    currentlyZoomed = this;

                    // Unparent so the card escapes the patient's SortingGroup and renders above everything.
                    transform.SetParent(null);

                    targetScale = originalScale * zoomMultiplier;

                    Camera cam = CameraUtil.Main;
                    float camX = cam != null ? cam.transform.position.x : 0f;
                    float camY = cam != null ? cam.transform.position.y : 0f;
                    targetWorldPosition = new Vector3(camX, camY, -5f);

                    if (sortingGroup != null)
                    {
                        // Move to the hand's layer (above the board and PlayZone) at the maximum order.
                        sortingGroup.sortingLayerName = zoomSortingLayer;
                        sortingGroup.sortingOrder = 30000;
                    }
                }
                else
                {
                    ResetZoom();
                }
            }
            else
            {
                // We were dragging, so tell the parent to drop
                ParentDrag()?.PointerUp();
            }
        }

        /// <summary>
        /// Restores the sub-card to its original scale, re-parents it to <see cref="originalParent"/>,
        /// and resets the sorting order.
        /// </summary>
        private void ResetZoom()
        {
            isZoomed = false;
            if (currentlyZoomed == this) currentlyZoomed = null;
            targetScale = originalScale;

            if (originalParent != null)
            {
                transform.SetParent(originalParent);
            }

            if (sortingGroup != null)
            {
                sortingGroup.sortingLayerID = originalSortingLayerID;
                sortingGroup.sortingOrder = originalSortingOrder;
            }
        }
    }
}
