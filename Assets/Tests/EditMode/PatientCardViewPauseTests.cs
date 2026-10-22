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
    /// A paused stability clock (<see cref="PatientCardView.PauseStabilityClock"/>) skips one shift of decay, but the
    /// patient's own OnTurnEnd triggers still run in <see cref="PatientCardView.AdvanceShiftDecay"/>.
    /// </summary>
    [Category("Board")]
    public class PatientCardViewPauseTests
    {
        private const int Stability = 10;
        private const int Decay = 2;

        private readonly List<Object> created = new List<Object>();
        private readonly Dictionary<PropertyInfo, object> previousInstances = new Dictionary<PropertyInfo, object>();

        private GameManager gm;
        private PatientCardView view;
        private PatientCardSO patient;

        [SetUp]
        public void Setup()
        {
            gm = NewComponent<GameManager>("PauseTestsGameManager");
            Override(gm);
            Override<BoardView>(null);
            Override<DeckManager>(null);
            gm.ResetAP();

            view = NewComponent<PatientCardView>("PauseTestsPatient");
            patient = ScriptableObject.CreateInstance<PatientCardSO>();
            patient.patientName = "Pause Test Patient";
            patient.startingStability = Stability;
            patient.stabilityDecayPerTurn = Decay;
            patient.triggers = new List<PatientTrigger>
            {
                new PatientTrigger { condition = PatientTriggerCondition.OnTurnEnd, effect = PatientTriggerEffect.LoseAP, effectValue = 1 },
            };
            created.Add(patient);

            var symptom = ScriptableObject.CreateInstance<SymptomCardSO>();
            symptom.specialtyCategory = SymptomCategory.Internal;
            created.Add(symptom);
            view.InitialiseCard(patient, new List<SymptomCardSO> { symptom }, new List<SymptomTier> { SymptomTier.Tier1 }, null);
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
        public void AdvanceShiftDecay_Paused_SkipsDecayButStillRunsOnTurnEnd()
        {
            view.PauseStabilityClock();

            view.AdvanceShiftDecay();

            Assert.AreEqual(Stability, view.currentStability, "A paused patient must not decay.");
            Assert.IsFalse(view.stabilityClockPaused, "The pause is used up.");
            Assert.AreEqual(gm.baseAP - 1, gm.CurrentAP, "The OnTurnEnd LoseAP trigger still fires while paused.");
        }

        [Test]
        public void AdvanceShiftDecay_NotPaused_DecaysAndRunsOnTurnEnd()
        {
            view.AdvanceShiftDecay();

            Assert.AreEqual(Stability - Decay, view.currentStability, "Control: an unpaused Triage patient takes full decay.");
            Assert.AreEqual(gm.baseAP - 1, gm.CurrentAP, "Control: the OnTurnEnd trigger fires once.");
        }

        [Test]
        public void AdvanceShiftDecay_PausedThenNot_DecaysOnTheSecondShiftAndFiresEachTime()
        {
            view.PauseStabilityClock();

            view.AdvanceShiftDecay();
            view.AdvanceShiftDecay();

            Assert.AreEqual(Stability - Decay, view.currentStability, "Only the second shift decays.");
            Assert.AreEqual(gm.baseAP - 2, gm.CurrentAP, "The OnTurnEnd trigger fires on both shifts.");
        }
    }
}
