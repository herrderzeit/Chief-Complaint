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

        // The rule: each escalation type pays its own configured reward. Distinct sentinel values
        // catch a type being mapped to the wrong field, whatever the tuning happens to be.
        [TestCase(EscalationType.WalkIn, 11)]
        [TestCase(EscalationType.Ambulance, 22)]
        [TestCase(EscalationType.TraumaFlight, 33)]
        public void GetFundingReward_UsesTheConfiguredRewardForEachType(EscalationType type, int expected)
        {
            gm.walkInFundingReward = 11;
            gm.ambulanceFundingReward = 22;
            gm.traumaFlightFundingReward = 33;

            Assert.AreEqual(expected, gm.GetFundingReward(type), $"{type} should pay its own configured Funding reward.");
        }

        // Pins the decided "option A" tuning (component defaults; the MainBoard scene is checked by the
        // BalanceSim ProjectDataTests). Update these numbers deliberately when the tuning changes.
        [TestCase(EscalationType.WalkIn, 3)]
        [TestCase(EscalationType.Ambulance, 6)]
        [TestCase(EscalationType.TraumaFlight, 8)]
        public void GetFundingReward_DefaultTuning(EscalationType type, int expected)
        {
            Assert.AreEqual(expected, gm.GetFundingReward(type));
        }

        [Test]
        public void StartingReputation_DefaultTuning_IsSeven()
        {
            Assert.AreEqual(7, gm.CurrentReputation, "Final balance pass: a run starts on 7 Reputation.");
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
        public void EnterDraftPhase_OnFinalShift_WinsTheRun()
        {
            gm.shiftsToWin = 1; // the run starts on shift 1

            gm.EnterDraftPhase();

            Assert.AreEqual(GamePhase.GameOver, gm.CurrentPhase, "Ending the final shift should end the run as a win.");
        }

        [Test]
        public void EnterDraftPhase_BeforeFinalShift_OpensDraft()
        {
            gm.shiftsToWin = 15;

            gm.EnterDraftPhase();

            Assert.AreEqual(GamePhase.Draft, gm.CurrentPhase);
        }

        [Test]
        public void CureReputationCap_NeverExceedsFundingForHarderPatients()
        {
            Assert.LessOrEqual(gm.cureReputationCap, gm.ambulanceFundingReward);
            Assert.LessOrEqual(gm.cureReputationCap, gm.traumaFlightFundingReward);
        }
    }
}
