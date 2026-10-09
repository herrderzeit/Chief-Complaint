using UnityEngine;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.UI;

namespace ChiefComplaint.Cards.Effects
{
    [CreateAssetMenu(fileName = "CureSymptomEffect", menuName = "Chief Complaint/Effects/Cure Symptom")]
    public class CureSymptomEffectSO : CardEffectSO
    {
public SymptomTier curesSymptomTier = SymptomTier.Tier3;

        public override bool RequiresPatientTarget => true;
        public override bool RequiresCardTarget => false;

        public override bool CanApply(PatientCardView targetPatient, ActionCardView targetCard) => targetPatient != null;

        public override bool Apply(PatientCardView targetPatient, ActionCardView targetCard, bool rngFailed, Vector3 mousePos)
        {
            return targetPatient.TryCureSymptom(curesSymptomTier, rngFailed);
        }
    }
}
