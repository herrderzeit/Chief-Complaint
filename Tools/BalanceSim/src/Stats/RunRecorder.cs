using System;
using System.Collections.Generic;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Engine;

namespace ChiefComplaint.Sim.Stats
{
    /// <summary>State at the end of one shift (after surgeries resolve, before the draft).</summary>
    public struct ShiftSnapshot
    {
        public int Shift;
        public int Reputation;
        public int Funding;
        public int CuredSoFar;
        public int DeathsSoFar;
        public int DeckSize;
        public int ApLeftOver;
        public int PatientsOnBoard;
    }

    /// <summary>Everything measured about one run.</summary>
    public sealed class RunRecord
    {
        public int Seed;
        public bool Won;
        public int LastShift;
        public string LossCause = "";
        public int FinalReputation;
        public int FinalFunding;
        public readonly List<ShiftSnapshot> Shifts = new List<ShiftSnapshot>();

        public readonly int[] Arrivals = new int[3];
        public readonly int[] Cures = new int[3];
        public readonly int[] Deaths = new int[3];
        /// <summary>Deaths by where the patient was when they died: Triage or Ward.</summary>
        public int DeathsInTriage, DeathsInWard;
        /// <summary>Shifts from arrival to discharge, per pool (sum, for averaging).</summary>
        public readonly int[] ShiftsToCureSum = new int[3];

        public readonly Dictionary<string, int> ReputationBySource = new Dictionary<string, int>();
        public int FundingEarned, FundingSpent;
        public readonly List<string> Purchases = new List<string>();
        public readonly Dictionary<string, int> CardsPlayed = new Dictionary<string, int>();
        public int SymptomsPaged, SymptomsTreatedWithCards, ApSpentOnPages;
        public int TeamAssessments;
        public int SpawnsSkippedTriageFull;
        public int AmbulanceUnlockShift, TraumaUnlockShift;
        public int Steps;
    }

    /// <summary>Observer that fills a <see cref="RunRecord"/> as the run plays out.</summary>
    public sealed class RunRecorder : RunObserverBase
    {
        public readonly RunRecord Record = new RunRecord();
        private int deaths;

        public override void OnPatientArrived(Game game, Patient patient)
        {
            Record.Arrivals[(int)patient.Pool]++;
            // Escalation also unlocks by shift (5 and 10), not only by cures, so record the unlock when it's first seen.
            if (Record.AmbulanceUnlockShift == 0 && game.AmbulancesUnlocked) Record.AmbulanceUnlockShift = game.Shift;
            if (Record.TraumaUnlockShift == 0 && game.TraumaFlightsUnlocked) Record.TraumaUnlockShift = game.Shift;
        }

        public override void OnSpawnSkipped(Game game, string reason)
        {
            if (reason == "triage full") Record.SpawnsSkippedTriageFull++;
        }

        public override void OnPatientDied(Game game, Patient patient)
        {
            Record.Deaths[(int)patient.Pool]++;
            if (patient.HasWardBed) Record.DeathsInWard++; else Record.DeathsInTriage++;
            deaths++;
        }

        public override void OnPatientCured(Game game, Patient patient, int repGained, int fundingGained)
        {
            Record.Cures[(int)patient.Pool]++;
            Record.ShiftsToCureSum[(int)patient.Pool] += game.Shift - patient.ArrivalShift + 1;
            // Escalation thresholds are checked right after this event, so test the next cure count.
            if (Record.AmbulanceUnlockShift == 0 && game.PatientsCured + 1 >= game.Config.AmbulanceCureThreshold) Record.AmbulanceUnlockShift = game.Shift;
            if (Record.TraumaUnlockShift == 0 && game.PatientsCured + 1 >= game.Config.TraumaFlightCureThreshold) Record.TraumaUnlockShift = game.Shift;
        }

        public override void OnSymptomTreated(Game game, Patient patient, Symptom symptom, bool paged, int apCost)
        {
            if (paged) { Record.SymptomsPaged++; Record.ApSpentOnPages += apCost; }
            else Record.SymptomsTreatedWithCards++;
        }

        public override void OnReputationChanged(Game game, int delta, string source)
        {
            Record.ReputationBySource.TryGetValue(source, out int v);
            Record.ReputationBySource[source] = v + delta;
        }

        public override void OnFundingChanged(Game game, int delta, string source)
        {
            if (delta > 0) Record.FundingEarned += delta;
            else Record.FundingSpent -= delta;
        }

        public override void OnCardPlayed(Game game, CardDef card, Patient target)
        {
            Record.CardsPlayed.TryGetValue(card.Name, out int v);
            Record.CardsPlayed[card.Name] = v + 1;
        }

        public override void OnTeamAssessment(Game game, int tier, Patient target) => Record.TeamAssessments++;

        public override void OnPurchase(Game game, string item, CardDef card, int cost) => Record.Purchases.Add(item);

        public override void OnShiftEnded(Game game, int apLeftOver)
        {
            int onBoard = 0, deck = 0;
            foreach (Patient _ in game.ActivePatients) onBoard++;
            foreach (CardDef _ in game.OwnedCards) deck++;
            Record.Shifts.Add(new ShiftSnapshot
            {
                Shift = game.Shift,
                Reputation = game.Reputation,
                Funding = game.Funding,
                CuredSoFar = game.PatientsCured,
                DeathsSoFar = deaths,
                DeckSize = deck,
                ApLeftOver = apLeftOver,
                PatientsOnBoard = onBoard
            });
        }

        public override void OnRunEnded(Game game)
        {
            Record.Won = game.Phase == Phase.Won;
            Record.LastShift = game.Shift;
            Record.LossCause = game.LossCause;
            Record.FinalReputation = game.Reputation;
            Record.FinalFunding = game.Funding;
        }
    }

    /// <summary>Fans events out to several observers.</summary>
    public sealed class CompositeObserver : IRunObserver
    {
        private readonly IRunObserver[] observers;
        public CompositeObserver(params IRunObserver[] observers) { this.observers = Array.FindAll(observers, o => o != null); }

        public void OnPatientArrived(Game g, Patient p) { foreach (var o in observers) o.OnPatientArrived(g, p); }
        public void OnSpawnSkipped(Game g, string r) { foreach (var o in observers) o.OnSpawnSkipped(g, r); }
        public void OnPatientDied(Game g, Patient p) { foreach (var o in observers) o.OnPatientDied(g, p); }
        public void OnPatientCured(Game g, Patient p, int r, int f) { foreach (var o in observers) o.OnPatientCured(g, p, r, f); }
        public void OnSymptomTreated(Game g, Patient p, Symptom s, bool paged, int ap) { foreach (var o in observers) o.OnSymptomTreated(g, p, s, paged, ap); }
        public void OnReputationChanged(Game g, int d, string s) { foreach (var o in observers) o.OnReputationChanged(g, d, s); }
        public void OnFundingChanged(Game g, int d, string s) { foreach (var o in observers) o.OnFundingChanged(g, d, s); }
        public void OnCardPlayed(Game g, CardDef c, Patient t) { foreach (var o in observers) o.OnCardPlayed(g, c, t); }
        public void OnTeamAssessment(Game g, int tier, Patient t) { foreach (var o in observers) o.OnTeamAssessment(g, tier, t); }
        public void OnPurchase(Game g, string i, CardDef c, int cost) { foreach (var o in observers) o.OnPurchase(g, i, c, cost); }
        public void OnShiftEnded(Game g, int ap) { foreach (var o in observers) o.OnShiftEnded(g, ap); }
        public void OnRunEnded(Game g) { foreach (var o in observers) o.OnRunEnded(g); }
        public void OnAction(Game g, GameAction a) { foreach (var o in observers) o.OnAction(g, a); }
    }
}
