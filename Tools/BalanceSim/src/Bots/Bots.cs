using System;
using System.Collections.Generic;
using System.Linq;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Engine;

namespace ChiefComplaint.Sim.Bots
{
    /// <summary>A player. Picks one of the legal actions each step; the runner loops until the run ends.</summary>
    public interface IBot
    {
        string Name { get; }
        GameAction Choose(Game game, List<GameAction> legal);
    }

    /// <summary>Creates a fresh bot per run, so bots can keep per-run state and their own seeded RNG.</summary>
    public delegate IBot BotFactory(int seed);

    /// <summary>
    /// Baseline: plays random legal actions. Ends the shift with a 10% chance per step (otherwise it would end
    /// most shifts after one action), and buys random affordable items in the staff room.
    /// </summary>
    public sealed class RandomBot : IBot
    {
        private readonly Random rng;
        public RandomBot(int seed) { rng = new Random(seed); }
        public string Name => "random";

        public GameAction Choose(Game game, List<GameAction> legal)
        {
            if (game.Phase == Phase.Shift)
            {
                var others = legal.Where(a => a.Kind != ActionKind.EndShift).ToList();
                if (others.Count == 0 || rng.NextDouble() < 0.1) return GameAction.EndShift;
                return others[rng.Next(others.Count)];
            }
            return legal[rng.Next(legal.Count)];
        }
    }

    public sealed class GreedyOptions
    {
        /// <summary>Never complete a cure once this many patients are cured (tests the "stall on Walk-Ins" exploit). -1 = off.</summary>
        public int StallAtCures = -1;
        /// <summary>Play Red Tape that only costs Reputation (to thin the deck) when Reputation is comfortably high.</summary>
        public bool PlayReputationRedTape = true;
    }

    /// <summary>
    /// A competent heuristic player: scores every legal action and takes the best one until nothing scores
    /// above zero, then ends the shift. Staff room spending is delegated to a <see cref="IPurchasePolicy"/>.
    /// </summary>
    public sealed class GreedyBot : IBot
    {
        private readonly IPurchasePolicy purchases;
        private readonly GreedyOptions options;
        private readonly Random rng;
        private readonly string name;

        public GreedyBot(int seed, IPurchasePolicy purchases, GreedyOptions options = null, string name = null)
        {
            rng = new Random(seed);
            this.purchases = purchases;
            this.options = options ?? new GreedyOptions();
            this.name = name ?? "greedy-" + purchases.Name;
        }

        public string Name => name;

        public GameAction Choose(Game game, List<GameAction> legal)
        {
            if (game.Phase == Phase.StaffRoom) return purchases.Choose(game, legal, rng);

            GameAction best = null;
            double bestScore = 0;
            foreach (GameAction action in legal)
            {
                double score = Score(game, action);
                if (score > bestScore) { bestScore = score; best = action; }
            }
            if (best != null) return best;
            return game.Phase == Phase.TeamAssessment ? GameAction.SkipTeamAssess : GameAction.EndShift;
        }

        // ---------------------------------------------------------------------
        // Scoring
        // ---------------------------------------------------------------------

        private double Score(Game g, GameAction a)
        {
            switch (a.Kind)
            {
                case ActionKind.Admit: return ScoreAdmit(g, a.Patient);
                case ActionKind.Treat: return ScoreTreat(g, a);
                case ActionKind.PlayCard: return ScorePlay(g, a);
                case ActionKind.TeamAssess:
                    return 100 + (a.Patient.Location == Location.Ward ? 60 : 0) + Urgency(a.Patient);
                default: return 0;
            }
        }

        private double ScoreAdmit(Game g, Patient p)
        {
            bool feasible = Feasible(g, p);
            // Beds are a commitment (no bed hopping, no return once diagnosed): keep the last bed for a patient we can cure.
            if (!feasible && g.WardBedsFree <= 1 && g.ActivePatients.Any(o => o != p && o.Location == Location.Triage && Feasible(g, o)))
                return -1;
            if (options.StallAtCures >= 0 && g.PatientsCured >= options.StallAtCures && p.Def.Phase == EscalationType.WalkIn)
                return 300 + Urgency(p); // still admit: Ward halves decay
            return (feasible ? 800 : 200) + Urgency(p) + (p.Def.Phase == EscalationType.WalkIn ? 10 : 0);
        }

        private double ScoreTreat(Game g, GameAction a)
        {
            Patient p = a.Patient;
            bool final = p.Symptoms.Count == 1;
            if (final && options.StallAtCures >= 0 && g.PatientsCured + PendingDischarges(g) >= options.StallAtCures) return -1;
            double score = (final ? 700 : 600) + Urgency(p) - a.Cost * 25;
            if (!a.Page) score += 10; // keep the paging AP for something else when a card does the job
            return score;
        }

        private static int PendingDischarges(Game g) => g.ActivePatients.Count(p => p.IsAwaitingDischarge);

        private double ScorePlay(Game g, GameAction a)
        {
            CardDef card = a.Card;
            Patient target = a.Patient;
            double score = 0;
            bool anyValue = false;

            if (card.Effects.Count == 0)
            {
                // Red Tape AP sinks (Prior Authorization, Strike Action...): clear them with AP that would otherwise be wasted.
                return card.Type == CardType.RedTape ? 10 : 1;
            }

            foreach (EffectDef effect in card.Effects)
            {
                double v = EffectValue(g, effect, target, a.Option, card);
                if (double.IsNegativeInfinity(v)) return -1;
                score += v;
                anyValue = true;
            }
            if (!anyValue) return -1;

            score += card.CardsToDraw * 90;
            score -= card.ApCost * 20;

            // First Doctor + Nurse pair of the shift: a free Team Assessment.
            if (!g.SynergyUsedThisShift && g.ActivePatients.Any(p => !p.IsFullyDiagnosed))
            {
                if (card.HasTag(CardTag.Nurse) && g.BestDoctorTierThisShift > 0) score += 200;
                if (card.HasTag(CardTag.Doctor) && g.BestNurseTierThisShift > 0) score += 200;
            }
            return score;
        }

        private double EffectValue(Game g, EffectDef effect, Patient target, int option, CardDef card)
        {
            switch (effect)
            {
                case GainApEffect ap:
                    return ap.Amount > 0 ? 1000 + ap.Amount : 0;
                case DrawCardsEffect d:
                    return 90 * d.Amount;
                case GainReputationEffect r:
                    if (r.Amount > 0) return 940 + r.Amount;
                    // Paperwork that only costs Reputation: optional. Playing it removes it from the deck for good.
                    if (options.PlayReputationRedTape && g.Reputation + r.Amount >= 15) return 30;
                    return double.NegativeInfinity;
                case EitherOrEffect e:
                    return option == 1 ? 1000 + e.Ap : (g.Reputation < 8 ? 960 : 300);
                case CleanseRedTapeEffect _:
                {
                    int redTape = g.OwnedCards.Count(c => c.Type == CardType.RedTape);
                    return redTape == 0 ? double.NegativeInfinity : 150 + 60 * redTape;
                }
                case DiagnoseEffect d:
                {
                    if (target == null || !g.WouldSucceed(d, target)) return 0;
                    double v = 500 + Urgency(target) - d.MaxTier * 5;
                    if (target.DiagnosedCount + 1 == target.Symptoms.Count) v += 40; // completes the diagnosis
                    if (card.SuccessChance < 1f) v *= card.SuccessChance;
                    return v;
                }
                case HealEffect h:
                    return target == null ? 0 : StabiliseValue(target, h.Amount, false);
                case PauseEffect _:
                    return target == null ? 0 : StabiliseValue(target, 0, true);
                case ChoiceEffect c:
                {
                    EffectDef picked;
                    if (option == 0) picked = c.OptionA;
                    else if (option == 1) picked = c.OptionB;
                    else picked = g.WouldSucceed(c.OptionA, target) ? c.OptionA : c.OptionB;
                    return EffectValue(g, picked, target, -1, card);
                }
                case WardWideEffect w:
                {
                    double total = 0;
                    foreach (Patient p in g.ActivePatients.Where(p => p.HasWardBed))
                        if (g.CanApply(w.PatientEffect, p)) total += EffectValue(g, w.PatientEffect, p, -1, card);
                    return total;
                }
                default:
                    return 0;
            }
        }

        /// <summary>Value of healing and/or pausing a patient, combined with any heal/pause sibling effect on the same card.</summary>
        private static double StabiliseValue(Patient p, int heal, bool pause)
        {
            int gained = Math.Min(heal, p.MaxStability - p.Stability);
            int decay = p.PendingDecay;
            bool dyingNextShift = !p.IsAwaitingDischarge && p.Stability - decay <= 0;
            double v = 0;
            if (dyingNextShift && (pause || p.Stability + gained - decay > 0)) v += 750;
            if (pause) v += decay * 8;
            v += gained * 8;
            // Discharging at >= half Stability pays +1 Rep and +1 Funding.
            if (p.IsAwaitingDischarge && p.Stability < p.MaxStability / 2f && p.Stability + gained >= p.MaxStability / 2f) v += 60;
            return v;
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------

        /// <summary>0..40: higher when the patient will die within a few shifts where they are now.</summary>
        private static double Urgency(Patient p)
        {
            int decay = p.HasWardBed ? Math.Max(1, (p.DecayRate + 1) / 2) : p.DecayRate;
            int turns = (int)Math.Ceiling(p.Stability / (double)Math.Max(1, decay));
            return Math.Max(0, 40 - 10 * turns);
        }

        /// <summary>Can the cards we own ever reveal every hidden symptom of this patient?</summary>
        private static bool Feasible(Game g, Patient p)
        {
            int maxDiag = DeckDiagnoseTier(g);
            for (int i = p.DiagnosedCount; i < p.Symptoms.Count; i++)
                if (p.Symptoms[i].Tier > maxDiag) return false;
            return true;
        }

        public static int DeckDiagnoseTier(Game g)
        {
            int best = 0, doctor = 0, nurse = 0;
            foreach (CardDef c in g.OwnedCards)
            {
                foreach (EffectDef e in c.Effects)
                    if (e is DiagnoseEffect d) best = Math.Max(best, d.MaxTier);
                if (c.HasTag(CardTag.Doctor)) doctor = Math.Max(doctor, c.PowerTier);
                if (c.HasTag(CardTag.Nurse)) nurse = Math.Max(nurse, c.PowerTier);
            }
            return Math.Max(best, Math.Min(doctor, nurse));
        }
    }
}
