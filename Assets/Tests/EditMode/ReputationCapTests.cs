using System.Linq;
using ChiefComplaint.Core;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChiefComplaint.Tests.EditMode
{
    /// <summary>
    /// Covers the Reputation cap: <see cref="GameManager.ApplyReputationCap"/> (pure maths), the clamp inside
    /// <see cref="GameManager.AddReputation"/>, and the decided tuning of <see cref="GameManager.maxReputation"/>
    /// (component default and the MainBoard scene value, both 14), plus the starting Reputation (7) and the
    /// diversion penalty (3) from the final balance pass, and the cure Reputation cap (2, component default and scene).
    /// </summary>
    [Category("Economy")]
    public class ReputationCapTests
    {
        private const string ScenePath = "Assets/Scenes/MainBoard.unity";
        private const int DecidedCap = 14;
        private const int DecidedStartingReputation = 7;
        private const int DecidedDiversionPenalty = 3;
        private const int DecidedCureReputationCap = 2;

        // reputation after the gain, cap, reputation before the gain -> expected
        [TestCase(30, 0, 10, 30, Description = "Cap 0 is off: no clamp at all")]
        [TestCase(12, 14, 10, 12, Description = "Under the cap: unchanged")]
        [TestCase(14, 14, 12, 14, Description = "Landing exactly on the cap: unchanged")]
        [TestCase(17, 14, 12, 14, Description = "Crossing the cap: clamped to the cap")]
        [TestCase(15, 14, 14, 14, Description = "Already at the cap: a gain is lost")]
        [TestCase(21, 14, 20, 20, Description = "Already above the cap (lowered mid-run): stays, never reduced")]
        [TestCase(20, 14, 20, 20, Description = "Above the cap, zero gain: stays")]
        [TestCase(-3, 14, 2, -3, Description = "Below the cap the value passes through untouched")]
        public void ApplyReputationCap_Cases(int reputation, int cap, int before, int expected)
        {
            Assert.AreEqual(expected, GameManager.ApplyReputationCap(reputation, cap, before),
                $"ApplyReputationCap({reputation}, cap {cap}, before {before})");
        }

        private static GameManager NewGameManager(out GameObject go)
        {
            go = new GameObject("ReputationCapTestsGameManager");
            return go.AddComponent<GameManager>();
        }

        [Test]
        public void MaxReputation_ComponentDefault_IsFourteen()
        {
            var gm = NewGameManager(out var go);
            try
            {
                Assert.AreEqual(DecidedCap, gm.maxReputation, "Decided tuning: Reputation caps at 14.");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void MaxReputation_SceneValue_IsFourteen()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var gm = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameManager>(true)).First();
                Assert.AreEqual(DecidedCap, gm.maxReputation, "MainBoard's GameManager.maxReputation should be 14.");
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void StartingReputationAndDiversionPenalty_ComponentDefaults()
        {
            var gm = NewGameManager(out var go);
            try
            {
                Assert.AreEqual(DecidedStartingReputation, gm.CurrentReputation, "Decided tuning: a run starts on 7 Reputation.");
                Assert.AreEqual(DecidedDiversionPenalty, gm.DiversionReputationPenalty, "Decided tuning: a diverted patient costs 3 Reputation.");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void StartingReputationAndDiversionPenalty_SceneValues()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var gm = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameManager>(true)).First();
                Assert.AreEqual(DecidedStartingReputation, gm.CurrentReputation, "MainBoard's GameManager should start on 7 Reputation.");
                Assert.AreEqual(DecidedDiversionPenalty, gm.DiversionReputationPenalty, "MainBoard's diversion penalty should be 3.");
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }

        // starting Reputation (7 by default), gain, cap -> expected
        [TestCase(10, 14, 14, Description = "7 + 10 clamps to 14")]
        [TestCase(7, 14, 14, Description = "7 + 7 lands exactly on 14")]
        [TestCase(3, 14, 10, Description = "Under the cap is unaffected")]
        [TestCase(50, 0, 57, Description = "0 disables the cap")]
        [TestCase(5, 10, 10, Description = "A lower cap clamps too")]
        public void AddReputation_ClampsAtMaxReputation(int gain, int cap, int expected)
        {
            var gm = NewGameManager(out var go);
            try
            {
                gm.maxReputation = cap;
                int start = gm.CurrentReputation;
                Assume.That(start, Is.EqualTo(DecidedStartingReputation), "Fixture assumes the default starting Reputation of 7.");
                int? raised = null;
                gm.OnReputationChanged += v => raised = v;

                gm.AddReputation(gain);

                Assert.AreEqual(expected, gm.CurrentReputation);
                Assert.AreEqual(expected, raised, "OnReputationChanged should report the clamped total.");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void AddReputation_RepeatedGainsNeverPassTheCap()
        {
            var gm = NewGameManager(out var go);
            try
            {
                gm.maxReputation = DecidedCap;
                for (int i = 0; i < 20; i++)
                {
                    gm.AddReputation(3);
                    Assert.LessOrEqual(gm.CurrentReputation, DecidedCap, $"after gain #{i + 1}");
                }
                Assert.AreEqual(DecidedCap, gm.CurrentReputation);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void AddReputation_AboveCapAfterCapLowered_IsNotReduced()
        {
            var gm = NewGameManager(out var go);
            try
            {
                gm.maxReputation = 0;
                gm.AddReputation(12); // well above the cap, uncapped
                int aboveCap = gm.CurrentReputation;
                Assert.Greater(aboveCap, DecidedCap, "Fixture: Reputation must start above the cap for this test.");
                gm.maxReputation = DecidedCap;

                gm.AddReputation(2);

                Assert.AreEqual(aboveCap, gm.CurrentReputation, "Lowering the cap mid-run must not take Reputation away on the next gain.");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void SpendReputation_AtTheCap_StillDeducts()
        {
            var gm = NewGameManager(out var go);
            try
            {
                gm.maxReputation = DecidedCap;
                gm.AddReputation(100);
                gm.SpendReputation(4);
                Assert.AreEqual(DecidedCap - 4, gm.CurrentReputation, "The cap only limits gains.");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void CureReputationCap_ComponentDefault_IsTwo()
        {
            var gm = NewGameManager(out var go);
            try
            {
                Assert.AreEqual(DecidedCureReputationCap, gm.cureReputationCap, "Decided tuning: a cure awards at most 2 Reputation (was 3).");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void CureReputationCap_SceneValue_IsTwo()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var gm = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameManager>(true)).First();
                Assert.AreEqual(DecidedCureReputationCap, gm.cureReputationCap, "MainBoard's GameManager.cureReputationCap should be 2.");
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
