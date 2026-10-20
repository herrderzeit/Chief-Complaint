using UnityEngine;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.UI;

namespace ChiefComplaint.Cards.Effects
{
    [CreateAssetMenu(fileName = "DiagnoseEffect", menuName = "Chief Complaint/Effects/Diagnose")]
    public class DiagnoseEffectSO : CardEffectSO
    {
public SymptomTier diagnosesSymptomTier = SymptomTier.Tier3;

        public override bool RequiresPatientTarget => true;
        public override bool RequiresCardTarget => false;

        public override string Describe() => $"Diagnose (up to Tier {(int)diagnosesSymptomTier})";
        public override bool IsValidTarget(PatientCardView patient) => patient != null && patient.CanDiagnoseWith(diagnosesSymptomTier);
        public override bool CanApply(PatientCardView targetPatient, ActionCardView targetCard) => targetPatient != null;

        public override bool Apply(PatientCardView targetPatient, ActionCardView targetCard, bool rngFailed, Vector3 mousePos)
        {
            return targetPatient.TryDiagnoseNext(diagnosesSymptomTier, rngFailed);
        }
    }
}
