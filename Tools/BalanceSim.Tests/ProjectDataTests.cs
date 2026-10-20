using System.IO;
using System.Linq;
using ChiefComplaint.Sim.Bots;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Stats;
using Xunit;

namespace ChiefComplaint.Sim.Tests
{
    /// <summary>Loads the real Unity project this repo contains and checks the content reached the simulator intact.</summary>
    public class ProjectDataTests
    {
        private static readonly GameData Data = new ProjectLoader(ProjectLoader.FindProjectRoot(Directory.GetCurrentDirectory())).Load();

        [Fact]
        public void Loads_every_pool_and_deck_from_the_scene()
        {
            Assert.Equal(10, Data.StartingDeck.Count);
            Assert.NotEmpty(Data.DraftPool);
            Assert.NotEmpty(Data.RedTapePool);
            Assert.NotEmpty(Data.WalkInPool);
            Assert.NotEmpty(Data.AmbulancePool);
            Assert.NotEmpty(Data.TraumaFlightPool);
            Assert.Equal(4, Data.SymptomDecks.Count);
            Assert.All(Data.SymptomDecks.Values, d => Assert.NotEmpty(d));
        }

        [Fact]
        public void Every_card_effect_is_understood()
        {
            Assert.DoesNotContain(Data.AllCards.Values.SelectMany(c => c.Effects), e => e is UnknownEffect);
            Assert.DoesNotContain(Data.Warnings, w => w.Contains("not simulated"));
        }

        [Fact]
        public void Starter_cards_load_with_their_effects_and_upgrades()
        {
            CardDef student = Data.StartingDeck.First(c => c.Name == "Medical Student");
            Assert.Equal(0, student.ApCost);
            Assert.Contains(CardTag.Doctor, student.Tags);
            Assert.Equal(1, student.Effects.OfType<GainApEffect>().Single().Amount);
            Assert.NotNull(student.Upgrade);

            CardDef assessment = Data.StartingDeck.First(c => c.Name == "Initial Assessment");
            Assert.Equal(1, assessment.Effects.OfType<DiagnoseEffect>().Single().MaxTier);
        }

        [Fact]
        public void Patients_have_categories_and_vitals()
        {
            foreach (PatientDef p in Data.WalkInPool.Concat(Data.AmbulancePool).Concat(Data.TraumaFlightPool))
            {
                Assert.InRange(p.Categories.Count, 1, 3);
                Assert.True(p.StartingStability > 0, p.Name);
                Assert.True(p.DecayPerTurn > 0, p.Name);
            }
        }

        [Fact]
        public void Runs_are_reproducible_from_the_seed()
        {
            RunRecord a = Runner.RunOne(Data, Data.Config, Program.Bot("smart"), 42);
            RunRecord b = Runner.RunOne(Data, Data.Config, Program.Bot("smart"), 42);
            Assert.Equal(a.Won, b.Won);
            Assert.Equal(a.Steps, b.Steps);
            Assert.Equal(a.Shifts.Select(s => s.Reputation), b.Shifts.Select(s => s.Reputation));
        }

        [Theory]
        [InlineData("random")]
        [InlineData("nobuy")]
        [InlineData("randombuy")]
        [InlineData("smart")]
        [InlineData("stall")]
        public void Every_bot_finishes_its_runs(string bot)
        {
            RunRecord[] runs = Runner.RunBatch(Data, Data.Config, Program.Bot(bot), 50, 1);
            Assert.All(runs, r => Assert.True(r.Won || r.LastShift <= Data.Config.ShiftsToWin));
            Assert.All(runs, r => Assert.True(r.Steps < Runner.MaxStepsPerShift * Data.Config.ShiftsToWin * 3));
        }
    }
}
