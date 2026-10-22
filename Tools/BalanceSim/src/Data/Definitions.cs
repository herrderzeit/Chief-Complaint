using System;
using System.Collections.Generic;
using System.Linq;

namespace ChiefComplaint.Sim.Data
{
    // Enum values match the Unity enums one-for-one, because the assets store them as ints.

    public enum CardType { Staff, Diagnostic, Treatment, Utility, RedTape, Administration, Supply }

    public enum CardTag { None, Surgical, Pharmacy, Emergency, Training, Management, Policy, Lab, Rookie, Doctor, Nurse }

    public enum SymptomCategory { Trauma, Cardiopulmonary, Infectious, Internal }

    public enum EscalationType { WalkIn, Ambulance, TraumaFlight }

    public enum TriggerCondition { None, OnSpawn, OnDeath, OnDiagnosed, OnCured, OnTurnEnd }

    public enum TriggerEffect { None, GainAP, LoseAP, GainReputation, LoseReputation, DrawCard, DiscardCard, ChangeStability, AddCardToDiscard, AddCardToHand }

    // -------------------------------------------------------------------------
    // Card effects (mirror Assets/Scripts/Cards/Effects/*EffectSO.cs)
    // -------------------------------------------------------------------------

    public abstract class EffectDef
    {
        public abstract bool RequiresPatientTarget { get; }
        public virtual bool CanTargetPatient => RequiresPatientTarget;
    }

    public sealed class GainApEffect : EffectDef
    {
        public int Amount;
        public override bool RequiresPatientTarget => false;
    }

    public sealed class GainReputationEffect : EffectDef
    {
        public int Amount;
        public override bool RequiresPatientTarget => false;
    }

    public sealed class DrawCardsEffect : EffectDef
    {
        public int Amount = 1;
        public override bool RequiresPatientTarget => false;
    }

    public sealed class DiagnoseEffect : EffectDef
    {
        public int MaxTier = 3;
        public override bool RequiresPatientTarget => true;
    }

    public sealed class CureEffect : EffectDef
    {
        public int MaxTier = 3;
        public override bool RequiresPatientTarget => true;
    }

    public sealed class HealEffect : EffectDef
    {
        public int Amount = 1;
        public override bool RequiresPatientTarget => true;
    }

    public sealed class PauseEffect : EffectDef
    {
        public override bool RequiresPatientTarget => true;
    }

    /// <summary>
    /// TrashCardEffectSO: removes a card in hand from the run. The game lets the player drop it on any hand card;
    /// the simulator has no card targets, so it trashes Red Tape first, else the cheapest Tier 1 card.
    /// </summary>
    public sealed class TrashEffect : EffectDef
    {
        public override bool RequiresPatientTarget => false;
    }

    public sealed class CleanseRedTapeEffect : EffectDef
    {
        public int RepPerCard = 1;
        public override bool RequiresPatientTarget => false;
    }

    /// <summary>"Gain X Rep or Y AP" (EitherOrEffectSO). Always offers both options.</summary>
    public sealed class EitherOrEffect : EffectDef
    {
        public int Reputation = 1;
        public int Ap = 1;
        public override bool RequiresPatientTarget => false;
    }

    /// <summary>Applies a patient effect to every Ward patient (WardWideEffectSO).</summary>
    public sealed class WardWideEffect : EffectDef
    {
        public EffectDef PatientEffect;
        public override bool RequiresPatientTarget => false;
    }

    /// <summary>"Choose one" between two effects (ChoiceEffectSO).</summary>
    public sealed class ChoiceEffect : EffectDef
    {
        public string LabelA, LabelB;
        public EffectDef OptionA, OptionB;

        public override bool RequiresPatientTarget =>
            (OptionA == null || OptionA.RequiresPatientTarget) && (OptionB == null || OptionB.RequiresPatientTarget);

        public override bool CanTargetPatient =>
            (OptionA != null && OptionA.CanTargetPatient) || (OptionB != null && OptionB.CanTargetPatient);
    }

    /// <summary>An effect asset whose script the simulator does not know. Never applies.</summary>
    public sealed class UnknownEffect : EffectDef
    {
        public string ScriptName;
        public override bool RequiresPatientTarget => false;
    }

    // -------------------------------------------------------------------------
    // Cards, symptoms, patients
    // -------------------------------------------------------------------------

    public sealed class CardDef
    {
        public string Id;          // asset file name, e.g. Card_IVSaline
        public string Name;
        public CardType Type;
        public List<CardTag> Tags = new List<CardTag>();
        public bool IsPlayable = true;
        public float SuccessChance = 1f;
        public int ApCost;
        public int ReputationCost;   // all or nothing: needs Reputation above it; paid only if the card lands
        public int CardsToDraw;
        public int PowerTier = 1;
        public bool ExhaustsOnUse;
        public bool ExhaustsAtShiftEnd;
        public List<EffectDef> Effects = new List<EffectDef>();
        public CardDef Upgrade;
        public int UpgradeCost = 5;
        public int UpgradeTokenCost;              // ActionCardSO.upgradeTokenCost (0 = no tokens)
        public bool UpgradeTokenAnyColour;        // ActionCardSO.upgradeTokenAnyColour
        public SymptomCategory UpgradeTokenColour = SymptomCategory.Trauma;

        public bool HasTag(CardTag tag) => Tags.Contains(tag);

        /// <summary>Market price. Mirrors <c>ActionCardSO.CalculateDraftValue</c>.</summary>
        public int DraftValue
        {
            get
            {
                int cost = Type switch
                {
                    CardType.Staff => 4,
                    CardType.Utility => 3,
                    CardType.Diagnostic => 3,
                    CardType.Treatment => 3,
                    CardType.Administration => 3,
                    CardType.Supply => 2,
                    CardType.RedTape => 0,
                    _ => 2
                };
                cost += PowerTier * PowerTier;
                if (ApCost == 0) cost += 2;
                else if (ApCost >= 2) cost -= 1;
                if (CardsToDraw > 0) cost += CardsToDraw * 2;
                if (ExhaustsOnUse) cost -= 2;
                cost -= 2 * Math.Max(0, ReputationCost);
                if (SuccessChance < 1f) cost -= 2;
                return Type == CardType.RedTape ? 0 : Math.Max(2, cost);
            }
        }

        /// <summary>Mirrors <c>ActionCardSO.TotalReputationCost</c>: the card's cost plus Reputation its effects take.</summary>
        public int TotalReputationCost =>
            Math.Max(0, ReputationCost) + Effects.OfType<GainReputationEffect>().Sum(e => Math.Max(0, -e.Amount));

        public override string ToString() => Name;
    }

    public sealed class SymptomDef
    {
        public string Id;
        public SymptomCategory Category;
        public List<string> Tags = new List<string>();
        public string Tier1Name, Tier2Name, Tier3Name;

        public string NameAt(int tier) => tier switch { 2 => Tier2Name, 3 => Tier3Name, _ => Tier1Name };
        public override string ToString() => Tier1Name;
    }

    public sealed class PatientTriggerDef
    {
        public TriggerCondition Condition;
        public TriggerEffect Effect;
        public int Value;
        public CardDef CardReference;
    }

    public sealed class PatientDef
    {
        public string Id;
        public string Name;
        public string Complaint;
        public EscalationType Phase;
        public int TriageLevel = 3;
        public List<SymptomCategory> Categories = new List<SymptomCategory>();
        public List<string> Tags = new List<string>();
        public int StartingStability = 10;
        public int DecayPerTurn = 1;
        public int ReputationReward = 2;
        public bool ArrivesWithHandover;   // paramedic handover: primary symptom arrives diagnosed
        public List<PatientTriggerDef> Triggers = new List<PatientTriggerDef>();

        /// <summary>Mirrors <c>PatientCardSO.HasOwnDeathPenalty</c>: an OnDeath Reputation trigger replaces the flat death penalty.</summary>
        public bool HasOwnDeathPenalty => Triggers.Exists(t => t.Condition == TriggerCondition.OnDeath && t.Effect == TriggerEffect.LoseReputation && t.Value > 0);

        public override string ToString() => Name;
    }

    /// <summary>Everything a run needs, loaded from the Unity project (or built by hand in tests).</summary>
    public sealed class GameData
    {
        public List<CardDef> StartingDeck = new List<CardDef>();
        public List<CardDef> RedTapePool = new List<CardDef>();
        public List<CardDef> DraftPool = new List<CardDef>();   // the supply piles' cards, in pile order
        public List<SupplyPileDef> SupplyPiles = new List<SupplyPileDef>();
        public List<PatientDef> WalkInPool = new List<PatientDef>();
        public List<PatientDef> AmbulancePool = new List<PatientDef>();
        public List<PatientDef> TraumaFlightPool = new List<PatientDef>();
        public Dictionary<SymptomCategory, List<SymptomDef>> SymptomDecks = new Dictionary<SymptomCategory, List<SymptomDef>>();
        /// <summary>Every action card asset found, by asset name, for lookups such as upgrade chains.</summary>
        public Dictionary<string, CardDef> AllCards = new Dictionary<string, CardDef>();
        public GameConfig Config = new GameConfig();
        /// <summary>Loader notes: unknown scripts, missing references and so on.</summary>
        public List<string> Warnings = new List<string>();

        public List<PatientDef> PoolFor(EscalationType phase) => phase switch
        {
            EscalationType.Ambulance => AmbulancePool,
            EscalationType.TraumaFlight => TraumaFlightPool,
            _ => WalkInPool
        };
    }

    /// <summary>One supply-room pile (DraftManager.supplyPiles / SupplyPileConfig).</summary>
    public sealed class SupplyPileDef
    {
        public CardDef Card;
        public EscalationType UnlockAt = EscalationType.WalkIn;
        public int MaxStock = 3;
    }
}
