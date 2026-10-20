using System.Collections.Generic;
using ChiefComplaint.Sim.Data;

namespace ChiefComplaint.Sim.Engine
{
    public enum Location
    {
        /// <summary>The Triage river: full decay, cannot be diagnosed.</summary>
        Triage,
        /// <summary>A Ward bed: half decay, can be diagnosed.</summary>
        Ward,
        /// <summary>A specialty department. The patient keeps their Ward bed (the "ghost") while there.</summary>
        Department
    }

    public sealed class Symptom
    {
        public SymptomDef Def;
        public int Tier;
        public SymptomCategory Category => Def.Category;
        public override string ToString() => $"{Def.NameAt(Tier)} (T{Tier} {Def.Category})";
    }

    /// <summary>Runtime state of one patient. Mirrors the state fields of <c>PatientCardView</c>.</summary>
    public sealed class Patient
    {
        public int Id;
        public PatientDef Def;
        /// <summary>Sorted by tier ascending at spawn, so diagnosis reveals the lowest tiers first.</summary>
        public List<Symptom> Symptoms = new List<Symptom>();
        public int DiagnosedCount;
        public int Stability;
        public int MaxStability;
        public int DecayRate;
        public bool ClockPaused;
        public bool InSurgery;
        public bool Removed;
        public Location Location = Location.Triage;
        /// <summary>The department the patient is in while <see cref="Location"/> is Department.</summary>
        public SymptomCategory Department;
        public int ArrivalShift;
        /// <summary>Number of symptoms at arrival, for reporting.</summary>
        public int InitialSymptomCount;
        public int InitialMaxTier;

        public bool IsFullyDiagnosed => DiagnosedCount >= Symptoms.Count;
        public bool IsAwaitingDischarge => InSurgery && Symptoms.Count == 0;
        /// <summary>The patient holds a Ward bed, either lying in it or visiting a department from it.</summary>
        public bool HasWardBed => Location == Location.Ward || Location == Location.Department;
        public EscalationType Pool => Def.Phase;

        /// <summary>Tier of the next hidden symptom, or 0 when fully diagnosed.</summary>
        public int NextHiddenTier => IsFullyDiagnosed ? 0 : Symptoms[DiagnosedCount].Tier;

        /// <summary>Decay this patient will take at the start of next shift, given where it is now.</summary>
        public int PendingDecay
        {
            get
            {
                if (ClockPaused) return 0;
                return HasWardBed ? System.Math.Max(1, (DecayRate + 1) / 2) : DecayRate;
            }
        }

        public bool CanTeamAssess(int maxTier) => !Removed && !IsFullyDiagnosed && Symptoms[DiagnosedCount].Tier <= maxTier;

        /// <summary>Lowest tier among the symptoms of a category, or 0 if none (or not fully diagnosed).</summary>
        public int LowestTierIn(SymptomCategory category)
        {
            if (!IsFullyDiagnosed) return 0;
            int lowest = 0;
            foreach (Symptom s in Symptoms)
                if (s.Category == category && (lowest == 0 || s.Tier < lowest)) lowest = s.Tier;
            return lowest;
        }

        public override string ToString() => $"#{Id} {Def.Name} [{Def.Phase}] stab {Stability}/{MaxStability} dx {DiagnosedCount}/{Symptoms.Count} @{Location}";
    }
}
