using System.Linq;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Engine;
using Xunit;
using static ChiefComplaint.Sim.Tests.Fixtures;

namespace ChiefComplaint.Sim.Tests
{
    /// <summary>
    /// Simulator parity for the supply-room change: staged supply piles, the Reputation-cost gate,
    /// paramedic handover patients and Team Assessment needing a Ward bed.
    /// </summary>
    public class SupplyRoomEngineTests
    {
        private static CardDef Stock(string name) => new CardDef { Id = name, Name = name, Type = CardType.Supply, ApCost = 1, PowerTier = 1 };

        private static GameData SupplyContent(CardDef walkIn, CardDef ambulance, params PatientDef[] patients)
        {
            var data = Content(new[] { Diagnose(1), Cure(1) }, patients);
            data.SupplyPiles.Add(new SupplyPileDef { Card = walkIn, UnlockAt = EscalationType.WalkIn, MaxStock = 2 });
            data.SupplyPiles.Add(new SupplyPileDef { Card = ambulance, UnlockAt = EscalationType.Ambulance, MaxStock = 3 });
            data.DraftPool.Add(walkIn);
            data.DraftPool.Add(ambulance);
            data.Config.StartingFunding = 100;
            return data;
        }

        private static GameAction BuyAction(Game g, int slot) => Find(g, ActionKind.Buy, a => a.MarketSlot == slot);

        // ------------------------------------------------------------------ supply room

        [Fact]
        public void First_draft_unlocks_walk_in_piles_full_and_keeps_ambulance_piles_locked()
        {
            CardDef a = Stock("A"), b = Stock("B");
            var g = Start(SupplyContent(a, b, Patient(stability: 50)));
            g.Apply(GameAction.EndShift);

            Assert.Equal(Phase.Draft, g.Phase);
            Assert.True(g.PileUnlocked[0]);
            Assert.Equal(2, g.PileStock[0]);
            Assert.False(g.PileUnlocked[1]);
            Assert.Equal(0, g.PileStock[1]);
            Assert.NotNull(BuyAction(g, 0));
            Assert.Null(BuyAction(g, 1)); // locked piles can't be bought
        }

        [Fact]
        public void Buying_decrements_stock_and_charges_funding_until_the_pile_is_empty()
        {
            CardDef a = Stock("A"), b = Stock("B");
            var g = Start(SupplyContent(a, b, Patient(stability: 50)));
            g.Apply(GameAction.EndShift);
            int funding = g.Funding;

            g.Apply(BuyAction(g, 0));
            Assert.Equal(1, g.PileStock[0]);
            Assert.Equal(funding - a.DraftValue, g.Funding);

            g.Apply(BuyAction(g, 0));
            Assert.Equal(0, g.PileStock[0]);
            Assert.Null(BuyAction(g, 0)); // empty pile is no longer offered

            int before = g.Funding;
            g.Apply(new GameAction { Kind = ActionKind.Buy, MarketSlot = 0 }); // forced buy on an empty pile
            Assert.Equal(0, g.PileStock[0]);
            Assert.Equal(before, g.Funding);

            g.Apply(GameAction.FinishDraft);
            Assert.Equal(2, g.OwnedCards.Count(c => c == a)); // purchases join the deck for the next shift
        }

        [Fact]
        public void Open_piles_restock_one_per_draft_capped_at_max()
        {
            CardDef a = Stock("A"), b = Stock("B");
            var g = Start(SupplyContent(a, b, Patient(stability: 50)));
            g.Apply(GameAction.EndShift);
            g.Apply(BuyAction(g, 0));
            g.Apply(BuyAction(g, 0));
            Assert.Equal(0, g.PileStock[0]);

            int[] expected = { 1, 2, 2 };
            foreach (int stock in expected)
            {
                g.Apply(GameAction.FinishDraft);
                g.Apply(GameAction.EndShift);
                Assert.Equal(Phase.Draft, g.Phase);
                Assert.Equal(stock, g.PileStock[0]);
                Assert.False(g.PileUnlocked[1]);
                Assert.Equal(0, g.PileStock[1]); // locked piles never restock
            }
        }

        [Fact]
        public void Reaching_ambulances_unlocks_ambulance_piles_full()
        {
            CardDef a = Stock("A"), b = Stock("B");
            var data = SupplyContent(a, b, Patient(), Patient(), Patient());
            data.AmbulancePool.Add(Patient(EscalationType.Ambulance, 3, 7, 2));
            data.Config.BaseAP = 10;
            data.Config.AmbulanceCureThreshold = 1;
            var g = Start(data);
            Patient p = g.Patients[0];
            g.Apply(new GameAction { Kind = ActionKind.Admit, Patient = p });
            g.PlayCard(g.Hand.First(c => c.Name.StartsWith("Diagnose")), p);
            g.Apply(Find(g, ActionKind.Treat, x => x.Patient == p && !x.Page));

            g.Apply(GameAction.EndShift);

            Assert.True(g.AmbulancesUnlocked);
            Assert.Equal(Phase.Draft, g.Phase);
            Assert.True(g.PileUnlocked[1]);
            Assert.Equal(3, g.PileStock[1]);
            Assert.NotNull(BuyAction(g, 1));
        }

        // ------------------------------------------------------------------ Reputation cost

        private static CardDef CostlyDiagnose()
        {
            CardDef card = Diagnose(1);
            card.Id = card.Name = "Costly Diagnose";
            card.ReputationCost = 2;
            return card;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void Reputation_cost_card_is_refused_at_or_below_its_cost(int reputation)
        {
            CardDef card = CostlyDiagnose();
            var data = Content(new[] { card }, Patient());
            data.Config.StartingReputation = reputation;
            var g = Start(data);
            Patient p = g.Patients.Single();
            g.Apply(Find(g, ActionKind.Admit));

            Assert.DoesNotContain(g.LegalActions(), x => x.Card == card);
            Assert.False(g.PlayCard(card, p));
            Assert.Contains(card, g.Hand);
            Assert.Equal(reputation, g.Reputation);
            Assert.Equal(0, p.DiagnosedCount);
        }

        [Fact]
        public void Reputation_cost_is_only_charged_when_the_card_lands()
        {
            CardDef card = CostlyDiagnose();
            var data = Content(new[] { card }, Patient());
            data.Config.StartingReputation = 3;
            var g = Start(data);
            Patient p = g.Patients.Single();
            int ap = g.AP;

            Assert.False(g.PlayCard(card, p)); // patient still in Triage: no effect lands
            Assert.Equal(3, g.Reputation);
            Assert.Contains(card, g.Hand);

            g.Apply(Find(g, ActionKind.Admit));
            Assert.True(g.PlayCard(card, p));
            Assert.Equal(1, g.Reputation);
            Assert.Equal(1, p.DiagnosedCount);
            Assert.Equal(ap - card.ApCost, g.AP);
            Assert.Equal(Phase.Shift, g.Phase);
        }

        // ------------------------------------------------------------------ handover

        [Fact]
        public void Handover_patient_spawns_with_primary_symptom_first_and_diagnosed()
        {
            PatientDef def = Patient(EscalationType.Ambulance, 2, 10, 1, SymptomCategory.Trauma, SymptomCategory.Internal);
            def.ArrivesWithHandover = true;
            var g = Start(Content(new CardDef[0], def));
            Patient p = g.Patients.Single();

            Assert.Equal(2, p.Symptoms.Count);
            Assert.Equal(SymptomCategory.Trauma, p.Symptoms[0].Category);
            Assert.Equal(2, p.Symptoms[0].Tier);
            Assert.Equal(SymptomCategory.Internal, p.Symptoms[1].Category);
            Assert.Equal(1, p.Symptoms[1].Tier);
            Assert.Equal(1, p.DiagnosedCount);
            Assert.False(p.IsFullyDiagnosed);
        }

        [Fact]
        public void Patient_without_handover_spawns_sorted_lowest_tier_first_and_undiagnosed()
        {
            PatientDef def = Patient(EscalationType.Ambulance, 2, 10, 1, SymptomCategory.Trauma, SymptomCategory.Internal);
            var g = Start(Content(new CardDef[0], def));
            Patient p = g.Patients.Single();

            Assert.Equal(SymptomCategory.Internal, p.Symptoms[0].Category);
            Assert.Equal(SymptomCategory.Trauma, p.Symptoms[1].Category);
            Assert.Equal(0, p.DiagnosedCount);
        }

        // ------------------------------------------------------------------ Team Assessment

        [Fact]
        public void Team_assessment_is_wasted_when_the_only_patient_is_in_triage()
        {
            var data = Content(new[] { Doctor(), Nurse() }, Patient(EscalationType.WalkIn, 5, 10, 1, SymptomCategory.Trauma, SymptomCategory.Internal));
            var g = Start(data);
            Patient p = g.Patients.Single();

            g.PlayCard(g.Hand.First(c => c.HasTag(CardTag.Doctor)), null);
            g.PlayCard(g.Hand.First(c => c.HasTag(CardTag.Nurse)), null);

            Assert.Equal(Phase.Shift, g.Phase);
            Assert.True(g.SynergyUsedThisShift);
            Assert.Equal(0, p.DiagnosedCount);
        }

        [Fact]
        public void Team_assessment_offers_only_ward_patients()
        {
            var two = Patient(EscalationType.WalkIn, 5, 10, 1, SymptomCategory.Trauma, SymptomCategory.Internal);
            var data = Content(new[] { Doctor(), Nurse() }, two, two);
            var g = Start(data);
            Patient ward = g.Patients[0], triage = g.Patients[1];
            g.Apply(new GameAction { Kind = ActionKind.Admit, Patient = ward });

            g.PlayCard(g.Hand.First(c => c.HasTag(CardTag.Doctor)), null);
            g.PlayCard(g.Hand.First(c => c.HasTag(CardTag.Nurse)), null);
            Assert.Equal(Phase.TeamAssessment, g.Phase);

            var offered = g.LegalActions().Where(a => a.Kind == ActionKind.TeamAssess).Select(a => a.Patient).ToList();
            Assert.Equal(new[] { ward }, offered);

            g.Apply(new GameAction { Kind = ActionKind.TeamAssess, Patient = triage }); // forced pick on a Triage patient
            Assert.Equal(0, triage.DiagnosedCount);
            Assert.Equal(Phase.Shift, g.Phase);
        }
    }
}
