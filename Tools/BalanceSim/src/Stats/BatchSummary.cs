using System;
using System.Collections.Generic;
using System.Linq;

namespace ChiefComplaint.Sim.Stats
{
    public sealed class Percentiles
    {
        public double Mean, P10, P50, P90;
        public int N;

        public static Percentiles Of(IEnumerable<int> values)
        {
            int[] v = values.OrderBy(x => x).ToArray();
            if (v.Length == 0) return new Percentiles();
            return new Percentiles { N = v.Length, Mean = v.Average(), P10 = At(v, 0.1), P50 = At(v, 0.5), P90 = At(v, 0.9) };
        }

        private static double At(int[] sorted, double q) => sorted[Math.Min(sorted.Length - 1, (int)Math.Floor(q * sorted.Length))];
    }

    public sealed class ShiftCurvePoint
    {
        public int Shift;
        /// <summary>Share of runs that finished this shift (Reputation still above 0).</summary>
        public double Alive;
        public Percentiles Reputation, Funding, Cured, Deaths, DeckSize;
        public double ApLeftOver;
    }

    public sealed class PoolStats
    {
        public string Pool;
        public double ArrivalsPerRun, CuresPerRun, DeathsPerRun;
        /// <summary>Of the patients that arrived: share cured, share died, share still on the board when the run ended.</summary>
        public double CureRate, DeathRate, UnresolvedRate;
        public double AvgShiftsToCure;
    }

    public sealed class PurchaseStats
    {
        public string Item;
        public double PerRun;
        public double ShareOfRunsBuying;
        public double WinRateWhenBought, WinRateWhenNot;
        public double AvgFirstBuyIndex;
    }

    /// <summary>Aggregate of a batch of runs played by one bot under one config.</summary>
    public sealed class BatchSummary
    {
        public string Label;
        public int Runs;
        public double WinRate, WinRateCi95;
        public double AvgLastShift;
        public Dictionary<int, int> LossesByShift = new Dictionary<int, int>();
        public Dictionary<string, int> LossCauses = new Dictionary<string, int>();
        public Percentiles FinalReputationOfWinners, FinalFundingOfWinners;
        public List<ShiftCurvePoint> Curve = new List<ShiftCurvePoint>();
        public List<PoolStats> Pools = new List<PoolStats>();
        public Dictionary<string, double> ReputationBySourcePerRun = new Dictionary<string, double>();
        public double FundingEarnedPerRun, FundingSpentPerRun;
        public List<PurchaseStats> Purchases = new List<PurchaseStats>();
        public List<KeyValuePair<string, double>> CardsPlayedPerRun = new List<KeyValuePair<string, double>>();
        public double AmbulanceReachedShare, TraumaReachedShare;
        public double AvgAmbulanceUnlockShift, AvgTraumaUnlockShift;
        public double DeathsInTriagePerRun, DeathsInWardPerRun;
        public double PagesPerRun, CardTreatmentsPerRun, ApOnPagesPerRun;
        public double TeamAssessmentsPerRun, SpawnsSkippedPerRun;

        public static BatchSummary From(string label, IReadOnlyList<RunRecord> runs, int shiftsToWin)
        {
            var s = new BatchSummary { Label = label, Runs = runs.Count };
            if (runs.Count == 0) return s;
            int n = runs.Count;

            s.WinRate = runs.Count(r => r.Won) / (double)n;
            s.WinRateCi95 = 1.96 * Math.Sqrt(s.WinRate * (1 - s.WinRate) / n);
            s.AvgLastShift = runs.Average(r => r.LastShift);
            foreach (RunRecord r in runs.Where(r => !r.Won))
            {
                s.LossesByShift.TryGetValue(r.LastShift, out int c);
                s.LossesByShift[r.LastShift] = c + 1;
                string cause = string.IsNullOrEmpty(r.LossCause) ? "unknown" : r.LossCause;
                s.LossCauses.TryGetValue(cause, out int lc);
                s.LossCauses[cause] = lc + 1;
            }
            s.FinalReputationOfWinners = Percentiles.Of(runs.Where(r => r.Won).Select(r => r.FinalReputation));
            s.FinalFundingOfWinners = Percentiles.Of(runs.Where(r => r.Won).Select(r => r.FinalFunding));

            int maxShift = Math.Max(shiftsToWin, runs.Max(r => r.Shifts.Count));
            for (int shift = 1; shift <= maxShift; shift++)
            {
                var snaps = runs.Where(r => r.Shifts.Count >= shift).Select(r => r.Shifts[shift - 1]).ToList();
                if (snaps.Count == 0) break;
                s.Curve.Add(new ShiftCurvePoint
                {
                    Shift = shift,
                    Alive = snaps.Count / (double)n,
                    Reputation = Percentiles.Of(snaps.Select(x => x.Reputation)),
                    Funding = Percentiles.Of(snaps.Select(x => x.Funding)),
                    Cured = Percentiles.Of(snaps.Select(x => x.CuredSoFar)),
                    Deaths = Percentiles.Of(snaps.Select(x => x.DeathsSoFar)),
                    DeckSize = Percentiles.Of(snaps.Select(x => x.DeckSize)),
                    ApLeftOver = snaps.Average(x => x.ApLeftOver)
                });
            }

            string[] poolNames = { "Walk-In", "Ambulance", "Trauma Flight" };
            for (int pool = 0; pool < 3; pool++)
            {
                double arrivals = runs.Sum(r => r.Arrivals[pool]);
                double cures = runs.Sum(r => r.Cures[pool]);
                double deaths = runs.Sum(r => r.Deaths[pool]);
                s.Pools.Add(new PoolStats
                {
                    Pool = poolNames[pool],
                    ArrivalsPerRun = arrivals / n,
                    CuresPerRun = cures / n,
                    DeathsPerRun = deaths / n,
                    CureRate = arrivals > 0 ? cures / arrivals : 0,
                    DeathRate = arrivals > 0 ? deaths / arrivals : 0,
                    UnresolvedRate = arrivals > 0 ? (arrivals - cures - deaths) / arrivals : 0,
                    AvgShiftsToCure = cures > 0 ? runs.Sum(r => r.ShiftsToCureSum[pool]) / cures : 0
                });
            }

            foreach (string source in runs.SelectMany(r => r.ReputationBySource.Keys).Distinct().OrderBy(x => x))
                s.ReputationBySourcePerRun[source] = runs.Sum(r => r.ReputationBySource.TryGetValue(source, out int v) ? v : 0) / (double)n;
            s.FundingEarnedPerRun = runs.Average(r => r.FundingEarned);
            s.FundingSpentPerRun = runs.Average(r => r.FundingSpent);

            foreach (string item in runs.SelectMany(r => r.Purchases).Distinct().OrderBy(x => x))
            {
                var buyers = runs.Where(r => r.Purchases.Contains(item)).ToList();
                var others = runs.Where(r => !r.Purchases.Contains(item)).ToList();
                s.Purchases.Add(new PurchaseStats
                {
                    Item = item,
                    PerRun = runs.Sum(r => r.Purchases.Count(p => p == item)) / (double)n,
                    ShareOfRunsBuying = buyers.Count / (double)n,
                    WinRateWhenBought = buyers.Count > 0 ? buyers.Count(r => r.Won) / (double)buyers.Count : 0,
                    WinRateWhenNot = others.Count > 0 ? others.Count(r => r.Won) / (double)others.Count : 0,
                    AvgFirstBuyIndex = buyers.Count > 0 ? buyers.Average(r => r.Purchases.IndexOf(item) + 1) : 0
                });
            }
            s.Purchases = s.Purchases.OrderByDescending(p => p.PerRun).ToList();

            s.CardsPlayedPerRun = runs.SelectMany(r => r.CardsPlayed)
                .GroupBy(kv => kv.Key)
                .Select(g => new KeyValuePair<string, double>(g.Key, g.Sum(kv => kv.Value) / (double)n))
                .OrderByDescending(kv => kv.Value).ToList();

            s.AmbulanceReachedShare = runs.Count(r => r.AmbulanceUnlockShift > 0) / (double)n;
            s.TraumaReachedShare = runs.Count(r => r.TraumaUnlockShift > 0) / (double)n;
            s.AvgAmbulanceUnlockShift = runs.Where(r => r.AmbulanceUnlockShift > 0).Select(r => (double)r.AmbulanceUnlockShift).DefaultIfEmpty(0).Average();
            s.AvgTraumaUnlockShift = runs.Where(r => r.TraumaUnlockShift > 0).Select(r => (double)r.TraumaUnlockShift).DefaultIfEmpty(0).Average();
            s.DeathsInTriagePerRun = runs.Average(r => r.DeathsInTriage);
            s.DeathsInWardPerRun = runs.Average(r => r.DeathsInWard);
            s.PagesPerRun = runs.Average(r => r.SymptomsPaged);
            s.CardTreatmentsPerRun = runs.Average(r => r.SymptomsTreatedWithCards);
            s.ApOnPagesPerRun = runs.Average(r => r.ApSpentOnPages);
            s.TeamAssessmentsPerRun = runs.Average(r => r.TeamAssessments);
            s.SpawnsSkippedPerRun = runs.Average(r => r.SpawnsSkippedTriageFull);
            return s;
        }
    }
}
