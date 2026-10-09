using UnityEngine;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.UI;

namespace ChiefComplaint.Cards.Effects
{
    [CreateAssetMenu(fileName = "PauseStabilityEffect", menuName = "Chief Complaint/Effects/Pause Stability Clock")]
    public class PauseStabilityEffectSO : CardEffectSO
    {
public override bool RequiresPatientTarget => true;
        public override bool RequiresCardTarget => false;

        public override bool CanApply(PatientCardView targetPatient, ActionCardView targetCard) => targetPatient != null && !targetPatient.stabilityClockPaused;

        public override bool Apply(PatientCardView targetPatient, ActionCardView targetCard, bool rngFailed, Vector3 mousePos)
        {
            if (rngFailed)
            {
                GameFeedback.WarnAt("Failed!", targetPatient.transform.position);
                return true; // Effect resolved (but failed)
            }
            targetPatient.PauseStabilityClock();
            return true;
        }
    }
}
