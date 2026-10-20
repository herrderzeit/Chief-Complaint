namespace ChiefComplaint.Bureaucracy
{
    public enum CitationPenalty
    {
        Warning,
        Fine,
        RedTape
    }

    /// <summary>
    /// Pure citation rules: the first <see cref="warningsPerShift"/> citations in a shift are warnings,
    /// the next is a Funding fine, and every one after that adds a Red Tape card.
    /// </summary>
    public class CitationLedger
    {
        public int warningsPerShift = 2;

        /// <summary>Citations issued this shift.</summary>
        public int ShiftCount { get; private set; }

        /// <summary>Citations issued since the last inspection.</summary>
        public int PeriodCount { get; private set; }

        /// <summary>Citations issued this run.</summary>
        public int TotalCount { get; private set; }

        /// <summary>Records a citation and returns the penalty it carries.</summary>
        public CitationPenalty Issue()
        {
            ShiftCount++;
            PeriodCount++;
            TotalCount++;

            if (ShiftCount <= warningsPerShift) return CitationPenalty.Warning;
            if (ShiftCount == warningsPerShift + 1) return CitationPenalty.Fine;
            return CitationPenalty.RedTape;
        }

        public void ResetShift() => ShiftCount = 0;
        public void ResetPeriod() => PeriodCount = 0;
    }
}
