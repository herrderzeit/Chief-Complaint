using UnityEngine;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Cameras
{
    /// <summary>
    /// Provides a hold-to-zoom camera that slides toward the mouse cursor on zoom-in
    /// and smoothly returns to the default position and orthographic size on release.
    /// Triggered by holding <see cref="zoomKey"/> or the right mouse button.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class CameraController : MonoBehaviour
    {
        [Header("Zoom Settings")]
        public float defaultZoom = 12f;
        public float zoomInSize = 5f;
        public float zoomSpeed = 10f;
        public UnityEngine.InputSystem.Key zoomKey = UnityEngine.InputSystem.Key.Z;

        private UnityEngine.Camera cam;
        private float targetZoom;

        /// <summary>The camera's starting world position, captured at <c>Start</c>. Used as the reset target on zoom-out.</summary>
        private Vector3 defaultPosition;
        private Vector3 targetPosition;

        void Start()
        {
            cam = GetComponent<UnityEngine.Camera>();
            targetZoom = defaultZoom;

            // Capture the starting position, including Z-depth, for the reset target.
            defaultPosition = transform.position;
            targetPosition = defaultPosition;
        }

        void Update()
        {
            // On the exact frame the zoom key is pressed, capture where the mouse is pointing
            // and use that as the slide target. Z-axis is forced to the camera's default depth
            // to prevent the camera from diving into the board plane.
            if ((UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current[zoomKey].wasPressedThisFrame) || (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame))
            {
                // Without a mouse there is nothing to aim at, so zoom on the current view.
                var mouse = UnityEngine.InputSystem.Mouse.current;
                Vector3 zoomTarget = mouse != null ? cam.ScreenToWorldPoint(mouse.position.ReadValue()) : transform.position;
                targetPosition = new Vector3(zoomTarget.x, zoomTarget.y, defaultPosition.z);
            }

            if ((UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current[zoomKey].isPressed) || (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.isPressed))
            {
                targetZoom = zoomInSize;
            }
            else
            {
                targetZoom = defaultZoom;
                targetPosition = defaultPosition;
            }

            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSpeed);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * zoomSpeed);
        }
    }
}
