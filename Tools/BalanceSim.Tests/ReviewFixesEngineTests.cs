using System.Linq;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Engine;
using ChiefComplaint.Sim.Stats;
using Xunit;
using static ChiefComplaint.Sim.Tests.Fixtures;

namespace ChiefComplaint.Sim.Tests
{
    /// <summary>
    /// Simulator parity for the review fixes: a card's Reputation cost is not charged when its random roll fails,
    /// a cure awards at most <see cref="GameConfig.CureReputationCap"/> (2) Reputation before the stability modifier,
    /// and <see cref="RunRecorder"/> records time-based escalation unlocks even in a run that never cures.
    /// </summary>
    public class ReviewFixesEngineTests
    {
        // ------------------------------------------------------------------ Reputation cost on a failed roll

        /// <summary>A free card that gains 1 AP, costs <paramref name="reputationCost"/> Reputation and lands with <paramref name="successChance"/>.</summary>
        private static CardDef Gamble(float successChance, int reputationCost = 1) => new CardDef
        {
            Id = "gamble", Name = "Gamble", Type = CardType.Utility, ApCost = 0,
            ReputationCost = reputationCost, SuccessChance = successChance,
            Effects = { new GainApEffect { Amount = 1 } }
        };

        private static Game StartWith(CardDef card, int startingReputation = 10)
        {
            var data = Content(new[] { card }, Patient());
            data.Config.StartingReputation = startingReputation;
            data.Config.MaxReputation = 0;
            return Start(data);
        }

        [Fact]
        public void Failed_roll_costs_no_Reputation()
        {
            Game g = StartWith(Gamble(successChance: 0f));
            int repBefore = g.Reputation, apBefore = g.AP;

            Assert.True(g.PlayCard(g.Hand.Single(), null), "A card whose roll fails is still played (and discarded).");

            Assert.Equal(repBefore, g.Reputation);
            Assert.Equal(apBefore, g.AP); // the effect fizzled too
            Assert.Empty(g.Hand);
        }

        [Fact]
        public void Successful_roll_costs_its_Reputation()
        {
            Game g = StartWith(Gamble(successChance: 1f));
            int repBefore = g.Reputation, apBefore = g.AP;

            Assert.True(g.PlayCard(g.Hand.Single(), null));

            Assert.Equal(repBefore - 1, g.Reputation);
            Assert.Equal(apBefore + 1, g.AP);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(7)]
        public void Failed_rolls_never_charge_Reputation_across_seeds(int seed)
        {
            var data = Content(new[] { Gamble(successChance: 0f, reputationCost: 3) }, Patient());
            data.Config.StartingReputation = 10;
            data.Config.MaxReputation = 0;
            Game g = Start(data, seed);

            Assert.True(g.PlayCard(g.Hand.Single(), null));

            Assert.Equal(10, g.Reputation);
        }

        // ------------------------------------------------------------------ cure Reputation cap

        /// <summary>Admits, diagnoses and treats the single Walk-In, then ends the shift so it is discharged at full stability.</summary>
        private static int ReputationGainedFromFullStabilityCure(int reputationReward, int? cap = null)
        {
            PatientDef def = Patient(stability: 10);
            def.ReputationReward = reputationReward;
            var data = Content(new[] { Diagnose(1), Cure(1) }, def);
            data.Config.StartingReputation = 5;
            data.Config.MaxReputation = 0; // the life cap is not under test here
            if (cap.HasValue) data.Config.CureReputationCap = cap.Value;
            Game g = Start(data);
            Patient p = g.Patients.Single();
            int before = g.Reputation;

            g.Apply(Find(g, ActionKind.Admit));
            g.Apply(Find(g, ActionKind.PlayCard, a => a.Patient == p));
            g.Apply(Find(g, ActionKind.Treat, a => !a.Page));
            g.Apply(GameAction.EndShift);

            Assert.True(p.Removed, "Fixture error: the patient should have been discharged.");
            Assert.True(p.Stability >= p.MaxStability / 2f, "Fixture error: the patient should earn the +1 stability bonus.");
            return g.Reputation - before;
        }

        [Fact]
        public void CureReputationCap_default_is_two()
        {
            Assert.Equal(2, new GameConfig().CureReputationCap);
        }

        [Theory]
        // reputationReward -> Reputation gained: min(reward, cap 2) + 1 for keeping at least half stability
        [InlineData(1, 2)]
        [InlineData(2, 3)]
        [InlineData(3, 3)]
        [InlineData(5, 3)]
        public void Cure_reward_is_capped_at_two_plus_the_stability_bonus(int reward, int expected)
        {
            Assert.Equal(expected, ReputationGainedFromFullStabilityCure(reward));
        }

        [Fact]
        public void Cure_reward_three_with_the_old_cap_three_gave_four()
        {
            // Shows the cap is what limits the reward: the same cure with the previous cap of 3 pays 3 + 1.
            Assert.Equal(4, ReputationGainedFromFullStabilityCure(3, cap: 3));
        }

        // ------------------------------------------------------------------ RunRecorder: time-based unlocks

        [Fact]
        public void RunRecorder_records_time_based_unlocks_in_a_run_that_never_cures()
        {
            // One durable Walk-In (the Walk-In deck is then empty), plus an Ambulance and a Trauma Flight to arrive
            // when their pools unlock by shift. The run only ends shifts, so nobody is ever cured.
            var data = Content(new[] { Diagnose(1), Cure(1) }, Patient(stability: 100));
            data.AmbulancePool.Add(Patient(EscalationType.Ambulance, stability: 100));
            data.TraumaFlightPool.Add(Patient(EscalationType.TraumaFlight, stability: 100));
            data.Config.AmbulanceShiftThreshold = 5;
            data.Config.TraumaFlightShiftThreshold = 10;
            data.Config.ShiftsToWin = 0;
            data.Config.TriageSlots = 10;

            var recorder = new RunRecorder();
            var g = new Game(data, 1, recorder);
            g.Start();
            while (g.Shift < 10)
            {
                g.Apply(GameAction.EndShift);
                Assert.Equal(Phase.Draft, g.Phase);
                g.Apply(GameAction.FinishDraft);
            }

            RunRecord record = recorder.Record;
            Assert.Equal(0, g.PatientsCured);
            Assert.Equal(0, record.Cures.Sum());
            Assert.Equal(5, record.AmbulanceUnlockShift);
            Assert.Equal(10, record.TraumaUnlockShift);

            BatchSummary summary = BatchSummary.From("never cures", new[] { record }, shiftsToWin: 0);
            Assert.True(summary.AmbulanceReachedShare > 0, "A time-based Ambulance unlock should count as reaching Ambulances.");
            Assert.True(summary.TraumaReachedShare > 0, "A time-based Trauma Flight unlock should count as reaching Trauma Flights.");
            Assert.Equal(5, summary.AvgAmbulanceUnlockShift);
        }
    }
}
