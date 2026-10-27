using System.IO;
using System.Linq;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Engine;

namespace ChiefComplaint.Sim.Stats
{
    /// <summary>Prints a readable play-by-play of one run, for checking the rules and the bots by eye.</summary>
    public sealed class TracePrinter : RunObserverBase
    {
        private readonly TextWriter w;
        private int lastShiftHeader;

        public TracePrinter(TextWriter writer) { w = writer; }

        public override void OnAction(Game g, GameAction a)
        {
            if (g.Shift != lastShiftHeader && g.Phase == Phase.Shift)
            {
                lastShiftHeader = g.Shift;
                w.WriteLine();
                w.WriteLine($"=== Shift {g.Shift} | AP {g.AP} | Rep {g.Reputation} | Funding {g.Funding} | {g.EscalationPhase} | cured {g.PatientsCured}");
                w.WriteLine("  Hand: " + string.Join(", ", g.Hand.Select(c => c.Name)));
                foreach (Patient p in g.ActivePatients)
                    w.WriteLine($"  {p}  symptoms: {string.Join("; ", p.Symptoms.Select((s, i) => (i < p.DiagnosedCount ? "" : "?") + s))}");
            }
            if (a.Kind == ActionKind.LeaveStaffRoom) return;
            w.WriteLine($"  > {a}   (AP {g.AP})");
        }

        public override void OnPatientArrived(Game g, Patient p) => w.WriteLine($"  + arrives: {p.Def.Name} ({p.Def.Phase}, triage {p.Def.TriageLevel}, stab {p.MaxStability}, decay {p.DecayRate}) {string.Join("; ", p.Symptoms)}");
        public override void OnSpawnSkipped(Game g, string reason) => w.WriteLine($"  ! spawn skipped: {reason}");
        public override void OnPatientDied(Game g, Patient p) => w.WriteLine($"  x DIED: {p.Def.Name} ({p.Def.Phase}) at {p.Location}");
        public override void OnPatientCured(Game g, Patient p, int rep, int funding) => w.WriteLine($"  * discharged {p.Def.Name}: +{rep} Rep, +{funding} Funding");
        public override void OnTeamAssessment(Game g, int tier, Patient p) => w.WriteLine($"  ~ Team Assessment T{tier} on {p.Def.Name}");
        public override void OnShiftEnded(Game g, int ap) => w.WriteLine($"  -- end of shift {g.Shift}: Rep {g.Reputation}, Funding {g.Funding}, AP unused {ap}");
        public override void OnPurchase(Game g, string item, CardDef card, int cost) => w.WriteLine($"  $ {item} ({cost} Funding)");
        public override void OnReputationChanged(Game g, int delta, string source)
        {
            if (source != "cure") w.WriteLine($"  rep {(delta >= 0 ? "+" : "")}{delta} ({source}) -> {g.Reputation}");
        }
        public override void OnRunEnded(Game g) => w.WriteLine($"\n#### RUN OVER: {g.Phase} at shift {g.Shift}, Rep {g.Reputation}, Funding {g.Funding}{(g.LossCause.Length > 0 ? ", final blow: " + g.LossCause : "")}");
    }
}
