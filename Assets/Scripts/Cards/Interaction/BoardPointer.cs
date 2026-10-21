using UnityEngine;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Cards.Interaction
{
    /// <summary>
    /// Receives pointer events for a world-space object with a <see cref="Collider2D"/> from <see cref="BoardPointer"/>.
    /// Replaces the legacy <c>OnMouseDown</c>/<c>OnMouseDrag</c>/<c>OnMouseUp</c>/<c>OnMouseEnter</c>/<c>OnMouseExit</c>
    /// messages, which need the old Input Manager. Implement only the ones you need.
    /// </summary>
    public interface IBoardPointerHandler
    {
        void PointerEnter() { }
        void PointerExit() { }
        void PointerDown() { }

        /// <summary>Called every frame the button is held after a press on this object, wherever the pointer is.</summary>
        void PointerDrag() { }

        /// <summary>Called on release for the object that received <see cref="PointerDown"/>, wherever the pointer is.</summary>
        void PointerUp() { }
    }

    /// <summary>
    /// Input System replacement for Unity's built-in mouse messages on 2D colliders, driven by the Gameplay
    /// actions in <see cref="GameInput"/> (Point and Select). Each frame it raycasts the pointer into the 2D physics world through the main camera and sends enter / exit / down / drag / up to the
    /// <see cref="IBoardPointerHandler"/> components on the nearest collider's GameObject, with the same rules as
    /// the legacy messages: drag and up go to whatever was pressed, and UI does not block the board.
    /// Created automatically; no scene setup needed.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class BoardPointer : MonoBehaviour
    {
        private GameObject hovered;
        private GameObject pressed;
        private bool wasButtonDown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            var go = new GameObject(nameof(BoardPointer)) { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            go.AddComponent<BoardPointer>();
        }

        void Update()
        {
            Camera cam = CameraUtil.Main;
            if (cam == null) return;

            SetHovered(Raycast(cam, GameInput.PointerScreenPosition));

            // Edge detection against our own last state, not wasPressedThisFrame, so presses still register when
            // the Input System is updated more than once a frame (e.g. by the showcase recorder's simulated input).
            bool buttonDown = GameInput.Select.IsPressed();
            if (buttonDown && !wasButtonDown)
            {
                pressed = hovered;
                Send(pressed, h => h.PointerDown());
            }
            else if (buttonDown)
            {
                Send(pressed, h => h.PointerDrag());
            }
            else if (wasButtonDown)
            {
                GameObject released = pressed;
                pressed = null;
                Send(released, h => h.PointerUp());
            }
            wasButtonDown = buttonDown;
        }

        private static GameObject Raycast(Camera cam, Vector2 screenPosition)
        {
            Ray ray = cam.ScreenPointToRay(screenPosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, cam.eventMask & ~(1 << 2));
            return hit.collider != null ? hit.collider.gameObject : null;
        }

        private void SetHovered(GameObject target)
        {
            if (target == hovered) return;
            GameObject previous = hovered;
            hovered = target;
            Send(previous, h => h.PointerExit());
            Send(hovered, h => h.PointerEnter());
        }

        private static readonly System.Collections.Generic.List<IBoardPointerHandler> handlers = new();

        private static void Send(GameObject target, System.Action<IBoardPointerHandler> message)
        {
            if (target == null) return;
            target.GetComponents(handlers);
            foreach (var handler in handlers)
            {
                if (handler is Behaviour b && !b.isActiveAndEnabled) continue;
                message(handler);
            }
            handlers.Clear();
        }
    }
}
