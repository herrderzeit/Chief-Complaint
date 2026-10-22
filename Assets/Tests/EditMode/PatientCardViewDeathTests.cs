using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ChiefComplaint.Tests.EditMode
{
    /// <summary>
    /// Covers the Reputation cost of a death in <see cref="PatientCardView.AdvanceShiftDecay"/>: a patient without
    /// an OnDeath LoseReputation trigger pays the flat <see cref="GameManager.deathReputationPenalty"/>; a patient
    /// with one pays only the trigger's value (the trigger replaces the flat penalty rather than stacking).
    /// Stability values are set on runtime-created SOs here, never read from Assets/Data.
    /// </summary>
    [Category("Board")]
    public class PatientCardViewDeathTests
    {
        private const int StartingReputation = 20;
        private const int FlatPenalty = 5;

        private readonly List<Object> created = new List<Object>();
        private readonly Dictionary<PropertyInfo, object> previousInstances = new Dictionary<PropertyInfo, object>();

        private GameManager gm;
        private PatientCardView view;
        private PatientCardSO patient;

        [SetUp]
        public void Setup()
        {
            gm = NewComponent<GameManager>("DeathTestsGameManager");
            gm.deathReputationPenalty = FlatPenalty;
            gm.deathAddsRedTape = false; // keep the test independent of DeckManager
            gm.maxReputation = 0; // StartingReputation sits above the default cap of 16; the cap is not under test here
            gm.AddReputation(StartingReputation - gm.CurrentReputation);
            Assert.AreEqual(StartingReputation, gm.CurrentReputation, "Fixture error: starting Reputation not set.");
            Override(gm);

            // No BoardView: the dead card then falls back to Destroy(gameObject), which only logs in EditMode.
            Override<BoardView>(null);
            Override<DeckManager>(null);

            view = NewComponent<PatientCardView>("DeathTestsPatient");
            patient = ScriptableObject.CreateInstance<PatientCardSO>();
            patient.patientName = "Death Test Patient";
            patient.startingStability = 1;
            patient.stabilityDecayPerTurn = 2;
            patient.triggers = new List<PatientTrigger>();
            created.Add(patient);
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

        private void InitialiseAndKill()
        {
            var symptom = ScriptableObject.CreateInstance<SymptomCardSO>();
            symptom.specialtyCategory = SymptomCategory.Internal;
            created.Add(symptom);
            view.InitialiseCard(patient, new List<SymptomCardSO> { symptom }, new List<SymptomTier> { SymptomTier.Tier1 }, null);

            bool died = false;
            view.OnDied += _ => died = true;

            // EditMode cannot Destroy; the fallback logs this error, which is expected and harmless here.
            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            view.AdvanceShiftDecay(); // stability 1 - decay 2 -> death

            Assert.IsTrue(died, "Fixture error: the patient should have died this shift.");
            Assert.IsTrue(view.isRemoved);
        }

        [Test]
        public void Death_WithoutOwnPenalty_CostsTheFlatPenalty()
        {
            InitialiseAndKill();

            Assert.AreEqual(StartingReputation - FlatPenalty, gm.CurrentReputation,
                "A patient without an OnDeath LoseReputation trigger pays exactly deathReputationPenalty.");
        }

        [Test]
        public void Death_WithUnrelatedOnDeathTrigger_StillCostsTheFlatPenalty()
        {
            patient.triggers.Add(new PatientTrigger { condition = PatientTriggerCondition.OnDeath, effect = PatientTriggerEffect.LoseAP, effectValue = 1 });

            InitialiseAndKill();

            Assert.AreEqual(StartingReputation - FlatPenalty, gm.CurrentReputation,
                "An OnDeath trigger that does not take Reputation must not waive the flat penalty.");
        }

        [Test]
        public void Death_WithOnDeathLoseReputationTrigger_CostsOnlyTheTriggerValue()
        {
            const int TriggerPenalty = 4;
            patient.triggers.Add(new PatientTrigger
            {
                condition = PatientTriggerCondition.OnDeath,
                effect = PatientTriggerEffect.LoseReputation,
                effectValue = TriggerPenalty,
            });

            InitialiseAndKill();

            Assert.AreEqual(StartingReputation - TriggerPenalty, gm.CurrentReputation,
                $"The OnDeath trigger replaces the flat penalty: expected a loss of {TriggerPenalty}, not {TriggerPenalty + FlatPenalty}.");
        }

        [Test]
        public void Death_WithZeroValueLoseReputationTrigger_StillCostsTheFlatPenalty()
        {
            const int value = 0;
            patient.triggers.Add(new PatientTrigger
            {
                condition = PatientTriggerCondition.OnDeath,
                effect = PatientTriggerEffect.LoseReputation,
                effectValue = value,
            });

            InitialiseAndKill();

            Assert.AreEqual(StartingReputation - FlatPenalty, gm.CurrentReputation,
                $"A LoseReputation trigger of {value} must not make a death free: the flat {FlatPenalty} applies.");
        }

        [Test]
        public void Death_WithNegativeLoseReputationTrigger_CostsTheFlatPenaltyAndGivesNothingBack()
        {
            const int value = -3;
            patient.triggers.Add(new PatientTrigger
            {
                condition = PatientTriggerCondition.OnDeath,
                effect = PatientTriggerEffect.LoseReputation,
                effectValue = value,
            });

            InitialiseAndKill();

            Assert.AreEqual(StartingReputation - FlatPenalty, gm.CurrentReputation,
                $"A LoseReputation trigger of {value} is clamped to 0: the death pays the flat {FlatPenalty} " +
                $"and the trigger must not hand back {-value} Reputation.");
        }

        // The remaining tests drive OnSpawn triggers, which run at the end of InitialiseCard (no death needed).
        private void InitialiseOnly()
        {
            var symptom = ScriptableObject.CreateInstance<SymptomCardSO>();
            symptom.specialtyCategory = SymptomCategory.Internal;
            created.Add(symptom);
            view.InitialiseCard(patient, new List<SymptomCardSO> { symptom }, new List<SymptomTier> { SymptomTier.Tier1 }, null);
        }

        [Test]
        public void OnSpawn_NegativeGainReputationTrigger_LeavesReputationUnchanged()
        {
            patient.triggers.Add(new PatientTrigger
            {
                condition = PatientTriggerCondition.OnSpawn,
                effect = PatientTriggerEffect.GainReputation,
                effectValue = -2,
            });

            InitialiseOnly();

            Assert.AreEqual(StartingReputation, gm.CurrentReputation,
                "A GainReputation trigger of -2 is clamped to 0 and must never take Reputation.");
        }

        [Test]
        public void OnSpawn_NegativeLoseAPTrigger_DoesNotAddAP()
        {
            gm.GainAP(3 - gm.CurrentAP);
            Assert.AreEqual(3, gm.CurrentAP, "Fixture error: starting AP not set.");
            patient.triggers.Add(new PatientTrigger
            {
                condition = PatientTriggerCondition.OnSpawn,
                effect = PatientTriggerEffect.LoseAP,
                effectValue = -2,
            });

            InitialiseOnly();

            Assert.AreEqual(3, gm.CurrentAP, "A LoseAP trigger of -2 is clamped to 0 and must never give AP.");
        }
    }
}
