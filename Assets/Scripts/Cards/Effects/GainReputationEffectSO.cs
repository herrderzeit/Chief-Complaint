using UnityEngine;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.UI;

namespace ChiefComplaint.Cards.Effects
{
    [CreateAssetMenu(fileName = "GainReputationEffect", menuName = "Chief Complaint/Effects/Gain Reputation")]
    public class GainReputationEffectSO : CardEffectSO
    {
public int reputationGenerated = 1;

        public override bool RequiresPatientTarget => false;
        public override bool RequiresCardTarget => false;

        public override string Describe() => reputationGenerated >= 0 ? $"+{reputationGenerated} Rep" : $"{reputationGenerated} Rep";
        public override bool CanApply(PatientCardView targetPatient, ActionCardView targetCard) => true;
        public override int ReputationLoss => Mathf.Max(0, -reputationGenerated);

        public override bool Apply(PatientCardView targetPatient, ActionCardView targetCard, bool rngFailed, Vector3 mousePos)
        {
            if (rngFailed)
            {
                GameFeedback.WarnAt("Failed!", mousePos);
                return true;
            }
            
            if (reputationGenerated > 0) GameManager.Instance.AddReputation(reputationGenerated);
            else if (reputationGenerated < 0) GameManager.Instance.SpendReputation(Mathf.Abs(reputationGenerated));

            if (reputationGenerated != 0)
                GameFeedback.ShowAt($"{(reputationGenerated > 0 ? "+" : "")}{reputationGenerated} Rep", mousePos,
                    reputationGenerated > 0 ? GameFeedback.RewardColour : GameFeedback.WarningColour);
            
            return true;
        }
    }
}
