using UnityEngine;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.UI;

namespace ChiefComplaint.Cards.Effects
{
    [CreateAssetMenu(fileName = "GainAPEffect", menuName = "Chief Complaint/Effects/Gain AP")]
    public class GainAPEffectSO : CardEffectSO
    {
public int apGenerated = 1;

        public override bool RequiresPatientTarget => false;
        public override bool RequiresCardTarget => false;

        public override bool CanApply(PatientCardView targetPatient, ActionCardView targetCard) => true;

        public override bool Apply(PatientCardView targetPatient, ActionCardView targetCard, bool rngFailed, Vector3 mousePos)
        {
            if (rngFailed)
            {
                GameFeedback.WarnAt("Failed!", mousePos);
                return true;
            }
            GameManager.Instance.GainAP(apGenerated);
            GameFeedback.ShowAt($"+{apGenerated} AP", mousePos, GameFeedback.APColour);
            return true;
        }
    }
}
