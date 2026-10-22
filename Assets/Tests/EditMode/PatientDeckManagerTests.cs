using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using ChiefComplaint.Core;
using ChiefComplaint.Cards.Data;

namespace ChiefComplaint.Tests.EditMode
{
    public class PatientDeckManagerTests
    {
        private GameObject go;
        private PatientDeckManager patientDeckManager;
        // Every ScriptableObject made at runtime, destroyed in TearDown so none leak between tests.
        private readonly List<ScriptableObject> createdAssets = new List<ScriptableObject>();

        private T NewAsset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            createdAssets.Add(asset);
            return asset;
        }

        [SetUp]
        public void Setup()
        {
            go = new GameObject();
            patientDeckManager = go.AddComponent<PatientDeckManager>();

            // Setup Symptom Decks
            patientDeckManager.internalDeck = new List<SymptomCardSO>();
            for (int i = 0; i < 5; i++)
            {
                var sym = NewAsset<SymptomCardSO>();
                sym.specialtyCategory = SymptomCategory.Internal;
                patientDeckManager.internalDeck.Add(sym);
            }
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(go);
            foreach (var asset in createdAssets) if (asset != null) Object.DestroyImmediate(asset);
            createdAssets.Clear();
        }

        [Test]
        public void GenerateSymptomsForPatient_AssignsCorrectTiersBasedOnTriage()
        {
            // Triage 1 -> Tier 3
            var criticalPatient = NewAsset<PatientCardSO>();
            criticalPatient.triageLevel = 1;
            criticalPatient.incomingCategories = new List<SymptomCategory> { SymptomCategory.Internal };

            var symptoms = patientDeckManager.GenerateSymptomsForPatient(criticalPatient);
            
            Assert.AreEqual(1, symptoms.Count);
            Assert.AreEqual(SymptomTier.Tier3, symptoms[0].Tier, "Triage 1 patient should get Tier 3 primary symptom.");

            // Triage 4 -> Tier 1
            var minorPatient = NewAsset<PatientCardSO>();
            minorPatient.triageLevel = 4;
            minorPatient.incomingCategories = new List<SymptomCategory> { SymptomCategory.Internal };

            var minorSymptoms = patientDeckManager.GenerateSymptomsForPatient(minorPatient);
            
            Assert.AreEqual(1, minorSymptoms.Count);
            Assert.AreEqual(SymptomTier.Tier1, minorSymptoms[0].Tier, "Triage 4 patient should get Tier 1 primary symptom.");
        }
        
        [Test]
        public void GenerateSymptomsForPatient_MultipleCategories_SubsequentAreTier1()
        {
            var patient = NewAsset<PatientCardSO>();
            patient.triageLevel = 2; // Tier 2 primary
            patient.incomingCategories = new List<SymptomCategory> { SymptomCategory.Internal, SymptomCategory.Internal };

            var symptoms = patientDeckManager.GenerateSymptomsForPatient(patient);
            
            Assert.AreEqual(2, symptoms.Count);
            Assert.AreEqual(SymptomTier.Tier2, symptoms[0].Tier, "Primary symptom should be Tier 2.");
            Assert.AreEqual(SymptomTier.Tier1, symptoms[1].Tier, "Secondary symptom should default to Tier 1.");
        }

        // The full ESI mapping (mirrored by the simulator's Game.TierFromTriage).
        [TestCase(1, SymptomTier.Tier3)]
        [TestCase(2, SymptomTier.Tier2)]
        [TestCase(3, SymptomTier.Tier2)]
        [TestCase(4, SymptomTier.Tier1)]
        [TestCase(5, SymptomTier.Tier1)]
        public void DetermineTierFromTriage_MapsEveryTriageLevel(int triage, SymptomTier expected)
        {
            Assert.AreEqual(expected, PatientDeckManager.DetermineTierFromTriage(triage), $"Triage {triage}");
        }
    }
}
