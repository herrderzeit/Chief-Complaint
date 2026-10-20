using System.Collections.Generic;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using NUnit.Framework;
using UnityEngine;

namespace ChiefComplaint.Tests.EditMode
{
    /// <summary>Team Assessment synergy: minimum-tier rule and the order symptoms are revealed in.</summary>
    [Category("Synergy")]
    public class TeamAssessmentTests
    {
        private GameObject ward;
        private GameObject go;
        private PatientCardView view;
        private readonly List<Object> created = new List<Object>();

        [SetUp]
        public void Setup()
        {
            ward = new GameObject("Ward") { tag = "WardSlot" };
            go = new GameObject("Patient");
            go.transform.SetParent(ward.transform);
            view = go.AddComponent<PatientCardView>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(ward);
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        private void Initialise(params SymptomTier[] tiers) => Initialise(false, tiers);

        private void Initialise(bool handover, params SymptomTier[] tiers)
        {
            var patient = ScriptableObject.CreateInstance<PatientCardSO>();
            patient.startingStability = 10;
            patient.arrivesWithHandover = handover;
            created.Add(patient);
            var symptoms = new List<SymptomCardSO>();
            for (int i = 0; i < tiers.Length; i++)
            {
                var s = ScriptableObject.CreateInstance<SymptomCardSO>();
                s.name = "Symptom" + i;
                created.Add(s);
                symptoms.Add(s);
            }
            view.InitialiseCard(patient, symptoms, new List<SymptomTier>(tiers), null);
        }

        [TestCase(1, 1, 1)]
        [TestCase(3, 1, 1)]
        [TestCase(1, 3, 1)]
        [TestCase(2, 3, 2)]
        [TestCase(3, 3, 3)]
        public void SynergyTier_IsTheWeakerCard(int doctor, int nurse, int expected)
        {
            Assert.AreEqual(expected, StaffSynergyManager.SynergyTier(doctor, nurse));
        }

        [Test]
        public void TeamAssess_RevealsTier1SymptomsBeforeTheMainSymptom()
        {
            Initialise(SymptomTier.Tier2, SymptomTier.Tier1, SymptomTier.Tier1);

            Assert.IsTrue(view.TeamAssess(SymptomTier.Tier2, new System.Random(1)));
            Assert.AreEqual(SymptomTier.Tier1, view.assignedTiers[0], "The first reveal must be a Tier 1 symptom.");
            Assert.IsTrue(view.TeamAssess(SymptomTier.Tier2, new System.Random(2)));
            Assert.AreEqual(SymptomTier.Tier1, view.assignedTiers[1]);
            Assert.IsTrue(view.TeamAssess(SymptomTier.Tier2, new System.Random(3)));
            Assert.AreEqual(SymptomTier.Tier2, view.assignedTiers[2], "The main symptom comes last.");
            Assert.IsTrue(view.isFullyDiagnosed);
        }

        [Test]
        public void TeamAssess_PicksRandomlyAmongTheLowestTier()
        {
            var firstRevealed = new HashSet<SymptomCardSO>();
            for (int seed = 0; seed < 20; seed++)
            {
                Initialise(SymptomTier.Tier1, SymptomTier.Tier1, SymptomTier.Tier2);
                view.TeamAssess(SymptomTier.Tier1, new System.Random(seed));
                firstRevealed.Add(view.assignedSymptoms[0]);
            }
            Assert.Greater(firstRevealed.Count, 1, "Either Tier 1 symptom can be the one revealed.");
        }

        [Test]
        public void Handover_NextHiddenSymptomIsTier1ForADiagnosticCard()
        {
            Initialise(true, SymptomTier.Tier1, SymptomTier.Tier1, SymptomTier.Tier3);

            Assert.IsTrue(view.TryDiagnoseNext(SymptomTier.Tier1));
            Assert.AreEqual(2, view.diagnosedCount);
            Assert.AreEqual(SymptomTier.Tier1, view.assignedTiers[1]);
        }

        [Test]
        public void Handover_TeamAssessPicksAmongTheLowestHiddenTier()
        {
            var revealed = new HashSet<SymptomCardSO>();
            for (int seed = 0; seed < 20; seed++)
            {
                Initialise(true, SymptomTier.Tier1, SymptomTier.Tier1, SymptomTier.Tier3);
                SymptomCardSO primary = view.assignedSymptoms[0];

                Assert.IsTrue(view.TeamAssess(SymptomTier.Tier1, new System.Random(seed)));
                Assert.AreSame(primary, view.assignedSymptoms[0], "The handed-over symptom stays revealed in place.");
                Assert.AreEqual(SymptomTier.Tier1, view.assignedTiers[1]);
                revealed.Add(view.assignedSymptoms[1]);
            }
            Assert.Greater(revealed.Count, 1, "Either hidden Tier 1 symptom can be the one revealed.");
        }

        [Test]
        public void TeamAssess_BlockedWhenTheLowestHiddenSymptomIsAboveTheTier()
        {
            Initialise(SymptomTier.Tier2);
            Assert.IsFalse(view.CanTeamAssess(SymptomTier.Tier1));
            Assert.IsFalse(view.TeamAssess(SymptomTier.Tier1));
            Assert.AreEqual(0, view.diagnosedCount);
        }

        [Test]
        public void TeamAssess_RequiresAWardBed()
        {
            Initialise(SymptomTier.Tier1);
            go.transform.SetParent(null);
            Assert.IsFalse(view.CanTeamAssess(SymptomTier.Tier3));
        }
    }
}
