using UnityEngine;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.UI;

namespace ChiefComplaint.Cards.Effects
{
    /// <summary>
    /// Lets the player choose one of two effects when the card is played. If only one option can
    /// apply to the target (e.g. the patient's decay is already paused), it is applied without asking.
    /// </summary>
    [CreateAssetMenu(fileName = "ChoiceEffect", menuName = "Chief Complaint/Effects/Choice")]
    public class ChoiceEffectSO : CardEffectSO
    {
        [Tooltip("Heading shown on the choice prompt (usually the card name).")]
        public string promptTitle;

        public string optionALabel;
        public CardEffectSO optionA;

        public string optionBLabel;
        public CardEffectSO optionB;

        // A patient is only required when neither option can be played without one; otherwise the card
        // can go on the play zone (using the untargeted option) or on a patient (offering both).
        public override bool RequiresPatientTarget =>
            (optionA == null || optionA.RequiresPatientTarget) && (optionB == null || optionB.RequiresPatientTarget);

        public override bool CanTargetPatient =>
            (optionA != null && optionA.CanTargetPatient) || (optionB != null && optionB.CanTargetPatient);

        public override bool RequiresCardTarget =>
            (optionA != null && optionA.RequiresCardTarget) || (optionB != null && optionB.RequiresCardTarget);

        public override bool CanApply(PatientCardView targetPatient, ActionCardView targetCard) =>
            CanUse(optionA, targetPatient, targetCard) || CanUse(optionB, targetPatient, targetCard);

        public override bool Apply(PatientCardView targetPatient, ActionCardView targetCard, bool rngFailed, Vector3 mousePos)
        {
            if (rngFailed)
            {
                GameFeedback.WarnAt("Failed!", mousePos);
                return true;
            }

            bool canA = CanUse(optionA, targetPatient, targetCard);
            bool canB = CanUse(optionB, targetPatient, targetCard);

            if (canA && !canB) return optionA.Apply(targetPatient, targetCard, false, mousePos);
            if (canB && !canA) return optionB.Apply(targetPatient, targetCard, false, mousePos);
            if (!canA && !canB) return false;

            ChoicePrompt.Show(promptTitle,
                optionALabel, () => ApplyIfStillValid(optionA, targetPatient, targetCard, mousePos),
                optionBLabel, () => ApplyIfStillValid(optionB, targetPatient, targetCard, mousePos));
            return true;
        }

        private static bool CanUse(CardEffectSO effect, PatientCardView patient, ActionCardView card) =>
            effect != null && effect.CanApply(patient, card);

        /// <summary>The prompt resolves later, so re-check the target still exists before applying.</summary>
        private static void ApplyIfStillValid(CardEffectSO effect, PatientCardView patient, ActionCardView card, Vector3 mousePos)
        {
            if (effect.RequiresPatientTarget && (patient == null || patient.isRemoved)) return;
            if (effect.CanApply(patient, card)) effect.Apply(patient, card, false, mousePos);
        }
    }
}
