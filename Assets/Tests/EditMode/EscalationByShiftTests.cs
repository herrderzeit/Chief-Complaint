using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Core;
using ChiefComplaint.Utils;
using NUnit.Framework;
using UnityEngine;

namespace ChiefComplaint.Tests.EditMode
{
    /// <summary>
    /// Covers time-based escalation (the stalling-exploit fix): <see cref="PatientDeckManager"/> unlocks the
    /// Ambulance and Trauma Flight pools on enough cures OR when a set shift starts, each pool exactly once,
    /// and <see cref="GameManager.StartNewShift"/> reports the new shift to it.
    /// </summary>
    [Category("Escalation")]
    public class EscalationByShiftTests
    {
        private readonly List<Object> created = new List<Object>();
        private PatientDeckManager pdm;
        private PatientCardSO walkIn;
        private List<PatientCardSO> ambulances;
        private List<PatientCardSO> traumas;

        private PatientDeckManager previousPdmInstance;
        private bool pdmOverridden;

        [SetUp]
        public void Setup()
        {
            var go = new GameObject("EscalationTestsPatientDeckManager");
            created.Add(go);
            pdm = go.AddComponent<PatientDeckManager>();
            pdm.ambulanceCureThreshold = 3;
            pdm.traumaFlightCureThreshold = 8;

            walkIn = NewPatient(EscalationType.WalkIn);
            ambulances = new List<PatientCardSO> { NewPatient(EscalationType.Ambulance), NewPatient(EscalationType.Ambulance) };
            traumas = new List<PatientCardSO> { NewPatient(EscalationType.TraumaFlight) };

            pdm.walkInPool = new List<PatientCardSO> { walkIn };
            pdm.ambulancePool = new List<PatientCardSO>(ambulances);
            pdm.traumaFlightPool = new List<PatientCardSO>(traumas);
            // Awake does not run for AddComponent in EditMode, so build the opening deck as Awake would.
            pdm.activePatientDeck = new List<PatientCardSO> { walkIn };
        }

        [TearDown]
        public void Teardown()
        {
            // Restore the singleton before destroying ours, so its OnDestroy cannot clear a real instance.
            if (pdmOverridden) SetInstance(previousPdmInstance);
            pdmOverridden = false;
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        private PatientCardSO NewPatient(EscalationType phase)
        {
            var p = ScriptableObject.CreateInstance<PatientCardSO>();
            p.escalationPhase = phase;
            created.Add(p);
            return p;
        }

        private void Cure(int times)
        {
            for (int i = 0; i < times; i++) pdm.PatientSuccessfullyCured(walkIn);
        }

        private bool AmbulancesInDeck => ambulances.All(pdm.activePatientDeck.Contains);
        private bool TraumasInDeck => traumas.All(pdm.activePatientDeck.Contains);

        private static void SetInstance(PatientDeckManager value) =>
            typeof(Singleton<PatientDeckManager>).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
                .GetSetMethod(true).Invoke(null, new object[] { value });

        // ------------------------------------------------------------------ shift / cure matrix

        [TestCase(4, 0, 5, 10, false, false, TestName = "Shift4_NoCures_NothingUnlocked")]
        [TestCase(5, 0, 5, 10, true, false, TestName = "Shift5_NoCures_AmbulancesUnlock")]
        [TestCase(10, 0, 5, 10, true, true, TestName = "Shift10_NoCures_BothUnlock")]
        [TestCase(3, 3, 5, 10, true, false, TestName = "Shift3_ThreeCures_CuresStillUnlockAmbulances")]
        [TestCase(20, 0, 0, 0, false, false, TestName = "Shift20_ZeroThresholds_TimeRuleDisabled")]
        public void Escalation_UnlocksByShiftOrCures(int shift, int cures, int ambulanceShift, int traumaShift,
            bool expectAmbulance, bool expectTrauma)
        {
            pdm.ambulanceShiftThreshold = ambulanceShift;
            pdm.traumaFlightShiftThreshold = traumaShift;

            Cure(cures);
            pdm.OnShiftStarted(shift);

            Assert.AreEqual(expectAmbulance, AmbulancesInDeck,
                $"Ambulance pool in deck at shift {shift} with {cures} cures (shift threshold {ambulanceShift}).");
            Assert.AreEqual(expectTrauma, TraumasInDeck,
                $"Trauma Flight pool in deck at shift {shift} with {cures} cures (shift threshold {traumaShift}).");

            EscalationType expectedPhase = expectTrauma ? EscalationType.TraumaFlight
                : expectAmbulance ? EscalationType.Ambulance : EscalationType.WalkIn;
            Assert.AreEqual(expectedPhase, pdm.CurrentEscalation);
        }

        [Test]
        public void Escalation_ShiftJustBelowThreshold_StaysOnWalkIns()
        {
            pdm.ambulanceShiftThreshold = 5;
            pdm.traumaFlightShiftThreshold = 10;

            for (int shift = 1; shift <= 4; shift++) pdm.OnShiftStarted(shift);

            Assert.AreEqual("Walk-Ins", pdm.GetEscalationPhase());
            CollectionAssert.AreEqual(new[] { walkIn }, pdm.activePatientDeck, "Only the opening Walk-In should be in the deck.");
        }

        // ------------------------------------------------------------------ exactly once

        [Test]
        public void Pools_AreAddedExactlyOnce_WhenCureAndShiftConditionsAreBothMet()
        {
            pdm.ambulanceShiftThreshold = 5;
            pdm.traumaFlightShiftThreshold = 10;

            Cure(3);                    // Ambulances by cures
            pdm.OnShiftStarted(5);      // ...and by shift
            pdm.OnShiftStarted(6);
            pdm.OnShiftStarted(10);     // Trauma Flights by shift
            Cure(5);                    // ...and by cures (8 total)
            pdm.OnShiftStarted(11);
            Cure(1);

            foreach (var a in ambulances)
                Assert.AreEqual(1, pdm.activePatientDeck.Count(p => p == a), "Each Ambulance patient should be in the deck exactly once.");
            foreach (var t in traumas)
                Assert.AreEqual(1, pdm.activePatientDeck.Count(p => p == t), "Each Trauma Flight patient should be in the deck exactly once.");
            Assert.AreEqual(1 + ambulances.Count + traumas.Count, pdm.activePatientDeck.Count);
        }

        [Test]
        public void Unlocking_DoesNotMutateThePoolLists()
        {
            pdm.ambulanceShiftThreshold = 5;
            pdm.traumaFlightShiftThreshold = 10;

            pdm.OnShiftStarted(10);
            pdm.DrawRandomPatient();
            pdm.DrawRandomPatient();

            CollectionAssert.AreEqual(ambulances, pdm.ambulancePool, "Drawing must remove from the active deck, not the Ambulance pool.");
            CollectionAssert.AreEqual(traumas, pdm.traumaFlightPool, "Drawing must remove from the active deck, not the Trauma Flight pool.");
        }

        // ------------------------------------------------------------------ events

        [Test]
        public void OnEscalationPhaseChanged_FiresOncePerNewPhase()
        {
            pdm.ambulanceShiftThreshold = 5;
            pdm.traumaFlightShiftThreshold = 10;
            var raised = new List<string>();
            pdm.OnEscalationPhaseChanged += raised.Add;

            for (int shift = 1; shift <= 15; shift++)
            {
                pdm.OnShiftStarted(shift);
                if (shift == 7) Cure(3); // a cure after the time unlock must not re-announce Ambulances
            }
            Cure(8);

            CollectionAssert.AreEqual(new[] { "Ambulances", "Trauma Flights" }, raised);
        }

        [Test]
        public void OnEscalationPhaseChanged_CureUnlockThenShift_DoesNotFireAgain()
        {
            pdm.ambulanceShiftThreshold = 5;
            pdm.traumaFlightShiftThreshold = 0;
            var raised = new List<string>();
            pdm.OnEscalationPhaseChanged += raised.Add;

            Cure(3);
            pdm.OnShiftStarted(5);
            pdm.OnShiftStarted(6);

            CollectionAssert.AreEqual(new[] { "Ambulances" }, raised);
        }

        [Test]
        public void OnEscalationPhaseChanged_JumpingStraightPastBothThresholds_FiresOnceWithTheHighestPhase()
        {
            // Documents current behaviour: one CheckProgressionTriggers call that unlocks both pools raises one event.
            pdm.ambulanceShiftThreshold = 5;
            pdm.traumaFlightShiftThreshold = 10;
            var raised = new List<string>();
            pdm.OnEscalationPhaseChanged += raised.Add;

            pdm.OnShiftStarted(10);

            CollectionAssert.AreEqual(new[] { "Trauma Flights" }, raised);
            Assert.IsTrue(AmbulancesInDeck && TraumasInDeck, "Both pools should be added when both thresholds are passed at once.");
        }

        // ------------------------------------------------------------------ GameManager wiring

        [Test]
        public void GameManagerStartNewShift_ReportsTheShift_SoAmbulancesArriveAtShiftFive()
        {
            pdm.ambulanceShiftThreshold = 5;
            pdm.traumaFlightShiftThreshold = 10;
            previousPdmInstance = PatientDeckManager.Instance;
            pdmOverridden = true;
            SetInstance(pdm);

            var gmGo = new GameObject("EscalationTestsGameManager");
            created.Add(gmGo);
            var gm = gmGo.AddComponent<GameManager>(); // no BoardManager, so nothing spawns

            for (int i = 0; i < 3; i++) gm.StartNewShift();
            Assert.AreEqual(4, gm.CurrentShift);
            Assert.IsFalse(AmbulancesInDeck, "Ambulances must not unlock before shift 5.");

            gm.StartNewShift();
            Assert.AreEqual(5, gm.CurrentShift);
            Assert.IsTrue(AmbulancesInDeck, "StartNewShift should pass shift 5 to PatientDeckManager and unlock Ambulances.");
            Assert.IsFalse(TraumasInDeck);
        }
    }
}
