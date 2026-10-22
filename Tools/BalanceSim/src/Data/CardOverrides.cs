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
    /// Patients can be changed one by one (<c>"patients": { "Arthur Miller": { "arrivesWithHandover": false } }</c>)
    /// or a whole pool at once (<c>"pools": { "Ambulance": { "decayPerTurn": 3 } }</c>, pools Walk-In, Ambulance,
    /// Trauma Flight); single-patient entries are applied after pools.
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

            if (root.TryGetProperty("pools", out JsonElement pools))
            {
                foreach (JsonProperty entry in pools.EnumerateObject())
                {
                    List<PatientDef> pool = entry.Name.Replace(" ", "").Replace("-", "").ToLowerInvariant() switch
                    {
                        "walkin" => data.WalkInPool,
                        "ambulance" => data.AmbulancePool,
                        "traumaflight" => data.TraumaFlightPool,
                        _ => throw new ArgumentException($"--cards: unknown pool '{entry.Name}'.")
                    };
                    foreach (PatientDef patient in pool.Distinct())
                        foreach (JsonProperty field in entry.Value.EnumerateObject())
                            SetPatient(patient, field.Name, field.Value);
                    log.Add($"pool {entry.Name}: {entry.Value.GetRawText()}");
                }
            }

            if (root.TryGetProperty("patients", out JsonElement patients))
            {
                foreach (JsonProperty entry in patients.EnumerateObject())
                {
                    PatientDef patient = AllPatients(data).FirstOrDefault(p => string.Equals(p.Name, entry.Name, StringComparison.OrdinalIgnoreCase))
                        ?? throw new ArgumentException($"--cards: no patient named '{entry.Name}'.");
                    foreach (JsonProperty field in entry.Value.EnumerateObject())
                        SetPatient(patient, field.Name, field.Value);
                    log.Add($"patient {patient.Name}: {entry.Value.GetRawText()}");
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

        private static IEnumerable<PatientDef> AllPatients(GameData data) =>
            data.WalkInPool.Concat(data.AmbulancePool).Concat(data.TraumaFlightPool).Distinct();

        private static void SetPatient(PatientDef patient, string field, JsonElement value)
        {
            switch (field)
            {
                case "arrivesWithHandover": patient.ArrivesWithHandover = value.GetBoolean(); break;
                case "startingStability": patient.StartingStability = value.GetInt32(); break;
                case "decayPerTurn": patient.DecayPerTurn = value.GetInt32(); break;
                case "reputationReward": patient.ReputationReward = value.GetInt32(); break;
                case "triageLevel": patient.TriageLevel = value.GetInt32(); break;
                default: throw new ArgumentException($"--cards: patient field '{field}' can't be overridden (on {patient.Name}).");
            }
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
                case "upgradeTokenAnyColour": card.UpgradeTokenAnyColour = value.GetBoolean(); break;
                case "trashRedTapeOnly": foreach (TrashEffect e in card.Effects.OfType<TrashEffect>()) e.RedTapeOnly = value.GetBoolean(); break;
                case "exhaustsAtShiftEnd": card.ExhaustsAtShiftEnd = value.GetBoolean(); break;
                case "addGainAp": card.Effects.Add(new GainApEffect { Amount = value.GetInt32() }); break;
                case "healAmount":
                    foreach (HealEffect e in card.Effects.OfType<HealEffect>()) e.Amount = value.GetInt32();
                    break;
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
            "upgradeTokenAnyColour" => card.UpgradeTokenAnyColour.ToString(),
            "trashRedTapeOnly" => string.Join(",", card.Effects.OfType<TrashEffect>().Select(e => e.RedTapeOnly)),
            "exhaustsAtShiftEnd" => card.ExhaustsAtShiftEnd.ToString(),
            "addGainAp" => string.Join(",", card.Effects.OfType<GainApEffect>().Select(e => e.Amount)),
            "healAmount" => string.Join(",", card.Effects.OfType<HealEffect>().Select(e => e.Amount)),
            "cleanseRepPerCard" => string.Join(",", card.Effects.OfType<CleanseRedTapeEffect>().Select(e => e.RepPerCard)),
            _ => "?"
        };
    }
}
