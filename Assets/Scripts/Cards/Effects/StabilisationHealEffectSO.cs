using UnityEngine;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.UI;

namespace ChiefComplaint.Cards.Effects
{
    [CreateAssetMenu(fileName = "StabilisationHealEffect", menuName = "Chief Complaint/Effects/Stabilisation Heal")]
    public class StabilisationHealEffectSO : CardEffectSO
    {
public int healAmount = 1;

        public override bool RequiresPatientTarget => true;
        public override bool RequiresCardTarget => false;

        // Healing a patient already at full Stability would waste the card.
        public override bool CanApply(PatientCardView targetPatient, ActionCardView targetCard) =>
            targetPatient != null && targetPatient.currentStability < targetPatient.maxStability;

        public override bool Apply(PatientCardView targetPatient, ActionCardView targetCard, bool rngFailed, Vector3 mousePos)
        {
            if (rngFailed)
            {
                GameFeedback.WarnAt("Failed!", targetPatient.transform.position);
                return true;
            }
            targetPatient.ApplyStabilisation(healAmount);
            return true;
        }
    }
}
