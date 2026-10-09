using UnityEngine;

namespace ChiefComplaint.Utils
{
    /// <summary>
    /// Generic MonoBehaviour Singleton base class. Enforces a single live instance per type
    /// across the scene. Duplicate GameObjects are automatically destroyed in <see cref="Awake"/>.
    /// Subclasses must call <c>base.Awake()</c> when overriding.
    /// </summary>
    /// <typeparam name="T">The concrete MonoBehaviour type that should be a singleton.</typeparam>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        /// <summary>The single live instance of <typeparamref name="T"/>. <c>null</c> when not yet initialised or after destruction.</summary>
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }

}
