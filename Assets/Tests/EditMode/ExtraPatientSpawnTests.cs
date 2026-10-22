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
    /// Live check that <see cref="GameManager.StartNewShift"/> really asks the board for the extra patient on every
    /// Nth shift (<see cref="ExtraPatientTests"/> only covers the pure rule). A real <see cref="BoardManager"/> is
    /// wired with every Triage slot occupied, so each arrival becomes a diversion that
    /// <see cref="BoardManager.OnPatientDiverted"/> lets us count without spawning anything.
    /// </summary>
    [Category("Shift")]
    public class ExtraPatientSpawnTests
    {
        private const int TriageSlotCount = 3;

        private readonly List<Object> created = new List<Object>();
        private readonly Dictionary<PropertyInfo, object> previousInstances = new Dictionary<PropertyInfo, object>();

        private GameManager gm;
        private BoardManager board;
        private int arrivals;

        [SetUp]
        public void Setup()
        {
            gm = NewComponent<GameManager>("ExtraSpawnTestsGameManager");
            Override(gm);
            // Keep the shift step to the board alone: no deck to draw, no escalation checks.
            Override<DeckManager>(null);
            Override<PatientDeckManager>(null);

            var view = NewComponent<BoardView>("ExtraSpawnTestsBoardView");
            view.triageSlots = new Transform[TriageSlotCount];
            for (int i = 0; i < TriageSlotCount; i++)
            {
                view.triageSlots[i] = new GameObject("TriageSlot" + i).transform;
                created.Add(view.triageSlots[i].gameObject);
            }
            Override(view);

            board = NewComponent<BoardManager>("ExtraSpawnTestsBoardManager");
            gm.boardManager = board;
            // GameManager.Start (which would subscribe DivertPatient) doesn't run in EditMode, so diversions cost
            // nothing here; we only count them.
            board.OnPatientDiverted += _ => arrivals++;

            // Occupy every Triage slot with a live, initialised patient that decay can't kill during the test.
            var registry = (List<PatientCardView>)board.ActivePatients;
            foreach (var slot in view.triageSlots)
            {
                var data = ScriptableObject.CreateInstance<PatientCardSO>();
                data.patientName = "Occupant";
                data.startingStability = 1000;
                data.stabilityDecayPerTurn = 1;
                created.Add(data);
                var symptom = ScriptableObject.CreateInstance<SymptomCardSO>();
                symptom.specialtyCategory = SymptomCategory.Internal;
                created.Add(symptom);

                var patient = NewComponent<PatientCardView>("OccupyingPatient");
                patient.InitialiseCard(data, new List<SymptomCardSO> { symptom }, new List<SymptomTier> { SymptomTier.Tier1 }, null);
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
            arrivals = 0;
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

        /// <summary>Runs one shift transition and returns how many patients arrived during it.</summary>
        private int ArrivalsForNextShift()
        {
            arrivals = 0;
            gm.StartNewShift();
            return arrivals;
        }

        [Test]
        public void Setup_StartsOnShiftOneWithTriageFull()
        {
            Assert.AreEqual(1, gm.CurrentShift);
            Assert.IsNull(BoardView.Instance.GetEmptyTriageSlot(board.ActivePatients), "The fixture should leave no free Triage slot.");
        }

        [Test]
        public void StartNewShift_EveryThirdShift_SpawnsAnExtraPatient()
        {
            gm.extraPatientEveryNShifts = 3;
            // shift reached by each StartNewShift -> arrivals expected
            var expected = new[] { (2, 1), (3, 2), (4, 1), (5, 1), (6, 2), (7, 1), (8, 1), (9, 2) };

            foreach (var (shift, count) in expected)
            {
                int got = ArrivalsForNextShift();
                Assert.AreEqual(shift, gm.CurrentShift, "Fixture error: the shift counter did not advance.");
                Assert.AreEqual(count, got, $"Shift {shift} should bring {count} patient(s) with extraPatientEveryNShifts = 3.");
            }

            Assert.AreEqual(TriageSlotCount, board.ActivePatients.Count, "Diverted patients must not join the board, and decay must kill nobody.");
        }

        [Test]
        public void StartNewShift_ExtraPatientsOff_SpawnsExactlyOnePerShift()
        {
            gm.extraPatientEveryNShifts = 0;

            for (int shift = 2; shift <= 10; shift++)
                Assert.AreEqual(1, ArrivalsForNextShift(), $"Shift {shift} with the extra-patient rule off.");
        }

        [Test]
        public void StartNewShift_EveryShift_SpawnsTwoPerShift()
        {
            gm.extraPatientEveryNShifts = 1;

            for (int shift = 2; shift <= 5; shift++)
                Assert.AreEqual(2, ArrivalsForNextShift(), $"Shift {shift} with extraPatientEveryNShifts = 1.");
        }

        [Test]
        public void StartNewShift_FinalShift_BringsNoExtraPatient()
        {
            gm.extraPatientEveryNShifts = 3;
            gm.shiftsToWin = 6;
            // Shift 6 is both an extra-patient shift and the final one: only the usual arrival.
            var expected = new[] { (2, 1), (3, 2), (4, 1), (5, 1), (6, 1) };

            foreach (var (shift, count) in expected)
            {
                int got = ArrivalsForNextShift();
                Assert.AreEqual(shift, gm.CurrentShift, "Fixture error: the shift counter did not advance.");
                Assert.AreEqual(count, got, $"Shift {shift} of 6 should bring {count} patient(s) with extraPatientEveryNShifts = 3.");
            }
            Assert.IsTrue(gm.IsFinalShift);
        }

        [TestCase(6, 5, false)]
        [TestCase(6, 6, true)]
        [TestCase(0, 6, false)]
        public void IsFinalShift_NeedsAWinConditionAndTheLastShift(int shiftsToWin, int reachShift, bool expected)
        {
            gm.extraPatientEveryNShifts = 0;
            gm.shiftsToWin = 0; // never win while advancing
            while (gm.CurrentShift < reachShift) ArrivalsForNextShift();
            gm.shiftsToWin = shiftsToWin;

            Assert.AreEqual(expected, gm.IsFinalShift);
        }

        [Test]
        public void StartNewShift_DiversionEndsTheRun_NoSecondArrival()
        {
            // Wire the diversion penalty as GameManager.Start would, with just enough Reputation for one diversion.
            board.OnPatientDiverted += gm.DivertPatient;
            gm.DiversionReputationPenalty = 2;
            gm.SpendReputation(gm.CurrentReputation - 2);
            Assert.AreEqual(2, gm.CurrentReputation, "Fixture error: starting Reputation not set.");
            gm.extraPatientEveryNShifts = 1; // every shift would bring a second arrival

            int got = ArrivalsForNextShift();

            Assert.AreEqual(GamePhase.GameOver, gm.CurrentPhase, "The first diversion should end the run.");
            Assert.AreEqual(1, got, "No extra patient may arrive once the run is over.");
        }

        [Test]
        public void StartNewShift_OccupantsDecayEachShift()
        {
            // Guards the fixture: AdvanceShift really ran on the board (so this is the live loop, not a stub).
            ArrivalsForNextShift();
            foreach (var p in board.ActivePatients)
                Assert.AreEqual(999, p.currentStability, "Each occupant should have decayed by 1.");
        }
    }
}
