using ChiefComplaint.Cards.Data;
using ChiefComplaint.Core;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        // Pins the decided tuning (component default; the MainBoard scene value is pinned below).
        private const int DecidedStartingFunding = 10;

        [Test]
        public void StartingFunding_IsTen()
        {
            Assert.AreEqual(DecidedStartingFunding, gm.CurrentFunding, "Decided tuning: a run starts with 10 Funding (was 6).");
        }

        [Test]
        public void StartingFunding_SceneValue_IsTen()
        {
            const string scenePath = "Assets/Scenes/MainBoard.unity";
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var sceneGm = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameManager>(true)).First();
                Assert.AreEqual(DecidedStartingFunding, sceneGm.CurrentFunding, "MainBoard's GameManager should start with 10 Funding.");
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
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
            int start = gm.CurrentFunding;

            gm.AddFunding(4);

            Assert.AreEqual(start + 4, gm.CurrentFunding);
            Assert.AreEqual(start + 4, raised);
        }

        // Rule test, independent of the starting tuning: the cost is given relative to the starting balance
        // (null = free), so every boundary (free, just under, exactly, one over) is covered whatever it is.
        [TestCase(null, true, Description = "A free purchase always succeeds")]
        [TestCase(-1, true, Description = "One under the balance")]
        [TestCase(0, true, Description = "Exactly the balance: spends down to 0")]
        [TestCase(1, false, Description = "One over the balance: refused, nothing taken")]
        public void TrySpendFunding_RespectsBalance(int? offsetFromBalance, bool expectedResult)
        {
            int start = gm.CurrentFunding;
            Assume.That(start, Is.GreaterThan(1), "Fixture needs a starting balance above 1.");
            int cost = offsetFromBalance.HasValue ? start + offsetFromBalance.Value : 0;

            bool result = gm.TrySpendFunding(cost);

            Assert.AreEqual(expectedResult, result, $"TrySpendFunding({cost}) with a balance of {start}");
            Assert.AreEqual(expectedResult ? start - cost : start, gm.CurrentFunding);
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
