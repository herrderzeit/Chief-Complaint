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
    /// (component default and the MainBoard scene value, both 16).
    /// </summary>
    [Category("Economy")]
    public class ReputationCapTests
    {
        private const string ScenePath = "Assets/Scenes/MainBoard.unity";
        private const int DecidedCap = 16;

        // reputation after the gain, cap, reputation before the gain -> expected
        [TestCase(30, 0, 10, 30, Description = "Cap 0 is off: no clamp at all")]
        [TestCase(12, 16, 10, 12, Description = "Under the cap: unchanged")]
        [TestCase(16, 16, 14, 16, Description = "Landing exactly on the cap: unchanged")]
        [TestCase(19, 16, 14, 16, Description = "Crossing the cap: clamped to the cap")]
        [TestCase(17, 16, 16, 16, Description = "Already at the cap: a gain is lost")]
        [TestCase(21, 16, 20, 20, Description = "Already above the cap (lowered mid-run): stays, never reduced")]
        [TestCase(20, 16, 20, 20, Description = "Above the cap, zero gain: stays")]
        [TestCase(-3, 16, 2, -3, Description = "Below the cap the value passes through untouched")]
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
        public void MaxReputation_ComponentDefault_IsSixteen()
        {
            var gm = NewGameManager(out var go);
            try
            {
                Assert.AreEqual(DecidedCap, gm.maxReputation, "Decided tuning: Reputation caps at 16.");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void MaxReputation_SceneValue_IsSixteen()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var gm = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameManager>(true)).First();
                Assert.AreEqual(DecidedCap, gm.maxReputation, "MainBoard's GameManager.maxReputation should be 16.");
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }

        // starting Reputation (8 by default), gain, cap -> expected
        [TestCase(10, 16, 16, Description = "8 + 10 clamps to 16")]
        [TestCase(8, 16, 16, Description = "8 + 8 lands exactly on 16")]
        [TestCase(3, 16, 11, Description = "Under the cap is unaffected")]
        [TestCase(50, 0, 58, Description = "0 disables the cap")]
        [TestCase(5, 10, 10, Description = "A lower cap clamps too")]
        public void AddReputation_ClampsAtMaxReputation(int gain, int cap, int expected)
        {
            var gm = NewGameManager(out var go);
            try
            {
                gm.maxReputation = cap;
                int start = gm.CurrentReputation;
                Assume.That(start, Is.EqualTo(8), "Fixture assumes the default starting Reputation of 8.");
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
                gm.AddReputation(12); // 20, uncapped
                gm.maxReputation = DecidedCap;

                gm.AddReputation(2);

                Assert.AreEqual(20, gm.CurrentReputation, "Lowering the cap mid-run must not take Reputation away on the next gain.");
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
    }
}
