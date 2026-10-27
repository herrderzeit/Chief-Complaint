using UnityEngine;
using System.Collections.Generic;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.StaffRoom;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Board
{
    /// <summary>
    /// Manages the physical board: triage slots, ward slots, and department slots.
    /// Responsible for spawning patients, advancing turn decay, and resetting
    /// department zone availability at the start of each shift.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class BoardManager : MonoBehaviour
    {
        /// <summary>Raised with the number of patients currently in critical condition (Code Blue).</summary>
        public event System.Action<int> OnCodeBlueCountChanged;
        /// <summary>Raised when an arriving patient is diverted because every Triage slot is full (world position for feedback).</summary>
        public event System.Action<Vector3> OnPatientDiverted;

        // Board slots and Prefabs have been moved to BoardView.

        /// <summary>
        /// Every live patient on the board, wherever they currently sit (triage, ward or department).
        /// Patients remove themselves when destroyed. Ward ghosts are never registered.
        /// </summary>
        private readonly List<PatientCardView> activePatients = new List<PatientCardView>();

        /// <summary>
        /// Read-only registry of live patients. Prefer this over <c>FindObjectsByType</c>,
        /// which allocates and scans the whole scene.
        /// </summary>
        public IReadOnlyList<PatientCardView> ActivePatients => activePatients;

        // -------------------------------------------------------------------------
        // Shift Loop
        // -------------------------------------------------------------------------

        /// <summary>
        /// Ends every surgery stay: fully treated patients are discharged (paying their rewards) and the
        /// rest return to their beds. Called when End Shift is pressed, before the staff room opens.
        /// </summary>
        public void ResolveSurgeries()
        {
            // Iterate backwards: a discharge removes the patient from activePatients immediately.
            for (int i = activePatients.Count - 1; i >= 0; i--)
            {
                if (i < activePatients.Count && activePatients[i] != null) activePatients[i].ResolveSurgery();
            }
        }

        /// <summary>
        /// Called by <see cref="GameManager.StartNewShift"/> to advance the board state:
        /// applies stability decay to every patient on the board and resets department
        /// zone indicators. Spawning is a separate step (<see cref="SpawnPatient"/>) so
        /// <see cref="GameManager"/> controls the order relative to AP and hand setup.
        /// </summary>
        public void AdvanceShift()
        {
            // Safety net: surgeries are normally resolved when End Shift is pressed.
            ResolveSurgeries();

            // A death removes the patient from activePatients immediately, so iterate backwards
            // over the list: no patient is skipped when an earlier one is removed.
            for (int i = activePatients.Count - 1; i >= 0; i--)
            {
                if (i >= activePatients.Count) continue;
                PatientCardView patient = activePatients[i];
                if (patient != null && !patient.isRemoved) patient.AdvanceShiftDecay();
            }

            if (BoardView.Instance != null)
            {
                BoardView.Instance.ResetDepartmentZones();
            }

            NotifyCodeBlueStatus();
        }

        /// <summary>Broadcasts the number of Code Blues (patients in critical stability) currently on the board.</summary>
        public void NotifyCodeBlueStatus()
        {
            int codeBlueCount = 0;
            for (int i = 0; i < activePatients.Count; i++)
            {
                if (activePatients[i] != null && !activePatients[i].isRemoved && activePatients[i].currentStability <= PatientCardView.LowStabilityThreshold)
                {
                    codeBlueCount++;
                }
            }
            OnCodeBlueCountChanged?.Invoke(codeBlueCount);
        }

        private void HandlePatientDied(PatientCardView patient)
        {
            NotifyCodeBlueStatus(); // Update count since they are no longer alive
        }

        private void HandleStabilityChanged(PatientCardView patient)
        {
            NotifyCodeBlueStatus(); // A heal may have lifted the patient out of Code Blue
        }

        private void HandlePatientRemoved(PatientCardView patient)
        {
            activePatients.Remove(patient);
            patient.OnDied -= HandlePatientDied;
            patient.OnRemoved -= HandlePatientRemoved;
            patient.OnStabilityChanged -= HandleStabilityChanged;
            NotifyCodeBlueStatus(); // Keep UI perfectly in sync if a critical patient is cured/removed
        }

        // -------------------------------------------------------------------------
        // Patient Spawning
        // -------------------------------------------------------------------------

        /// <summary>
        /// A triage slot is occupied while a live patient is parented to it.
        /// Moving a patient to a ward or department therefore frees their triage slot.
        /// </summary>
        public void SpawnPatient()
        {
            if (BoardView.Instance == null) return;
            
            Transform emptySlot = BoardView.Instance.GetEmptyTriageSlot(activePatients);
            if (emptySlot == null)
            {
                // Ambulance diversion: the patient goes to another hospital (GameManager applies the penalty).
                GameLog.Warn("Triage River is full! Patient diverted.");
                var slots = BoardView.Instance.triageSlots;
                OnPatientDiverted?.Invoke(slots.Length > 0 ? slots[slots.Length / 2].position : Vector3.zero);
                return;
            }

            if (PatientDeckManager.Instance == null) return;

            PatientCardSO randomPatient = PatientDeckManager.Instance.DrawRandomPatient();
            if (randomPatient == null) return;

            List<GeneratedSymptom> generated = PatientDeckManager.Instance.GenerateSymptomsForPatient(randomPatient);
            List<SymptomCardSO> drawnSymptoms = new List<SymptomCardSO>();
            List<SymptomTier> drawnTiers = new List<SymptomTier>();
            foreach (var sym in generated)
            {
                drawnSymptoms.Add(sym.Card);
                drawnTiers.Add(sym.Tier);
            }

            PatientCardView view = BoardView.Instance.GetPatientCard();
            view.transform.position = emptySlot.position;
            view.transform.rotation = Quaternion.identity;
            view.transform.SetParent(emptySlot);
            view.transform.localPosition = new Vector3(0, 0, -1f);

            activePatients.Add(view);
            view.OnDied += HandlePatientDied;
            view.OnRemoved += HandlePatientRemoved;
            view.OnStabilityChanged += HandleStabilityChanged;

            view.InitialiseCard(randomPatient, drawnSymptoms, drawnTiers, null); // Prefab logic handled in BoardView

            // A new arrival may already be critical, so refresh the Code Blue count straight away.
            NotifyCodeBlueStatus();
        }
    }
}
