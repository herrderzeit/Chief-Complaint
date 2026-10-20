using UnityEngine;
using TMPro;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Core
{
    /// <summary>Top-level phases of a run. Input handlers use this to know when the board is interactive.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public enum GamePhase
    {
        /// <summary>The player is working a shift: cards and patients can be dragged.</summary>
        Shift,
        /// <summary>The inter-shift draft/market panel is open.</summary>
        Draft,
        /// <summary>The hospital has gone bankrupt; the run is over.</summary>
        GameOver,
        /// <summary>A tutorial dialog or prompt is currently active.</summary>
        Tutorial,
        /// <summary>The player pressed Escape and is deciding whether to quit; the board is locked.</summary>
        Paused
    }

    /// <summary>
    /// Central game orchestrator. Owns the AP and Reputation economy, drives the
    /// shift loop, and delegates board/draft phase control to its sub-managers.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class GameManager : Singleton<GameManager>
    {
        /// <summary>PlayerPrefs key set once the player has completed or skipped the tutorial.</summary>
        public const string TutorialCompletedKey = "TutorialCompleted";

        [Header("Sub-Managers")]
        public BoardManager boardManager;
        public DraftManager draftManager;

        [Header("Game Over")]
        /// <summary>The full-screen Game Over overlay. Assign in the Inspector.</summary>
        public GameOverScreen gameOverScreen;

        // Economy Events
        public event System.Action<int> OnAPChanged;
        public event System.Action<int> OnReputationChanged;
        /// <summary>Raised whenever the Activity Funding balance changes.</summary>
        public event System.Action<int> OnFundingChanged;
        public event System.Action<int> OnShiftChanged;

        /// <summary>Raised whenever <see cref="CurrentPhase"/> changes.</summary>
        public event System.Action<GamePhase> OnPhaseChanged;

        [Header("Economy Stats")]
        /// <summary>Action Points restored to the player at the start of every shift.</summary>
        public int baseAP = 3;
        [SerializeField] private int currentAP;
        [SerializeField] private int currentReputation = 10;
        [Tooltip("Activity Funding the run starts with. Spent on draft cards and card upgrades.")]
        [SerializeField] private int currentFunding = 6;
        [SerializeField] private int currentShift = 1;

        [Header("Run Balance")]
        [Tooltip("Number of patients placed in Triage when a run begins.")]
        public int initialPatientCount = 2;
        [Tooltip("Reputation lost when a patient dies (Code Blue).")]
        public int deathReputationPenalty = 5;

        [Header("Activity Funding")]
        [Tooltip("Funding paid for curing a Walk-In patient.")]
        public int walkInFundingReward = 2;
        [Tooltip("Funding paid for curing an Ambulance patient.")]
        public int ambulanceFundingReward = 6;
        [Tooltip("Funding paid for curing a Trauma Flight patient.")]
        public int traumaFlightFundingReward = 8;
        [Tooltip("Maximum Reputation a cure can award. Reputation is the life total; Funding is the spending currency.")]
        public int cureReputationCap = 3;

        /// <summary>Action Points the player can currently spend.</summary>
        public int CurrentAP => currentAP;

        /// <summary>The hospital's current Reputation (also the lose condition).</summary>
        public int CurrentReputation => currentReputation;

        /// <summary>Activity Funding the player can currently spend on drafts and upgrades.</summary>
        public int CurrentFunding => currentFunding;

        /// <summary>The current shift number, starting at 1.</summary>
        public int CurrentShift => currentShift;

        /// <summary>The phase the run is currently in.</summary>
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Shift;

        /// <summary><c>true</c> when the board (cards and patients) may be interacted with.</summary>
        public bool CanInteract => CurrentPhase == GamePhase.Shift;

        private bool isGameOver = false;

        private void Start()
        {
            if (gameOverScreen != null) gameOverScreen.gameObject.SetActive(false);

            if (PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 0)
            {
                SetPhase(GamePhase.Tutorial);
                // Call TutorialManager to show prompt. It will call StartRun when done.
                ChiefComplaint.UI.TutorialManager.ShowStartPrompt(() => StartRun());
            }
            else
            {
                StartRun();
            }
        }

        private GamePhase phaseBeforePause;

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                HandleEscape();
            }
        }

        /// <summary>
        /// Escape asks for confirmation during play so a stray key press can't lose a run.
        /// It is ignored while another modal (the tutorial prompt) owns the shared popup,
        /// and quits immediately on the Game Over screen where there is nothing left to lose.
        /// </summary>
        private void HandleEscape()
        {
            switch (CurrentPhase)
            {
                case GamePhase.GameOver:
                    QuitGame();
                    break;
                case GamePhase.Shift:
                case GamePhase.Draft:
                    phaseBeforePause = CurrentPhase;
                    SetPhase(GamePhase.Paused);
                    ChoicePrompt.Show("Quit the game?",
                        "Resume", () => SetPhase(phaseBeforePause),
                        "Quit", QuitGame);
                    break;
            }
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // -------------------------------------------------------------------------
        // Run / Shift Loop
        // -------------------------------------------------------------------------

        /// <summary>
        /// Bootstraps a fresh run in a deterministic order, independent of Unity's Start order:
        /// restore AP, build and draw the player's deck, then place the opening patients.
        /// Patient triggers fire last so they are never overwritten by the setup steps.
        /// </summary>
        private void StartRun()
        {
            SetPhase(GamePhase.Shift);
            ResetAP();

            if (DeckManager.Instance != null) DeckManager.Instance.StartRun();

            if (boardManager != null)
            {
                for (int i = 0; i < initialPatientCount; i++) boardManager.SpawnPatient();
                boardManager.NotifyCodeBlueStatus();
            }
        }

        /// <summary>
        /// Advances the game to the next shift in a fixed order:
        /// restore AP → decay patients and reset departments → draw a new hand → spawn a new patient.
        /// Resetting AP and drawing before spawning means patient triggers (GainAP, DrawCard, etc.)
        /// are applied on top of the fresh shift rather than being wiped by it.
        /// </summary>
        public void StartNewShift()
        {
            if (CurrentPhase == GamePhase.GameOver) return;

            SetPhase(GamePhase.Shift);

            currentShift++;
            OnShiftChanged?.Invoke(currentShift);

            ResetAP();

            if (boardManager != null) boardManager.AdvanceShift();

            // A patient death during decay can end the run.
            if (CurrentPhase == GamePhase.GameOver) return;

            // DeckManager is a Singleton because cards need global access to it;
            // GameManager commands it here during the shift-state transition.
            if (DeckManager.Instance != null) DeckManager.Instance.StartNewShift();

            if (boardManager != null) boardManager.SpawnPatient();
        }

        /// <summary>Opens the inter-shift draft/market panel. Ignored unless a shift is in progress.</summary>
        public void EnterDraftPhase()
        {
            if (CurrentPhase != GamePhase.Shift) return;

            // Patients finish their surgeries: discharges pay out before the draft opens.
            if (boardManager != null) boardManager.ResolveSurgeries();

            SetPhase(GamePhase.Draft);
            if (DeckManager.Instance != null) DeckManager.Instance.DiscardHand();
            if (draftManager != null) draftManager.OpenDraftPhase();
        }

        private void SetPhase(GamePhase phase)
        {
            if (CurrentPhase == phase) return;
            CurrentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }

        // -------------------------------------------------------------------------
        // Economy
        // -------------------------------------------------------------------------

        /// <summary>Deducts <paramref name="amount"/> Action Points (never below zero) and refreshes the UI.</summary>
        /// <param name="amount">Number of AP to spend.</param>
        public void SpendAP(int amount)
        {
            currentAP = Mathf.Max(0, currentAP - amount);
            OnAPChanged?.Invoke(currentAP);
        }

        /// <summary>Adds <paramref name="amount"/> Action Points and refreshes the UI.</summary>
        /// <param name="amount">Number of AP to grant.</param>
        public void GainAP(int amount)
        {
            currentAP += amount;
            OnAPChanged?.Invoke(currentAP);
        }

        /// <summary>Returns the base Activity Funding paid for curing a patient of the given escalation type.</summary>
        /// <param name="type">The patient's escalation phase.</param>
        public int GetFundingReward(EscalationType type)
        {
            switch (type)
            {
                case EscalationType.Ambulance:    return ambulanceFundingReward;
                case EscalationType.TraumaFlight: return traumaFlightFundingReward;
                default:                          return walkInFundingReward;
            }
        }

        /// <summary>Adds Activity Funding (income or a refund) and refreshes the UI.</summary>
        /// <param name="amount">Funding to add.</param>
        public void AddFunding(int amount)
        {
            currentFunding += amount;
            OnFundingChanged?.Invoke(currentFunding);
            GameLog.Info($"Gained {amount} Funding! Total: {currentFunding}");
        }

        /// <summary>
        /// Spends Activity Funding if the player can afford it. Unlike Reputation, running out never ends the run.
        /// </summary>
        /// <param name="amount">Funding to spend.</param>
        /// <returns><c>true</c> if the Funding was spent; <c>false</c> if the balance was too low.</returns>
        public bool TrySpendFunding(int amount)
        {
            if (amount > currentFunding) return false;
            currentFunding -= amount;
            OnFundingChanged?.Invoke(currentFunding);
            GameLog.Info($"Spent {amount} Funding! Total: {currentFunding}");
            return true;
        }

        /// <summary>Awards Reputation and refreshes the UI.</summary>
        /// <param name="amount">Reputation points to add.</param>
        public void AddReputation(int amount)
        {
            currentReputation += amount;
            OnReputationChanged?.Invoke(currentReputation);
            GameLog.Info($"Gained {amount} Reputation! Total: {currentReputation}");
        }

        /// <summary>
        /// Deducts Reputation (e.g. malpractice penalties) and refreshes the UI.
        /// Automatically triggers Game Over if Reputation reaches zero or below.
        /// </summary>
        /// <param name="amount">Reputation points to remove.</param>
        public void SpendReputation(int amount)
        {
            currentReputation -= amount;
            OnReputationChanged?.Invoke(currentReputation);
            GameLog.Info($"Spent {amount} Reputation! Total: {currentReputation}");

            if (currentReputation <= 0)
            {
                currentReputation = 0;
                OnReputationChanged?.Invoke(currentReputation);
                TriggerGameOver();
            }
        }

        /// <summary>Restores <see cref="currentAP"/> to <see cref="baseAP"/> at shift start.</summary>
        public void ResetAP()
        {
            currentAP = baseAP;
            OnAPChanged?.Invoke(currentAP);
            GameLog.Info("AP Refreshed to " + baseAP);
        }

        // -------------------------------------------------------------------------
        // Game Over
        // -------------------------------------------------------------------------

        /// <summary>
        /// Activates the Game Over screen when the hospital goes bankrupt (Reputation ≤ 0).
        /// Guarded by <see cref="isGameOver"/> so it can only fire once per run.
        /// </summary>
        public void TriggerGameOver()
        {
            if (isGameOver) return;
            isGameOver = true;
            SetPhase(GamePhase.GameOver);

            GameLog.Warn("GAME OVER: The hospital has gone bankrupt from malpractice!");

            if (gameOverScreen != null)
            {
                gameOverScreen.gameObject.SetActive(true);
                gameOverScreen.Show();
            }
            else
            {
                Debug.LogWarning("GameManager: No GameOverScreen assigned in the Inspector!");
            }
        }
    }
}
