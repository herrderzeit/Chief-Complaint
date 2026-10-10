using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChiefComplaint.UI
{
    /// <summary>
    /// Sequential UI dialog built at runtime to display tutorial text steps.
    /// Uses a dark overlay and a central box with a 'Next' button.
    /// </summary>
    public class TutorialDialog : MonoBehaviour
    {
        private static TutorialDialog instance;
        private GameObject panel;
        private TextMeshProUGUI bodyText;
        private TextMeshProUGUI buttonLabel;
        private Button nextButton;
        private Button skipButton;

        private string[] steps;
        private int currentStep;
        private Action onComplete;

        public static void Show(string[] steps, Action onComplete)
        {
            if (instance == null)
            {
                var go = new GameObject("TutorialDialog");
                instance = go.AddComponent<TutorialDialog>();
                instance.Build();
            }
            instance.Open(steps, onComplete);
        }

        private void Open(string[] steps, Action onComplete)
        {
            this.steps = steps;
            this.onComplete = onComplete;
            this.currentStep = 0;
            UpdateStep();
            panel.SetActive(true);
        }

        private void UpdateStep()
        {
            if (currentStep >= steps.Length)
            {
                panel.SetActive(false);
                onComplete?.Invoke();
                return;
            }

            bodyText.text = steps[currentStep];
            
            if (currentStep == steps.Length - 1)
            {
                buttonLabel.text = "Finish";
            }
            else
            {
                buttonLabel.text = "Next";
            }

            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() =>
            {
                currentStep++;
                UpdateStep();
            });

            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(() =>
            {
                panel.SetActive(false);
                onComplete?.Invoke();
            });
        }

        private void Build()
        {
            var canvasGO = RuntimeUIBuilder.CreateOverlayCanvas(transform, "TutorialCanvas", 510); // slightly above ChoicePrompt

            // Dim backdrop
            panel = RuntimeUIBuilder.CreateBackdrop(canvasGO.transform, 0.7f);

            // Center box: 1.5x bigger
            var box = RuntimeUIBuilder.CreateBox(panel.transform, new Vector2(1800, 900));

            // Title
            RuntimeUIBuilder.MakeText(box, "Title", 50, new Vector2(0, 380), new Vector2(1740, 75), "TUTORIAL", FontStyles.Bold, Color.yellow);

            // Body text
            bodyText = RuntimeUIBuilder.MakeText(box, "Body", 42, new Vector2(0, 50), new Vector2(1700, 600), "", FontStyles.Normal, Color.white);
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.alignment = TextAlignmentOptions.Center;

            // Next Button
            var buttonSize = new Vector2(400, 100);
            nextButton = RuntimeUIBuilder.MakeButton(box, "NextButton", new Vector2(400, -350), buttonSize, 40, out buttonLabel);

            // Skip Button
            TextMeshProUGUI skipLabel;
            skipButton = RuntimeUIBuilder.MakeButton(box, "SkipButton", new Vector2(-400, -350), buttonSize, 40, out skipLabel);
            skipLabel.text = "Skip Tutorial";

            panel.SetActive(false);
        }
    }
}
