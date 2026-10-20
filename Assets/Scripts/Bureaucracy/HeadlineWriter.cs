
namespace ChiefComplaint.Bureaucracy
{
    /// <summary>What happened during a shift, as the press sees it.</summary>
    public struct ShiftReport
    {
        public int shift;
        public int cured;
        public int deaths;
        public int citations;
        public int referred;
        public int fraudCaught;
        public string newlyAccredited; // department name, or null
        public InspectionOutcome? inspection;
    }

    /// <summary>Pure headline writer: picks the most newsworthy event and a satirical template for it.</summary>
    public static class HeadlineWriter
    {
        public static string Write(ShiftReport r, System.Random rng)
        {
            if (r.shift <= 0) return Pick(rng, Opening);
            if (r.inspection == InspectionOutcome.Fail) return Pick(rng, InspectionFailed);
            if (r.deaths > 0) return string.Format(Pick(rng, Deaths), r.deaths, r.deaths == 1 ? "patient" : "patients");
            if (r.inspection == InspectionOutcome.Pass) return Pick(rng, InspectionPassed);
            if (r.newlyAccredited != null) return string.Format(Pick(rng, Accredited), r.newlyAccredited);
            if (r.fraudCaught > 0) return Pick(rng, Fraud);
            if (r.citations >= 3) return string.Format(Pick(rng, Citations), r.citations);
            if (r.referred > 0) return Pick(rng, Referrals);
            if (r.cured >= 3) return string.Format(Pick(rng, GoodDay), r.cured);
            return Pick(rng, Quiet);
        }

        private static string Pick(System.Random rng, string[] options) => options[rng.Next(options.Length)];

        private static readonly string[] Opening =
        {
            "New hospital management promises shorter waits, longer forms.",
            "Hospital opens its doors. Coffee machine already broken."
        };
        private static readonly string[] InspectionFailed =
        {
            "BOARD INSPECTOR LEAVES IN TEARS. Hospital rating plummets.",
            "Inspection disaster: 'I have never seen so many missing signatures.'"
        };
        private static readonly string[] Deaths =
        {
            "Tragedy on the ward: {0} {1} lost overnight. Board demands answers.",
            "Families grieve after {0} {1} die in hospital care.",
            "{0} {1} lost. Union blames understaffing, Board blames the union."
        };
        private static readonly string[] InspectionPassed =
        {
            "Inspector impressed: 'Remarkably few fires today.'",
            "Hospital sails through inspection. Staff celebrate with vending machine snacks."
        };
        private static readonly string[] Accredited =
        {
            "{0} earns accreditation. Plaque ordered, plaque budget exceeded.",
            "Board accredits {0}. Staff ask if this means a pay rise. It does not."
        };
        private static readonly string[] Fraud =
        {
            "Sharp-eyed staff foil insurance fraud attempt at Triage.",
            "Patient's 'chronic back pain' found to be chronic paperwork forgery."
        };
        private static readonly string[] Citations =
        {
            "Hospital racks up {0} citations in one shift. Compliance office 'not surprised'.",
            "{0} rule breaches in a day: Board considers sending a strongly worded memo."
        };
        private static readonly string[] Referrals =
        {
            "Patients turned away at the door. Hospital says 'it's complicated'.",
            "Referral numbers climb. Clinic down the road 'very busy, thanks'."
        };
        private static readonly string[] GoodDay =
        {
            "{0} patients sent home healthy. Local paper runs out of complaints.",
            "Busy shift ends with {0} recoveries and only mild chaos."
        };
        private static readonly string[] Quiet =
        {
            "Quiet shift at the hospital. Staff suspicious.",
            "Nothing to report. Board schedules a meeting about it.",
            "Hospital survives another shift. Cafeteria lasagne does not."
        };
    }
}
