using UnityEngine;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.UI;

namespace ChiefComplaint.Cards.Effects
{
    [CreateAssetMenu(fileName = "CleanseRedTapeEffect", menuName = "Chief Complaint/Effects/Cleanse Red Tape")]
    public class CleanseRedTapeEffectSO : CardEffectSO
    {
public int repBonusPerRedTapeCleansed = 1;

        public override bool RequiresPatientTarget => false;
        public override bool RequiresCardTarget => false;

        public override string Describe() => "Cleanse Red Tape";
        public override bool CanApply(PatientCardView targetPatient, ActionCardView targetCard) => true;

        public override bool Apply(PatientCardView targetPatient, ActionCardView targetCard, bool rngFailed, Vector3 mousePos)
        {
            if (rngFailed)
            {
                GameFeedback.WarnAt("Failed!", mousePos);
                return true;
            }

            if (DeckManager.Instance != null)
            {
                int cleansed = DeckManager.Instance.CleanseRedTape(repBonusPerRedTapeCleansed);
                GameFeedback.ShowAt(cleansed > 0 ? $"{cleansed} Red Tape cleansed" : "No Red Tape found", mousePos,
                    cleansed > 0 ? GameFeedback.CardColour : GameFeedback.WarningColour);
            }
            return true;
        }
    }
}
