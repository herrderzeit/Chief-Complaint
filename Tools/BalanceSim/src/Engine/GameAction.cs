using ChiefComplaint.Sim.Data;

namespace ChiefComplaint.Sim.Engine
{
    public enum ActionKind
    {
        // Shift phase
        /// <summary>Drag a patient from Triage to a free Ward bed.</summary>
        Admit,
        /// <summary>Play a card from hand onto the play zone (Patient null) or onto a patient.</summary>
        PlayCard,
        /// <summary>Move a diagnosed Ward patient into a department and treat one symptom there, with a card or by paging.</summary>
        Treat,
        /// <summary>Press End Shift.</summary>
        EndShift,

        // Team Assessment pick (after the first Doctor + Nurse pair of a shift)
        TeamAssess,
        SkipTeamAssess,

        // staff room
        Buy,
        Upgrade,
        LeaveStaffRoom
    }

    public sealed class GameAction
    {
        public ActionKind Kind;
        public Patient Patient;
        public CardDef Card;
        /// <summary>For a "choose one" card: 0 = option A, 1 = option B, -1 = no choice needed.</summary>
        public int Option = -1;
        public SymptomCategory Department;
        /// <summary>Treat by paging the on-call team instead of playing a card.</summary>
        public bool Page;
        public int MarketSlot = -1;
        /// <summary>AP (shift) or Funding (staff room) the action costs up front.</summary>
        public int Cost;

        public static readonly GameAction EndShift = new GameAction { Kind = ActionKind.EndShift };
        public static readonly GameAction SkipTeamAssess = new GameAction { Kind = ActionKind.SkipTeamAssess };
        public static readonly GameAction LeaveStaffRoom = new GameAction { Kind = ActionKind.LeaveStaffRoom };

        public override string ToString() => Kind switch
        {
            ActionKind.Admit => $"Admit {Patient}",
            ActionKind.PlayCard => $"Play {Card.Name}{(Option >= 0 ? (Option == 0 ? " [A]" : " [B]") : "")}{(Patient != null ? " on " + Patient : "")}",
            ActionKind.Treat => $"Treat {Patient} in {Department} with {(Page ? "page (" + Cost + " AP)" : Card.Name)}",
            ActionKind.TeamAssess => $"Team Assessment on {Patient}",
            ActionKind.Buy => $"Buy {Card.Name} for {Cost}",
            ActionKind.Upgrade => $"Upgrade {Card.Name} -> {Card.Upgrade?.Name} for {Cost}",
            _ => Kind.ToString()
        };
    }
}
