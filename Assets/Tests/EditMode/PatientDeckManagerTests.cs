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

        [SetUp]
        public void Setup()
        {
            go = new GameObject();
            patientDeckManager = go.AddComponent<PatientDeckManager>();

            // Setup Symptom Decks
            patientDeckManager.internalDeck = new List<SymptomCardSO>();
            for (int i = 0; i < 5; i++)
            {
                var sym = ScriptableObject.CreateInstance<SymptomCardSO>();
                sym.specialtyCategory = SymptomCategory.Internal;
                patientDeckManager.internalDeck.Add(sym);
            }
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(go);
        }

        [Test]
        public void GenerateSymptomsForPatient_AssignsCorrectTiersBasedOnTriage()
        {
            // Triage 1 -> Tier 3
            var criticalPatient = ScriptableObject.CreateInstance<PatientCardSO>();
            criticalPatient.triageLevel = 1;
            criticalPatient.incomingCategories = new List<SymptomCategory> { SymptomCategory.Internal };

            var symptoms = patientDeckManager.GenerateSymptomsForPatient(criticalPatient);
            
            Assert.AreEqual(1, symptoms.Count);
            Assert.AreEqual(SymptomTier.Tier3, symptoms[0].Tier, "Triage 1 patient should get Tier 3 primary symptom.");

            // Triage 4 -> Tier 1
            var minorPatient = ScriptableObject.CreateInstance<PatientCardSO>();
            minorPatient.triageLevel = 4;
            minorPatient.incomingCategories = new List<SymptomCategory> { SymptomCategory.Internal };

            var minorSymptoms = patientDeckManager.GenerateSymptomsForPatient(minorPatient);
            
            Assert.AreEqual(1, minorSymptoms.Count);
            Assert.AreEqual(SymptomTier.Tier1, minorSymptoms[0].Tier, "Triage 4 patient should get Tier 1 primary symptom.");
        }
        
        [Test]
        public void GenerateSymptomsForPatient_MultipleCategories_SubsequentAreTier1()
        {
            var patient = ScriptableObject.CreateInstance<PatientCardSO>();
            patient.triageLevel = 2; // Tier 2 primary
            patient.incomingCategories = new List<SymptomCategory> { SymptomCategory.Internal, SymptomCategory.Internal };

            var symptoms = patientDeckManager.GenerateSymptomsForPatient(patient);
            
            Assert.AreEqual(2, symptoms.Count);
            Assert.AreEqual(SymptomTier.Tier2, symptoms[0].Tier, "Primary symptom should be Tier 2.");
            Assert.AreEqual(SymptomTier.Tier1, symptoms[1].Tier, "Secondary symptom should default to Tier 1.");
        }
    }
}
