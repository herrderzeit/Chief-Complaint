using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using ChiefComplaint.Core;

namespace ChiefComplaint.Tests.EditMode
{
    /// <summary>
    /// Covers the AP / Reputation economy, phase transitions and Game Over guard of <see cref="GameManager"/>.
    /// Awake/Start do not run in EditMode, so only the public economy and phase API is exercised.
    /// </summary>
    public class GameManagerTests
    {
        private GameObject go;
        private GameManager gm;

        [SetUp]
        public void Setup()
        {
            go = new GameObject("GameManagerUnderTest");
            gm = go.AddComponent<GameManager>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ResetAP_RestoresBaseAPAndRaisesEvent()
        {
            int? raised = null;
            gm.OnAPChanged += v => raised = v;

            gm.ResetAP();

            Assert.AreEqual(gm.baseAP, gm.CurrentAP);
            Assert.AreEqual(gm.baseAP, raised);
        }

        [Test]
        public void SpendAP_NeverGoesBelowZero()
        {
            gm.ResetAP();
            gm.SpendAP(gm.baseAP + 100);
            Assert.AreEqual(0, gm.CurrentAP);
        }

        [Test]
        public void GainAP_AddsToCurrentAP()
        {
            gm.ResetAP();
            gm.GainAP(2);
            Assert.AreEqual(gm.baseAP + 2, gm.CurrentAP);
        }

        [Test]
        public void AddReputation_IncreasesAndRaisesEvent()
        {
            int start = gm.CurrentReputation;
            int? raised = null;
            gm.OnReputationChanged += v => raised = v;

            gm.AddReputation(3);

            Assert.AreEqual(start + 3, gm.CurrentReputation);
            Assert.AreEqual(start + 3, raised);
        }

        [Test]
        public void SpendReputation_AboveZero_DoesNotEndTheRun()
        {
            gm.SpendReputation(1);
            Assert.AreNotEqual(GamePhase.GameOver, gm.CurrentPhase);
        }

        [Test]
        public void SpendReputation_ToZero_ClampsAndTriggersGameOver()
        {
            gm.SpendReputation(gm.CurrentReputation + 50);

            Assert.AreEqual(0, gm.CurrentReputation, "Reputation must clamp at zero.");
            Assert.AreEqual(GamePhase.GameOver, gm.CurrentPhase);
            Assert.IsFalse(gm.CanInteract, "The board must lock once the run is over.");
        }

        [Test]
        public void SpendReputation_BelowZero_NeverRaisesANegativeValue()
        {
            var raised = new List<int>();
            gm.OnReputationChanged += v => raised.Add(v);

            gm.SpendReputation(gm.CurrentReputation + 50);

            CollectionAssert.AreEqual(new[] { 0 }, raised, "Listeners must see one clamped value, never a negative one.");
        }

        [Test]
        public void TriggerGameOver_IsIdempotent()
        {
            int phaseChanges = 0;
            gm.OnPhaseChanged += _ => phaseChanges++;

            gm.TriggerGameOver();
            gm.TriggerGameOver();

            Assert.AreEqual(1, phaseChanges, "Game Over must only be announced once per run.");
        }

        [Test]
        public void StartNewShift_AdvancesShiftAndRestoresAP()
        {
            gm.ResetAP();
            gm.SpendAP(gm.baseAP);
            int shiftBefore = gm.CurrentShift;

            gm.StartNewShift();

            Assert.AreEqual(shiftBefore + 1, gm.CurrentShift);
            Assert.AreEqual(gm.baseAP, gm.CurrentAP);
            Assert.AreEqual(GamePhase.Shift, gm.CurrentPhase);
        }

        [Test]
        public void StartNewShift_AfterGameOver_IsIgnored()
        {
            gm.TriggerGameOver();
            int shiftBefore = gm.CurrentShift;

            gm.StartNewShift();

            Assert.AreEqual(shiftBefore, gm.CurrentShift);
            Assert.AreEqual(GamePhase.GameOver, gm.CurrentPhase);
        }

        [Test]
        public void EnterDraftPhase_LocksBoardAndOnlyWorksFromShift()
        {
            gm.EnterDraftPhase();
            Assert.AreEqual(GamePhase.Draft, gm.CurrentPhase);
            Assert.IsFalse(gm.CanInteract);

            // A second call (already in Draft) must not change anything or raise another event.
            int phaseChanges = 0;
            gm.OnPhaseChanged += _ => phaseChanges++;
            gm.EnterDraftPhase();
            Assert.AreEqual(0, phaseChanges);
        }

        [Test]
        public void TutorialCompletedKey_IsStable()
        {
            // Guards against accidental renames that would silently re-show the tutorial to existing players.
            Assert.AreEqual("TutorialCompleted", GameManager.TutorialCompletedKey);
        }
    }
}
