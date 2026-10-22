using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace ChiefComplaint.Sim.Data
{
    /// <summary>
    /// Tunable numbers of a run. Defaults match the code; the loader then overwrites them with the values
    /// serialized in MainBoard.unity, and <c>--set name=value</c> overrides them for balance experiments.
    /// </summary>
    public sealed class GameConfig
    {
        // GameManager
        public int BaseAP = 3;
        public int StartingReputation = 8;
        public int StartingFunding = 6;
        public int InitialPatientCount = 2;
        public int DeathReputationPenalty = 5;
        public bool DeathAddsRedTape = true;
        public int ShiftsToWin = 15;
        public int WalkInFundingReward = 3;
        public int AmbulanceFundingReward = 6;
        public int TraumaFlightFundingReward = 8;
        public int CureReputationCap = 3;

        // PatientDeckManager
        public int AmbulanceCureThreshold = 3;
        public int TraumaFlightCureThreshold = 8;
        // Time-based escalation: the pool also unlocks at the start of this shift (0 = cures only).
        public int AmbulanceShiftThreshold = 5;
        public int TraumaFlightShiftThreshold = 10;
        // GameManager: Reputation lost when an arriving patient is diverted because Triage is full (0 = free).
        public int DiversionReputationPenalty = 2;

        // GameManager.extraPatientEveryNShifts: an extra patient arrives at the start of every Nth shift (0 = off).
        public int ExtraPatientEveryNShifts = 3;
        // GameManager.maxReputation: Reputation gains stop here (0 = no cap).
        public int MaxReputation = 16;

        // DeckManager
        public int HandSize = 5;

        // BoardView (slot counts in the scene)
        public int TriageSlots = 5;
        public int WardSlots = 4;

        // DraftManager (const MARKET_SIZE)
        public int MarketSize = 5;

        // DepartmentFallbackButton ("Page the on-call team" cost by symptom tier)
        public int PageCostTier1 = 2;
        public int PageCostTier2 = 2;
        public int PageCostTier3 = 3;

        // PatientCardView
        public int LowStabilityThreshold = 3;
        public int BarelyStableThreshold = 2;

        public int FundingRewardFor(EscalationType phase) => phase switch
        {
            EscalationType.Ambulance => AmbulanceFundingReward,
            EscalationType.TraumaFlight => TraumaFlightFundingReward,
            _ => WalkInFundingReward
        };

        public int PageCost(int tier) => tier switch { 1 => PageCostTier1, 2 => PageCostTier2, _ => PageCostTier3 };

        public GameConfig Clone() => (GameConfig)MemberwiseClone();

        /// <summary>Sets a field by name (case-insensitive). Throws on an unknown name or bad value.</summary>
        public void Set(string name, string value)
        {
            FieldInfo field = Array.Find(typeof(GameConfig).GetFields(BindingFlags.Public | BindingFlags.Instance),
                f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
            if (field == null) throw new ArgumentException($"Unknown config field '{name}'. Known: {string.Join(", ", FieldNames())}");
            object parsed = field.FieldType == typeof(bool)
                ? (object)(value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase))
                : Convert.ChangeType(value, field.FieldType, CultureInfo.InvariantCulture);
            field.SetValue(this, parsed);
        }

        public static IEnumerable<string> FieldNames()
        {
            foreach (FieldInfo f in typeof(GameConfig).GetFields(BindingFlags.Public | BindingFlags.Instance)) yield return f.Name;
        }

        public IEnumerable<KeyValuePair<string, string>> Describe()
        {
            foreach (FieldInfo f in typeof(GameConfig).GetFields(BindingFlags.Public | BindingFlags.Instance))
                yield return new KeyValuePair<string, string>(f.Name, Convert.ToString(f.GetValue(this), CultureInfo.InvariantCulture));
        }
    }
}
