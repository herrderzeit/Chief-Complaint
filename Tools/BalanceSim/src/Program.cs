using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using ChiefComplaint.Sim.Bots;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Stats;

namespace ChiefComplaint.Sim
{
    public static class Program
    {
        private const string Usage = @"Chief Complaint headless balance simulator

Usage:
  dotnet run --project Tools/BalanceSim -c Release -- <command> [options]

Commands:
  report   Play every bot (and the purchase-focus experiment) and write report.md, curves.csv, summary.json
  run      Play one bot and print its summary          (--bot <name>)
  trace    Print a play-by-play of a single run         (--bot <name> --seed <n>)
  data     Print the cards, patients and config loaded from the Unity project

Options:
  --runs <n>         Runs per bot (default 2000; trace ignores it)
  --focus-runs <n>   Runs per purchase group in the focus experiment (default: --runs / 2, 0 to skip)
  --seed <n>         Base seed (default 1). Run i uses seed + i, so results are reproducible
  --bot <name>       random | nobuy | randombuy | smart | stall   (default smart)
  --set k=v          Override a config value, e.g. --set walkInFundingReward=3 (repeatable)
  --cards <file>     Card overrides from a JSON file, for balance prototypes (see Data/CardOverrides.cs)
  --project <path>   Unity project root (default: found by walking up from the current folder)
  --out <dir>        Output folder for report (default Tools/BalanceSim/out)";

        private static readonly Dictionary<string, string> BotDescriptions = new Dictionary<string, string>
        {
            ["random"] = "random legal actions, random purchases (a floor, not a player)",
            ["nobuy"] = "greedy heuristic player that never spends Funding",
            ["randombuy"] = "greedy player, buys random affordable items",
            ["smart"] = "greedy player with a sensible shopping list (AP engine, diagnosis tier, treatment tier when needed, Rep when low)",
            ["stall"] = "smart player that stops completing cures after 2, to keep the run on Walk-Ins (escalation exploit test)"
        };

        public static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
            {
                Console.WriteLine(Usage);
                return 0;
            }
            try
            {
                return Execute(args);
            }
            catch (ArgumentException e)
            {
                Console.Error.WriteLine(e.Message);
                return 2;
            }
        }

        private static int Execute(string[] args)
        {
            string command = args[0];
            var opts = ParseOptions(args.Skip(1).ToArray(), out List<string> sets);
            string project = opts.TryGetValue("project", out string p) ? p : ProjectLoader.FindProjectRoot(Directory.GetCurrentDirectory());
            int runs = opts.TryGetValue("runs", out string r) ? int.Parse(r) : 2000;
            int seed = opts.TryGetValue("seed", out string s) ? int.Parse(s) : 1;
            string botName = opts.TryGetValue("bot", out string b) ? b : "smart";

            GameData data = new ProjectLoader(project).Load();
            if (opts.TryGetValue("cards", out string cardsFile))
                foreach (string change in CardOverrides.Apply(data, cardsFile)) Console.Error.WriteLine("override " + change);
            GameConfig config = data.Config.Clone();
            foreach (string kv in sets)
            {
                int eq = kv.IndexOf('=');
                if (eq <= 0) throw new ArgumentException($"--set expects name=value, got '{kv}'");
                config.Set(kv.Substring(0, eq), kv.Substring(eq + 1));
            }

            switch (command)
            {
                case "data":
                    PrintData(data, config);
                    return 0;

                case "trace":
                {
                    Runner.RunOne(data, config, Bot(botName), seed, new TracePrinter(Console.Out));
                    return 0;
                }

                case "run":
                {
                    var sw = Stopwatch.StartNew();
                    RunRecord[] records = Runner.RunBatch(data, config, Bot(botName), runs, seed);
                    BatchSummary summary = BatchSummary.From(botName, records, config.ShiftsToWin);
                    Console.WriteLine(ReportWriter.Markdown(new[] { summary }, null, data, config, seed, Describe(botName)));
                    Console.Error.WriteLine($"{runs} runs in {sw.Elapsed.TotalSeconds:0.0}s");
                    return 0;
                }

                case "report":
                {
                    string outDir = opts.TryGetValue("out", out string o) ? o : Path.Combine(project, "Tools", "BalanceSim", "out");
                    int focusRuns = opts.TryGetValue("focus-runs", out string fr) ? int.Parse(fr) : runs / 2;
                    Report(data, config, runs, focusRuns, seed, outDir);
                    return 0;
                }

                default:
                    throw new ArgumentException($"Unknown command '{command}'.\n\n{Usage}");
            }
        }

        private static void Report(GameData data, GameConfig config, int runs, int focusRuns, int seed, string outDir)
        {
            var sw = Stopwatch.StartNew();
            var batches = new List<BatchSummary>();
            foreach (string name in BotDescriptions.Keys)
            {
                RunRecord[] records = Runner.RunBatch(data, config, Bot(name), runs, seed);
                batches.Add(BatchSummary.From(name, records, config.ShiftsToWin));
                Console.Error.WriteLine($"{name}: win {batches.Last().WinRate:P1} ({sw.Elapsed.TotalSeconds:0}s)");
            }

            var focus = new List<FocusResult>();
            if (focusRuns > 0)
            {
                var groups = new List<KeyValuePair<string, List<string>>> { new KeyValuePair<string, List<string>>("nobuy (baseline)", new List<string>()) };
                groups.AddRange(PurchaseItems.FocusGroups(data));
                foreach (var g in groups)
                {
                    var group = g;
                    BotFactory factory = sd => new GreedyBot(sd, new FocusPurchases(group.Key, group.Value), name: "focus");
                    RunRecord[] records = Runner.RunBatch(data, config, factory, focusRuns, seed);
                    focus.Add(new FocusResult { Group = group.Key, Summary = BatchSummary.From(group.Key, records, config.ShiftsToWin) });
                    Console.Error.WriteLine($"focus {group.Key}: win {focus.Last().Summary.WinRate:P1}");
                }
            }

            string descriptions = "Bots: " + string.Join("; ", BotDescriptions.Select(kv => $"`{kv.Key}` = {kv.Value}")) + ".";
            Directory.CreateDirectory(outDir);
            File.WriteAllText(Path.Combine(outDir, "report.md"), ReportWriter.Markdown(batches, focus, data, config, seed, descriptions));
            File.WriteAllText(Path.Combine(outDir, "curves.csv"), ReportWriter.CurvesCsv(batches));
            var json = new { config = config.Describe().ToDictionary(kv => kv.Key, kv => kv.Value), batches, focus };
            File.WriteAllText(Path.Combine(outDir, "summary.json"),
                JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
            Console.Error.WriteLine($"Wrote {outDir}/report.md, curves.csv, summary.json in {sw.Elapsed.TotalSeconds:0.0}s");
        }

        private static string Describe(string botName) =>
            BotDescriptions.TryGetValue(botName, out string d) ? $"Bot `{botName}`: {d}." : "";

        public static BotFactory Bot(string name) => name switch
        {
            "random" => sd => new RandomBot(sd),
            "nobuy" => sd => new GreedyBot(sd, new NoPurchases(), name: "nobuy"),
            "randombuy" => sd => new GreedyBot(sd, new RandomPurchases(), name: "randombuy"),
            "smart" => sd => new GreedyBot(sd, new HeuristicPurchases(), name: "smart"),
            "stall" => sd => new GreedyBot(sd, new HeuristicPurchases(), new GreedyOptions { StallAtCures = 2 }, name: "stall"),
            _ => throw new ArgumentException($"Unknown bot '{name}'. Known: {string.Join(", ", BotDescriptions.Keys)}")
        };

        private static Dictionary<string, string> ParseOptions(string[] args, out List<string> sets)
        {
            var opts = new Dictionary<string, string>();
            sets = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException($"Unexpected argument '{args[i]}'.");
                string key = args[i].Substring(2);
                if (i + 1 >= args.Length) throw new ArgumentException($"--{key} needs a value.");
                string value = args[++i];
                if (key == "set") sets.Add(value);
                else opts[key] = value;
            }
            return opts;
        }

        private static void PrintData(GameData data, GameConfig config)
        {
            Console.WriteLine("Config: " + string.Join(", ", config.Describe().Select(kv => $"{kv.Key}={kv.Value}")));
            Console.WriteLine();
            void Cards(string title, IEnumerable<CardDef> cards)
            {
                Console.WriteLine(title);
                foreach (CardDef c in cards)
                    Console.WriteLine($"  {c.Name,-26} {c.Type,-14} T{c.PowerTier} {c.ApCost}AP draw{c.CardsToDraw} p{c.SuccessChance:0.##} " +
                                      $"{(c.IsPlayable ? "" : "UNPLAYABLE ")}{(c.ExhaustsOnUse ? "exhaust " : "")}{(c.ExhaustsAtShiftEnd ? "exhaust@end " : "")}" +
                                      $"tags[{string.Join(",", c.Tags)}] effects[{string.Join(", ", c.Effects.Select(Describe))}] " +
                                      $"{(c.Upgrade != null ? $"-> {c.Upgrade.Name} ({c.UpgradeCost}F)" : "")} market {c.DraftValue}F");
                Console.WriteLine();
            }
            Cards("Starting deck", data.StartingDeck);
            Cards("Draft pool", data.DraftPool);
            Cards("Red Tape", data.RedTapePool);
            Cards("All cards", data.AllCards.Values.OrderBy(c => c.Id));
            foreach (var pool in new[] { data.WalkInPool, data.AmbulancePool, data.TraumaFlightPool })
                foreach (PatientDef pd in pool)
                    Console.WriteLine($"  {pd.Phase,-12} {pd.Name,-22} triage {pd.TriageLevel} stab {pd.StartingStability,2} decay {pd.DecayPerTurn} rep {pd.ReputationReward} " +
                                      $"[{string.Join(",", pd.Categories)}] triggers[{string.Join(", ", pd.Triggers.Select(t => $"{t.Condition}:{t.Effect} {t.Value}"))}]");
            Console.WriteLine();
            Console.WriteLine("Symptom decks: " + string.Join(", ", data.SymptomDecks.Select(kv => $"{kv.Key} {kv.Value.Count}")));
            foreach (string w in data.Warnings) Console.WriteLine("WARNING: " + w);
        }

        private static string Describe(EffectDef e) => e switch
        {
            GainApEffect g => $"+{g.Amount}AP",
            GainReputationEffect r => $"{(r.Amount >= 0 ? "+" : "")}{r.Amount}Rep",
            DrawCardsEffect d => $"draw{d.Amount}",
            DiagnoseEffect d => $"diagnose<=T{d.MaxTier}",
            CureEffect c => $"cure<=T{c.MaxTier}",
            HealEffect h => $"heal{h.Amount}",
            PauseEffect _ => "pause",
            CleanseRedTapeEffect c => $"cleanse(+{c.RepPerCard}/card)",
            EitherOrEffect eo => $"{eo.Reputation}Rep|{eo.Ap}AP",
            WardWideEffect w => $"ward({(w.PatientEffect == null ? "?" : Describe(w.PatientEffect))})",
            ChoiceEffect ch => $"choose({(ch.OptionA == null ? "?" : Describe(ch.OptionA))} | {(ch.OptionB == null ? "?" : Describe(ch.OptionB))})",
            UnknownEffect u => $"unknown:{u.ScriptName}",
            _ => e.GetType().Name
        };
    }
}
