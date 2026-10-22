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

        /// <summary>Short result text for the drag preview, e.g. "+2 AP" or "Pause decay". Null if there is nothing to show.</summary>
        public virtual string Describe() => null;

        /// <summary>Reputation this effect takes when played (e.g. a Red Tape "Lose 3 Reputation"), counted by the Reputation gate.</summary>
        public virtual int ReputationLoss => 0;

        /// <summary>What this effect gives when the card is dropped on the play zone; null if it needs a target.</summary>
        public virtual string DescribeOnPlayZone() => RequiresPatientTarget || RequiresCardTarget ? null : Describe();

        /// <summary>
        /// <c>true</c> if dropping the card on <paramref name="patient"/> would actually work (used to glow valid
        /// targets while dragging). Stricter than <see cref="CanApply"/>, which only checks a target exists.
        /// </summary>
        public virtual bool IsValidTarget(PatientCardView patient) => CanTargetPatient && patient != null && CanApply(patient, null);
        public abstract bool Apply(PatientCardView targetPatient, ActionCardView targetCard, bool rngFailed, Vector3 mousePos);
    }
}
