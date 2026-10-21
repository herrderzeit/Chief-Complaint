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
    /// Covers ambulance diversion (the stalling-exploit fix): when every Triage slot is occupied,
    /// <see cref="BoardManager.SpawnPatient"/> turns the patient away and charges
    /// <see cref="GameManager.diversionReputationPenalty"/> instead of spawning.
    /// </summary>
    [Category("Board")]
    public class DiversionTests
    {
        private const int TriageSlotCount = 3;

        private readonly List<Object> created = new List<Object>();
        private readonly Dictionary<PropertyInfo, object> previousInstances = new Dictionary<PropertyInfo, object>();

        private BoardManager board;
        private GameManager gm;
        private PatientDeckManager pdm;
        private PatientCardSO waitingPatient;

        [SetUp]
        public void Setup()
        {
            gm = NewComponent<GameManager>("DiversionTestsGameManager");
            Override(gm);

            pdm = NewComponent<PatientDeckManager>("DiversionTestsPatientDeckManager");
            waitingPatient = ScriptableObject.CreateInstance<PatientCardSO>();
            created.Add(waitingPatient);
            pdm.activePatientDeck = new List<PatientCardSO> { waitingPatient };
            Override(pdm);

            var view = NewComponent<BoardView>("DiversionTestsBoardView");
            view.triageSlots = new Transform[TriageSlotCount];
            for (int i = 0; i < TriageSlotCount; i++) view.triageSlots[i] = new GameObject("TriageSlot" + i).transform;
            foreach (var t in view.triageSlots) created.Add(t.gameObject);
            Override(view);

            board = NewComponent<BoardManager>("DiversionTestsBoardManager");
            // GameManager.Start does this in play; Start doesn't run in EditMode.
            board.OnPatientDiverted += gm.DivertPatient;

            // Occupy every Triage slot: a live patient parented to each slot, registered on the board.
            var registry = (List<PatientCardView>)board.ActivePatients;
            foreach (var slot in view.triageSlots)
            {
                var patient = NewComponent<PatientCardView>("OccupyingPatient");
                patient.transform.SetParent(slot);
                registry.Add(patient);
            }
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

        [Test]
        public void Setup_EveryTriageSlotIsOccupied()
        {
            Assert.IsNull(BoardView.Instance.GetEmptyTriageSlot(board.ActivePatients), "The fixture should leave no free Triage slot.");
        }

        [TestCase(2)]
        [TestCase(1)]
        [TestCase(5)]
        public void SpawnPatient_TriageFull_ChargesTheDiversionPenalty(int penalty)
        {
            gm.diversionReputationPenalty = penalty;
            int before = gm.CurrentReputation;
            int? raised = null;
            gm.OnReputationChanged += v => raised = v;

            board.SpawnPatient();

            Assert.AreEqual(before - penalty, gm.CurrentReputation, $"A diverted patient should cost {penalty} Reputation.");
            Assert.AreEqual(before - penalty, raised, "OnReputationChanged should report the new Reputation.");
        }

        [Test]
        public void SpawnPatient_TriageFull_NothingSpawnsAndNoPatientIsDrawn()
        {
            gm.diversionReputationPenalty = 2;

            board.SpawnPatient();

            Assert.AreEqual(TriageSlotCount, board.ActivePatients.Count, "A diverted patient must not join the board.");
            CollectionAssert.AreEqual(new[] { waitingPatient }, pdm.activePatientDeck,
                "A diverted patient must not be drawn from the patient deck (it stays available for later).");
        }

        [Test]
        public void SpawnPatient_TriageFull_ZeroPenalty_IsFree()
        {
            gm.diversionReputationPenalty = 0;
            int before = gm.CurrentReputation;
            bool raised = false;
            gm.OnReputationChanged += _ => raised = true;

            board.SpawnPatient();

            Assert.AreEqual(before, gm.CurrentReputation);
            Assert.IsFalse(raised, "A free diversion should not touch Reputation.");
            Assert.AreEqual(TriageSlotCount, board.ActivePatients.Count);
        }

        [Test]
        public void SpawnPatient_TriageFull_RepeatedDiversionsEndTheRun()
        {
            gm.diversionReputationPenalty = 2;
            gm.SpendReputation(gm.CurrentReputation - 4); // leave 4 Reputation

            board.SpawnPatient();
            Assert.AreEqual(2, gm.CurrentReputation);
            Assert.AreNotEqual(GamePhase.GameOver, gm.CurrentPhase);

            board.SpawnPatient();
            Assert.AreEqual(0, gm.CurrentReputation);
            Assert.AreEqual(GamePhase.GameOver, gm.CurrentPhase, "Reputation is life: diverting at 2 Reputation should end the run.");
        }
    }
}
