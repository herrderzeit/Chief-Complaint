using ChiefComplaint.Cards.Data;
using ChiefComplaint.Core;
using NUnit.Framework;

namespace ChiefComplaint.Tests
{
    /// <summary>"Page the on-call team" AP costs by symptom tier (<see cref="PagingRules.ApCost"/>), mirrored by BalanceSim's PageCostTier1-3.</summary>
    [TestFixture]
    [Category("Economy")]
    public class PagingRulesTests
    {
        [TestCase(SymptomTier.Tier1, 2)]
        [TestCase(SymptomTier.Tier2, 3)]
        [TestCase(SymptomTier.Tier3, 3)]
        public void ApCost_ByTier(SymptomTier tier, int expected)
        {
            Assert.AreEqual(expected, PagingRules.ApCost(tier), $"Paging cost for {tier}.");
        }

        [TestCase(SymptomTier.Tier1)]
        [TestCase(SymptomTier.Tier2)]
        [TestCase(SymptomTier.Tier3)]
        public void ApCost_NeverExceedsAShiftsBaseAP(SymptomTier tier)
        {
            Assert.LessOrEqual(PagingRules.ApCost(tier), 3, $"Paging a {tier} symptom must fit in one shift's base AP (3).");
        }

        [Test]
        public void ApCost_NeverFallsAsTheTierRises()
        {
            Assert.LessOrEqual(PagingRules.ApCost(SymptomTier.Tier1), PagingRules.ApCost(SymptomTier.Tier2));
            Assert.LessOrEqual(PagingRules.ApCost(SymptomTier.Tier2), PagingRules.ApCost(SymptomTier.Tier3));
        }
    }
}
