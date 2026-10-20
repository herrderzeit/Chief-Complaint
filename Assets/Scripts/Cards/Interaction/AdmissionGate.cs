using System;
using ChiefComplaint.Cards.Views;

namespace ChiefComplaint.Cards.Interaction
{
    /// <summary>
    /// Lets another system (the intake paperwork) intercept a patient's first move from Triage into a
    /// Ward bed. The interceptor returns <c>true</c> if it has taken over; it then calls the admit
    /// action later if the patient is admitted.
    /// </summary>
    public static class AdmissionGate
    {
        public static Func<PatientCardView, Action, bool> interceptor;

        /// <summary>Returns <c>true</c> if the admission was intercepted (the caller should not admit yet).</summary>
        public static bool TryIntercept(PatientCardView patient, Action admit) =>
            interceptor != null && interceptor(patient, admit);

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => interceptor = null;
    }
}
