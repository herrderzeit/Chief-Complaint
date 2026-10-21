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
    /// Triggered by holding the Gameplay "Zoom" action (right mouse button or Z by default; rebind it in the
    /// Input Actions Editor).
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class CameraController : MonoBehaviour
    {
        [Header("Zoom Settings")]
        public float defaultZoom = 12f;
        public float zoomInSize = 5f;
        public float zoomSpeed = 10f;

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
            // On the exact frame zoom is pressed, capture where the pointer is aiming and use that as the
            // slide target. Z-axis is forced to the camera's default depth to prevent the camera from
            // diving into the board plane.
            if (GameInput.Zoom.WasPressedThisFrame())
            {
                Vector3 zoomTarget = cam.ScreenToWorldPoint(GameInput.PointerScreenPosition);
                targetPosition = new Vector3(zoomTarget.x, zoomTarget.y, defaultPosition.z);
            }

            if (GameInput.Zoom.IsPressed())
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
