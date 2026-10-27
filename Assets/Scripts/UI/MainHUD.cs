using UnityEngine;
using TMPro;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.StaffRoom;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.UI
{
    /// <summary>
    /// Dedicated UI controller for the main game HUD. 
    /// Subscribes to events from the GameManager and other logic systems to display data.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class MainHUD : MonoBehaviour
    {
        [Header("Economy UI")]
        public TextMeshProUGUI apText;
        public TextMeshProUGUI repText;
        public TextMeshProUGUI fundingText;

        [Header("Progression UI")]
        public TextMeshProUGUI shiftText;
        public TextMeshProUGUI curedText;
        public TextMeshProUGUI escalationText;

        [Header("Deck UI")]
        public TextMeshProUGUI deckCountText;
        public TextMeshProUGUI discardCountText;

        [Header("Warning UI")]
        public TextMeshProUGUI codeBlueText;

        private void Start()
        {
            // Subscribe to GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnAPChanged += UpdateAPDisplay;
                GameManager.Instance.OnReputationChanged += UpdateReputationDisplay;
                GameManager.Instance.OnFundingChanged += UpdateFundingDisplay;
                GameManager.Instance.OnShiftChanged += UpdateShiftDisplay;

                // Fetch initial values
                UpdateAPDisplay(GameManager.Instance.CurrentAP);
                UpdateReputationDisplay(GameManager.Instance.CurrentReputation);
                UpdateFundingDisplay(GameManager.Instance.CurrentFunding);
                UpdateShiftDisplay(GameManager.Instance.CurrentShift);

                if (GameManager.Instance.boardManager != null)
                {
                    GameManager.Instance.boardManager.OnCodeBlueCountChanged += UpdateCodeBlueDisplay;
                    GameManager.Instance.boardManager.NotifyCodeBlueStatus();
                }
            }

            // Subscribe to PatientDeckManager events
            if (PatientDeckManager.Instance != null)
            {
                PatientDeckManager.Instance.OnPatientsCuredChanged += UpdateCuredDisplay;
                PatientDeckManager.Instance.OnEscalationPhaseChanged += UpdateEscalationDisplay;

                UpdateCuredDisplay(PatientDeckManager.Instance.patientsCured);
                UpdateEscalationDisplay(PatientDeckManager.Instance.GetEscalationPhase());
            }

            // Subscribe to DeckManager events
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.OnDeckCountChanged += UpdateDeckCountDisplay;
                DeckManager.Instance.OnDiscardCountChanged += UpdateDiscardCountDisplay;

                UpdateDeckCountDisplay(DeckManager.Instance.drawPile.Count);
                UpdateDiscardCountDisplay(DeckManager.Instance.discardPile.Count);
            }
        }

        private void OnDestroy()
        {
            // Always unsubscribe to prevent memory leaks
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnAPChanged -= UpdateAPDisplay;
                GameManager.Instance.OnReputationChanged -= UpdateReputationDisplay;
                GameManager.Instance.OnFundingChanged -= UpdateFundingDisplay;
                GameManager.Instance.OnShiftChanged -= UpdateShiftDisplay;

                if (GameManager.Instance.boardManager != null)
                {
                    GameManager.Instance.boardManager.OnCodeBlueCountChanged -= UpdateCodeBlueDisplay;
                }
            }

            if (PatientDeckManager.Instance != null)
            {
                PatientDeckManager.Instance.OnPatientsCuredChanged -= UpdateCuredDisplay;
                PatientDeckManager.Instance.OnEscalationPhaseChanged -= UpdateEscalationDisplay;
            }

            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.OnDeckCountChanged -= UpdateDeckCountDisplay;
                DeckManager.Instance.OnDiscardCountChanged -= UpdateDiscardCountDisplay;
            }
        }

        private void UpdateAPDisplay(int newAP)
        {
            if (apText != null) apText.text = $"AP: {newAP}";
        }

        private void UpdateReputationDisplay(int newReputation)
        {
            if (repText != null) repText.text = $"Reputation: {newReputation}";
        }

        private void UpdateFundingDisplay(int newFunding)
        {
            if (fundingText != null) fundingText.text = $"Funding: {newFunding}";
        }

        private void UpdateShiftDisplay(int newShift)
        {
            int goal = GameManager.Instance != null ? GameManager.Instance.shiftsToWin : 0;
            if (shiftText != null && goal > 0) { shiftText.text = $"Shift: {newShift} / {goal}"; return; }
            if (shiftText != null) shiftText.text = $"Shift: {newShift}";
        }

        private void UpdateCuredDisplay(int newCuredCount)
        {
            if (curedText != null) curedText.text = $"Patients Cured: {newCuredCount}";
        }

        private void UpdateEscalationDisplay(string phase)
        {
            if (escalationText != null) escalationText.text = $"Escalation: {phase}";
        }

        private void UpdateDeckCountDisplay(int count)
        {
            if (deckCountText != null) deckCountText.text = $"Deck: {count}";
        }

        private void UpdateDiscardCountDisplay(int count)
        {
            if (discardCountText != null) discardCountText.text = $"Discard: {count}";
        }

        private void UpdateCodeBlueDisplay(int criticalCount)
        {
            if (codeBlueText != null)
            {
                if (criticalCount > 0)
                {
                    codeBlueText.text = $"CODE BLUE: {criticalCount} Critical!";
                    codeBlueText.color = Color.red;
                }
                else
                {
                    codeBlueText.text = "Code Blue: None";
                    codeBlueText.color = Color.white;
                }
            }
        }
    }

}
