using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.UI
{
    /// <summary>
    /// Controls the Game Over full-screen overlay. Activated by <see cref="GameManager.TriggerGameOver"/>
    /// when the player's Reputation reaches zero or below (bankruptcy).
    /// On activation: pauses the game via <c>Time.timeScale = 0</c> and fades the overlay in.
    /// The Restart button reloads the active scene for a fresh run.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class GameOverScreen : MonoBehaviour
    {
        [Header("Stats Display")]
        /// <summary>Text label used to display the total number of patients cured this run.</summary>
        public TextMeshProUGUI patientsCuredText;
        /// <summary>The large heading; set to a win or loss message when the screen is shown.</summary>
        public TextMeshProUGUI titleText;

        [Header("Buttons")]
        public Button restartButton;

        [Header("Fade Settings")]
        [Tooltip("Duration in seconds for the overlay to fade from transparent to fully opaque.")]
        public float fadeDuration = 1.2f;

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            // Ensure the overlay starts fully hidden and non-interactive.
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);
        }

        // -------------------------------------------------------------------------
        // Activation
        // -------------------------------------------------------------------------

        /// <summary>
        /// Displays the Game Over screen: populates stats, pauses the game, and
        /// fades the overlay in. Call this from <see cref="GameManager.TriggerGameOver"/>.
        /// </summary>
        public void Show(bool victory = false)
        {
            if (titleText != null)
                titleText.text = victory ? "ACCREDITED!" : "GAME OVER";

            // Populate the patients-cured stat from the PatientDeckManager.
            if (patientsCuredText != null && PatientDeckManager.Instance != null)
            {
                var pdm = PatientDeckManager.Instance;
                int total = pdm.patientsCured;
                string phase = pdm.GetEscalationPhase();

                patientsCuredText.text = $"Escalation Reached: <color=#ffaaaa>{phase}</color>\n\n" +
                                         $"Total Patients Cured: {total}\n" +
                                         $"  • Walk-Ins: {pdm.walkInsCured}\n" +
                                         $"  • Ambulances: {pdm.ambulancesCured}\n" +
                                         $"  • Trauma Flights: {pdm.traumaFlightsCured}";
            }

            // Freeze the game world.
            Time.timeScale = 0f;

            // Fade the overlay in using unscaled time so it works while paused.
            StartCoroutine(FadeIn());
        }

        // -------------------------------------------------------------------------
        // Animation
        // -------------------------------------------------------------------------

        /// <summary>
        /// Coroutine that smoothly fades the overlay's <see cref="CanvasGroup"/> alpha
        /// from 0 to 1 over <see cref="fadeDuration"/> seconds using unscaled delta time.
        /// Enables button interaction once fully opaque.
        /// </summary>
        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            canvasGroup.blocksRaycasts = true;

            while (elapsed < fadeDuration)
            {
                // Use unscaledDeltaTime so the fade works even while Time.timeScale == 0.
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
        }

        // -------------------------------------------------------------------------
        // Actions
        // -------------------------------------------------------------------------

        /// <summary>
        /// Restores normal time scale and reloads the active scene to begin a fresh run.
        /// </summary>
        private void OnRestartClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

}
