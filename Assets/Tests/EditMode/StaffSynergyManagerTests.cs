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
    /// Team Assessment trigger rules in <see cref="StaffSynergyManager"/>: a Doctor plus a Nurse queues it,
    /// a Doctor alone never does, and with no one eligible to assess it is not used up for the shift.
    /// Awake/Start don't run in EditMode, so the manager is driven through its public test hooks.
    /// </summary>
    [Category("Synergy")]
    public class StaffSynergyManagerTests
    {
        private readonly List<Object> created = new List<Object>();
        private readonly Dictionary<PropertyInfo, object> previousInstances = new Dictionary<PropertyInfo, object>();

        private GameManager gm;
        private BoardManager board;
        private StaffSynergyManager synergy;

        [SetUp]
        public void Setup()
        {
            gm = NewComponent<GameManager>("SynergyTestsGameManager");
            Override(gm);

            board = NewComponent<BoardManager>("SynergyTestsBoardManager");
            gm.boardManager = board;

            synergy = NewComponent<StaffSynergyManager>("SynergyTestsStaffSynergyManager");
            Override(synergy);
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

        private ActionCardSO Card(CardTag tag, int tier)
        {
            var card = ScriptableObject.CreateInstance<ActionCardSO>();
            card.name = $"{tag}T{tier}";
            card.tags = new List<CardTag> { tag };
            card.powerTier = tier;
            created.Add(card);
            return card;
        }

        /// <summary>Puts a patient in a Ward bed whose only hidden symptom is above the team's tier.</summary>
        private void AddIneligibleWardPatient(SymptomTier hiddenTier)
        {
            var ward = new GameObject("Ward") { tag = "WardSlot" };
            created.Add(ward);
            var view = NewComponent<PatientCardView>("IneligiblePatient");
            view.transform.SetParent(ward.transform);

            var patient = ScriptableObject.CreateInstance<PatientCardSO>();
            patient.startingStability = 10;
            created.Add(patient);
            var symptom = ScriptableObject.CreateInstance<SymptomCardSO>();
            created.Add(symptom);
            view.InitialiseCard(patient, new List<SymptomCardSO> { symptom }, new List<SymptomTier> { hiddenTier }, null);

            ((List<PatientCardView>)board.ActivePatients).Add(view);
        }

        [Test]
        public void DoctorAndNurse_NoPatients_IsNotUsedUp()
        {
            synergy.RegisterPlayedCard(Card(CardTag.Doctor, 2), Vector3.zero);
            synergy.RegisterPlayedCard(Card(CardTag.Nurse, 1), Vector3.zero);

            Assert.IsTrue(synergy.UsedThisShift, "A Doctor plus a Nurse should trigger the Team Assessment.");
            Assert.AreEqual(1, synergy.PendingTier, "The pending tier is the weaker of the two cards.");

            synergy.StartPendingAssessment();

            Assert.IsFalse(synergy.UsedThisShift, "With no one to assess, the Team Assessment must not be used up.");
            Assert.AreEqual(0, synergy.PendingTier, "The pending assessment should be cleared once attempted.");
        }

        [Test]
        public void DoctorAndNurse_OnlyIneligiblePatient_IsNotUsedUp()
        {
            AddIneligibleWardPatient(SymptomTier.Tier3);
            Assert.IsFalse(board.ActivePatients[0].CanTeamAssess(SymptomTier.Tier1), "Fixture: the patient should be out of reach of a Tier 1 team.");

            synergy.RegisterPlayedCard(Card(CardTag.Doctor, 1), Vector3.zero);
            synergy.RegisterPlayedCard(Card(CardTag.Nurse, 1), Vector3.zero);
            synergy.StartPendingAssessment();

            Assert.IsFalse(synergy.UsedThisShift, "A patient the team can't assess must not use up the Team Assessment.");
            Assert.AreEqual(0, synergy.PendingTier);
        }

        [TestCase(CardTag.Doctor)]
        [TestCase(CardTag.Nurse)]
        public void NoOneToAssess_NextStaffCardSameShift_RetriesIt(CardTag retryTag)
        {
            synergy.RegisterPlayedCard(Card(CardTag.Doctor, 2), Vector3.zero);
            synergy.RegisterPlayedCard(Card(CardTag.Nurse, 2), Vector3.zero);
            synergy.StartPendingAssessment();
            Assume.That(synergy.UsedThisShift, Is.False);

            synergy.RegisterPlayedCard(Card(retryTag, 1), Vector3.zero);

            Assert.IsTrue(synergy.UsedThisShift, $"A further {retryTag} card the same shift should retry the Team Assessment.");
            Assert.AreEqual(2, synergy.PendingTier, "The retry keeps the best Doctor and Nurse tiers seen this shift.");
        }

        [TestCase(1)]
        [TestCase(3)]
        public void DoctorAlone_NeverTriggers(int tier)
        {
            synergy.RegisterPlayedCard(Card(CardTag.Doctor, tier), Vector3.zero);
            synergy.RegisterPlayedCard(Card(CardTag.Doctor, tier), Vector3.zero);
            synergy.StartPendingAssessment();

            Assert.IsFalse(synergy.UsedThisShift, "Doctor cards alone must not trigger the Team Assessment.");
            Assert.AreEqual(0, synergy.PendingTier);
        }
    }
}
