using System.Linq;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Engine;
using Xunit;
using static ChiefComplaint.Sim.Tests.Fixtures;

namespace ChiefComplaint.Sim.Tests
{
    /// <summary>
    /// The simulator's mirrors of the Reputation cap (GameManager.maxReputation / ApplyReputationCap), the "a death is
    /// never free" rule (HasOwnDeathPenalty needs a positive value) and the triage-to-tier mapping.
    /// </summary>
    public class ReputationCapEngineTests
    {
        private static CardDef Donation(int amount) => new CardDef
        {
            Id = "donation", Name = "Donation", Type = CardType.Utility, ApCost = 0,
            Effects = { new GainReputationEffect { Amount = amount } }
        };

        private static Game StartWithReputation(int starting, int cap, params CardDef[] deck)
        {
            var data = Content(deck, Patient());
            data.Config.StartingReputation = starting;
            data.Config.MaxReputation = cap;
            return Start(data);
        }

        [Theory]
        // starting, gain, cap, expected
        [InlineData(8, 10, 16, 16)]   // crossing the cap clamps
        [InlineData(8, 8, 16, 16)]    // landing exactly on the cap
        [InlineData(8, 3, 16, 11)]    // under the cap is unaffected
        [InlineData(16, 2, 16, 16)]   // at the cap a gain is lost
        [InlineData(20, 2, 16, 20)]   // already above the cap: never reduced
        [InlineData(8, 50, 0, 58)]    // 0 disables the cap
        public void Card_GainReputation_respects_MaxReputation(int starting, int gain, int cap, int expected)
        {
            Game g = StartWithReputation(starting, cap, Donation(gain));
            Assert.True(g.PlayCard(g.Hand.Single(), null), "Fixture error: the donation card should play.");
            Assert.Equal(expected, g.Reputation);
        }

        [Theory]
        // starting, cap, expected after a full-stability Walk-In cure (+2)
        [InlineData(15, 16, 16)]
        [InlineData(16, 16, 16)]
        [InlineData(10, 16, 12)]
        [InlineData(15, 0, 17)]
        public void Cure_reward_respects_MaxReputation(int starting, int cap, int expected)
        {
            Game g = StartWithReputation(starting, cap, Diagnose(1), Cure(1));
            Patient p = g.Patients.Single();
            g.Apply(Find(g, ActionKind.Admit));
            g.Apply(Find(g, ActionKind.PlayCard, a => a.Patient == p));
            g.Apply(Find(g, ActionKind.Treat, a => !a.Page));

            g.Apply(GameAction.EndShift);

            Assert.True(p.Removed, "Fixture error: the patient should have been discharged.");
            Assert.Equal(expected, g.Reputation);
        }

        [Fact]
        public void Repeated_gains_never_pass_the_cap()
        {
            var deck = Enumerable.Range(0, 5).Select(_ => Donation(3)).ToArray();
            Game g = StartWithReputation(8, 16, deck);
            while (g.Hand.Count > 0)
            {
                Assert.True(g.PlayCard(g.Hand[0], null));
                Assert.True(g.Reputation <= 16, $"Reputation {g.Reputation} passed the cap");
            }
            Assert.Equal(16, g.Reputation);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-5)]
        public void HasOwnDeathPenalty_needs_a_positive_value(int value)
        {
            PatientDef def = Patient();
            def.Triggers.Add(new PatientTriggerDef { Condition = TriggerCondition.OnDeath, Effect = TriggerEffect.LoseReputation, Value = value });
            Assert.False(def.HasOwnDeathPenalty);
        }

        [Fact]
        public void HasOwnDeathPenalty_value_one_counts()
        {
            PatientDef def = Patient();
            def.Triggers.Add(new PatientTriggerDef { Condition = TriggerCondition.OnDeath, Effect = TriggerEffect.LoseReputation, Value = 1 });
            Assert.True(def.HasOwnDeathPenalty);
        }

        [Fact]
        public void Death_with_a_zero_value_LoseReputation_trigger_pays_the_flat_penalty()
        {
            PatientDef def = Patient(stability: 1, decay: 1);
            def.Triggers.Add(new PatientTriggerDef { Condition = TriggerCondition.OnDeath, Effect = TriggerEffect.LoseReputation, Value = 0 });
            var data = Content(new CardDef[0], def);
            data.Config.StartingReputation = 20;
            data.Config.DeathReputationPenalty = 5;
            Game g = Start(data);

            g.Apply(GameAction.EndShift);
            if (g.Phase == Phase.Draft) g.Apply(GameAction.FinishDraft);

            Assert.Empty(g.ActivePatients);
            Assert.Equal(20 - 5, g.Reputation);
        }

        [Theory]
        [InlineData(1, 3)]
        [InlineData(2, 2)]
        [InlineData(3, 2)]
        [InlineData(4, 1)]
        [InlineData(5, 1)]
        public void TierFromTriage_maps_every_triage_level(int triage, int tier)
        {
            Assert.Equal(tier, Game.TierFromTriage(triage));
        }
    }
}
