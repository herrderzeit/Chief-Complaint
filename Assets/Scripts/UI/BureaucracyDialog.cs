using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChiefComplaint.UI
{
    /// <summary>
    /// General-purpose memo dialog for the bureaucracy layer (briefings, intake forms, inspection
    /// reports, bills): a title, a rich-text body and up to three buttons. Built at runtime with
    /// <see cref="RuntimeUIBuilder"/>; one instance is reused.
    /// </summary>
    public class BureaucracyDialog : MonoBehaviour
    {
        public struct Option
        {
            public string label;
            public Action onClick;
            public Color? colour;

            public Option(string label, Action onClick, Color? colour = null)
            {
                this.label = label; this.onClick = onClick; this.colour = colour;
            }
        }

        public static readonly Color StampGreen = new Color(0.25f, 0.75f, 0.35f);
        public static readonly Color StampRed = new Color(0.85f, 0.25f, 0.2f);

        private const int MaxOptions = 3;
        private static BureaucracyDialog instance;

        private GameObject panel;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI bodyText;
        private readonly Button[] buttons = new Button[MaxOptions];
        private readonly TextMeshProUGUI[] labels = new TextMeshProUGUI[MaxOptions];

        /// <summary><c>true</c> while the dialog is on screen.</summary>
        public static bool IsOpen => instance != null && instance.panel.activeSelf;

        /// <summary>Shows the dialog. Each button closes it, then runs its action.</summary>
        public static void Show(string title, string body, params Option[] options)
        {
            if (instance == null)
            {
                var go = new GameObject("BureaucracyDialog");
                instance = go.AddComponent<BureaucracyDialog>();
                instance.Build();
            }
            instance.Open(title, body, options);
        }

        private void Open(string title, string body, Option[] options)
        {
            titleText.text = title;
            bodyText.text = body;

            int count = Mathf.Clamp(options.Length, 1, MaxOptions);
            float spacing = 340f;
            float startX = -(count - 1) * spacing / 2f;

            for (int i = 0; i < MaxOptions; i++)
            {
                bool used = i < options.Length && i < MaxOptions;
                buttons[i].gameObject.SetActive(used);
                if (!used) continue;

                var option = options[i];
                labels[i].text = option.label;
                buttons[i].GetComponent<Image>().color = option.colour ?? RuntimeUIBuilder.ButtonColour;
                ((RectTransform)buttons[i].transform).anchoredPosition = new Vector2(startX + i * spacing, -300);
                buttons[i].onClick.RemoveAllListeners();
                buttons[i].onClick.AddListener(() =>
                {
                    panel.SetActive(false);
                    option.onClick?.Invoke();
                });
            }

            panel.SetActive(true);
        }

        private void Build()
        {
            var canvasGO = RuntimeUIBuilder.CreateOverlayCanvas(transform, "BureaucracyCanvas", 505);
            panel = RuntimeUIBuilder.CreateBackdrop(canvasGO.transform, 0.75f);
            var box = RuntimeUIBuilder.CreateBox(panel.transform, new Vector2(1200, 760));

            titleText = RuntimeUIBuilder.MakeText(box, "Title", 46, new Vector2(0, 320), new Vector2(1140, 70), "", FontStyles.Bold, new Color(1f, 0.85f, 0.4f));

            bodyText = RuntimeUIBuilder.MakeText(box, "Body", 30, new Vector2(0, 10), new Vector2(1100, 500));
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.richText = true;

            for (int i = 0; i < MaxOptions; i++)
            {
                buttons[i] = RuntimeUIBuilder.MakeButton(box, "Option" + i, new Vector2(0, -300), new Vector2(300, 80), 30, out labels[i]);
            }

            panel.SetActive(false);
        }
    }
}
