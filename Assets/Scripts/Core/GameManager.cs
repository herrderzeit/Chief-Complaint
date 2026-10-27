using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.StaffRoom;
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
        /// <summary>The inter-shift staff room panel is open.</summary>
        StaffRoom,
        /// <summary>The hospital has gone bankrupt; the run is over.</summary>
        GameOver,
        /// <summary>A tutorial dialog or prompt is currently active.</summary>
        Tutorial,
        /// <summary>The player pressed Escape and is deciding whether to quit; the board is locked.</summary>
        Paused
    }

    /// <summary>
    /// Central game orchestrator. Owns the AP and Reputation economy, drives the
    /// shift loop, and delegates board/staff room control to its sub-managers.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class GameManager : Singleton<GameManager>
    {
        /// <summary>PlayerPrefs key set once the player has completed or skipped the tutorial.</summary>
        public const string TutorialCompletedKey = "TutorialCompleted";

        [Header("Sub-Managers")]
        public BoardManager boardManager;
        [FormerlySerializedAs("draftManager")] public StaffRoomManager staffRoomManager;

        [Header("Game Over")]
        /// <summary>The full-screen Game Over overlay. Assign in the Inspector.</summary>
        public GameOverScreen gameOverScreen;

        // Economy Events
        public event System.Action<int> OnAPChanged;
        public event System.Action<int> OnReputationChanged;
        /// <summary>Raised whenever the Activity Funding balance changes.</summary>
        public event System.Action<int> OnFundingChanged;
        /// <summary>Raised when an upgrade-token count changes: (colour, new count of that colour).</summary>
        public event System.Action<SymptomCategory, int> OnUpgradeTokensChanged;
        public event System.Action<int> OnShiftChanged;

        /// <summary>Raised whenever <see cref="CurrentPhase"/> changes.</summary>
        public event System.Action<GamePhase> OnPhaseChanged;

        [Header("Economy Stats")]
        /// <summary>Action Points restored to the player at the start of every shift.</summary>
        public int baseAP = 3;
        [SerializeField] private int currentAP;
        [SerializeField] private int currentReputation = 7;
        [Tooltip("Activity Funding the run starts with. Spent on market cards and card upgrades.")]
        [SerializeField] private int currentFunding = 10;
        [SerializeField] private int currentShift = 1;

        [Header("Run Balance")]
        [Tooltip("Number of patients placed in Triage when a run begins.")]
        public int initialPatientCount = 2;
        [Tooltip("An extra patient arrives at the start of every Nth shift, on top of the usual one (0 = off).")]
        [Min(0)] public int extraPatientEveryNShifts = 3;
        [Tooltip("Reputation can't be raised above this (0 = no cap), so a strong run can't bank an unlimited buffer.")]
        [Min(0)] public int maxReputation = 15;
        [Tooltip("Reputation lost when a patient dies (Code Blue).")]
        public int deathReputationPenalty = 5;

        [Tooltip("Reputation lost when an arriving patient has to be diverted because every Triage slot is full (0 = free).")]
        [SerializeField] private int diversionReputationPenalty = 3;

        /// <summary>Reputation lost per diverted patient (0 = diversion is free).</summary>
        public int DiversionReputationPenalty
        {
            get => diversionReputationPenalty;
            set => diversionReputationPenalty = Mathf.Max(0, value);
        }
        [Tooltip("When a patient dies, a random Red Tape card (incident paperwork) is added to the discard pile.")]
        public bool deathAddsRedTape = true;
        [Tooltip("Surviving this many shifts wins the run. 0 disables the win condition.")]
        [Min(0)] public int shiftsToWin = 15;

        [Header("Activity Funding")]
        [Tooltip("Funding paid for curing a Walk-In patient.")]
        public int walkInFundingReward = 3;
        [Tooltip("Funding paid for curing an Ambulance patient.")]
        public int ambulanceFundingReward = 6;
        [Tooltip("Funding paid for curing a Trauma Flight patient.")]
        public int traumaFlightFundingReward = 8;
        [Tooltip("Maximum Reputation a cure can award. Reputation is the life total; Funding is the spending currency.")]
        public int cureReputationCap = 2;

        /// <summary>Action Points the player can currently spend.</summary>
        public int CurrentAP => currentAP;

        /// <summary>The hospital's current Reputation (also the lose condition).</summary>
        public int CurrentReputation => currentReputation;

        /// <summary>Activity Funding the player can currently spend in the market and on upgrades.</summary>
        public int CurrentFunding => currentFunding;

        /// <summary>Symptom upgrade tokens earned this run (one colour per department).</summary>
        public UpgradeTokenWallet UpgradeTokens { get; } = new UpgradeTokenWallet();

        /// <summary>The current shift number, starting at 1.</summary>
        public int CurrentShift => currentShift;

        /// <summary>The phase the run is currently in.</summary>
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Shift;

        /// <summary><c>true</c> when the board (cards and patients) may be interacted with.</summary>
        public bool CanInteract => CurrentPhase == GamePhase.Shift && !ChoicePrompt.IsOpen && !PatientPicker.IsPicking;

        private bool isGameOver = false;

        private void Start()
        {
            if (gameOverScreen != null) gameOverScreen.gameObject.SetActive(false);
            if (boardManager != null) boardManager.OnPatientDiverted += DivertPatient;

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
            if (GameInput.Pause.WasPressedThisFrame())
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
            // A card's "choose one" prompt is open: answering it comes first, or its effect would be lost.
            if (ChoicePrompt.IsOpen) return;

            // Escape skips a pending patient pick (e.g. Team Assessment).
            if (PatientPicker.IsPicking) { PatientPicker.Cancel(); return; }

            switch (CurrentPhase)
            {
                case GamePhase.GameOver:
                    QuitGame();
                    break;
                case GamePhase.Shift:
                case GamePhase.StaffRoom:
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
            if (carriedAP > 0)
            {
                int carried = carriedAP;
                carriedAP = 0;
                GainAP(carried);
            }

            if (boardManager != null) boardManager.AdvanceShift();

            // A patient death during decay can end the run.
            if (CurrentPhase == GamePhase.GameOver) return;

            // DeckManager is a Singleton because cards need global access to it;
            // GameManager commands it here during the shift-state transition.
            if (DeckManager.Instance != null) DeckManager.Instance.StartNewShift();

            // Escalation can also be time-based, so check it before this shift's patient arrives.
            if (PatientDeckManager.Instance != null) PatientDeckManager.Instance.NotifyShiftStarted(currentShift);

            if (boardManager != null)
            {
                boardManager.SpawnPatient();
                // No extra arrival on the final shift (it could only cost Reputation through diversion), nor after a
                // diversion has already ended the run.
                if (CurrentPhase != GamePhase.GameOver && !IsFinalShift && IsExtraPatientShift(currentShift, extraPatientEveryNShifts))
                    boardManager.SpawnPatient();
            }
        }

        /// <summary>
        /// <c>true</c> when an extra patient arrives at the start of <paramref name="shift"/>: every
        /// <paramref name="everyNShifts"/>th shift (shift 3, 6, 9 … for 3), never when it is 0.
        /// </summary>
        public static bool IsExtraPatientShift(int shift, int everyNShifts) => everyNShifts > 0 && shift % everyNShifts == 0;

        /// <summary><c>true</c> during the last shift of a run with a win condition.</summary>
        public bool IsFinalShift => shiftsToWin > 0 && currentShift >= shiftsToWin;

        /// <summary>Opens the inter-shift staff room panel. Ignored unless a shift is in progress.</summary>
        public void EnterStaffRoom()
        {
            if (CurrentPhase != GamePhase.Shift) return;

            // Ending the shift abandons any pending patient pick.
            PatientPicker.Cancel();

            // Patients finish their surgeries: discharges pay out before the staff room opens.
            resolvingShiftEnd = true;
            if (boardManager != null) boardManager.ResolveSurgeries();
            resolvingShiftEnd = false;

            // Finishing the final shift wins the run instead of opening the staff room. There is deliberately no last round
            // of decay: the goal is to get through the final shift, and a decay after it decided runs on whoever
            // happened to be on the board (BalanceSim: most smart-bot losses moved to that last tick).
            if (IsFinalShift && CurrentPhase != GamePhase.GameOver)
            {
                TriggerVictory();
                return;
            }

            SetPhase(GamePhase.StaffRoom);
            if (DeckManager.Instance != null) DeckManager.Instance.DiscardHand();
            if (staffRoomManager != null) staffRoomManager.OpenStaffRoom();
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
            // AP gained while the shift is being wrapped up (e.g. an OnCured trigger at discharge) would be wiped by
            // the next shift's reset, so it carries over instead.
            if (resolvingShiftEnd) { carriedAP += amount; return; }
            currentAP += amount;
            OnAPChanged?.Invoke(currentAP);
        }

        private bool resolvingShiftEnd;
        private int carriedAP;

        /// <summary>AP waiting to be added at the start of the next shift (from effects resolved at End Shift).</summary>
        public int CarriedAP => carriedAP;

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

        protected override void OnDestroy()
        {
            if (boardManager != null) boardManager.OnPatientDiverted -= DivertPatient;
            base.OnDestroy();
        }

        /// <summary>
        /// A patient was diverted because Triage was full: costs <see cref="DiversionReputationPenalty"/> Reputation.
        /// Subscribed to <see cref="BoardManager.OnPatientDiverted"/>.
        /// </summary>
        public void DivertPatient(Vector3 at)
        {
            if (diversionReputationPenalty <= 0) return;
            GameFeedback.ShowAt($"Triage full - patient diverted! -{diversionReputationPenalty} Rep", at, GameFeedback.WarningColour);
            SpendReputation(diversionReputationPenalty);
        }

        /// <summary>Awards upgrade tokens of a department's colour (one per symptom treated there).</summary>
        public void AddUpgradeTokens(SymptomCategory colour, int amount = 1)
        {
            UpgradeTokens.Add(colour, amount);
            OnUpgradeTokensChanged?.Invoke(colour, UpgradeTokens.Get(colour));
        }

        /// <summary><c>true</c> if the player has the Funding and upgrade tokens for <paramref name="card"/>'s direct upgrade.</summary>
        public bool CanAffordUpgrade(ActionCardSO card) =>
            card != null && card.upgradesTo != null && currentFunding >= card.upgradeCost &&
            UpgradeTokens.CanPay(card.upgradeTokenCost, card.upgradeTokenAnyColour, card.upgradeTokenColour);

        /// <summary>Pays the Funding and upgrade tokens for <paramref name="card"/>'s direct upgrade, or nothing if either is short.</summary>
        public bool TryPayForUpgrade(ActionCardSO card)
        {
            if (!CanAffordUpgrade(card)) return false;
            if (card.upgradeTokenCost > 0)
            {
                UpgradeTokens.TryPay(card.upgradeTokenCost, card.upgradeTokenAnyColour, card.upgradeTokenColour);
                // An any-colour cost may draw from several colours, so refresh them all.
                for (int i = 0; i < 4; i++) OnUpgradeTokensChanged?.Invoke((SymptomCategory)i, UpgradeTokens.Get((SymptomCategory)i));
            }
            return TrySpendFunding(card.upgradeCost);
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
            int before = currentReputation;
            currentReputation = ApplyReputationCap(currentReputation + amount, maxReputation, before);
            OnReputationChanged?.Invoke(currentReputation);
            GameLog.Info($"Gained {currentReputation - before} Reputation! Total: {currentReputation}");
        }

        /// <summary>
        /// Clamps a Reputation gain to <paramref name="cap"/> (0 = no cap). A total already above the cap (e.g. the cap
        /// was lowered mid-run) is never reduced by a gain.
        /// </summary>
        public static int ApplyReputationCap(int reputation, int cap, int before) =>
            cap > 0 && reputation > cap ? Mathf.Max(cap, before) : reputation;

        /// <summary>
        /// Deducts Reputation (e.g. malpractice penalties) and refreshes the UI.
        /// Automatically triggers Game Over if Reputation reaches zero or below.
        /// </summary>
        /// <param name="amount">Reputation points to remove.</param>
        public void SpendReputation(int amount)
        {
            currentReputation = Mathf.Max(0, currentReputation - amount);
            OnReputationChanged?.Invoke(currentReputation);
            GameLog.Info($"Spent {amount} Reputation! Total: {currentReputation}");

            if (currentReputation == 0) TriggerGameOver();
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
                gameOverScreen.Show(false);
            }
            else
            {
                Debug.LogWarning("GameManager: No GameOverScreen assigned in the Inspector!");
            }
        }

        /// <summary>
        /// Ends the run as a win after the final shift. Locks the board and shows the end screen
        /// in its victory form. Guarded by <see cref="isGameOver"/> so it fires once per run.
        /// </summary>
        public void TriggerVictory()
        {
            if (isGameOver) return;
            isGameOver = true;
            SetPhase(GamePhase.GameOver);

            GameLog.Info($"VICTORY: The hospital survived all {shiftsToWin} shifts!");

            if (gameOverScreen != null)
            {
                gameOverScreen.gameObject.SetActive(true);
                gameOverScreen.Show(true);
            }
        }
    }
}
