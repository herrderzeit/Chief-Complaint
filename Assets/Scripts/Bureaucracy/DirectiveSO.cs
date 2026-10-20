using UnityEngine;

namespace ChiefComplaint.Bureaucracy
{
    public enum DirectiveType
    {
        /// <summary>One department is closed for audit; treating there is a citation.</summary>
        ClosedDepartment,
        /// <summary>Admitting a Walk-In without valid insurance on their form is a citation.</summary>
        InsuranceRequired,
        /// <summary>An Ambulance or Trauma Flight patient still waiting in Triage at End Shift is a citation.</summary>
        EmergenciesFirst,
        /// <summary>Diagnostic cards cost extra AP this shift.</summary>
        DiagnosticsSurcharge,
        /// <summary>Referring any patient away is a citation.</summary>
        ReferralFreeze,
        /// <summary>More than <see cref="DirectiveSO.value"/> Ward beds occupied at End Shift is a citation.</summary>
        BedCap,
        /// <summary>No intake forms this shift.</summary>
        PaperlessDay,
        /// <summary>The shift starts with less AP.</summary>
        StaffingShortage
    }

    /// <summary>
    /// A shift directive from the Hospital Board: one rule change for a single shift.
    /// <c>{department}</c> and <c>{value}</c> in the memo are filled in when shown.
    /// </summary>
    [CreateAssetMenu(fileName = "New Directive", menuName = "Chief Complaint/Bureaucracy/Directive")]
    public class DirectiveSO : ScriptableObject
    {
        public DirectiveType type;
        public string title = "Directive";
        [TextArea(2, 4)] public string memo;
        [Tooltip("Numeric parameter: extra AP for a surcharge, AP lost for a shortage, the bed cap...")]
        public int value = 1;
    }
}
