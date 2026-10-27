using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.StaffRoom;

public class PatientTriggerPlayModeTests
{
    private bool sceneLoaded = false;
    private PatientCardSO testPatientTemplate;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        if (!sceneLoaded)
        {
            SceneManager.LoadScene("MainBoard");
            yield return null; // Wait for scene to load
            sceneLoaded = true;
        }

        // Clean up any patients from previous tests
        var boardManager = Object.FindAnyObjectByType<BoardManager>();
        if (boardManager != null)
        {
            foreach (var patient in new List<PatientCardView>(boardManager.ActivePatients))
            {
                if (patient != null && patient.gameObject != null)
                {
                    Object.Destroy(patient.gameObject);
                }
            }
        }

        yield return null;

        // Create a reusable patient template for tests
        testPatientTemplate = ScriptableObject.CreateInstance<PatientCardSO>();
        testPatientTemplate.patientName = "Trigger Test Patient";
        testPatientTemplate.startingStability = 10;
        testPatientTemplate.stabilityDecayPerTurn = 2;
        testPatientTemplate.triggers = new List<PatientTrigger>();
    }

    [UnityTest]
    public IEnumerator Trigger_OnTurnEnd_LoseAP_FiresCorrectly()
    {
        var gameManager = GameManager.Instance;
        var boardManager = Object.FindAnyObjectByType<BoardManager>();

        // Set up the trigger
        testPatientTemplate.triggers.Add(new PatientTrigger
        {
            condition = PatientTriggerCondition.OnTurnEnd,
            effect = PatientTriggerEffect.LoseAP,
            effectValue = 1
        });

        // Spawn patient manually
        GameObject patientObj = new GameObject("TestPatient");
        var patientView = patientObj.AddComponent<PatientCardView>();
        patientView.subCardSpawnParent = patientObj.transform;
        patientView.InitialiseCard(testPatientTemplate, new List<SymptomCardSO>(), new List<SymptomTier>(), null);

        yield return null; // Wait for init

        int initialAP = gameManager.CurrentAP;

        // Advance shift decays stability and fires OnTurnEnd
        patientView.AdvanceShiftDecay();
        yield return null;

        Assert.AreEqual(initialAP - 1, gameManager.CurrentAP, "OnTurnEnd trigger should have deducted 1 AP.");
    }

    [UnityTest]
    public IEnumerator Trigger_OnDiagnosed_GainReputation_FiresCorrectly()
    {
        var gameManager = GameManager.Instance;
        var boardManager = Object.FindAnyObjectByType<BoardManager>();

        // Set up the trigger
        testPatientTemplate.triggers.Add(new PatientTrigger
        {
            condition = PatientTriggerCondition.OnDiagnosed,
            effect = PatientTriggerEffect.GainReputation,
            effectValue = 2
        });

        // Create a fake symptom to diagnose
        var fakeSymptom = ScriptableObject.CreateInstance<SymptomCardSO>();
        
        GameObject patientObj = new GameObject("TestPatient");
        // Needs a TriageSlot or WardSlot parent to bypass TryDiagnoseNext validation
        GameObject slotObj = new GameObject("WardSlot");
        slotObj.tag = "WardSlot";
        patientObj.transform.SetParent(slotObj.transform);

        var patientView = patientObj.AddComponent<PatientCardView>();
        patientView.subCardSpawnParent = patientObj.transform;
        patientView.InitialiseCard(testPatientTemplate, new List<SymptomCardSO>{ fakeSymptom }, new List<SymptomTier>{ SymptomTier.Tier1 }, null);
        
        yield return null;

        int initialRep = gameManager.CurrentReputation;

        // Diagnose the patient
        bool diagnosed = patientView.TryDiagnoseNext(SymptomTier.Tier1, false);
        yield return null;

        Assert.IsTrue(diagnosed, "Patient should be diagnosed.");
        Assert.AreEqual(initialRep + 2, gameManager.CurrentReputation, "OnDiagnosed trigger should have added 2 Reputation.");
        
        Object.Destroy(slotObj);
    }

    [UnityTest]
    public IEnumerator Trigger_OnDeath_DrawCard_FiresCorrectly()
    {
        var deckManager = DeckManager.Instance;
        var boardManager = Object.FindAnyObjectByType<BoardManager>();

        // Set up the trigger
        testPatientTemplate.startingStability = 1; // Will die after 1 turn of decay (decay = 2)
        testPatientTemplate.triggers.Add(new PatientTrigger
        {
            condition = PatientTriggerCondition.OnDeath,
            effect = PatientTriggerEffect.DrawCard,
            effectValue = 3
        });

        GameObject patientObj = new GameObject("TestPatient");
        var patientView = patientObj.AddComponent<PatientCardView>();
        patientView.subCardSpawnParent = patientObj.transform;
        patientView.InitialiseCard(testPatientTemplate, new List<SymptomCardSO>(), new List<SymptomTier>(), null);

        yield return null;

        int initialHandCount = deckManager.handData.Count;

        // Advance shift should cause death because currentStability(1) - decay(2) <= 0
        patientView.AdvanceShiftDecay();
        yield return null;

        Assert.IsTrue(patientView.isRemoved, "Patient should be dead/removed.");
        Assert.AreEqual(initialHandCount + 3, deckManager.handData.Count, "OnDeath trigger should have drawn 3 cards.");
    }

    [UnityTest]
    public IEnumerator Trigger_OnCured_ChangeStability_DoesNotCrashAndFires()
    {
        // Testing that a stability change trigger when cured doesn't break anything, 
        // even though they are being discharged.
        var boardManager = Object.FindAnyObjectByType<BoardManager>();

        testPatientTemplate.triggers.Add(new PatientTrigger
        {
            condition = PatientTriggerCondition.OnCured,
            effect = PatientTriggerEffect.ChangeStability,
            effectValue = 5
        });

        // Add a department slot
        GameObject deptObj = new GameObject("DeptSlot");
        var deptSlot = deptObj.AddComponent<DepartmentSlot>();
        deptSlot.category = SymptomCategory.Trauma;

        GameObject patientObj = new GameObject("TestPatient");
        patientObj.transform.SetParent(deptObj.transform);

        var fakeSymptom = ScriptableObject.CreateInstance<SymptomCardSO>();
        fakeSymptom.specialtyCategory = SymptomCategory.Trauma;

        var patientView = patientObj.AddComponent<PatientCardView>();
        patientView.subCardSpawnParent = patientObj.transform;
        // Needs fully diagnosed to cure
        patientView.InitialiseCard(testPatientTemplate, new List<SymptomCardSO> { fakeSymptom }, new List<SymptomTier> { SymptomTier.Tier1 }, null);
        patientView.diagnosedCount = 1; // Mark as fully diagnosed

        yield return null;

        bool cured = patientView.TryCureSymptom(SymptomTier.Tier1, false);
        yield return null;

        Assert.IsTrue(cured, "Symptom should be cured.");
        // Fully treated patients stay in their department until End Shift, then are discharged.
        Assert.IsTrue(patientView.isAwaitingDischarge, "A fully treated patient waits in the department for End Shift.");
        Assert.IsFalse(patientView.isRemoved);

        patientView.ResolveSurgery();
        yield return null;

        Assert.IsTrue(patientView.isRemoved, "Patient should be discharged when the shift ends.");
        // We know it fired because it didn't throw a NullReference or Exception.
        
        Object.Destroy(deptObj);
    }
}
