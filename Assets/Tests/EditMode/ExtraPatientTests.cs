using ChiefComplaint.Core;
using NUnit.Framework;
using UnityEngine;

namespace ChiefComplaint.Tests.EditMode
{
    /// <summary>
    /// Covers the busier-shift rule <see cref="GameManager.IsExtraPatientShift"/>: a second patient arrives
    /// at the start of every Nth shift, and never when the setting is 0.
    /// </summary>
    [Category("Shift")]
    public class ExtraPatientTests
    {
        [TestCase(3, 3, true, Description = "First busy shift")]
        [TestCase(6, 3, true, Description = "Every multiple of N is busy")]
        [TestCase(4, 3, false, Description = "Between multiples stays normal")]
        [TestCase(1, 3, false, Description = "The opening shift is never busy for N > 1")]
        [TestCase(3, 0, false, Description = "0 switches the rule off")]
        [TestCase(2, 1, true, Description = "N = 1 makes every shift busy")]
        [TestCase(5, 2, false, Description = "Odd shift with N = 2")]
        public void IsExtraPatientShift_EveryNthShift(int shift, int everyNShifts, bool expected)
        {
            Assert.AreEqual(expected, GameManager.IsExtraPatientShift(shift, everyNShifts),
                $"shift {shift}, every {everyNShifts} shifts");
        }

        [Test]
        public void IsExtraPatientShift_ZeroNeverFires_AcrossAFullRun()
        {
            for (int shift = 1; shift <= 30; shift++)
                Assert.IsFalse(GameManager.IsExtraPatientShift(shift, 0), $"shift {shift} with the rule switched off");
        }

        [Test]
        public void ExtraPatientEveryNShifts_DefaultTuning_IsThree()
        {
            var go = new GameObject("ExtraPatientTestsGameManager");
            try
            {
                var gm = go.AddComponent<GameManager>();
                Assert.AreEqual(3, gm.extraPatientEveryNShifts, "Option A tuning: an extra patient every 3rd shift.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
