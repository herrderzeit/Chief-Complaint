using System.Collections.Generic;
using System.Linq;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Engine;

namespace ChiefComplaint.Sim.Tests
{
    /// <summary>Small hand-built content so each rule can be tested in isolation.</summary>
    public static class Fixtures
    {
        public static CardDef Diagnose(int tier, int ap = 1) => new CardDef
        {
            Id = "diag" + tier, Name = "Diagnose T" + tier, Type = CardType.Diagnostic, ApCost = ap, PowerTier = tier,
            Effects = { new DiagnoseEffect { MaxTier = tier } }
        };

        public static CardDef Cure(int tier, int ap = 1) => new CardDef
        {
            Id = "cure" + tier, Name = "Cure T" + tier, Type = CardType.Treatment, ApCost = ap, PowerTier = tier,
            Effects = { new CureEffect { MaxTier = tier } }
        };

        public static CardDef Doctor(int tier = 1) => new CardDef
        {
            Id = "doc" + tier, Name = "Doctor T" + tier, Type = CardType.Staff, PowerTier = tier, Tags = { CardTag.Doctor },
            Effects = { new GainApEffect { Amount = 1 } }
        };

        public static CardDef Nurse(int tier = 1) => new CardDef
        {
            Id = "nurse" + tier, Name = "Nurse T" + tier, Type = CardType.Staff, PowerTier = tier, Tags = { CardTag.Nurse },
            Effects = { new ChoiceEffect { OptionA = new HealEffect { Amount = 2 }, OptionB = new DrawCardsEffect { Amount = 1 } } }
        };

        public static CardDef Pause() => new CardDef
        {
            Id = "iv", Name = "IV", Type = CardType.Treatment, ApCost = 1,
            Effects = { new PauseEffect(), new HealEffect { Amount = 1 } }
        };

        public static CardDef RedTape(string name = "Paperwork") => new CardDef { Id = name, Name = name, Type = CardType.RedTape, IsPlayable = false, ExhaustsAtShiftEnd = true };

        public static SymptomDef Symptom(SymptomCategory category, string name = null) =>
            new SymptomDef { Id = name ?? category.ToString(), Category = category, Tier1Name = name ?? category.ToString() };

        public static PatientDef Patient(EscalationType phase = EscalationType.WalkIn, int triage = 5, int stability = 10, int decay = 1,
            params SymptomCategory[] categories) => new PatientDef
        {
            Id = "p", Name = "Patient " + phase, Phase = phase, TriageLevel = triage, StartingStability = stability, DecayPerTurn = decay,
            ReputationReward = 1, Categories = categories.Length > 0 ? categories.ToList() : new List<SymptomCategory> { SymptomCategory.Trauma }
        };

        /// <summary>
        /// Content with one symptom per category, a 5-card hand that is exactly <paramref name="deck"/> (so it is
        /// drawn in full), and <paramref name="patients"/> as the Walk-In pool.
        /// </summary>
        public static GameData Content(IEnumerable<CardDef> deck, params PatientDef[] patients)
        {
            var data = new GameData();
            data.StartingDeck = deck.ToList();
            data.WalkInPool = patients.ToList();
            foreach (SymptomCategory c in new[] { SymptomCategory.Trauma, SymptomCategory.Cardiopulmonary, SymptomCategory.Infectious, SymptomCategory.Internal })
                data.SymptomDecks[c] = new List<SymptomDef> { Symptom(c), Symptom(c, c + " B"), Symptom(c, c + " C") };
            data.RedTapePool = new List<CardDef> { RedTape() };
            data.Config.InitialPatientCount = patients.Length;
            data.Config.HandSize = 5;
            return data;
        }

        public static Game Start(GameData data, int seed = 1)
        {
            var game = new Game(data, seed);
            game.Start();
            return game;
        }

        public static GameAction Find(Game game, ActionKind kind, System.Func<GameAction, bool> where = null) =>
            game.LegalActions().FirstOrDefault(a => a.Kind == kind && (where == null || where(a)));
    }
}
