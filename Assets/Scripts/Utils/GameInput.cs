using UnityEngine;
using UnityEngine.InputSystem;

namespace ChiefComplaint.Utils
{
    /// <summary>
    /// The game's input, read from the "Gameplay" map of the project-wide Input Actions
    /// (<c>Assets/Settings/InputSystem_Actions.inputactions</c>; edit bindings in the Input Actions Editor under
    /// Project Settings &gt; Input System Package). Gameplay code reads input only through here, never from
    /// <c>Mouse.current</c> or <c>Keyboard.current</c>, so rebinding happens in the asset, not in code.
    /// </summary>
    public static class GameInput
    {
        private static InputAction point, select, zoom, pause;

        /// <summary>The pointer's screen position (mouse, pen or primary touch).</summary>
        public static InputAction Point => point ??= Find("Point");

        /// <summary>Picks up, drags and clicks board objects (left mouse button, pen tip, touch).</summary>
        public static InputAction Select => select ??= Find("Select");

        /// <summary>Hold to zoom the camera towards the pointer (right mouse button or Z).</summary>
        public static InputAction Zoom => zoom ??= Find("Zoom");

        /// <summary>Pause / quit prompt (Escape or the gamepad Start button).</summary>
        public static InputAction Pause => pause ??= Find("Pause");

        /// <summary>The pointer's current screen position.</summary>
        public static Vector2 PointerScreenPosition => Point.ReadValue<Vector2>();

        /// <summary>The pointer's position on the board plane (z = 0) as seen through <paramref name="cam"/>.</summary>
        public static Vector3 PointerWorldPosition(Camera cam)
        {
            Vector3 screen = PointerScreenPosition;
            screen.z = Mathf.Abs(cam.transform.position.z);
            return cam.ScreenToWorldPoint(screen);
        }

        /// <summary><c>true</c> when the pointer lies inside <paramref name="bounds"/> on the board (x/y only).</summary>
        public static bool IsPointerOver(Bounds bounds, Camera cam)
        {
            Vector3 world = PointerWorldPosition(cam);
            return world.x >= bounds.min.x && world.x <= bounds.max.x && world.y >= bounds.min.y && world.y <= bounds.max.y;
        }

        // Project-wide actions are rebuilt when play mode starts without a domain reload, so drop the cache.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => point = select = zoom = pause = null;

        private static InputAction Find(string actionName)
        {
            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
                throw new System.InvalidOperationException("No project-wide Input Actions are assigned (Project Settings > Input System Package).");
            InputAction action = actions.FindAction("Gameplay/" + actionName, throwIfNotFound: true);
            if (!action.enabled) action.Enable();
            return action;
        }
    }
}
