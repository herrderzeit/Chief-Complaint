using System;
using System.Collections.Generic;
using System.Linq;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Engine;

namespace ChiefComplaint.Sim.Bots
{
    /// <summary>How a bot spends Funding in the draft. Returns FinishDraft to stop buying.</summary>
    public interface IPurchasePolicy
    {
        string Name { get; }
        GameAction Choose(Game game, List<GameAction> legal, Random rng);
    }

    public static class PurchaseItems
    {
        /// <summary>Stable key for a draft action, e.g. "buy:Stat Labs" or "upgrade:Medical Student".</summary>
        public static string Key(GameAction a) => a.Kind switch
        {
            ActionKind.Buy => "buy:" + a.Card.Name,
            ActionKind.Upgrade => "upgrade:" + a.Card.Name,
            _ => ""
        };

        /// <summary>
        /// Purchase groups for focus experiments: each market card on its own, and each upgrade chain
        /// (e.g. Medical Student -> Licensed Doctor -> Chief of Medicine) as one group.
        /// </summary>
        public static List<KeyValuePair<string, List<string>>> FocusGroups(GameData data)
        {
            var groups = new List<KeyValuePair<string, List<string>>>();
            foreach (CardDef c in data.DraftPool.Distinct())
                groups.Add(new KeyValuePair<string, List<string>>("buy " + c.Name, new List<string> { "buy:" + c.Name }));
            foreach (CardDef root in data.StartingDeck.Concat(data.DraftPool).Distinct())
            {
                if (root.Upgrade == null) continue;
                var chain = new List<string>();
                var names = new List<string> { root.Name };
                for (CardDef c = root; c != null && c.Upgrade != null && chain.Count < 5; c = c.Upgrade)
                {
                    chain.Add("upgrade:" + c.Name);
                    names.Add(c.Upgrade.Name);
                }
                groups.Add(new KeyValuePair<string, List<string>>("upgrade " + string.Join(" > ", names), chain));
            }
            return groups;
        }
    }

    /// <summary>Never spends anything.</summary>
    public sealed class NoPurchases : IPurchasePolicy
    {
        public string Name => "nobuy";
        public GameAction Choose(Game game, List<GameAction> legal, Random rng) => GameAction.FinishDraft;
    }

    /// <summary>Buys a random affordable item, with a 1-in-3 chance each step of stopping and saving the rest.</summary>
    public sealed class RandomPurchases : IPurchasePolicy
    {
        public string Name => "randombuy";
        public GameAction Choose(Game game, List<GameAction> legal, Random rng)
        {
            var buys = legal.Where(a => a.Kind != ActionKind.FinishDraft).ToList();
            if (buys.Count == 0 || rng.NextDouble() < 1 / 3.0) return GameAction.FinishDraft;
            return buys[rng.Next(buys.Count)];
        }
    }

    /// <summary>
    /// Buys only the items of one focus group (one market card, or every rung of one upgrade chain) whenever
    /// affordable, and nothing else. Comparing focus groups measures how strong each purchase is on its own.
    /// </summary>
    public sealed class FocusPurchases : IPurchasePolicy
    {
        private readonly HashSet<string> keys;
        public FocusPurchases(string label, IEnumerable<string> keys)
        {
            Name = "focus[" + label + "]";
            this.keys = new HashSet<string>(keys);
        }
        public string Name { get; }

        public GameAction Choose(Game game, List<GameAction> legal, Random rng) =>
            legal.FirstOrDefault(a => keys.Contains(PurchaseItems.Key(a))) ?? GameAction.FinishDraft;
    }

    /// <summary>
    /// A sensible player's shopping list: AP engine and diagnosis tier first, treatment tier when the patient
    /// pool needs it, Reputation when low. Each item gets a priority; it buys the best affordable one until
    /// nothing is worth buying.
    /// </summary>
    public sealed class HeuristicPurchases : IPurchasePolicy
    {
        public string Name => "smart";

        public GameAction Choose(Game game, List<GameAction> legal, Random rng)
        {
            GameAction best = null;
            double bestScore = 0;
            foreach (GameAction a in legal)
            {
                if (a.Kind == ActionKind.FinishDraft) continue;
                double s = Priority(game, a);
                if (s > bestScore) { bestScore = s; best = a; }
            }
            return best ?? GameAction.FinishDraft;
        }

        private static double Priority(Game g, GameAction a)
        {
            CardDef c = a.Card;
            List<CardDef> owned = g.OwnedCards.ToList();
            int diagTier = owned.SelectMany(x => x.Effects).OfType<DiagnoseEffect>().Select(d => d.MaxTier).DefaultIfEmpty(0).Max();
            int cureTier = owned.SelectMany(x => x.Effects).OfType<CureEffect>().Select(d => d.MaxTier).DefaultIfEmpty(0).Max();
            bool harder = g.AmbulancesUnlocked || g.PatientsCured >= 2;
            bool hardest = g.TraumaFlightsUnlocked || g.PatientsCured >= 6;

            if (a.Kind == ActionKind.Upgrade)
            {
                CardDef up = c.Upgrade;
                if (up.Effects.OfType<GainApEffect>().Any()) return up.PowerTier == 2 ? 90 : 60;
                if (up.Effects.OfType<DiagnoseEffect>().Any(d => d.MaxTier > diagTier)) return harder ? 95 : 70;
                if (up.Effects.OfType<DiagnoseEffect>().Any()) return 25;
                if (up.Effects.OfType<CureEffect>().Any(cu => cu.MaxTier > cureTier))
                    return (up.PowerTier == 2 && harder) || (up.PowerTier == 3 && hardest) ? 65 : 5;
                if (up.Effects.OfType<GainReputationEffect>().Any()) return g.Reputation < 10 ? 70 : 40;
                if (up.HasTag(CardTag.Nurse)) return 45;
                if (up.Effects.Any(e => e is PauseEffect || e is HealEffect)) return 35;
                return 10;
            }

            // Market
            if (c.Effects.OfType<GainReputationEffect>().Any(r => r.Amount > 0)) return g.Reputation < 10 ? 80 : 50;
            if (c.Effects.OfType<DiagnoseEffect>().Any()) return harder ? 60 : 40;
            if (c.Effects.OfType<CleanseRedTapeEffect>().Any())
            {
                int redTape = owned.Count(x => x.Type == CardType.RedTape);
                return redTape >= 2 ? 30 + 10 * redTape : 0;
            }
            if (c.Effects.Any(e => e is PauseEffect || e is HealEffect)) return hardest ? 45 : 30;
            return 15;
        }
    }
}
