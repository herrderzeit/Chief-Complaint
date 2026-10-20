using ChiefComplaint.Cards.Data;
using ChiefComplaint.Core;
using NUnit.Framework;
using UnityEngine;

namespace ChiefComplaint.Tests.EditMode
{
    /// <summary>Covers the Activity Funding currency on <see cref="GameManager"/>.</summary>
    [Category("Economy")]
    public class FundingTests
    {
        private GameObject go;
        private GameManager gm;

        [SetUp]
        public void Setup()
        {
            go = new GameObject("FundingTestsGameManager");
            gm = go.AddComponent<GameManager>();
        }

        [TearDown]
        public void Teardown() => Object.DestroyImmediate(go);

        [Test]
        public void StartingFunding_IsSix()
        {
            Assert.AreEqual(6, gm.CurrentFunding);
        }

        [TestCase(EscalationType.WalkIn, 2)]
        [TestCase(EscalationType.Ambulance, 5)]
        [TestCase(EscalationType.TraumaFlight, 8)]
        public void GetFundingReward_MatchesEscalationType(EscalationType type, int expected)
        {
            Assert.AreEqual(expected, gm.GetFundingReward(type));
        }

        [Test]
        public void AddFunding_IncreasesBalanceAndRaisesEvent()
        {
            int? raised = null;
            gm.OnFundingChanged += v => raised = v;

            gm.AddFunding(4);

            Assert.AreEqual(10, gm.CurrentFunding);
            Assert.AreEqual(10, raised);
        }

        // starting balance is 6
        [TestCase(0, true, 6)]
        [TestCase(5, true, 1)]
        [TestCase(6, true, 0)]
        [TestCase(7, false, 6)]
        public void TrySpendFunding_RespectsBalance(int cost, bool expectedResult, int expectedBalance)
        {
            bool result = gm.TrySpendFunding(cost);

            Assert.AreEqual(expectedResult, result);
            Assert.AreEqual(expectedBalance, gm.CurrentFunding);
        }

        [Test]
        public void TrySpendFunding_FailureDoesNotRaiseEvent()
        {
            bool raised = false;
            gm.OnFundingChanged += _ => raised = true;

            gm.TrySpendFunding(100);

            Assert.IsFalse(raised);
        }

        [Test]
        public void SpendingAllFunding_NeverAffectsReputationOrEndsTheRun()
        {
            int reputationBefore = gm.CurrentReputation;

            gm.TrySpendFunding(gm.CurrentFunding);

            Assert.AreEqual(0, gm.CurrentFunding);
            Assert.AreEqual(reputationBefore, gm.CurrentReputation);
            Assert.AreNotEqual(GamePhase.GameOver, gm.CurrentPhase);
        }

        [Test]
        public void CureReputationCap_IsSmallerThanTheLowestFundingReward()
        {
            Assert.LessOrEqual(gm.cureReputationCap, gm.walkInFundingReward);
        }
    }
}
