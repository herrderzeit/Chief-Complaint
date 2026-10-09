using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChiefComplaint.UI
{
    /// <summary>
    /// Shared helpers for the runtime-built overlay dialogs (<see cref="ChoicePrompt"/>, <see cref="TutorialDialog"/>)
    /// so canvas, backdrop, text and button styling is defined in exactly one place.
    /// </summary>
    public static class RuntimeUIBuilder
    {
        /// <summary>Accent colour used for dialog buttons.</summary>
        public static readonly Color ButtonColour = new Color(0.96f, 0.6f, 0.1f, 1f);

        /// <summary>Background colour of dialog boxes.</summary>
        public static readonly Color BoxColour = new Color(0.1f, 0.12f, 0.18f, 1f);

        /// <summary>Creates a full-screen overlay canvas (1920x1080 reference) under <paramref name="parent"/>.</summary>
        public static GameObject CreateOverlayCanvas(Transform parent, string name, int sortingOrder)
        {
            var canvasGO = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(parent, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            return canvasGO;
        }

        /// <summary>Creates a stretched, click-blocking dim backdrop.</summary>
        public static GameObject CreateBackdrop(Transform parent, float alpha)
        {
            var panel = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0, 0, 0, alpha);
            return panel;
        }

        /// <summary>Creates the centred dialog box.</summary>
        public static RectTransform CreateBox(Transform parent, Vector2 size)
        {
            var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(parent, false);
            var rt = (RectTransform)box.transform;
            rt.sizeDelta = size;
            box.GetComponent<Image>().color = BoxColour;
            return rt;
        }

        /// <summary>Creates a non-interactive centred TextMeshPro label.</summary>
        public static TextMeshProUGUI MakeText(Transform parent, string name, float size, Vector2 pos, Vector2 dim,
            string text = "", FontStyles style = FontStyles.Normal, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos; rt.sizeDelta = dim;
            var t = go.GetComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.text = text;
            t.fontStyle = style;
            t.color = color ?? Color.white;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Creates a bold-labelled accent button.</summary>
        public static Button MakeButton(Transform parent, string name, Vector2 pos, Vector2 buttonSize, float fontSize, out TextMeshProUGUI label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos; rt.sizeDelta = buttonSize;
            go.GetComponent<Image>().color = ButtonColour;
            label = MakeText(go.transform, "Label", fontSize, Vector2.zero, buttonSize - new Vector2(10, 10), "", FontStyles.Bold, Color.black);
            return go.GetComponent<Button>();
        }
    }
}
