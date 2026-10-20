using ChiefComplaint.Sim.Data;

namespace ChiefComplaint.Sim.Engine
{
    /// <summary>Receives the events of a run. The stats recorder and the trace printer implement it.</summary>
    public interface IRunObserver
    {
        void OnPatientArrived(Game game, Patient patient);
        void OnSpawnSkipped(Game game, string reason);
        void OnPatientDied(Game game, Patient patient);
        void OnPatientCured(Game game, Patient patient, int repGained, int fundingGained);
        void OnSymptomTreated(Game game, Patient patient, Symptom symptom, bool paged, int apCost);
        void OnReputationChanged(Game game, int delta, string source);
        void OnFundingChanged(Game game, int delta, string source);
        void OnCardPlayed(Game game, CardDef card, Patient target);
        void OnTeamAssessment(Game game, int tier, Patient target);
        void OnPurchase(Game game, string item, CardDef card, int cost);
        void OnShiftEnded(Game game, int apLeftOver);
        void OnRunEnded(Game game);
        void OnAction(Game game, GameAction action);
    }

    /// <summary>Convenience base with empty handlers.</summary>
    public abstract class RunObserverBase : IRunObserver
    {
        public virtual void OnPatientArrived(Game game, Patient patient) { }
        public virtual void OnSpawnSkipped(Game game, string reason) { }
        public virtual void OnPatientDied(Game game, Patient patient) { }
        public virtual void OnPatientCured(Game game, Patient patient, int repGained, int fundingGained) { }
        public virtual void OnSymptomTreated(Game game, Patient patient, Symptom symptom, bool paged, int apCost) { }
        public virtual void OnReputationChanged(Game game, int delta, string source) { }
        public virtual void OnFundingChanged(Game game, int delta, string source) { }
        public virtual void OnCardPlayed(Game game, CardDef card, Patient target) { }
        public virtual void OnTeamAssessment(Game game, int tier, Patient target) { }
        public virtual void OnPurchase(Game game, string item, CardDef card, int cost) { }
        public virtual void OnShiftEnded(Game game, int apLeftOver) { }
        public virtual void OnRunEnded(Game game) { }
        public virtual void OnAction(Game game, GameAction action) { }
    }
}
