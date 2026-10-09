using UnityEngine;
using ChiefComplaint.Cards.Views;

namespace ChiefComplaint.Cards.Effects
{
    public abstract class CardEffectSO : ScriptableObject
    {
        public abstract bool RequiresPatientTarget { get; }
        public abstract bool RequiresCardTarget { get; }

        public abstract bool CanApply(PatientCardView targetPatient, ActionCardView targetCard);
        public abstract bool Apply(PatientCardView targetPatient, ActionCardView targetCard, bool rngFailed, Vector3 mousePos);
    }
}
