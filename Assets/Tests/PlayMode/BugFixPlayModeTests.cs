using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using ChiefComplaint.Board;
using ChiefComplaint.Cards;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

public class BugFixPlayModeTests
{
    private bool sceneLoaded = false;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        if (!sceneLoaded)
        {
            SceneManager.LoadScene("MainBoard");
            yield return null; // Wait for scene to load
            sceneLoaded = true;
        }
        else
        {
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator P0_1_DeckManager_HandSync_DuplicateCardRemoval_DoesNotDesync()
    {
        var deckManager = DeckManager.Instance;
        var handView = HandView.Instance;
        Assert.IsNotNull(deckManager, "DeckManager should be present in the scene.");
        
        if (handView == null)
        {
            var go = new GameObject();
            handView = go.AddComponent<HandView>();
            handView.handZone = go.transform;
            var prefab = new GameObject("DummyCard");
            prefab.AddComponent<ActionCardView>();
            handView.actionCardPrefab = prefab;
        }
        
        yield return null;

        int initialDataCount = deckManager.handData.Count;
        int initialVisualsCount = handView.activeCards.Count;

        var testCard = ScriptableObject.CreateInstance<ActionCardSO>();
        testCard.cardName = "Test Card";

        deckManager.AddCardToHand(testCard);
        deckManager.AddCardToHand(testCard);

        Assert.AreEqual(initialDataCount + 2, deckManager.handData.Count, "Hand data should increase by 2.");
        Assert.AreEqual(initialVisualsCount + 2, handView.activeCards.Count, "Hand visuals should increase by 2.");

        var firstVisual = handView.activeCards[handView.activeCards.Count - 2];
        var secondVisual = handView.activeCards[handView.activeCards.Count - 1];

        handView.RemoveCardVisual(secondVisual.gameObject);
        deckManager.RemoveCardFromHand(testCard, false);

        Assert.AreEqual(initialDataCount + 1, deckManager.handData.Count, "Hand data should have decreased by 1.");
        Assert.AreEqual(initialVisualsCount + 1, handView.activeCards.Count, "Hand visuals should have decreased by 1.");
        
        Assert.IsTrue(handView.activeCards.Contains(firstVisual), "The first duplicate visual should still be in the list.");
        Assert.IsFalse(handView.activeCards.Contains(secondVisual), "The second duplicate visual should be removed.");

        yield return null;
    }

    [UnityTest]
    public IEnumerator P0_2_BoardManager_TriageSlot_DoesNotLeakWhenPatientMoves()
    {
        var boardManager = Object.FindFirstObjectByType<BoardManager>();
        Assert.IsNotNull(boardManager, "BoardManager should be present.");

        foreach(var patient in boardManager.ActivePatients)
        {
            Object.Destroy(patient.gameObject);
        }
        yield return null;

        boardManager.SpawnPatient();
        yield return null;

        Assert.AreEqual(1, boardManager.ActivePatients.Count, "One patient should be active.");
        
        var patientView = boardManager.ActivePatients[0];
        Assert.IsTrue(patientView.transform.parent.CompareTag("TriageSlot"), "Patient should spawn in a TriageSlot.");

        var wardSlot = BoardView.Instance.wardSlots[0];
        patientView.transform.SetParent(wardSlot);
        patientView.assignedWardSlot = wardSlot;
        
        for(int i = 0; i < BoardView.Instance.triageSlots.Length; i++)
        {
            boardManager.SpawnPatient();
        }
        yield return null;

        Assert.IsTrue(boardManager.ActivePatients.Count > BoardView.Instance.triageSlots.Length, "Should be able to spawn patients beyond triage capacity if moved to wards.");
    }

    [UnityTest]
    public IEnumerator P0_3_GameManager_ShiftStartOrdering_DoesNotOverwriteTriggers()
    {
        var gameManager = GameManager.Instance;
        var patientDeck = PatientDeckManager.Instance;
        var boardManager = Object.FindFirstObjectByType<BoardManager>();
        Assert.IsNotNull(gameManager);
        
        var triggerPatient = ScriptableObject.CreateInstance<PatientCardSO>();
        triggerPatient.patientName = "Trigger Test";
        triggerPatient.startingStability = 10;
        triggerPatient.triggers = new System.Collections.Generic.List<PatientTrigger>
        {
            new PatientTrigger { condition = PatientTriggerCondition.OnSpawn, effect = PatientTriggerEffect.GainAP, effectValue = 2 }
        };

        foreach(var patient in boardManager.ActivePatients)
        {
            Object.Destroy(patient.gameObject);
        }
        yield return null;

        patientDeck.activePatientDeck.Clear();
        patientDeck.activePatientDeck.Add(triggerPatient);        
        gameManager.StartNewShift();
        yield return null;
        
        Assert.AreEqual(gameManager.baseAP + 2, gameManager.CurrentAP, "AP should include the OnSpawn trigger bonus after shift reset.");
    }
}
