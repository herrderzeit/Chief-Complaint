using UnityEngine;
using UnityEngine.Pool;
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
    /// Singleton factory for spawning floating text notifications. Delegates
    /// animation and lifecycle to <see cref="NotificationView"/> on each instantiated prefab.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class NotificationManager : Singleton<NotificationManager>
    {
        [Header("Setup")]
        public GameObject notificationPrefab;
        /// <summary>The Canvas or panel transform used as the parent and default position for notifications.</summary>
        public Transform defaultContainer;

        private IObjectPool<NotificationView> pool;

        protected override void Awake()
        {
            base.Awake();
            pool = new ObjectPool<NotificationView>(
                CreateNotification,
                view => view.gameObject.SetActive(true),
                view => view.gameObject.SetActive(false),
                view => { if (view != null) Destroy(view.gameObject); },
                false, 8, 32);
        }

        private NotificationView CreateNotification()
        {
            GameObject obj = Instantiate(notificationPrefab, defaultContainer);
            return obj.GetComponent<NotificationView>();
        }

        /// <summary>
        /// Spawns a floating text notification at the centre of <see cref="defaultContainer"/>.
        /// </summary>
        /// <param name="message">The text to display.</param>
        /// <param name="color">Optional tint colour; defaults to white.</param>
        public void ShowNotification(string message, Color? color = null)
        {
            if (notificationPrefab == null || defaultContainer == null)
            {
                Debug.LogWarning($"NotificationManager: Cannot show '{message}'. Prefab or Container is missing!");
                return;
            }
            Spawn(message, color ?? Color.white, defaultContainer.position);
        }

        /// <summary>
        /// Spawns a floating text notification at a specific screen-space position
        /// (e.g. <c>GameInput.PointerScreenPosition</c>) converted to world space by the prefab's placement.
        /// </summary>
        /// <param name="message">The text to display.</param>
        /// <param name="screenPosition">The screen-space position to place the notification.</param>
        /// <param name="color">Optional tint colour; defaults to white.</param>
        public void ShowNotificationAtPosition(string message, Vector3 screenPosition, Color? color = null)
        {
            if (notificationPrefab == null || defaultContainer == null)
            {
                Debug.LogWarning($"NotificationManager: Cannot show '{message}'. Prefab or Container is missing!");
                return;
            }

            Spawn(message, color ?? Color.white, screenPosition);
        }

        /// <summary>
        /// Instantiates the notification prefab, positions it, and calls
        /// <see cref="NotificationView.Setup"/> to begin the float-and-fade animation.
        /// </summary>
        /// <param name="message">The text to display.</param>
        /// <param name="color">The text colour.</param>
        /// <param name="position">The world or screen position to place the notification at.</param>
        private void Spawn(string message, Color color, Vector3 position)
        {
            NotificationView view = pool.Get();
            if (view == null) return;

            view.transform.position = position;
            view.Setup(message, color, pool.Release);
        }
    }

}
