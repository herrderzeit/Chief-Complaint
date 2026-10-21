using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ChiefComplaint.Sim.Data
{
    /// <summary>
    /// Balance prototypes without touching the Unity assets: a JSON file (passed with <c>--cards</c>) that changes
    /// card numbers in memory after the project is loaded. Example:
    /// <code>
    /// {
    ///   "upgradePricePercent": 60,
    ///   "cards": { "Double Shift": { "reputationCost": 1 }, "Ward Round": { "apCost": 1 } }
    /// }
    /// </code>
    /// <c>upgradePricePercent</c> sets every upgrade's Funding cost to that percentage of the upgraded card's market
    /// price (rounded, at least 1); a card's own <c>upgradeCost</c> override wins over it.
    /// </summary>
    public static class CardOverrides
    {
        /// <summary>Applies the overrides in <paramref name="path"/> and returns one line per change made.</summary>
        public static List<string> Apply(GameData data, string path)
        {
            var log = new List<string>();
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement root = doc.RootElement;
            var explicitUpgradeCost = new HashSet<CardDef>();

            if (root.TryGetProperty("cards", out JsonElement cards))
            {
                foreach (JsonProperty entry in cards.EnumerateObject())
                {
                    CardDef card = Find(data, entry.Name);
                    foreach (JsonProperty field in entry.Value.EnumerateObject())
                    {
                        string before = Describe(card, field.Name);
                        Set(card, field.Name, field.Value);
                        if (field.Name == "upgradeCost") explicitUpgradeCost.Add(card);
                        log.Add($"{card.Name}: {field.Name} {before} -> {Describe(card, field.Name)}");
                    }
                }
            }

            if (root.TryGetProperty("upgradePricePercent", out JsonElement pct))
            {
                double percent = pct.GetDouble();
                foreach (CardDef card in data.AllCards.Values.Distinct())
                {
                    if (card.Upgrade == null || explicitUpgradeCost.Contains(card)) continue;
                    int cost = Math.Max(1, (int)Math.Round(card.Upgrade.DraftValue * percent / 100.0, MidpointRounding.AwayFromZero));
                    if (cost == card.UpgradeCost) continue;
                    log.Add($"{card.Name} -> {card.Upgrade.Name}: upgradeCost {card.UpgradeCost} -> {cost} ({percent}% of {card.Upgrade.DraftValue}F)");
                    card.UpgradeCost = cost;
                }
            }
            return log;
        }

        private static CardDef Find(GameData data, string name) =>
            data.AllCards.Values.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"--cards: no card named '{name}'.");

        private static void Set(CardDef card, string field, JsonElement value)
        {
            switch (field)
            {
                case "apCost": card.ApCost = value.GetInt32(); break;
                case "reputationCost": card.ReputationCost = value.GetInt32(); break;
                case "cardsToDraw": card.CardsToDraw = value.GetInt32(); break;
                case "powerTier": card.PowerTier = value.GetInt32(); break;
                case "successChance": card.SuccessChance = value.GetSingle(); break;
                case "exhaustsOnUse": card.ExhaustsOnUse = value.GetBoolean(); break;
                case "upgradeCost": card.UpgradeCost = value.GetInt32(); break;
                case "upgradeTokenCost": card.UpgradeTokenCost = value.GetInt32(); break;
                case "cleanseRepPerCard":
                    foreach (CleanseRedTapeEffect e in card.Effects.OfType<CleanseRedTapeEffect>()) e.RepPerCard = value.GetInt32();
                    break;
                default:
                    throw new ArgumentException($"--cards: '{field}' can't be overridden (on {card.Name}).");
            }
        }

        private static string Describe(CardDef card, string field) => field switch
        {
            "apCost" => card.ApCost.ToString(),
            "reputationCost" => card.ReputationCost.ToString(),
            "cardsToDraw" => card.CardsToDraw.ToString(),
            "powerTier" => card.PowerTier.ToString(),
            "successChance" => card.SuccessChance.ToString("0.##"),
            "exhaustsOnUse" => card.ExhaustsOnUse.ToString(),
            "upgradeCost" => card.UpgradeCost.ToString(),
            "upgradeTokenCost" => card.UpgradeTokenCost.ToString(),
            "cleanseRepPerCard" => string.Join(",", card.Effects.OfType<CleanseRedTapeEffect>().Select(e => e.RepPerCard)),
            _ => "?"
        };
    }
}
