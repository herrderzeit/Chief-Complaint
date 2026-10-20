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
    /// Handles mouse-driven drag-and-drop movement for patient cards on the board.
    /// On drag start, highlights valid/invalid drop targets (ward and triage slots).
    /// On drop, validates placement rules and snaps the patient to the chosen slot,
    /// managing ward reservations and the ghost-in-ward mechanic for department visits.
    /// </summary>
    [RequireComponent(typeof(PatientCardView))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class DraggablePatient : MonoBehaviour
    {
        private bool isDragging = false;
        private Vector3 offset;
        private Vector3 originalPosition;
        private SortingGroup sortingGroup;
        private int originalSortingOrder;
        private PatientCardView patientView;

        void Start()
        {
            sortingGroup = GetComponent<SortingGroup>();
            patientView = GetComponent<PatientCardView>();
            if (sortingGroup != null) originalSortingOrder = sortingGroup.sortingOrder;
        }

        /// <summary>
        /// Stores the original SpriteRenderer colours for all board slots so they can be
        /// restored after a drag ends. Shared across all DraggablePatient instances.
        /// </summary>
        private static System.Collections.Generic.Dictionary<SpriteRenderer, Color> defaultColors =
            new System.Collections.Generic.Dictionary<SpriteRenderer, Color>();

        private static System.Collections.Generic.Dictionary<SpriteRenderer, SpriteRenderer> highlightOverlays = 
            new System.Collections.Generic.Dictionary<SpriteRenderer, SpriteRenderer>();

        /// <summary>Clears the static colour cache so stale renderers never survive a scene reload or fast Play Mode entry.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            defaultColors.Clear();
            highlightOverlays.Clear();
        }

        private void SetSlotHighlight(SpriteRenderer sr, Color color, bool active)
        {
            if (!active)
            {
                if (highlightOverlays.TryGetValue(sr, out var overlay) && overlay != null)
                    overlay.gameObject.SetActive(false);
                return;
            }

            if (!highlightOverlays.TryGetValue(sr, out var overlayInstance) || overlayInstance == null)
            {
                GameObject go = new GameObject("DragHighlightOverlay");
                go.transform.SetParent(sr.transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                go.transform.localRotation = Quaternion.identity;
                overlayInstance = go.AddComponent<SpriteRenderer>();
                overlayInstance.sprite = sr.sprite;
                overlayInstance.sortingLayerID = sr.sortingLayerID;
                overlayInstance.sortingOrder = sr.sortingOrder + 1;
                highlightOverlays[sr] = overlayInstance;
            }

            overlayInstance.gameObject.SetActive(true);
            overlayInstance.color = color;
        }

        /// <summary>Live patients on the board, from the <see cref="BoardManager"/> registry (no scene-wide Find).</summary>
        private static System.Collections.Generic.IReadOnlyList<PatientCardView> GetAllPatients()
        {
            if (GameManager.Instance != null && GameManager.Instance.boardManager != null)
                return GameManager.Instance.boardManager.ActivePatients;
            return System.Array.Empty<PatientCardView>();
        }

        void OnMouseDown()
        {
            // The board is locked during the draft phase and after Game Over.
            if (GameManager.Instance != null && !GameManager.Instance.CanInteract) return;

            // A treated patient stays in surgery until the next shift.
            if (patientView != null && patientView.isInSurgery)
            {
                if (NotificationManager.Instance != null)
                    NotificationManager.Instance.ShowNotificationAtPosition("In surgery until next shift!", UnityEngine.InputSystem.Mouse.current.position.ReadValue(), new Color(1f, 0.4f, 0.4f));
                return;
            }

            isDragging = true;
            originalPosition = transform.position;
            offset = transform.position - GetMouseWorldPosition();

            if (sortingGroup != null) sortingGroup.sortingOrder = originalSortingOrder + 100;

            HighlightDropTargets();
        }

        void OnMouseDrag()
        {
            if (isDragging) transform.position = GetMouseWorldPosition() + offset;
        }

        void OnMouseUp()
        {
            if (!isDragging) return;
            isDragging = false;
            if (sortingGroup != null) sortingGroup.sortingOrder = originalSortingOrder;

            Collider2D[] hits = Physics2D.OverlapPointAll(GetMouseWorldPosition());
            bool droppedInValidSlot = false;

            System.Collections.Generic.IReadOnlyList<PatientCardView> allPatients = GetAllPatients();

            foreach (Collider2D hit in hits)
            {
                // --- Slot Occupancy Check ---
                bool isSlotEmpty = true;
                bool isMyOwnSlot = false;

                PatientCardView[] existings = hit.transform.GetComponentsInChildren<PatientCardView>();
                foreach (var existing in existings)
                {
                    if (existing != null)
                    {
                        if (patientView.ghostInstance != null && existing.gameObject == patientView.ghostInstance) continue;

                        if (existing.transform == this.transform)
                        {
                            isMyOwnSlot = true;
                        }
                        else
                        {
                            isSlotEmpty = false;
                        }
                    }
                }

                // --- Reservation Check ---
                // A ward slot can be "reserved" by a patient currently visiting a department.
                bool isSlotReserved = false;
                foreach (var p in allPatients)
                {
                    if (p == null) continue;
                    if (patientView.ghostInstance != null && p.gameObject == patientView.ghostInstance) continue;

                    if (p != patientView && p.assignedWardSlot == hit.transform)
                    {
                        isSlotReserved = true;
                        break;
                    }
                }

                bool isMyReservedSlot = (hit.transform == patientView.assignedWardSlot);

                // Bed-hopping rule: a hospitalised patient cannot claim a different ward bed.
                bool isForbiddenBedHopping = (hit.CompareTag("WardSlot") && patientView.assignedWardSlot != null && !isMyReservedSlot);

                if (isForbiddenBedHopping)
                {
                    if (NotificationManager.Instance != null)
                        NotificationManager.Instance.ShowNotificationAtPosition("Patient already has a bed!", UnityEngine.InputSystem.Mouse.current.position.ReadValue(), new Color(1f, 0.4f, 0.4f));
                    continue;
                }

                // --- Department Validation (Evaluated in strict logical order) ---
                DepartmentSlot deptZone = hit.GetComponent<DepartmentSlot>();
                if (deptZone != null)
                {
                    // 1. Patient must be diagnosed to know where they go
                    if (!patientView.isFullyDiagnosed)
                    {
                        if (NotificationManager.Instance != null)
                            NotificationManager.Instance.ShowNotificationAtPosition("Patient needs full diagnosis!", UnityEngine.InputSystem.Mouse.current.position.ReadValue(), new Color(1f, 0.4f, 0.4f));
                        continue;
                    }

                    // 2. Patient must match the specific specialty
                    if (!IsMatchingSpecialtyDepartment(deptZone.category))
                    {
                        if (NotificationManager.Instance != null)
                            NotificationManager.Instance.ShowNotificationAtPosition("Wrong department for patient!", UnityEngine.InputSystem.Mouse.current.position.ReadValue(), new Color(1f, 0.4f, 0.4f));
                        continue;
                    }

                    // 3. Department must be open (or we must already be in it)
                    bool isDepartmentClosed = !deptZone.isAvailable;
                    if (isDepartmentClosed && !isMyOwnSlot)
                    {
                        if (NotificationManager.Instance != null)
                            NotificationManager.Instance.ShowNotificationAtPosition("Department used this shift!", UnityEngine.InputSystem.Mouse.current.position.ReadValue(), new Color(1f, 0.4f, 0.4f));
                        continue;
                    }
                }

                if (!isForbiddenBedHopping && ((isSlotEmpty && !isSlotReserved) || isMyOwnSlot || isMyReservedSlot))
                {
                    // --- Rule 1: Triage & Ward Slots ---
                    if (hit.CompareTag("WardSlot") || hit.CompareTag("TriageSlot"))
                    {
                        // Hospitalised patients cannot be returned to triage.
                        if (hit.CompareTag("TriageSlot") && patientView.assignedWardSlot != null && patientView.diagnosedCount > 0)
                        {
                            if (NotificationManager.Instance != null)
                                NotificationManager.Instance.ShowNotificationAtPosition("Cannot move hospitalized patient to Triage!", UnityEngine.InputSystem.Mouse.current.position.ReadValue(), new Color(1f, 0.4f, 0.4f));
                            continue;
                        }

                        SnapToSlot(hit.transform);
                        droppedInValidSlot = true;

                        if (hit.CompareTag("WardSlot"))
                        {
                            patientView.assignedWardSlot = hit.transform;
                            patientView.ClearGhost();
                        }
                        else
                        {
                            // Moving back to triage clears the ward reservation.
                            patientView.assignedWardSlot = null;
                            patientView.ClearGhost();
                        }
                        break;
                    }

                    // --- Rule 2: Specialty Departments ---
                    if (deptZone != null)
                    {
                        // Validation (diagnosis, match, open status) already passed above
                        SnapToSlot(hit.transform);
                        droppedInValidSlot = true;
                        // The ward reservation is intentionally preserved so the ghost can occupy it.
                        patientView.SpawnGhostInWard();
                        break;
                    }
                }
            }

            if (!droppedInValidSlot)
            {
                transform.position = originalPosition;
            }

            RestoreSlotColors();
        }

        // -------------------------------------------------------------------------
        // Slot Highlighting
        // -------------------------------------------------------------------------

        /// <summary>
        /// Tints all ward and triage slot sprites green (available) or red (unavailable)
        /// to give the player clear visual feedback while dragging.
        /// </summary>
        private void HighlightDropTargets()
        {
            System.Collections.Generic.IReadOnlyList<PatientCardView> allPatients = GetAllPatients();

            if (BoardView.Instance == null) return;

            foreach (Transform ward in BoardView.Instance.wardSlots)
            {
                SpriteRenderer sr = ward.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                if (!defaultColors.ContainsKey(sr)) defaultColors[sr] = sr.color;

                bool isReservedBySomeoneElse = false;
                foreach (var p in allPatients)
                {
                    if (p == null) continue;
                    // Exclude our own ghost to avoid false positives.
                    if (patientView.ghostInstance != null && p.gameObject == patientView.ghostInstance) continue;
                    if (p.assignedWardSlot == ward && p != patientView) { isReservedBySomeoneElse = true; break; }
                }

                bool isPhysicallyOccupiedBySomeoneElse = false;
                PatientCardView[] existings = ward.GetComponentsInChildren<PatientCardView>();
                foreach (var existing in existings)
                {
                    if (existing != null && existing.transform != patientView.transform)
                    {
                        if (patientView.ghostInstance != null && existing.gameObject == patientView.ghostInstance) continue;
                        isPhysicallyOccupiedBySomeoneElse = true;
                        break;
                    }
                }

                // A patient with a ward bed cannot hop to a different bed.
                bool isForbiddenBedHopping = (patientView.assignedWardSlot != null && patientView.assignedWardSlot != ward);

                Color targetColor = (isReservedBySomeoneElse || isPhysicallyOccupiedBySomeoneElse || isForbiddenBedHopping)
                    ? new Color(1f, 0.4f, 0.4f, 0.5f)
                    : new Color(0.4f, 1f, 0.4f, 0.5f);

                SetSlotHighlight(sr, targetColor, true);
            }

            if (BoardView.Instance.triageSlots == null) return;

            bool isHospitalized = (patientView.assignedWardSlot != null && patientView.diagnosedCount > 0);

            foreach (Transform triage in BoardView.Instance.triageSlots)
            {
                SpriteRenderer sr = triage.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                if (!defaultColors.ContainsKey(sr)) defaultColors[sr] = sr.color;

                bool isPhysicallyOccupiedBySomeoneElse = false;
                PatientCardView existing = triage.GetComponentInChildren<PatientCardView>();
                if (existing != null && existing.transform != patientView.transform) isPhysicallyOccupiedBySomeoneElse = true;

                // Hospitalised patients and occupied slots are forbidden.
                Color targetColor = (isHospitalized || isPhysicallyOccupiedBySomeoneElse)
                    ? new Color(1f, 0.4f, 0.4f, 0.5f)
                    : new Color(0.4f, 1f, 0.4f, 0.5f);

                SetSlotHighlight(sr, targetColor, true);
            }

            if (BoardView.Instance.departmentSlots != null)
            {
                if (patientView.isFullyDiagnosed)
                {
                    foreach (DepartmentSlot dept in BoardView.Instance.departmentSlots)
                    {
                        if (dept == null) continue;

                        bool isMatch = IsMatchingSpecialtyDepartment(dept.category);
                        
                        bool isPhysicallyOccupiedBySomeoneElse = false;
                        PatientCardView[] existings = dept.GetComponentsInChildren<PatientCardView>();
                        foreach (var existing in existings)
                        {
                            if (existing != null && existing.transform != patientView.transform)
                            {
                                if (patientView.ghostInstance != null && existing.gameObject == patientView.ghostInstance) continue;
                                isPhysicallyOccupiedBySomeoneElse = true;
                                break;
                            }
                        }

                        bool isValidDrop = isMatch && dept.isAvailable && !isPhysicallyOccupiedBySomeoneElse;

                        if (isValidDrop)
                        {
                            dept.ResetColor();
                        }
                        else
                        {
                            dept.SetTargetColor(new Color(0.3f, 0.3f, 0.3f, 0.4f));
                        }
                    }
                }
            }
        }

        /// <summary>Restores all ward and triage slot sprites to their original colours after a drag ends.</summary>
        private void RestoreSlotColors()
        {
            if (BoardView.Instance == null) return;

            foreach (Transform ward in BoardView.Instance.wardSlots)
            {
                SpriteRenderer sr = ward.GetComponent<SpriteRenderer>();
                if (sr != null) SetSlotHighlight(sr, Color.white, false);
            }

            if (BoardView.Instance.triageSlots != null)
            {
                foreach (Transform triage in BoardView.Instance.triageSlots)
                {
                    SpriteRenderer sr = triage.GetComponent<SpriteRenderer>();
                    if (sr != null) SetSlotHighlight(sr, Color.white, false);
                }
            }

            if (BoardView.Instance.departmentSlots != null)
            {
                foreach (DepartmentSlot dept in BoardView.Instance.departmentSlots)
                {
                    if (dept == null) continue;
                    dept.ResetColor();
                }
            }
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Moves and re-parents this patient card to <paramref name="slotTransform"/>,
        /// keeping it at Z = −1 to render in front of slot backgrounds.
        /// Also updates <see cref="originalPosition"/> so a failed future drop snaps here.
        /// </summary>
        /// <param name="slotTransform">The target slot transform to snap to.</param>
        private void SnapToSlot(Transform slotTransform)
        {
            transform.position = new Vector3(slotTransform.position.x, slotTransform.position.y, -1f);
            transform.SetParent(slotTransform);
            originalPosition = transform.position;
        }

        /// <summary>
        /// Returns <c>true</c> if any of the patient's incoming symptom categories match
        /// the specialty of the department the patient was dropped on.
        /// </summary>
        /// <param name="departmentCategory">The <see cref="DepartmentSlot.category"/> of the target department.</param>
        /// <returns><c>true</c> when the patient has a matching specialty category.</returns>
        private bool IsMatchingSpecialtyDepartment(SymptomCategory departmentCategory)
        {
            foreach (var symptom in patientView.assignedSymptoms)
            {
                if (symptom.specialtyCategory == departmentCategory) return true;
            }
            return false;
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
