using UnityEngine;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.UI;

namespace ChiefComplaint.Cards.Effects
{
    /// <summary>
    /// Applies a patient-targeted effect (e.g. heal or pause decay) to every patient with a Ward bed,
    /// including patients currently in surgery. Played on the play zone rather than on a patient.
    /// </summary>
    [CreateAssetMenu(fileName = "WardWideEffect", menuName = "Chief Complaint/Effects/Ward-Wide")]
    public class WardWideEffectSO : CardEffectSO
    {
        [Tooltip("A patient-targeted effect to apply to every Ward patient.")]
        public CardEffectSO patientEffect;

        public override bool RequiresPatientTarget => false;
        public override bool RequiresCardTarget => false;

        public override bool CanApply(PatientCardView targetPatient, ActionCardView targetCard)
        {
            if (patientEffect == null) return false;
            foreach (var patient in WardPatients())
            {
                if (patientEffect.CanApply(patient, null)) return true;
            }
            return false;
        }

        public override bool Apply(PatientCardView targetPatient, ActionCardView targetCard, bool rngFailed, Vector3 mousePos)
        {
            if (rngFailed)
            {
                GameFeedback.WarnAt("Failed!", mousePos);
                return true;
            }

            bool applied = false;
            foreach (var patient in WardPatients())
            {
                if (patientEffect.CanApply(patient, null) && patientEffect.Apply(patient, null, false, patient.transform.position))
                    applied = true;
            }
            return applied;
        }

        private static System.Collections.Generic.IEnumerable<PatientCardView> WardPatients()
        {
            var board = GameManager.Instance != null ? GameManager.Instance.boardManager : null;
            if (board == null) yield break;

            // Copy first: an effect could change the board while we iterate.
            var patients = new System.Collections.Generic.List<PatientCardView>(board.ActivePatients);
            foreach (var p in patients)
            {
                if (p != null && !p.isRemoved && p.assignedWardSlot != null) yield return p;
            }
        }
    }
}
