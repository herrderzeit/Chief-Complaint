using System.Linq;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Engine;
using Xunit;
using static ChiefComplaint.Sim.Tests.Fixtures;

namespace ChiefComplaint.Sim.Tests
{
    public class EngineTests
    {
        private static void EndShiftAndSkipDraft(Game g)
        {
            g.Apply(GameAction.EndShift);
            if (g.Phase == Phase.Draft) g.Apply(GameAction.FinishDraft);
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
            // Full stability: rep reward 1 (capped at 3) + 1 bonus; Walk-In funding 2 + 1 bonus.
            Assert.Equal(rep + 2, g.Reputation);
            Assert.Equal(funding + 3, g.Funding);
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

            EndShiftAndSkipDraft(g);
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

            EndShiftAndSkipDraft(g);
            Assert.Equal(10, p.Stability); // healed 1 (capped at max), no decay
            EndShiftAndSkipDraft(g);
            Assert.Equal(8, p.Stability);
        }

        [Fact]
        public void Death_costs_reputation_and_adds_red_tape_to_discard()
        {
            var data = Content(new CardDef[0], Patient(stability: 1, decay: 1));
            data.Config.StartingReputation = 20;
            var g = Start(data);

            EndShiftAndSkipDraft(g);
            Assert.Empty(g.ActivePatients);
            Assert.Equal(20 - data.Config.DeathReputationPenalty, g.Reputation);
            Assert.Contains(g.OwnedCards, c => c.Type == CardType.RedTape);
        }

        [Fact]
        public void Reputation_at_zero_loses_the_run()
        {
            var data = Content(new CardDef[0], Patient(stability: 1, decay: 1));
            data.Config.StartingReputation = 5;
            var g = Start(data);
            EndShiftAndSkipDraft(g);
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

        [Fact]
        public void Paging_costs_2_3_4_AP_by_tier_and_only_charges_on_success()
        {
            var data = Content(new[] { Diagnose(3) }, Patient(EscalationType.Ambulance, triage: 3));
            data.Config.BaseAP = 5;
            var g = Start(data);
            Patient p = g.Patients.Single();
            g.Apply(Find(g, ActionKind.Admit));
            g.PlayCard(g.Hand[0], p);
            GameAction page = Find(g, ActionKind.Treat, a => a.Page);
            Assert.Equal(3, page.Cost);
            g.Apply(page);
            Assert.Equal(5 - 1 - 3, g.AP);
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
            Assert.Equal(funding + 1, g.Funding);      // 2 - 1
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
            for (int i = 0; i < 3; i++) EndShiftAndSkipDraft(g);
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
        public void Market_prices_follow_CalculateDraftValue()
        {
            var statLabs = new CardDef { Type = CardType.Diagnostic, PowerTier = 2, ApCost = 0 };
            var crashCart = new CardDef { Type = CardType.Utility, PowerTier = 3, ApCost = 2 };
            var licensed = new CardDef { Type = CardType.Staff, PowerTier = 2, ApCost = 0, CardsToDraw = 1 };
            Assert.Equal(9, statLabs.DraftValue);
            Assert.Equal(11, crashCart.DraftValue);
            Assert.Equal(12, licensed.DraftValue);
            Assert.Equal(0, new CardDef { Type = CardType.RedTape }.DraftValue);
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
