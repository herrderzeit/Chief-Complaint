using System.Linq;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Engine;
using Xunit;
using static ChiefComplaint.Sim.Tests.Fixtures;

namespace ChiefComplaint.Sim.Tests
{
    /// <summary>
    /// Simulator parity for symptom upgrade tokens (GameManager.UpgradeTokens / UpgradeTokenWallet):
    /// earning a token per symptom treated, and paying tokens plus Funding for an upgrade.
    /// </summary>
    public class UpgradeTokenEngineTests
    {
        private const int Trauma = (int)SymptomCategory.Trauma;
        private const int Cardio = (int)SymptomCategory.Cardiopulmonary;
        private const int Infectious = (int)SymptomCategory.Infectious;
        private const int Internal = (int)SymptomCategory.Internal;

        /// <summary>Admits and fully diagnoses the only patient, ready for treatment.</summary>
        private static Patient AdmitAndDiagnose(Game g)
        {
            Patient p = g.Patients.Single();
            g.Apply(Find(g, ActionKind.Admit));
            g.Apply(Find(g, ActionKind.PlayCard, a => a.Patient == p && a.Card.Type == CardType.Diagnostic));
            Assert.True(p.IsFullyDiagnosed);
            return p;
        }

        /// <summary>A Tier 1 → Tier 2 diagnostic line with the given upgrade costs.</summary>
        private static (CardDef t1, CardDef t2) UpgradeLine(int funding, int tokens, bool anyColour = false,
            SymptomCategory colour = SymptomCategory.Trauma)
        {
            CardDef t2 = Diagnose(2);
            CardDef t1 = Diagnose(1);
            t1.Upgrade = t2;
            t1.UpgradeCost = funding;
            t1.UpgradeTokenCost = tokens;
            t1.UpgradeTokenAnyColour = anyColour;
            t1.UpgradeTokenColour = colour;
            return (t1, t2);
        }

        /// <summary>Starts a run owning two copies of <paramref name="t1"/> and ends the first shift into the draft.</summary>
        private static Game StartInDraft(CardDef t1, int startingFunding)
        {
            var data = Content(new[] { t1, t1 }, Patient(stability: 50));
            data.Config.StartingFunding = startingFunding;
            var g = Start(data);
            g.Apply(GameAction.EndShift);
            Assert.Equal(Phase.Draft, g.Phase);
            return g;
        }

        [Theory]
        [InlineData(SymptomCategory.Trauma)]
        [InlineData(SymptomCategory.Cardiopulmonary)]
        [InlineData(SymptomCategory.Infectious)]
        [InlineData(SymptomCategory.Internal)]
        public void Treating_with_a_card_earns_one_token_of_the_departments_colour(SymptomCategory department)
        {
            var g = Start(Content(new[] { Diagnose(1), Cure(1) }, Patient(categories: department)));
            AdmitAndDiagnose(g);

            g.Apply(Find(g, ActionKind.Treat, a => !a.Page && a.Department == department));

            for (int i = 0; i < 4; i++)
                Assert.Equal(i == (int)department ? 1 : 0, g.UpgradeTokens[i]);
        }

        [Fact]
        public void Paging_the_on_call_team_also_earns_a_token()
        {
            var data = Content(new[] { Diagnose(1) }, Patient(categories: SymptomCategory.Infectious));
            data.Config.BaseAP = 5;
            var g = Start(data);
            AdmitAndDiagnose(g);

            g.Apply(Find(g, ActionKind.Treat, a => a.Page));

            Assert.Equal(1, g.UpgradeTokens[Infectious]);
            Assert.Equal(1, g.UpgradeTokens.Sum());
        }

        [Fact]
        public void A_treatment_that_fails_its_RNG_earns_no_token()
        {
            CardDef unreliable = Cure(1);
            unreliable.SuccessChance = 0f; // always fails
            var g = Start(Content(new[] { Diagnose(1), unreliable }, Patient(categories: SymptomCategory.Trauma)));
            Patient p = AdmitAndDiagnose(g);

            g.Apply(Find(g, ActionKind.Treat, a => !a.Page));

            Assert.Single(p.Symptoms);
            Assert.Equal(0, g.UpgradeTokens.Sum());
        }

        [Fact]
        public void Upgrade_with_a_token_cost_is_not_offered_without_tokens()
        {
            var (t1, _) = UpgradeLine(funding: 3, tokens: 1, colour: SymptomCategory.Trauma);
            var g = StartInDraft(t1, startingFunding: 10);
            g.UpgradeTokens[Cardio] = 5; // plenty of the wrong colour

            Assert.Null(Find(g, ActionKind.Upgrade));
        }

        [Fact]
        public void Upgrade_with_a_token_cost_is_refused_without_tokens_and_charges_nothing()
        {
            var (t1, t2) = UpgradeLine(funding: 3, tokens: 1, colour: SymptomCategory.Trauma);
            var g = StartInDraft(t1, startingFunding: 10);
            int funding = g.Funding;

            g.Apply(new GameAction { Kind = ActionKind.Upgrade, Card = t1 });

            Assert.Equal(funding, g.Funding);
            Assert.Equal(2, g.OwnedCards.Count(c => c == t1));
            Assert.Equal(0, g.OwnedCards.Count(c => c == t2));
        }

        [Fact]
        public void Upgrade_with_tokens_but_short_on_Funding_is_not_offered_and_keeps_the_tokens()
        {
            var (t1, _) = UpgradeLine(funding: 5, tokens: 2, colour: SymptomCategory.Internal);
            var g = StartInDraft(t1, startingFunding: 4);
            g.UpgradeTokens[Internal] = 2;

            Assert.Null(Find(g, ActionKind.Upgrade));
            g.Apply(new GameAction { Kind = ActionKind.Upgrade, Card = t1 });

            Assert.Equal(2, g.UpgradeTokens[Internal]);
            Assert.Equal(4, g.Funding);
        }

        [Fact]
        public void Upgrade_with_matching_tokens_pays_tokens_and_Funding()
        {
            var (t1, t2) = UpgradeLine(funding: 5, tokens: 2, colour: SymptomCategory.Internal);
            var g = StartInDraft(t1, startingFunding: 6);
            g.UpgradeTokens[Internal] = 3;
            g.UpgradeTokens[Trauma] = 4;

            GameAction upgrade = Find(g, ActionKind.Upgrade);
            Assert.NotNull(upgrade);
            g.Apply(upgrade);

            Assert.Equal(1, g.Funding);
            Assert.Equal(1, g.UpgradeTokens[Internal]);
            Assert.Equal(4, g.UpgradeTokens[Trauma]);
            Assert.Equal(1, g.OwnedCards.Count(c => c == t1));
            Assert.Equal(1, g.OwnedCards.Count(c => c == t2));
        }

        [Fact]
        public void Any_colour_cost_is_paid_from_the_most_plentiful_colour()
        {
            var (t1, _) = UpgradeLine(funding: 3, tokens: 2, anyColour: true);
            var g = StartInDraft(t1, startingFunding: 6);
            g.UpgradeTokens[Trauma] = 1;
            g.UpgradeTokens[Cardio] = 4;
            g.UpgradeTokens[Infectious] = 0;
            g.UpgradeTokens[Internal] = 2;

            g.Apply(Find(g, ActionKind.Upgrade));

            Assert.Equal(new[] { 1, 2, 0, 2 }, g.UpgradeTokens);
            Assert.Equal(3, g.Funding);
        }

        [Fact]
        public void Any_colour_cost_spreads_across_colours_as_the_largest_pile_shrinks()
        {
            var (t1, _) = UpgradeLine(funding: 0, tokens: 2, anyColour: true);
            var g = StartInDraft(t1, startingFunding: 0);
            g.UpgradeTokens[Trauma] = 1;
            g.UpgradeTokens[Internal] = 2;

            g.Apply(Find(g, ActionKind.Upgrade));

            // Internal 2 → 1 (largest), then tie Trauma 1 / Internal 1 goes to the lowest index (Trauma).
            Assert.Equal(new[] { 0, 0, 0, 1 }, g.UpgradeTokens);
        }

        [Fact]
        public void A_card_with_no_token_cost_upgrades_on_Funding_alone()
        {
            var (t1, t2) = UpgradeLine(funding: 3, tokens: 0);
            var g = StartInDraft(t1, startingFunding: 3);
            Assert.Equal(0, g.UpgradeTokens.Sum());

            GameAction upgrade = Find(g, ActionKind.Upgrade);
            Assert.NotNull(upgrade);
            g.Apply(upgrade);

            Assert.Equal(0, g.Funding);
            Assert.Equal(1, g.OwnedCards.Count(c => c == t2));
        }
    }
}
