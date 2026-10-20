using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ChiefComplaint.Cards.Views;

namespace ChiefComplaint.UI
{
    /// <summary>
    /// Asks the player to click one of a set of highlighted patients (e.g. for Team Assessment).
    /// Shows a banner with a Skip button; the rest of the board is locked until a patient is picked or skipped.
    /// </summary>
    public class PatientPicker : MonoBehaviour
    {
        private static PatientPicker instance;
        private static readonly List<PatientCardView> eligible = new List<PatientCardView>();
        private static Action<PatientCardView> onPicked;

        private GameObject banner;
        private TextMeshProUGUI promptText;

        /// <summary><c>true</c> while waiting for the player to pick a patient.</summary>
        public static bool IsPicking { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { IsPicking = false; eligible.Clear(); onPicked = null; instance = null; }

        /// <summary>Highlights <paramref name="candidates"/> and waits for the player to click one.</summary>
        public static void Begin(List<PatientCardView> candidates, string prompt, Action<PatientCardView> picked)
        {
            if (instance == null)
            {
                var go = new GameObject("PatientPicker");
                instance = go.AddComponent<PatientPicker>();
                instance.Build();
            }

            End();
            eligible.AddRange(candidates);
            onPicked = picked;
            IsPicking = true;
            foreach (var p in eligible) if (p != null) p.SetHighlight(true);
            instance.promptText.text = prompt;
            instance.banner.SetActive(true);
        }

        /// <summary>Called when a patient is clicked while picking. Returns <c>true</c> if the click was used.</summary>
        public static bool TryPick(PatientCardView patient)
        {
            if (!IsPicking) return false;
            if (patient == null || !eligible.Contains(patient))
            {
                if (patient != null) GameFeedback.WarnAt("Pick a highlighted patient", patient.transform.position);
                return true;
            }

            var callback = onPicked;
            End();
            callback?.Invoke(patient);
            return true;
        }

        /// <summary>Stops picking without choosing anyone (Skip, Escape or End Shift).</summary>
        public static void Cancel() => End();

        private static void End()
        {
            foreach (var p in eligible) if (p != null) p.SetHighlight(false);
            eligible.Clear();
            onPicked = null;
            IsPicking = false;
            if (instance != null && instance.banner != null) instance.banner.SetActive(false);
        }

        private void Build()
        {
            // No backdrop: the board must stay clickable so the player can pick a patient.
            var canvasGO = RuntimeUIBuilder.CreateOverlayCanvas(transform, "PatientPickerCanvas", 480);
            var box = RuntimeUIBuilder.CreateBox(canvasGO.transform, new Vector2(1100, 110));
            box.anchoredPosition = new Vector2(0, 420);
            banner = box.gameObject;

            promptText = RuntimeUIBuilder.MakeText(box, "Prompt", 28, new Vector2(-110, 0), new Vector2(840, 90), "", FontStyles.Bold, new Color(1f, 0.85f, 0.35f));
            var skip = RuntimeUIBuilder.MakeButton(box, "Skip", new Vector2(440, 0), new Vector2(170, 70), 28, out var label);
            label.text = "Skip";
            skip.onClick.AddListener(Cancel);

            banner.SetActive(false);
        }
    }
}
