using UnityEngine;
using TMPro;
using System.Collections;
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
    /// Drives the float-and-fade animation for a single notification popup.
    /// Requires a <see cref="CanvasGroup"/> component to handle alpha fading.
    /// The GameObject destroys itself automatically when the animation completes.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class NotificationView : MonoBehaviour
    {
        public TextMeshProUGUI messageText;

        [Header("Animation Settings")]
        public float displayTime = 1.0f;
        public float floatSpeed = 50f;
        public float fadeOutTime = 0.5f;

        private CanvasGroup canvasGroup;
        private System.Action<NotificationView> releaseAction;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // We cannot add an Image to the same GameObject as TextMeshPro, so we create a child background.
            if (transform.Find("Background") == null)
            {
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(this.transform, false);
                bgObj.transform.SetAsFirstSibling(); // Put it behind the text

                UnityEngine.UI.Image bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
                bgImage.color = new Color(0f, 0f, 0f, 0.85f);
                bgImage.raycastTarget = false;

                // Stretch the background to perfectly match the text container, plus padding
                RectTransform bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = new Vector2(-15, -10); // left, bottom padding
                bgRect.offsetMax = new Vector2(15, 10);   // right, top padding

                // Restore dynamic Canvas components because SetAsFirstSibling() does NOT render a child behind a parent's Graphic!
                Canvas textCanvas = gameObject.GetComponent<Canvas>();
                if (textCanvas == null) textCanvas = gameObject.AddComponent<Canvas>();
                textCanvas.overrideSorting = true;
                textCanvas.sortingOrder = 10000;

                Canvas bgCanvas = bgObj.AddComponent<Canvas>();
                bgCanvas.overrideSorting = true;
                bgCanvas.sortingOrder = 9999;
            }

            // Add a ContentSizeFitter to the root so the TextMeshPro bounds expand to fit the text,
            // which forces our stretched background child to expand with it!
            var fitter = gameObject.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (fitter == null) fitter = gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            if (messageText != null)
            {
                messageText.alignment = TextAlignmentOptions.Center;
                messageText.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        /// <summary>
        /// Applies the message text and colour, then starts the float-and-fade coroutine.
        /// Call this immediately after instantiating the prefab.
        /// </summary>
        /// <param name="message">The notification text to display.</param>
        /// <param name="color">The colour to apply to the message text.</param>
        /// <param name="release">Optional callback that returns this view to its pool when finished; if null the GameObject is destroyed.</param>
        public void Setup(string message, Color color, System.Action<NotificationView> release = null)
        {
            releaseAction = release;
            StopAllCoroutines();
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            if (messageText != null)
            {
                messageText.text = $"<b>{message}</b>";
                messageText.color = color;
            }

            StartCoroutine(AnimateNotification());
        }

        /// <summary>
        /// Two-phase animation coroutine:
        /// <list type="number">
        ///   <item><description>Float upward at full opacity for <see cref="displayTime"/> seconds.</description></item>
        ///   <item><description>Continue floating while fading out over <see cref="fadeOutTime"/> seconds, then self-destruct.</description></item>
        /// </list>
        /// </summary>
        private IEnumerator AnimateNotification()
        {
            float timer = 0f;

            while (timer < displayTime)
            {
                transform.position += Vector3.up * floatSpeed * Time.deltaTime;
                timer += Time.deltaTime;
                yield return null;
            }

            timer = 0f;
            while (timer < fadeOutTime)
            {
                transform.position += Vector3.up * floatSpeed * Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeOutTime);
                timer += Time.deltaTime;
                yield return null;
            }

            if (releaseAction != null) releaseAction(this);
            else Destroy(gameObject);
        }
    }

}
