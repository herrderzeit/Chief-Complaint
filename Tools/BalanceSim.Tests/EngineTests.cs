using System.Linq;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Engine;
using Xunit;
using static ChiefComplaint.Sim.Tests.Fixtures;

namespace ChiefComplaint.Sim.Tests
{
    public class EngineTests
    {
        private static void EndShiftAndSkipStaffRoom(Game g)
        {
            g.Apply(GameAction.EndShift);
            if (g.Phase == Phase.StaffRoom) g.Apply(GameAction.LeaveStaffRoom);
        }

        [Fact]
        public void Walk_in_can_be_admitted_diagnosed_and_cured_in_one_shift()
        {
            var g = Start(Content(new[] { Diagnose(1), Cure(1) }, Patient()));
            Patient p = g.Patients.Single();

            g.Apply(Find(g, ActionKind.Admit));
            g.Apply(Find(g, ActionKind.PlayCard, a => a.Patient == p));
            Assert.True(p.IsFullyDiagnosed);
            g.Apply(Find(g, ActionKind.Treat, a => !a.Page));
            Assert.True(p.InSurgery);
            Assert.Equal(1, g.AP);

            int rep = g.Reputation, funding = g.Funding;
            g.Apply(GameAction.EndShift);
            Assert.True(p.Removed);
            // Full stability: rep reward 1 (capped at 3) + 1 bonus; Walk-In funding + 1 bonus.
            Assert.Equal(rep + 2, g.Reputation);
            Assert.Equal(funding + g.Config.WalkInFundingReward + 1, g.Funding);
            Assert.Equal(1, g.PatientsCured);
        }

        [Fact]
        public void Diagnosis_needs_the_ward_and_a_high_enough_tier()
        {
            var g = Start(Content(new[] { Diagnose(1) }, Patient(EscalationType.WalkIn, triage: 2)));
            Patient p = g.Patients.Single();
            Assert.Equal(2, p.Symptoms.Single().Tier);

            Assert.False(g.PlayCard(g.Hand[0], p)); // in Triage
            g.Apply(Find(g, ActionKind.Admit));
            Assert.False(g.PlayCard(g.Hand[0], p)); // tier 1 card vs tier 2 symptom
            Assert.Single(g.Hand);
            Assert.Equal(3, g.AP);
        }

        [Fact]
        public void Ward_halves_decay_rounding_up_and_triage_takes_full_decay()
        {
            var data = Content(new CardDef[0], Patient(stability: 9, decay: 3), Patient(stability: 9, decay: 3));
            var g = Start(data);
            Patient ward = g.Patients[0], triage = g.Patients[1];
            g.Apply(new GameAction { Kind = ActionKind.Admit, Patient = ward });

            EndShiftAndSkipStaffRoom(g);
            Assert.Equal(9 - 2, ward.Stability);
            Assert.Equal(9 - 3, triage.Stability);
        }

        [Fact]
        public void Pause_skips_exactly_one_decay()
        {
            var g = Start(Content(new[] { Pause() }, Patient(stability: 10, decay: 2)));
            Patient p = g.Patients.Single();
            g.PlayCard(g.Hand[0], p);
            Assert.True(p.ClockPaused);

            EndShiftAndSkipStaffRoom(g);
            Assert.Equal(10, p.Stability); // healed 1 (capped at max), no decay
            EndShiftAndSkipStaffRoom(g);
            Assert.Equal(8, p.Stability);
        }

        [Fact]
        public void Death_costs_reputation_and_adds_red_tape_to_discard()
        {
            var data = Content(new CardDef[0], Patient(stability: 1, decay: 1));
            data.Config.StartingReputation = 20;
            var g = Start(data);

            EndShiftAndSkipStaffRoom(g);
            Assert.Empty(g.ActivePatients);
            Assert.Equal(20 - data.Config.DeathReputationPenalty, g.Reputation);
            Assert.Contains(g.OwnedCards, c => c.Type == CardType.RedTape);
        }

        [Fact]
        public void Death_without_own_penalty_costs_exactly_the_flat_penalty()
        {
            PatientDef def = Patient(stability: 1, decay: 1);
            def.Triggers.Add(new PatientTriggerDef { Condition = TriggerCondition.OnDeath, Effect = TriggerEffect.DrawCard, Value = 1 });
            var data = Content(new CardDef[0], def);
            data.Config.StartingReputation = 20;
            data.Config.DeathReputationPenalty = 5;
            var g = Start(data);

            EndShiftAndSkipStaffRoom(g);
            Assert.Empty(g.ActivePatients);
            Assert.False(def.HasOwnDeathPenalty);
            // An OnDeath trigger that doesn't take Reputation leaves the flat penalty in place.
            Assert.Equal(20 - 5, g.Reputation);
        }

        [Fact]
        public void Death_with_OnDeath_LoseReputation_trigger_replaces_the_flat_penalty()
        {
            PatientDef def = Patient(stability: 1, decay: 1);
            def.Triggers.Add(new PatientTriggerDef { Condition = TriggerCondition.OnDeath, Effect = TriggerEffect.LoseReputation, Value = 4 });
            var data = Content(new CardDef[0], def);
            data.Config.StartingReputation = 20;
            data.Config.DeathReputationPenalty = 5;
            var g = Start(data);

            EndShiftAndSkipStaffRoom(g);
            Assert.Empty(g.ActivePatients);
            Assert.True(def.HasOwnDeathPenalty);
            Assert.Equal(20 - 4, g.Reputation); // 4 from the trigger, not 4 + 5
        }

        [Fact]
        public void Death_with_negative_OnDeath_LoseReputation_trigger_pays_the_flat_penalty_and_gives_nothing_back()
        {
            PatientDef def = Patient(stability: 1, decay: 1);
            def.Triggers.Add(new PatientTriggerDef { Condition = TriggerCondition.OnDeath, Effect = TriggerEffect.LoseReputation, Value = -3 });
            var data = Content(new CardDef[0], def);
            data.Config.StartingReputation = 20;
            data.Config.DeathReputationPenalty = 5;
            var g = Start(data);

            EndShiftAndSkipStaffRoom(g);
            Assert.Empty(g.ActivePatients);
            // Mirrors PatientCardView: -3 clamps to 0, so the flat 5 applies and no Reputation is handed back.
            Assert.Equal(20 - 5, g.Reputation);
        }

        [Theory]
        // Start sets AP and Reputation before the opening patients spawn, so OnSpawn triggers are visible straight away.
        // Positive values are controls showing the trigger really fires; negative values are clamped to 0.
        [InlineData(TriggerEffect.GainReputation, -2, 0, 0)]
        [InlineData(TriggerEffect.GainReputation, 2, 0, 2)]
        [InlineData(TriggerEffect.LoseReputation, -2, 0, 0)]
        [InlineData(TriggerEffect.LoseAP, -2, 0, 0)]
        [InlineData(TriggerEffect.LoseAP, 2, -2, 0)]
        [InlineData(TriggerEffect.GainAP, -2, 0, 0)]
        public void OnSpawn_AP_and_Reputation_triggers_only_move_in_their_named_direction(TriggerEffect effect, int value, int apDelta, int repDelta)
        {
            PatientDef def = Patient();
            def.Triggers.Add(new PatientTriggerDef { Condition = TriggerCondition.OnSpawn, Effect = effect, Value = value });
            var data = Content(new CardDef[0], def);
            data.Config.StartingReputation = 10; // below the cap, so a gain would show
            var g = Start(data);

            Assert.Equal(data.Config.BaseAP + apDelta, g.AP);
            Assert.Equal(10 + repDelta, g.Reputation);
        }

        [Theory]
        [InlineData(TriggerCondition.OnDeath, TriggerEffect.LoseReputation, true)]
        [InlineData(TriggerCondition.OnDeath, TriggerEffect.DrawCard, false)]
        [InlineData(TriggerCondition.OnDeath, TriggerEffect.GainReputation, false)]
        [InlineData(TriggerCondition.OnTurnEnd, TriggerEffect.LoseReputation, false)]
        [InlineData(TriggerCondition.OnSpawn, TriggerEffect.LoseReputation, false)]
        public void HasOwnDeathPenalty_needs_OnDeath_and_LoseReputation(TriggerCondition condition, TriggerEffect effect, bool expected)
        {
            PatientDef def = Patient();
            Assert.False(def.HasOwnDeathPenalty); // no triggers
            def.Triggers.Add(new PatientTriggerDef { Condition = condition, Effect = effect, Value = 4 });
            Assert.Equal(expected, def.HasOwnDeathPenalty);
        }

        [Fact]
        public void Reputation_at_zero_loses_the_run()
        {
            var data = Content(new CardDef[0], Patient(stability: 1, decay: 1));
            data.Config.StartingReputation = 5;
            var g = Start(data);
            EndShiftAndSkipStaffRoom(g);
            Assert.Equal(Phase.Lost, g.Phase);
            Assert.Equal(0, g.Reputation);
        }

        [Fact]
        public void A_department_treats_one_symptom_per_shift_and_a_patient_one_per_shift()
        {
            var two = Patient(EscalationType.WalkIn, 5, 10, 1, SymptomCategory.Trauma, SymptomCategory.Internal);
            var other = Patient(EscalationType.WalkIn, 5, 10, 1, SymptomCategory.Trauma);
            var data = Content(new[] { Diagnose(1), Diagnose(1), Diagnose(1), Cure(1), Cure(1) }, two, other);
            data.Config.BaseAP = 10;
            var g = Start(data);
            Patient a = g.Patients[0], b = g.Patients[1];
            g.Apply(new GameAction { Kind = ActionKind.Admit, Patient = a });
            g.Apply(new GameAction { Kind = ActionKind.Admit, Patient = b });
            for (int i = 0; i < 2; i++) g.PlayCard(g.Hand.First(c => c.Name.StartsWith("Diagnose")), a);
            g.PlayCard(g.Hand.First(c => c.Name.StartsWith("Diagnose")), b);

            g.Apply(Find(g, ActionKind.Treat, x => x.Patient == a && x.Department == SymptomCategory.Trauma && !x.Page));
            Assert.Single(a.Symptoms);
            // a is in surgery: no second treatment this shift; Trauma is used: b cannot go there.
            Assert.Null(Find(g, ActionKind.Treat, x => x.Patient == a));
            Assert.Null(Find(g, ActionKind.Treat, x => x.Patient == b));
        }

        [Theory]
        // triage -> primary symptom tier -> page cost (PagingRules.ApCost: Tier 1 = 2, Tier 2 = 3, Tier 3 = 3)
        [InlineData(5, 1, 2)]
        [InlineData(3, 2, 3)]
        [InlineData(1, 3, 3)]
        public void Paging_costs_2_3_3_AP_by_tier_and_only_charges_on_success(int triage, int tier, int cost)
        {
            var data = Content(new[] { Diagnose(3) }, Patient(EscalationType.Ambulance, triage: triage));
            data.Config.BaseAP = 5;
            var g = Start(data);
            Patient p = g.Patients.Single();
            Assert.Equal(tier, p.Symptoms.Single().Tier);
            g.Apply(Find(g, ActionKind.Admit));
            g.PlayCard(g.Hand[0], p);
            GameAction page = Find(g, ActionKind.Treat, a => a.Page);
            Assert.Equal(cost, page.Cost);
            g.Apply(page);
            Assert.Equal(5 - 1 - cost, g.AP);
            Assert.True(p.IsAwaitingDischarge);
        }

        [Fact]
        public void Discharge_reward_drops_for_a_barely_stable_patient()
        {
            var data = Content(new[] { Diagnose(1), Cure(1) }, Patient(stability: 10, decay: 1));
            var g = Start(data);
            Patient p = g.Patients.Single();
            p.Stability = 2;
            g.Apply(Find(g, ActionKind.Admit));
            g.PlayCard(g.Hand.First(c => c.Name.StartsWith("Diagnose")), p);
            g.Apply(Find(g, ActionKind.Treat, a => !a.Page));
            int rep = g.Reputation, funding = g.Funding;
            g.Apply(GameAction.EndShift);
            Assert.Equal(rep + 0, g.Reputation);      // 1 - 1
            Assert.Equal(funding + g.Config.WalkInFundingReward - 1, g.Funding); // Walk-In funding - 1
        }

        [Fact]
        public void Doctor_plus_nurse_grants_one_team_assessment_per_shift()
        {
            var data = Content(new[] { Doctor(), Nurse(), Doctor(), Nurse() }, Patient(EscalationType.WalkIn, 5, 10, 1, SymptomCategory.Trauma, SymptomCategory.Internal));
            var g = Start(data);
            Patient p = g.Patients.Single();
            g.Apply(Find(g, ActionKind.Admit)); // Team Assessment needs a Ward bed

            g.PlayCard(g.Hand.First(c => c.HasTag(CardTag.Doctor)), null);
            Assert.Equal(Phase.Shift, g.Phase);
            g.PlayCard(g.Hand.First(c => c.HasTag(CardTag.Nurse)), null);
            Assert.Equal(Phase.TeamAssessment, g.Phase);
            g.Apply(Find(g, ActionKind.TeamAssess));
            Assert.Equal(1, p.DiagnosedCount);
            Assert.Equal(Phase.Shift, g.Phase);

            g.PlayCard(g.Hand.First(c => c.HasTag(CardTag.Doctor)), null);
            g.PlayCard(g.Hand.First(c => c.HasTag(CardTag.Nurse)), null);
            Assert.Equal(Phase.Shift, g.Phase);
            Assert.Equal(1, p.DiagnosedCount);
        }

        [Fact]
        public void Three_cures_unlock_ambulances()
        {
            var data = Content(new[] { Diagnose(1), Cure(1) }, Patient(), Patient(), Patient());
            data.AmbulancePool.Add(Patient(EscalationType.Ambulance, 3, 7, 2));
            data.Config.BaseAP = 10;
            data.Config.AmbulanceCureThreshold = 1;
            var g = Start(data);
            Patient p = g.Patients[0];
            g.Apply(new GameAction { Kind = ActionKind.Admit, Patient = p });
            g.PlayCard(g.Hand.First(c => c.Name.StartsWith("Diagnose")), p);
            g.Apply(Find(g, ActionKind.Treat, a => a.Patient == p && !a.Page));
            Assert.False(g.AmbulancesUnlocked);
            g.Apply(GameAction.EndShift);
            Assert.True(g.AmbulancesUnlocked);
        }

        [Fact]
        public void Finishing_the_last_shift_wins()
        {
            var data = Content(new CardDef[0], Patient(stability: 50));
            data.Config.ShiftsToWin = 3;
            var g = Start(data);
            for (int i = 0; i < 3; i++) EndShiftAndSkipStaffRoom(g);
            Assert.Equal(Phase.Won, g.Phase);
            Assert.Equal(3, g.Shift);
        }

        [Fact]
        public void Upgrade_replaces_one_copy_and_costs_the_base_cards_upgrade_cost()
        {
            CardDef t2 = Diagnose(2);
            CardDef t1 = Diagnose(1);
            t1.Upgrade = t2;
            t1.UpgradeCost = 5;
            var data = Content(new[] { t1, t1 }, Patient(stability: 50));
            data.Config.StartingFunding = 6;
            var g = Start(data);
            g.Apply(GameAction.EndShift);
            g.Apply(Find(g, ActionKind.Upgrade));
            Assert.Equal(1, g.Funding);
            Assert.Equal(1, g.OwnedCards.Count(c => c == t1));
            Assert.Equal(1, g.OwnedCards.Count(c => c == t2));
        }

        [Fact]
        public void Market_prices_follow_CalculateMarketPrice()
        {
            var statLabs = new CardDef { Type = CardType.Diagnostic, PowerTier = 2, ApCost = 0 };
            var crashCart = new CardDef { Type = CardType.Utility, PowerTier = 3, ApCost = 2 };
            var licensed = new CardDef { Type = CardType.Staff, PowerTier = 2, ApCost = 0, CardsToDraw = 1 };
            Assert.Equal(9, statLabs.MarketPrice);
            Assert.Equal(11, crashCart.MarketPrice);
            Assert.Equal(12, licensed.MarketPrice);
            Assert.Equal(0, new CardDef { Type = CardType.RedTape }.MarketPrice);
        }

        /// <summary>
        /// One stable patient on the board, a deep Walk-In pool and a roomy Triage, so no arrival is ever
        /// skipped (empty deck) or diverted (full Triage) and the count only reflects the spawn rule.
        /// </summary>
        private static Game StartForArrivals(int extraPatientEveryNShifts)
        {
            PatientDef[] pool = Enumerable.Range(0, 12).Select(_ => Patient(stability: 50)).ToArray();
            var data = Content(new CardDef[0], pool);
            data.Config.InitialPatientCount = 1;
            data.Config.TriageSlots = 20;
            data.Config.ExtraPatientEveryNShifts = extraPatientEveryNShifts;
            return Start(data);
        }

        [Fact]
        public void Every_third_shift_brings_an_extra_patient()
        {
            var g = StartForArrivals(3);
            Assert.Equal(1, g.Shift);
            Assert.Single(g.Patients);

            EndShiftAndSkipStaffRoom(g); // shift 2: the usual one arrival
            Assert.Equal(2, g.Shift);
            Assert.Equal(2, g.Patients.Count);

            EndShiftAndSkipStaffRoom(g); // shift 3: the usual arrival plus the extra one
            Assert.Equal(3, g.Shift);
            Assert.Equal(4, g.Patients.Count);

            EndShiftAndSkipStaffRoom(g); // shift 4: back to one
            Assert.Equal(5, g.Patients.Count);
        }

        [Fact]
        public void Extra_patient_rule_off_adds_exactly_one_patient_per_shift()
        {
            var g = StartForArrivals(0);
            for (int shift = 2; shift <= 7; shift++)
            {
                int before = g.Patients.Count;
                EndShiftAndSkipStaffRoom(g);
                Assert.Equal(shift, g.Shift);
                Assert.Equal(before + 1, g.Patients.Count);
            }
        }

        [Fact]
        public void Unplayable_cards_exhaust_at_shift_end()
        {
            CardDef paper = RedTape();
            var g = Start(Content(new[] { paper }, Patient(stability: 50)));
            Assert.Contains(paper, g.Hand);
            Assert.DoesNotContain(g.LegalActions(), a => a.Card == paper);
            g.Apply(GameAction.EndShift);
            Assert.DoesNotContain(paper, g.OwnedCards);
        }
    }
}
