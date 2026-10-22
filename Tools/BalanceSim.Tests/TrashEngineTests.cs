using System.IO;
using System.Linq;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Engine;
using Xunit;
using static ChiefComplaint.Sim.Tests.Fixtures;

namespace ChiefComplaint.Sim.Tests
{
    /// <summary>
    /// Trash effects in the simulator: Sharps Bin (TrashCardEffectSO.redTapeOnly) can only dispose of Red Tape, and an
    /// unrestricted trash card bins Red Tape first, else the cheapest Tier 1 card that isn't a diagnosis or cure,
    /// never another trash card.
    /// </summary>
    public class TrashEngineTests
    {
        private static readonly GameData Project = new ProjectLoader(ProjectLoader.FindProjectRoot(Directory.GetCurrentDirectory())).Load();

        private static CardDef Real(string name) => Project.AllCards.Values.First(c => c.Name == name);

        private static CardDef AnyTrash(string name = "Bin") => new CardDef
        {
            Id = name, Name = name, Type = CardType.Supply, PowerTier = 1, ApCost = 0, ExhaustsOnUse = true,
            Effects = { new TrashEffect { RedTapeOnly = false } }
        };

        // ------------------------------------------------------------------ Sharps Bin (real data)

        [Fact]
        public void Sharps_Bin_is_not_playable_without_Red_Tape_in_hand()
        {
            CardDef sharps = Real("Sharps Bin");
            CardDef doctor = Doctor();
            var g = Start(Content(new[] { sharps, doctor }, Patient(stability: 50)));
            g.DrawPile.Add(Cure(1));
            Assert.DoesNotContain(g.Hand, c => c.Type == CardType.RedTape);

            Assert.DoesNotContain(g.LegalActions(), a => a.Card == sharps);
            Assert.False(g.PlayCard(sharps, null), "Sharps Bin must not bin a non-Red Tape card.");
            Assert.Contains(sharps, g.Hand);
            Assert.Contains(doctor, g.Hand);
            Assert.Single(g.DrawPile);
        }

        [Fact]
        public void Sharps_Bin_disposes_of_Red_Tape_and_draws_one()
        {
            CardDef sharps = Real("Sharps Bin");
            CardDef doctor = Doctor();
            var g = Start(Content(new[] { sharps, doctor }, Patient(stability: 50)));
            CardDef redTape = RedTape();
            g.Hand.Add(redTape);
            CardDef next = Cure(1);
            g.DrawPile.Add(next);
            int ap = g.AP;

            Assert.Contains(g.LegalActions(), a => a.Card == sharps);
            Assert.True(g.PlayCard(sharps, null));

            Assert.DoesNotContain(redTape, g.Hand);
            Assert.DoesNotContain(redTape, g.DiscardPile);
            Assert.Contains(doctor, g.Hand);
            Assert.Contains(next, g.Hand);
            Assert.Empty(g.DrawPile);
            Assert.DoesNotContain(sharps, g.OwnedCards); // exhausts on use
            Assert.Equal(ap, g.AP);                      // costs 0 AP
        }

        // ------------------------------------------------------------------ unrestricted trash

        [Fact]
        public void Unrestricted_trash_prefers_Red_Tape()
        {
            CardDef bin = AnyTrash();
            CardDef doctor = Doctor();
            var g = Start(Content(new[] { bin, doctor }, Patient(stability: 50)));
            CardDef redTape = RedTape();
            g.Hand.Add(redTape);

            Assert.True(g.PlayCard(bin, null));
            Assert.DoesNotContain(redTape, g.Hand);
            Assert.Contains(doctor, g.Hand);
        }

        [Fact]
        public void Unrestricted_trash_never_removes_Initial_Assessment_or_Standard_Care()
        {
            CardDef assessment = Real("Initial Assessment");
            CardDef care = Real("Standard Care");
            Assert.True(assessment.PowerTier == 1 && care.PowerTier == 1, "Fixture: both starters should be Tier 1.");
            CardDef bin = AnyTrash();
            CardDef otherBin = AnyTrash("Other Bin");
            var g = Start(Content(new[] { bin, otherBin, assessment, care }, Patient(stability: 50)));

            Assert.DoesNotContain(g.LegalActions(), a => a.Card == bin);
            Assert.False(g.PlayCard(bin, null), "Only a diagnosis, a cure and another trash card are left: nothing to bin.");
            Assert.Contains(assessment, g.Hand);
            Assert.Contains(care, g.Hand);
            Assert.Contains(otherBin, g.Hand);
        }

        [Fact]
        public void Unrestricted_trash_bins_the_cheapest_other_Tier_1_card_and_spares_the_diagnosis_and_cure()
        {
            CardDef assessment = Real("Initial Assessment");
            CardDef care = Real("Standard Care");
            CardDef bin = AnyTrash();
            CardDef doctor = Doctor();
            var g = Start(Content(new[] { bin, assessment, care, doctor }, Patient(stability: 50)));

            Assert.True(g.PlayCard(bin, null));
            Assert.DoesNotContain(doctor, g.Hand);
            Assert.DoesNotContain(doctor, g.DiscardPile);
            Assert.Contains(assessment, g.Hand);
            Assert.Contains(care, g.Hand);
        }

        // ------------------------------------------------------------------ CardOverrides smoke test

        [Fact]
        public void Card_overrides_set_the_new_balance_fields()
        {
            GameData data = new ProjectLoader(ProjectLoader.FindProjectRoot(Directory.GetCurrentDirectory())).Load();
            string path = Path.Combine(Path.GetTempPath(), "cc-overrides-" + System.Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path, @"{ ""cards"": {
                ""Sharps Bin"": { ""trashRedTapeOnly"": false, ""exhaustsAtShiftEnd"": false },
                ""Agency Nurse"": { ""healAmount"": 2, ""addGainAp"": 1 },
                ""IV Saline"": { ""upgradeTokenAnyColour"": false } } }");
            try
            {
                var log = CardOverrides.Apply(data, path);
                CardDef sharps = data.AllCards.Values.First(c => c.Name == "Sharps Bin");
                CardDef nurse = data.AllCards.Values.First(c => c.Name == "Agency Nurse");
                CardDef saline = data.AllCards.Values.First(c => c.Name == "IV Saline");

                Assert.False(sharps.Effects.OfType<TrashEffect>().Single().RedTapeOnly);
                Assert.False(sharps.ExhaustsAtShiftEnd);
                Assert.Equal(2, nurse.Effects.OfType<HealEffect>().Single().Amount);
                Assert.Equal(1, nurse.Effects.OfType<GainApEffect>().Single().Amount);
                Assert.False(saline.UpgradeTokenAnyColour);
                Assert.Equal(5, log.Count);
                Assert.DoesNotContain(log, l => l.Contains("?"));

                // The shared project load is untouched by this private copy.
                Assert.True(Real("Sharps Bin").Effects.OfType<TrashEffect>().Single().RedTapeOnly);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
