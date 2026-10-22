using System.Collections.Generic;
using System.Reflection;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Utils;
using NUnit.Framework;
using UnityEngine;

namespace ChiefComplaint.Tests.EditMode
{
    /// <summary>
    /// AP gained while End Shift resolves surgeries (e.g. an OnCured GainAP trigger at discharge) carries into the
    /// next shift through <see cref="GameManager.CarriedAP"/> instead of being wiped by the next shift's AP reset.
    /// Drives the real <see cref="GameManager.EnterDraftPhase"/> → <see cref="BoardManager.ResolveSurgeries"/> →
    /// <see cref="PatientCardView.ResolveSurgery"/> path with a patient waiting for discharge.
    /// </summary>
    [Category("Shift")]
    public class CarriedAPTests
    {
        private const int CureAP = 2;

        private readonly List<Object> created = new List<Object>();
        private readonly Dictionary<PropertyInfo, object> previousInstances = new Dictionary<PropertyInfo, object>();

        private GameManager gm;
        private BoardManager board;

        [SetUp]
        public void Setup()
        {
            gm = NewComponent<GameManager>("CarriedAPTestsGameManager");
            Override(gm);
            // No BoardView: the discharged card is destroyed outright (DestroyImmediate outside play mode),
            // and SpawnPatient does nothing. No deck or patient deck: the shift step is the AP and the board alone.
            Override<BoardView>(null);
            Override<DeckManager>(null);
            Override<PatientDeckManager>(null);

            board = NewComponent<BoardManager>("CarriedAPTestsBoardManager");
            gm.boardManager = board;
            gm.ResetAP();
        }

        [TearDown]
        public void Teardown()
        {
            // Restore singletons before destroying ours, so their OnDestroy cannot clear a real instance.
            foreach (var kv in previousInstances) kv.Key.GetSetMethod(true).Invoke(null, new[] { kv.Value });
            previousInstances.Clear();
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        private T NewComponent<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            created.Add(go);
            return go.AddComponent<T>();
        }

        private void Override<T>(T instance) where T : Singleton<T>
        {
            PropertyInfo prop = typeof(Singleton<T>).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (!previousInstances.ContainsKey(prop)) previousInstances[prop] = prop.GetValue(null);
            prop.GetSetMethod(true).Invoke(null, new object[] { instance });
        }

        /// <summary>Puts a fully treated patient with an OnCured GainAP trigger in surgery, ready to be discharged at End Shift.</summary>
        private PatientCardView AddPatientAwaitingDischarge(int gainAP)
        {
            var data = ScriptableObject.CreateInstance<PatientCardSO>();
            data.patientName = "Carried AP Patient";
            data.startingStability = 10;
            data.stabilityDecayPerTurn = 1;
            data.triggers = new List<PatientTrigger>
            {
                new PatientTrigger { condition = PatientTriggerCondition.OnCured, effect = PatientTriggerEffect.GainAP, effectValue = gainAP },
            };
            created.Add(data);

            var patient = NewComponent<PatientCardView>("CarriedAPPatient");
            patient.InitialiseCard(data, new List<SymptomCardSO>(), new List<SymptomTier>(), null);
            patient.isInSurgery = true;
            Assert.IsTrue(patient.isAwaitingDischarge, "Fixture error: the patient should be waiting for discharge.");
            ((List<PatientCardView>)board.ActivePatients).Add(patient);
            return patient;
        }

        private void EndShift()
        {
            gm.EnterDraftPhase();
        }

        [Test]
        public void GainAP_DuringPlay_IsAddedStraightAway()
        {
            gm.GainAP(CureAP);

            Assert.AreEqual(gm.baseAP + CureAP, gm.CurrentAP);
            Assert.AreEqual(0, gm.CarriedAP, "AP gained mid-shift must not be carried.");
        }

        [Test]
        public void EnterDraftPhase_OnCuredGainAP_IsCarriedNotSpent()
        {
            var patient = AddPatientAwaitingDischarge(CureAP);
            gm.SpendAP(1);
            int apBefore = gm.CurrentAP;

            EndShift();

            Assert.IsTrue(patient.isRemoved, "Fixture error: the patient should have been discharged.");
            Assert.AreEqual(GamePhase.Draft, gm.CurrentPhase);
            Assert.AreEqual(CureAP, gm.CarriedAP, "AP from a discharge at End Shift is carried to the next shift.");
            Assert.AreEqual(apBefore, gm.CurrentAP, "The carried AP must not be added to the shift that just ended.");
        }

        [Test]
        public void StartNewShift_AddsTheCarriedAPOnTopOfBaseAPOnce()
        {
            AddPatientAwaitingDischarge(CureAP);
            EndShift();

            gm.StartNewShift();

            Assert.AreEqual(2, gm.CurrentShift);
            Assert.AreEqual(gm.baseAP + CureAP, gm.CurrentAP, "The next shift starts with base AP plus the carried AP.");
            Assert.AreEqual(0, gm.CarriedAP, "The carry is used up.");

            gm.EnterDraftPhase();
            gm.StartNewShift();
            Assert.AreEqual(gm.baseAP, gm.CurrentAP, "The carry applies to one shift only.");
        }

        [Test]
        public void EnterDraftPhase_GainAPAfterTheShiftEnds_IsNoLongerCarried()
        {
            AddPatientAwaitingDischarge(CureAP);
            EndShift();

            gm.GainAP(1); // e.g. something in the draft: End Shift has finished resolving

            Assert.AreEqual(CureAP, gm.CarriedAP, "Only AP gained while End Shift resolves is carried.");
        }
    }
}
