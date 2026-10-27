using System.IO;
using System.Linq;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Engine;
using ChiefComplaint.Sim.Stats;
using Xunit;
using static ChiefComplaint.Sim.Tests.Fixtures;

namespace ChiefComplaint.Sim.Tests
{
    /// <summary>
    /// Simulator parity for the stalling-exploit fix: time-based escalation (pools also unlock at a set shift)
    /// and ambulance diversion (an arrival with Triage full costs Reputation and is skipped).
    /// </summary>
    public class StallingFixEngineTests
    {
        /// <summary>
        /// One durable Walk-In (spawned at the start, so the Walk-In deck is then empty), plus one Ambulance and one
        /// Trauma Flight in their pools. No cures happen because the tests only end shifts.
        /// </summary>
        private static GameData EscalationContent(int ambulanceShift, int traumaShift)
        {
            var data = Content(new[] { Diagnose(1), Cure(1) }, Patient(stability: 100));
            data.AmbulancePool.Add(Patient(EscalationType.Ambulance, stability: 100));
            data.TraumaFlightPool.Add(Patient(EscalationType.TraumaFlight, stability: 100));
            data.Config.AmbulanceShiftThreshold = ambulanceShift;
            data.Config.TraumaFlightShiftThreshold = traumaShift;
            data.Config.ShiftsToWin = 0;
            data.Config.TriageSlots = 10;
            return data;
        }

        /// <summary>Ends the current shift and skips the staff room, so the next shift starts.</summary>
        private static void NextShift(Game g)
        {
            g.Apply(GameAction.EndShift);
            Assert.Equal(Phase.StaffRoom, g.Phase);
            g.Apply(GameAction.LeaveStaffRoom);
            Assert.Equal(Phase.Shift, g.Phase);
        }

        private static void AdvanceTo(Game g, int shift)
        {
            while (g.Shift < shift) NextShift(g);
        }

        // ------------------------------------------------------------------ time-based escalation

        [Fact]
        public void Ambulances_unlock_at_the_shift_threshold_without_any_cures_and_not_before()
        {
            var g = Start(EscalationContent(ambulanceShift: 5, traumaShift: 0));

            AdvanceTo(g, 4);
            Assert.Equal(0, g.PatientsCured);
            Assert.False(g.AmbulancesUnlocked, "Ambulances must still be locked at shift 4.");
            Assert.Single(g.ActivePatients); // the Walk-In deck is empty, so shifts 2-4 spawned nobody

            NextShift(g);
            Assert.Equal(5, g.Shift);
            Assert.True(g.AmbulancesUnlocked, "Ambulances should unlock when shift 5 starts.");
            Assert.False(g.TraumaFlightsUnlocked);
            Assert.Equal("Ambulances", g.EscalationPhase);
            // Escalation is checked before this shift's arrival, so the Ambulance arrives on shift 5 itself.
            Assert.Contains(g.ActivePatients, p => p.Def.Phase == EscalationType.Ambulance);
        }

        [Fact]
        public void Trauma_flights_unlock_at_their_own_shift_threshold()
        {
            var g = Start(EscalationContent(ambulanceShift: 5, traumaShift: 10));

            AdvanceTo(g, 9);
            Assert.True(g.AmbulancesUnlocked);
            Assert.False(g.TraumaFlightsUnlocked, "Trauma Flights must still be locked at shift 9.");

            NextShift(g);
            Assert.Equal(10, g.Shift);
            Assert.True(g.TraumaFlightsUnlocked, "Trauma Flights should unlock when shift 10 starts.");
            Assert.Equal("Trauma Flights", g.EscalationPhase);
            Assert.Contains(g.ActivePatients, p => p.Def.Phase == EscalationType.TraumaFlight);
        }

        [Fact]
        public void Trauma_shift_threshold_alone_also_unlocks_trauma_flights()
        {
            var g = Start(EscalationContent(ambulanceShift: 0, traumaShift: 3));

            AdvanceTo(g, 3);
            Assert.True(g.TraumaFlightsUnlocked);
            Assert.False(g.AmbulancesUnlocked, "With the Ambulance shift rule disabled and no cures, Ambulances stay locked.");
        }

        [Fact]
        public void Zero_shift_thresholds_mean_cures_only()
        {
            var g = Start(EscalationContent(ambulanceShift: 0, traumaShift: 0));

            AdvanceTo(g, 20);
            Assert.Equal(0, g.PatientsCured);
            Assert.False(g.AmbulancesUnlocked);
            Assert.False(g.TraumaFlightsUnlocked);
            Assert.Equal("Walk-Ins", g.EscalationPhase);
        }

        [Fact]
        public void Cures_still_unlock_ambulances_before_the_shift_threshold()
        {
            // Cure threshold 1: admit, diagnose and treat the single Walk-In on shift 1; it is discharged at End Shift.
            var data = EscalationContent(ambulanceShift: 5, traumaShift: 10);
            data.Config.AmbulanceCureThreshold = 1;
            data.Config.BaseAP = 10;
            var g = Start(data);
            Patient p = g.Patients[0];
            g.Apply(new GameAction { Kind = ActionKind.Admit, Patient = p });
            g.PlayCard(g.Hand.First(c => c.Name.StartsWith("Diagnose")), p);
            g.Apply(Find(g, ActionKind.Treat, a => a.Patient == p && !a.Page));
            Assert.False(g.AmbulancesUnlocked);

            g.Apply(GameAction.EndShift);

            Assert.Equal(1, g.Shift);
            Assert.Equal(1, g.PatientsCured);
            Assert.True(g.AmbulancesUnlocked, "One cure should unlock Ambulances on shift 1, long before shift 5.");
        }

        // ------------------------------------------------------------------ diversion

        /// <summary>One Triage slot, filled by the opening patient, and spare Walk-Ins to arrive later.</summary>
        private static GameData DiversionContent(int penalty)
        {
            var data = Content(new[] { Diagnose(1), Cure(1) }, Patient(stability: 100), Patient(stability: 100), Patient(stability: 100));
            data.Config.InitialPatientCount = 1;
            data.Config.TriageSlots = 1;
            data.Config.ShiftsToWin = 0;
            data.Config.DiversionReputationPenalty = penalty;
            return data;
        }

        [Fact]
        public void Spawning_with_triage_full_costs_the_diversion_penalty_and_skips_the_patient()
        {
            var recorder = new RunRecorder();
            var data = DiversionContent(penalty: 2);
            var g = new Game(data, 1, recorder);
            g.Start();
            Assert.Single(g.ActivePatients);
            int reputation = g.Reputation;

            NextShift(g);

            Assert.Equal(reputation - 2, g.Reputation);
            Assert.Single(g.ActivePatients); // nobody new on the board
            Assert.Equal(1, recorder.Record.SpawnsSkippedTriageFull);
            Assert.Equal(-2, recorder.Record.ReputationBySource["diversion"]);
        }

        [Fact]
        public void Diversion_with_zero_penalty_is_free()
        {
            var recorder = new RunRecorder();
            var g = new Game(DiversionContent(penalty: 0), 1, recorder);
            g.Start();
            int reputation = g.Reputation;

            NextShift(g);

            Assert.Equal(reputation, g.Reputation);
            Assert.Single(g.ActivePatients);
            Assert.Equal(1, recorder.Record.SpawnsSkippedTriageFull);
            Assert.False(recorder.Record.ReputationBySource.ContainsKey("diversion"));
        }

        [Fact]
        public void Repeated_diversions_can_end_the_run()
        {
            var data = DiversionContent(penalty: 2);
            data.Config.StartingReputation = 4;
            var g = Start(data);

            NextShift(g);
            Assert.Equal(2, g.Reputation);
            g.Apply(GameAction.EndShift);
            g.Apply(GameAction.LeaveStaffRoom);

            Assert.Equal(0, g.Reputation);
            Assert.Equal(Phase.Lost, g.Phase);
            Assert.Equal("diversion", g.LossCause);
        }

        // ------------------------------------------------------------------ balance

        [Fact]
        [Trait("Category", "Balance")]
        public void Stall_bot_rarely_wins_with_time_escalation_and_diversion()
        {
            GameData data = new ProjectLoader(ProjectLoader.FindProjectRoot(Directory.GetCurrentDirectory())).Load();
            GameConfig config = data.Config.Clone();
            config.AmbulanceShiftThreshold = 5;
            config.TraumaFlightShiftThreshold = 10;
            config.DiversionReputationPenalty = 2;

            RunRecord[] runs = Runner.RunBatch(data, config, Program.Bot("stall"), 500, 1);
            double winRate = runs.Count(r => r.Won) / (double)runs.Length;

            Assert.True(winRate < 0.20, $"Stall bot won {winRate:P1} of 500 runs; the exploit should be closed (< 20%).");
        }
    }
}
