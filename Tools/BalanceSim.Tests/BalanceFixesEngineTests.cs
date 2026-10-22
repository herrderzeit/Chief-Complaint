using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChiefComplaint.Sim.Bots;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Engine;
using Xunit;
using static ChiefComplaint.Sim.Tests.Fixtures;

namespace ChiefComplaint.Sim.Tests
{
    /// <summary>
    /// Simulator parity for the balance-fix round: mid-shift draws don't reshuffle, the Reputation gate counts
    /// effect losses, End Shift AP carries over, paused patients still run OnTurnEnd triggers, Team Assessment
    /// isn't wasted, the final shift has no extra arrival and no last decay, the new paging costs and price terms,
    /// the GameConfig fallbacks and the focus experiment's copy limit.
    /// </summary>
    public class BalanceFixesEngineTests
    {
        private static readonly GameData Project = new ProjectLoader(ProjectLoader.FindProjectRoot(Directory.GetCurrentDirectory())).Load();

        private static void EndShiftAndSkipDraft(Game g)
        {
            g.Apply(GameAction.EndShift);
            if (g.Phase == Phase.Draft) g.Apply(GameAction.FinishDraft);
        }

        private sealed class SpawnCounter : RunObserverBase
        {
            public int Arrived, Skipped;
            public override void OnPatientArrived(Game game, Patient patient) => Arrived++;
            public override void OnSpawnSkipped(Game game, string reason) => Skipped++;
        }

        // ------------------------------------------------------------------ 1. mid-shift draws

        [Fact]
        public void DrawCards_without_reshuffle_leaves_the_discard_pile_alone()
        {
            var g = Start(Content(new[] { Diagnose(1) }, Patient(stability: 50)));
            Assert.Empty(g.DrawPile); // the one-card deck is all in hand
            CardDef discarded = Cure(1);
            g.DiscardPile.Add(discarded);
            int hand = g.Hand.Count;

            g.DrawCards(1, reshuffleDiscard: false);
            Assert.Equal(hand, g.Hand.Count);
            Assert.Equal(new[] { discarded }, g.DiscardPile);

            g.DrawCards(1); // the start-of-shift draw does reshuffle
            Assert.Equal(hand + 1, g.Hand.Count);
            Assert.Contains(discarded, g.Hand);
            Assert.Empty(g.DiscardPile);
        }

        [Fact]
        public void Card_draw_mid_shift_does_not_reshuffle_but_the_next_hand_does()
        {
            CardDef drawer = Doctor();
            drawer.CardsToDraw = 1;
            var g = Start(Content(new[] { drawer }, Patient(stability: 50)));
            CardDef discarded = Cure(1);
            g.DiscardPile.Add(discarded);

            Assert.True(g.PlayCard(drawer, null));
            Assert.Empty(g.Hand);
            Assert.Contains(discarded, g.DiscardPile);

            EndShiftAndSkipDraft(g);
            Assert.Contains(discarded, g.Hand);
            Assert.Contains(drawer, g.Hand);
        }

        [Fact]
        public void Licensed_Doctor_and_Chief_of_Medicine_cannot_loop_for_unlimited_AP()
        {
            CardDef licensed = Project.AllCards.Values.First(c => c.Name == "Licensed Doctor");
            CardDef chief = Project.AllCards.Values.First(c => c.Name == "Chief of Medicine");
            Assert.True(licensed.ApCost == 0 && licensed.CardsToDraw > 0, "Fixture: Licensed Doctor should be a free card that draws.");
            Assert.True(chief.ApCost == 0 && chief.CardsToDraw > 0, "Fixture: Chief of Medicine should be a free card that draws.");

            var g = Start(Content(new[] { licensed, chief }, Patient(stability: 50)));
            Assert.Equal(2, g.Hand.Count);
            Assert.Empty(g.DrawPile);

            int plays = 0;
            for (int guard = 0; guard < 50; guard++)
            {
                if (g.Phase == Phase.TeamAssessment) { g.Apply(GameAction.SkipTeamAssess); continue; }
                GameAction play = g.LegalActions().FirstOrDefault(a => a.Kind == ActionKind.PlayCard);
                if (play == null) break;
                g.Apply(play);
                plays++;
            }

            Assert.Equal(2, plays);
            Assert.Empty(g.Hand);
            Assert.Equal(2, g.DiscardPile.Count);
        }

        // ------------------------------------------------------------------ 2. Reputation gate counts effect losses

        private static CardDef ReputationLossRedTape(int loss) => new CardDef
        {
            Id = "rt", Name = "Lose " + loss, Type = CardType.RedTape, IsPlayable = true, ExhaustsOnUse = true,
            Effects = { new GainReputationEffect { Amount = -loss } }
        };

        [Theory]
        [InlineData(0, -3, 3)]
        [InlineData(1, -2, 3)]
        [InlineData(2, 0, 2)]
        [InlineData(0, 2, 0)]
        [InlineData(-1, -1, 1)]
        public void TotalReputationCost_adds_effect_losses_to_the_card_cost(int cardCost, int effectAmount, int expected)
        {
            var card = new CardDef { ReputationCost = cardCost, Effects = { new GainReputationEffect { Amount = effectAmount } } };
            Assert.Equal(expected, card.TotalReputationCost);
        }

        [Theory]
        [InlineData(3, false)]
        [InlineData(4, true)]
        public void Red_tape_losing_3_Reputation_is_only_legal_above_3(int reputation, bool legal)
        {
            CardDef card = ReputationLossRedTape(3);
            Assert.Equal(3, card.TotalReputationCost);
            var data = Content(new[] { card }, Patient(stability: 50));
            data.Config.StartingReputation = reputation;
            var g = Start(data);

            Assert.Equal(legal, g.LegalActions().Any(a => a.Card == card));
            Assert.Equal(legal, g.PlayCard(card, null));
            Assert.Equal(legal ? reputation - 3 : reputation, g.Reputation);
            Assert.NotEqual(Phase.Lost, g.Phase);
        }

        // ------------------------------------------------------------------ 3. End Shift AP carries over

        [Fact]
        public void OnCured_GainAP_at_End_Shift_carries_into_the_next_shift()
        {
            PatientDef def = Patient(stability: 10);
            def.Triggers.Add(new PatientTriggerDef { Condition = TriggerCondition.OnCured, Effect = TriggerEffect.GainAP, Value = 2 });
            var data = Content(new[] { Diagnose(1), Cure(1) }, def);
            var g = Start(data);
            Patient p = g.Patients.Single();

            g.Apply(Find(g, ActionKind.Admit));
            g.PlayCard(g.Hand.First(c => c.Name.StartsWith("Diagnose")), p);
            g.Apply(Find(g, ActionKind.Treat, a => !a.Page));
            Assert.True(p.InSurgery);

            g.Apply(GameAction.EndShift);
            Assert.True(p.Removed, "Fixture: the patient should be discharged at End Shift.");
            Assert.Equal(Phase.Draft, g.Phase);

            g.Apply(GameAction.FinishDraft);
            Assert.Equal(2, g.Shift);
            Assert.Equal(data.Config.BaseAP + 2, g.AP);

            EndShiftAndSkipDraft(g);
            Assert.Equal(data.Config.BaseAP, g.AP); // carried once, not every shift
        }

        // ------------------------------------------------------------------ 4. paused patients still run OnTurnEnd

        [Fact]
        public void Paused_patient_skips_decay_but_still_runs_OnTurnEnd_triggers()
        {
            PatientDef def = Patient(stability: 10, decay: 2);
            def.Triggers.Add(new PatientTriggerDef { Condition = TriggerCondition.OnTurnEnd, Effect = TriggerEffect.LoseAP, Value = 1 });
            var data = Content(new[] { Pause() }, def);
            var g = Start(data);
            Patient p = g.Patients.Single();
            Assert.True(g.PlayCard(g.Hand[0], p));
            Assert.True(p.ClockPaused);

            EndShiftAndSkipDraft(g);

            Assert.Equal(10, p.Stability);
            Assert.False(p.ClockPaused);
            Assert.Equal(data.Config.BaseAP - 1, g.AP);
        }

        // ------------------------------------------------------------------ 6. final shift: no extra patient

        [Fact]
        public void Final_shift_brings_only_the_usual_arrival()
        {
            PatientDef[] pool = Enumerable.Range(0, 40).Select(_ => Patient(stability: 100)).ToArray();
            var data = Content(new CardDef[0], pool);
            data.Config.InitialPatientCount = 1;
            data.Config.TriageSlots = 60;
            data.Config.ShiftsToWin = 15;
            data.Config.ExtraPatientEveryNShifts = 3;
            var g = Start(data);

            var arrivals = new Dictionary<int, int>();
            for (int shift = 2; shift <= 15; shift++)
            {
                int before = g.Patients.Count;
                EndShiftAndSkipDraft(g);
                Assert.Equal(shift, g.Shift);
                arrivals[shift] = g.Patients.Count - before;
            }

            Assert.True(g.IsFinalShift);
            Assert.Equal(2, arrivals[12]); // control: an earlier extra-patient shift still brings two
            Assert.Equal(1, arrivals[15]);
        }

        [Fact]
        public void No_extra_spawn_after_a_diversion_ends_the_run()
        {
            PatientDef[] pool = Enumerable.Range(0, 10).Select(_ => Patient(stability: 100)).ToArray();
            var data = Content(new CardDef[0], pool);
            data.Config.InitialPatientCount = 1;
            data.Config.TriageSlots = 1;            // the opening patient fills Triage
            data.Config.ExtraPatientEveryNShifts = 1; // every shift would bring a second arrival
            data.Config.DiversionReputationPenalty = 2;
            data.Config.StartingReputation = 2;    // the first diversion ends the run
            var counter = new SpawnCounter();
            var g = new Game(data, 1, counter);
            g.Start();
            counter.Skipped = 0;

            EndShiftAndSkipDraft(g);

            Assert.Equal(Phase.Lost, g.Phase);
            Assert.Equal(1, counter.Skipped);
        }

        [Theory]
        [InlineData(15, 14, false)]
        [InlineData(15, 15, true)]
        [InlineData(0, 99, false)]
        public void IsFinalShift_needs_a_win_condition_and_the_last_shift(int shiftsToWin, int reachShift, bool expected)
        {
            var data = Content(new CardDef[0], Enumerable.Range(0, 120).Select(_ => Patient(stability: 500)).ToArray());
            data.Config.InitialPatientCount = 0;
            data.Config.ExtraPatientEveryNShifts = 0;
            data.Config.TriageSlots = 200;
            data.Config.ShiftsToWin = shiftsToWin;
            var g = Start(data);
            while (g.Shift < reachShift && !g.IsOver) EndShiftAndSkipDraft(g);
            Assert.Equal(reachShift, g.Shift);
            Assert.Equal(expected, g.IsFinalShift);
        }

        // ------------------------------------------------------------------ 7. final shift: no last decay

        [Fact]
        public void Ending_the_final_shift_wins_without_a_last_round_of_decay()
        {
            var data = Content(new CardDef[0], Patient(stability: 2, decay: 1));
            data.Config.ShiftsToWin = 2;
            data.Config.ExtraPatientEveryNShifts = 0;
            var g = Start(data);
            Patient p = g.Patients.Single();

            EndShiftAndSkipDraft(g);
            Assert.Equal(2, g.Shift);
            Assert.Equal(1, p.Stability);
            int reputation = g.Reputation;

            g.Apply(GameAction.EndShift);

            Assert.Equal(Phase.Won, g.Phase);
            Assert.False(p.Removed, "The patient at Stability 1 must survive ending the final shift.");
            Assert.Equal(1, p.Stability);
            Assert.Equal(reputation, g.Reputation);
        }

        // ------------------------------------------------------------------ 8. paging costs

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 2)]
        [InlineData(3, 3)]
        public void Default_paging_costs_are_2_2_3_by_tier(int tier, int cost)
        {
            Assert.Equal(cost, new GameConfig().PageCost(tier));
        }

        // ------------------------------------------------------------------ 9. Supply Room price terms

        [Theory]
        // type, tier, AP, draw, exhausts, rep cost, success, expected
        [InlineData(CardType.Staff, 1, 0, 0, true, 0, 1f, 5)]        // Agency Nurse: 4 + 1 + 2 - 2
        [InlineData(CardType.Staff, 1, 0, 1, true, 0, 1f, 7)]        // Double Shift: 4 + 1 + 2 + 2 - 2
        [InlineData(CardType.Staff, 3, 2, 0, false, 1, 1f, 10)]      // 4 + 9 - 1 - 2 × 1
        [InlineData(CardType.Staff, 3, 2, 0, false, 2, 1f, 8)]       // 4 + 9 - 1 - 2 × 2
        [InlineData(CardType.Staff, 3, 2, 0, false, -1, 1f, 12)]     // a negative Reputation cost is no cost
        [InlineData(CardType.Diagnostic, 2, 0, 0, false, 0, 0.5f, 7)] // Stat Labs: 3 + 4 + 2 - 2
        [InlineData(CardType.Diagnostic, 2, 0, 0, false, 0, 0.99f, 7)]
        [InlineData(CardType.Diagnostic, 2, 0, 0, false, 0, 1f, 9)]   // control: a sure thing pays full price
        [InlineData(CardType.Supply, 1, 1, 0, true, 0, 1f, 2)]        // Sharps Bin: 2 + 1 - 2 = 1, minimum 2
        [InlineData(CardType.Utility, 1, 2, 0, true, 3, 0.5f, 2)]     // every discount at once still floors at 2
        [InlineData(CardType.RedTape, 3, 0, 2, false, 0, 1f, 0)]      // Red Tape is always free
        public void DraftValue_discounts_exhaust_reputation_cost_and_failure_chance(
            CardType type, int tier, int ap, int draw, bool exhausts, int repCost, float success, int expected)
        {
            var card = new CardDef
            {
                Type = type, PowerTier = tier, ApCost = ap, CardsToDraw = draw, ExhaustsOnUse = exhausts,
                ReputationCost = repCost, SuccessChance = success
            };
            Assert.Equal(expected, card.DraftValue);
        }

        // ------------------------------------------------------------------ 10. GameConfig fallbacks

        [Fact]
        public void GameConfig_fallbacks_match_the_game_defaults()
        {
            var c = new GameConfig();
            Assert.Equal(2, c.DiversionReputationPenalty);
            Assert.Equal(5, c.AmbulanceShiftThreshold);
            Assert.Equal(10, c.TraumaFlightShiftThreshold);
            Assert.Equal(2, c.PageCostTier1);
            Assert.Equal(2, c.PageCostTier2);
            Assert.Equal(3, c.PageCostTier3);
        }

        // ------------------------------------------------------------------ 11. focus experiment copy limit

        [Fact]
        public void Focus_purchases_buy_at_most_three_copies_of_one_card()
        {
            var card = new CardDef { Id = "A", Name = "A", Type = CardType.Supply, ApCost = 1, PowerTier = 1 };
            var data = Content(new[] { Diagnose(1) }, Patient(stability: 100));
            data.SupplyPiles.Add(new SupplyPileDef { Card = card, UnlockAt = EscalationType.WalkIn, MaxStock = 10 });
            data.DraftPool.Add(card);
            data.Config.StartingFunding = 200;
            var g = Start(data);
            var policy = new FocusPurchases("A", new[] { "buy:A" });
            var rng = new Random(1);

            for (int draft = 0; draft < 3; draft++)
            {
                g.Apply(GameAction.EndShift);
                Assert.Equal(Phase.Draft, g.Phase);
                for (int guard = 0; guard < 30 && g.Phase == Phase.Draft; guard++)
                    g.Apply(policy.Choose(g, g.LegalActions(), rng));
                Assert.Equal(Phase.Shift, g.Phase);
            }

            Assert.Equal(FocusPurchases.MaxCopiesPerCard, g.OwnedCards.Count(c => c == card));
            Assert.Equal(3, FocusPurchases.MaxCopiesPerCard);
        }
    }
}
