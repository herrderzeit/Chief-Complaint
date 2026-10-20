using System;
using UnityEngine;
using UnityEngine.UI;
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
    /// Lightweight runtime-built popup that asks the player to pick between two options
    /// (e.g. "Gain 2 Rep" or "Gain 1 AP"). Builds its own overlay canvas on first use.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class ChoicePrompt : MonoBehaviour
    {
        private static ChoicePrompt instance;
        private GameObject panel;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI labelA, labelB;
        private Button buttonA, buttonB;

        /// <summary><c>true</c> while a choice is on screen; the board and Escape wait until it is answered.</summary>
        public static bool IsOpen => instance != null && instance.panel != null && instance.panel.activeSelf;

        public static void Show(string title, string optionA, Action onA, string optionB, Action onB)
        {
            if (instance == null)
            {
                var go = new GameObject("ChoicePrompt");
                instance = go.AddComponent<ChoicePrompt>();
                instance.Build();
            }
            instance.Open(title, optionA, onA, optionB, onB);
        }

        private void Open(string title, string a, Action onA, string b, Action onB)
        {
            titleText.text = title;
            labelA.text = a;
            labelB.text = b;
            buttonA.onClick.RemoveAllListeners();
            buttonB.onClick.RemoveAllListeners();
            buttonA.onClick.AddListener(() => { panel.SetActive(false); onA?.Invoke(); });
            buttonB.onClick.AddListener(() => { panel.SetActive(false); onB?.Invoke(); });
            panel.SetActive(true);
        }

        private void Build()
        {
            var canvasGO = RuntimeUIBuilder.CreateOverlayCanvas(transform, "ChoiceCanvas", 500);

            // Dim backdrop (also blocks clicks behind the popup)
            panel = RuntimeUIBuilder.CreateBackdrop(canvasGO.transform, 0.6f);

            var box = RuntimeUIBuilder.CreateBox(panel.transform, new Vector2(640, 260));

            titleText = RuntimeUIBuilder.MakeText(box, "Title", 34, new Vector2(0, 85), new Vector2(600, 60));
            var buttonSize = new Vector2(270, 90);
            buttonA = RuntimeUIBuilder.MakeButton(box, "ButtonA", new Vector2(-150, -45), buttonSize, 30, out labelA);
            buttonB = RuntimeUIBuilder.MakeButton(box, "ButtonB", new Vector2(150, -45), buttonSize, 30, out labelB);

            panel.SetActive(false);
        }
    }

}
