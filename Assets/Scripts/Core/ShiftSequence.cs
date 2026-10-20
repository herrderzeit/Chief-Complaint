using System;
using System.Collections.Generic;

namespace ChiefComplaint.Core
{
    /// <summary>
    /// An ordered list of steps that run one after another, each of which may wait for the player
    /// (e.g. a dialog) before calling its continuation. Used for the start-of-shift and End Shift sequences.
    /// </summary>
    public class ShiftSequence
    {
        private struct Step
        {
            public int order;
            public Action<Action> run;
        }

        private readonly List<Step> steps = new List<Step>();

        /// <summary>
        /// Adds a step. Lower <paramref name="order"/> runs first. The step must call the supplied
        /// continuation exactly once when it is finished (immediately if it has nothing to show).
        /// </summary>
        public void Register(int order, Action<Action> step)
        {
            steps.Add(new Step { order = order, run = step });
            steps.Sort((a, b) => a.order.CompareTo(b.order));
        }

        public void Unregister(Action<Action> step) => steps.RemoveAll(s => s.run == step);

        public bool IsEmpty => steps.Count == 0;

        /// <summary>Runs every step in order, then <paramref name="onComplete"/>. <paramref name="shouldStop"/> aborts early (e.g. Game Over).</summary>
        public void Run(Action onComplete, Func<bool> shouldStop = null)
        {
            var snapshot = new List<Step>(steps);
            RunFrom(snapshot, 0, onComplete, shouldStop);
        }

        private static void RunFrom(List<Step> snapshot, int index, Action onComplete, Func<bool> shouldStop)
        {
            if (shouldStop != null && shouldStop()) return;
            if (index >= snapshot.Count) { onComplete?.Invoke(); return; }

            bool continued = false;
            snapshot[index].run(() =>
            {
                if (continued) return; // guard against a step calling its continuation twice
                continued = true;
                RunFrom(snapshot, index + 1, onComplete, shouldStop);
            });
        }
    }
}
