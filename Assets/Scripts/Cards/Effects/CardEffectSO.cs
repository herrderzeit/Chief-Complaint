using UnityEngine;
using ChiefComplaint.Cards.Views;

namespace ChiefComplaint.Cards.Effects
{
    public abstract class CardEffectSO : ScriptableObject
    {
        public abstract bool RequiresPatientTarget { get; }

        /// <summary>
        /// <c>true</c> if this effect can use a patient when one is targeted, even if it doesn't require one
        /// (e.g. a choice between healing a patient and drawing a card).
        /// </summary>
        public virtual bool CanTargetPatient => RequiresPatientTarget;
        public abstract bool RequiresCardTarget { get; }

        public abstract bool CanApply(PatientCardView targetPatient, ActionCardView targetCard);
        public abstract bool Apply(PatientCardView targetPatient, ActionCardView targetCard, bool rngFailed, Vector3 mousePos);
    }
}
