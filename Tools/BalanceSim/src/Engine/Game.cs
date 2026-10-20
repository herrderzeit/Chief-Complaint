using System;
using System.Collections.Generic;
using System.Linq;
using ChiefComplaint.Sim.Data;

namespace ChiefComplaint.Sim.Engine
{
    public enum Phase { Shift, TeamAssessment, Draft, Won, Lost }

    /// <summary>
    /// The rules of one run, with no Unity dependency. Each method notes the Unity code it mirrors, so a
    /// rule change in the game should be copied here (the tests pin the behaviours that matter for balance).
    ///
    /// Drive it with <see cref="LegalActions"/> and <see cref="Apply"/> until <see cref="IsOver"/>.
    /// UI-only states (drag hover, the Escape menu, the tutorial) are not modelled.
    /// </summary>
    public sealed class Game
    {
        public readonly GameData Data;
        public readonly GameConfig Config;
        private readonly Random rng;
        private readonly IRunObserver observer;

        // Economy (GameManager)
        public int AP { get; private set; }
        public int Reputation { get; private set; }
        public int Funding { get; private set; }
        public int Shift { get; private set; } = 1;
        public Phase Phase { get; private set; } = Phase.Shift;
        public bool IsOver => Phase == Phase.Won || Phase == Phase.Lost;
        public string LossCause { get; private set; } = "";

        // Cards (DeckManager)
        public readonly List<CardDef> DrawPile = new List<CardDef>();
        public readonly List<CardDef> Hand = new List<CardDef>();
        public readonly List<CardDef> DiscardPile = new List<CardDef>();
        private readonly List<CardDef> redTapeDrawPile = new List<CardDef>();

        // Board (BoardManager / PatientDeckManager)
        public readonly List<Patient> Patients = new List<Patient>();
        private readonly List<PatientDef> activePatientDeck = new List<PatientDef>();
        private readonly HashSet<SymptomCategory> usedDepartments = new HashSet<SymptomCategory>();
        public int PatientsCured { get; private set; }
        public bool AmbulancesUnlocked { get; private set; }
        public bool TraumaFlightsUnlocked { get; private set; }
        private int nextPatientId = 1;

        // Team Assessment (StaffSynergyManager)
        private int bestDoctorTier, bestNurseTier;
        private bool synergyUsedThisShift;
        public int PendingTeamAssessmentTier { get; private set; }

        // Draft (DraftManager)
        public readonly List<CardDef> MarketOffers = new List<CardDef>();
        public readonly List<bool> MarketPurchased = new List<bool>();
        private readonly List<CardDef> pendingPurchases = new List<CardDef>();

        public Game(GameData data, int seed, IRunObserver observer = null, GameConfig config = null)
        {
            Data = data;
            Config = config ?? data.Config;
            rng = new Random(seed);
            this.observer = observer;
        }

        // =====================================================================
        // Queries used by bots
        // =====================================================================

        public IEnumerable<Patient> ActivePatients => Patients.Where(p => !p.Removed);
        public int WardBedsUsed => Patients.Count(p => !p.Removed && p.HasWardBed);
        public int WardBedsFree => Config.WardSlots - WardBedsUsed;
        public int TriageCount => Patients.Count(p => !p.Removed && p.Location == Location.Triage);
        public bool IsDepartmentUsed(SymptomCategory category) => usedDepartments.Contains(category);
        public bool SynergyUsedThisShift => synergyUsedThisShift;
        public int BestDoctorTierThisShift => bestDoctorTier;
        public int BestNurseTierThisShift => bestNurseTier;

        /// <summary>Every card the player owns right now (draw + discard + hand).</summary>
        public IEnumerable<CardDef> OwnedCards => DrawPile.Concat(DiscardPile).Concat(Hand);

        public string EscalationPhase => TraumaFlightsUnlocked ? "Trauma Flights" : AmbulancesUnlocked ? "Ambulances" : "Walk-Ins";

        public int RandomInt(int minInclusive, int maxExclusive) => rng.Next(minInclusive, maxExclusive);

        // =====================================================================
        // Run start (GameManager.StartRun)
        // =====================================================================

        public void Start()
        {
            AP = Config.BaseAP;
            Reputation = Config.StartingReputation;
            Funding = Config.StartingFunding;
            Shift = 1;
            Phase = Phase.Shift;

            // PatientDeckManager.Awake: Walk-Ins only.
            activePatientDeck.Clear();
            activePatientDeck.AddRange(Data.WalkInPool);

            // DeckManager.StartRun
            DrawPile.Clear();
            InitializeRedTapeDeck();
            DrawPile.AddRange(Data.StartingDeck);
            Shuffle(DrawPile);
            DrawCards(Config.HandSize);

            for (int i = 0; i < Config.InitialPatientCount; i++) SpawnPatient();
            NotifyIfOver();
        }

        // =====================================================================
        // Legal actions
        // =====================================================================

        public List<GameAction> LegalActions()
        {
            var actions = new List<GameAction>();
            switch (Phase)
            {
                case Phase.Shift: AddShiftActions(actions); break;
                case Phase.TeamAssessment:
                    foreach (Patient p in ActivePatients)
                        if (p.CanTeamAssess(PendingTeamAssessmentTier))
                            actions.Add(new GameAction { Kind = ActionKind.TeamAssess, Patient = p });
                    actions.Add(GameAction.SkipTeamAssess);
                    break;
                case Phase.Draft: AddDraftActions(actions); break;
            }
            return actions;
        }

        private void AddShiftActions(List<GameAction> actions)
        {
            // Admit: DraggablePatient onto an empty, unreserved Ward bed.
            if (WardBedsFree > 0)
                foreach (Patient p in ActivePatients)
                    if (p.Location == Location.Triage)
                        actions.Add(new GameAction { Kind = ActionKind.Admit, Patient = p });

            // Cards. Duplicate copies in hand produce one action each target, not one per copy.
            var seen = new HashSet<CardDef>();
            foreach (CardDef card in Hand)
            {
                if (!seen.Add(card) || !card.IsPlayable || AP < card.ApCost) continue;
                if (card.Effects.Any(e => e is CureEffect)) continue; // played through Treat

                bool requiresPatient = card.Effects.Any(e => e.RequiresPatientTarget);
                bool canTargetPatient = card.Effects.Any(e => e.CanTargetPatient);

                if (!requiresPatient) AddPlayActions(actions, card, null);
                if (requiresPatient || canTargetPatient)
                    foreach (Patient p in ActivePatients) AddPlayActions(actions, card, p);
            }

            // Treat: move to a matching, open department and cure one symptom.
            foreach (Patient p in ActivePatients)
            {
                if (p.Location != Location.Ward || p.InSurgery || !p.IsFullyDiagnosed || p.Symptoms.Count == 0) continue;
                foreach (SymptomCategory dept in p.Symptoms.Select(s => s.Category).Distinct())
                {
                    if (usedDepartments.Contains(dept)) continue;
                    int lowest = p.LowestTierIn(dept);
                    var seenCure = new HashSet<CardDef>();
                    foreach (CardDef card in Hand)
                    {
                        if (!seenCure.Add(card) || !card.IsPlayable || AP < card.ApCost) continue;
                        CureEffect cure = card.Effects.OfType<CureEffect>().FirstOrDefault();
                        if (cure == null || cure.MaxTier < lowest) continue;
                        actions.Add(new GameAction { Kind = ActionKind.Treat, Patient = p, Department = dept, Card = card, Cost = card.ApCost });
                    }
                    int pageCost = Config.PageCost(lowest);
                    if (AP >= pageCost)
                        actions.Add(new GameAction { Kind = ActionKind.Treat, Patient = p, Department = dept, Page = true, Cost = pageCost });
                }
            }

            actions.Add(GameAction.EndShift);
        }

        private void AddPlayActions(List<GameAction> actions, CardDef card, Patient target)
        {
            // A choice card offers both options only when both are usable on this target (ChoiceEffectSO.Apply).
            ChoiceEffect choice = card.Effects.OfType<ChoiceEffect>().FirstOrDefault();
            EitherOrEffect eitherOr = card.Effects.OfType<EitherOrEffect>().FirstOrDefault();
            if (choice != null && WouldSucceed(choice.OptionA, target) && WouldSucceed(choice.OptionB, target))
            {
                actions.Add(new GameAction { Kind = ActionKind.PlayCard, Card = card, Patient = target, Option = 0, Cost = card.ApCost });
                actions.Add(new GameAction { Kind = ActionKind.PlayCard, Card = card, Patient = target, Option = 1, Cost = card.ApCost });
                return;
            }
            if (eitherOr != null)
            {
                actions.Add(new GameAction { Kind = ActionKind.PlayCard, Card = card, Patient = target, Option = 0, Cost = card.ApCost });
                actions.Add(new GameAction { Kind = ActionKind.PlayCard, Card = card, Patient = target, Option = 1, Cost = card.ApCost });
                return;
            }
            if (WouldPlay(card, target))
                actions.Add(new GameAction { Kind = ActionKind.PlayCard, Card = card, Patient = target, Cost = card.ApCost });
        }

        /// <summary>Would dropping <paramref name="card"/> on this target be accepted (ignoring the success roll)?</summary>
        public bool WouldPlay(CardDef card, Patient target)
        {
            if (card.Effects.Count == 0) return target == null; // e.g. Prior Authorization on the play zone
            foreach (EffectDef effect in card.Effects)
                if (CanApply(effect, target) && WouldSucceed(effect, target)) return true;
            return false;
        }

        private void AddDraftActions(List<GameAction> actions)
        {
            for (int i = 0; i < MarketOffers.Count; i++)
            {
                if (MarketPurchased[i]) continue;
                int cost = MarketOffers[i].DraftValue;
                if (Funding >= cost)
                    actions.Add(new GameAction { Kind = ActionKind.Buy, Card = MarketOffers[i], MarketSlot = i, Cost = cost });
            }
            // DeckUpgradeManager lists every distinct card in draw + discard + hand (the hand is empty during the draft).
            foreach (CardDef card in OwnedCards.Distinct())
                if (card.Upgrade != null && Funding >= card.UpgradeCost)
                    actions.Add(new GameAction { Kind = ActionKind.Upgrade, Card = card, Cost = card.UpgradeCost });
            actions.Add(GameAction.FinishDraft);
        }

        // =====================================================================
        // Applying actions
        // =====================================================================

        public void Apply(GameAction action)
        {
            if (IsOver) return;
            observer?.OnAction(this, action);
            switch (action.Kind)
            {
                case ActionKind.Admit: Admit(action.Patient); break;
                case ActionKind.PlayCard: PlayCard(action.Card, action.Patient, action.Option); break;
                case ActionKind.Treat: Treat(action); break;
                case ActionKind.EndShift: EndShift(); break;
                case ActionKind.TeamAssess: ResolveTeamAssessment(action.Patient); break;
                case ActionKind.SkipTeamAssess: PendingTeamAssessmentTier = 0; Phase = Phase.Shift; break;
                case ActionKind.Buy: Buy(action.MarketSlot); break;
                case ActionKind.Upgrade: UpgradeCard(action.Card); break;
                case ActionKind.FinishDraft: FinishDraft(); break;
            }
            NotifyIfOver();
        }

        private bool endNotified;

        private void NotifyIfOver()
        {
            if (!IsOver || endNotified) return;
            endNotified = true;
            observer?.OnRunEnded(this);
        }

        private void Admit(Patient p)
        {
            if (Phase != Phase.Shift || p.Removed || p.Location != Location.Triage || WardBedsFree <= 0) return;
            p.Location = Location.Ward;
        }

        /// <summary>Mirrors <c>DraggableCard.OnMouseUp</c>. Returns true when the card was consumed.</summary>
        public bool PlayCard(CardDef card, Patient target, int option = -1)
        {
            if (Phase != Phase.Shift || !Hand.Contains(card) || !card.IsPlayable || AP < card.ApCost) return false;
            if (target != null && target.Removed) return false;

            bool requiresPatient = card.Effects.Any(e => e.RequiresPatientTarget);
            bool canTargetPatient = card.Effects.Any(e => e.CanTargetPatient);
            if (requiresPatient && target == null) return false;
            if (!requiresPatient && target != null && !canTargetPatient) return false;

            // Random.value > successChance
            bool rngFailed = card.SuccessChance < 1f && rng.NextDouble() > card.SuccessChance;

            bool applied = false;
            foreach (EffectDef effect in card.Effects)
                if (CanApply(effect, target) && ApplyEffect(effect, target, rngFailed, option, card.Name))
                    applied = true;
            if (card.Effects.Count == 0 && target == null) applied = true;
            if (!applied) return false;

            if (!rngFailed && card.CardsToDraw > 0) DrawCards(card.CardsToDraw);
            SpendAP(card.ApCost);
            Hand.Remove(card);
            if (!card.ExhaustsOnUse) DiscardPile.Add(card);
            observer?.OnCardPlayed(this, card, target);
            if (!IsOver) HandleSynergy(card);
            NotifyIfOver();
            return true;
        }

        /// <summary>
        /// Drag the patient into the department, then treat: play the cure card on them, or press the
        /// "Page the on-call team" button (DepartmentFallbackButton: pay only if the treatment worked).
        /// </summary>
        private void Treat(GameAction a)
        {
            Patient p = a.Patient;
            if (Phase != Phase.Shift || p.Removed || p.Location != Location.Ward || p.InSurgery || !p.IsFullyDiagnosed) return;
            if (usedDepartments.Contains(a.Department) || !p.Symptoms.Any(s => s.Category == a.Department)) return;

            p.Location = Location.Department;
            p.Department = a.Department;
            int symptomsBefore = p.Symptoms.Count;
            Symptom treated = null;

            if (a.Page)
            {
                int tier = p.LowestTierIn(a.Department);
                int cost = Config.PageCost(tier);
                if (AP >= cost)
                {
                    treated = FirstCurable(p, tier);
                    if (TryCureSymptom(p, tier, false)) SpendAP(cost);
                }
            }
            else
            {
                CureEffect cure = a.Card.Effects.OfType<CureEffect>().FirstOrDefault();
                if (cure != null) treated = FirstCurable(p, cure.MaxTier);
                PlayCard(a.Card, p);
            }

            if (p.Symptoms.Count < symptomsBefore && treated != null)
                observer?.OnSymptomTreated(this, p, treated, a.Page, a.Page ? Config.PageCost(treated.Tier) : a.Card.ApCost);
            // Untreated patients walk back to their bed rather than blocking the department.
            if (!p.InSurgery && !p.Removed) p.Location = Location.Ward;
        }

        private static Symptom FirstCurable(Patient p, int maxTier) =>
            p.Symptoms.FirstOrDefault(s => s.Category == p.Department && s.Tier <= maxTier);

        private void EndShift()
        {
            if (Phase != Phase.Shift) return;
            int leftOver = AP;

            // GameManager.EnterDraftPhase
            ResolveSurgeries();
            observer?.OnShiftEnded(this, leftOver);
            if (IsOver) return;

            if (Config.ShiftsToWin > 0 && Shift >= Config.ShiftsToWin)
            {
                Phase = Phase.Won;
                return;
            }

            Phase = Phase.Draft;
            DiscardHand();
            OpenDraft();
        }

        // =====================================================================
        // Effects (CardEffectSO.CanApply / Apply)
        // =====================================================================

        public bool CanApply(EffectDef effect, Patient target)
        {
            switch (effect)
            {
                case DiagnoseEffect _:
                case CureEffect _: return target != null;
                case HealEffect _: return target != null && target.Stability < target.MaxStability;
                case PauseEffect _: return target != null && !target.ClockPaused && !target.IsAwaitingDischarge;
                case ChoiceEffect c: return (c.OptionA != null && CanApply(c.OptionA, target)) || (c.OptionB != null && CanApply(c.OptionB, target));
                case WardWideEffect w:
                    return w.PatientEffect != null && WardPatients().Any(p => CanApply(w.PatientEffect, p));
                case UnknownEffect _: return false;
                default: return true;
            }
        }

        /// <summary>Would <c>Apply</c> return true (assuming the success roll passes)? Side-effect free.</summary>
        public bool WouldSucceed(EffectDef effect, Patient target)
        {
            if (effect == null || !CanApply(effect, target)) return false;
            switch (effect)
            {
                case DiagnoseEffect d:
                    return !target.IsFullyDiagnosed && target.Location == Location.Ward && target.NextHiddenTier <= d.MaxTier;
                case CureEffect c:
                    return !target.InSurgery && target.IsFullyDiagnosed && target.Location == Location.Department
                        && !usedDepartments.Contains(target.Department)
                        && target.Symptoms.Any(s => s.Category == target.Department && s.Tier <= c.MaxTier);
                case ChoiceEffect ch:
                    return WouldSucceed(ch.OptionA, target) || WouldSucceed(ch.OptionB, target);
                case WardWideEffect w:
                    return WardPatients().Any(p => WouldSucceed(w.PatientEffect, p));
                default: return true;
            }
        }

        private bool ApplyEffect(EffectDef effect, Patient target, bool rngFailed, int option, string source)
        {
            switch (effect)
            {
                case GainApEffect g:
                    if (!rngFailed) GainAP(g.Amount);
                    return true;
                case GainReputationEffect r:
                    if (!rngFailed)
                    {
                        if (r.Amount > 0) AddReputation(r.Amount, "card:" + source);
                        else if (r.Amount < 0) SpendReputation(-r.Amount, "card:" + source);
                    }
                    return true;
                case DrawCardsEffect d:
                    if (!rngFailed) DrawCards(d.Amount);
                    return true;
                case DiagnoseEffect d:
                    return TryDiagnoseNext(target, d.MaxTier, rngFailed);
                case CureEffect c:
                    return TryCureSymptom(target, c.MaxTier, rngFailed);
                case HealEffect h:
                    if (!rngFailed) ApplyStabilisation(target, h.Amount);
                    return true;
                case PauseEffect _:
                    if (!rngFailed) target.ClockPaused = true;
                    return true;
                case CleanseRedTapeEffect c:
                    if (!rngFailed) CleanseRedTape(c.RepPerCard);
                    return true;
                case EitherOrEffect e:
                    if (!rngFailed)
                    {
                        if (option == 1) GainAP(e.Ap);
                        else AddReputation(e.Reputation, "card:" + source);
                    }
                    return true;
                case WardWideEffect w:
                {
                    if (rngFailed) return true;
                    bool any = false;
                    foreach (Patient p in WardPatients().ToList())
                        if (CanApply(w.PatientEffect, p) && ApplyEffect(w.PatientEffect, p, false, -1, source)) any = true;
                    return any;
                }
                case ChoiceEffect c:
                {
                    if (rngFailed) return true;
                    bool canA = c.OptionA != null && CanApply(c.OptionA, target);
                    bool canB = c.OptionB != null && CanApply(c.OptionB, target);
                    if (canA && !canB) return ApplyEffect(c.OptionA, target, false, -1, source);
                    if (canB && !canA) return ApplyEffect(c.OptionB, target, false, -1, source);
                    if (!canA && !canB) return false;
                    // Both usable: the player picks on the prompt. The prompt counts as played even if the pick then fizzles.
                    EffectDef picked = option == 1 ? c.OptionB : c.OptionA;
                    if (CanApply(picked, target)) ApplyEffect(picked, target, false, -1, source);
                    return true;
                }
                default:
                    return false;
            }
        }

        private IEnumerable<Patient> WardPatients() => Patients.Where(p => !p.Removed && p.HasWardBed);

        // =====================================================================
        // Patients (PatientCardView)
        // =====================================================================

        private bool TryDiagnoseNext(Patient p, int maxTier, bool rngFailed)
        {
            if (p.IsFullyDiagnosed || p.Location != Location.Ward) return false;
            if (p.Symptoms[p.DiagnosedCount].Tier > maxTier) return false;
            if (rngFailed) return true;
            RevealNextSymptom(p);
            return true;
        }

        private void RevealNextSymptom(Patient p)
        {
            p.DiagnosedCount++;
            ExecuteTriggers(p, TriggerCondition.OnDiagnosed);
        }

        private bool TryCureSymptom(Patient p, int maxTier, bool rngFailed)
        {
            if (p.InSurgery || !p.IsFullyDiagnosed || p.Location != Location.Department) return false;
            if (usedDepartments.Contains(p.Department)) return false;

            int index = p.Symptoms.FindIndex(s => s.Category == p.Department && s.Tier <= maxTier);
            if (index < 0) return false;
            if (rngFailed)
            {
                p.Location = Location.Ward;
                return true;
            }
            p.Symptoms.RemoveAt(index);
            if (p.DiagnosedCount > 0) p.DiagnosedCount--;
            usedDepartments.Add(p.Department);
            p.InSurgery = true;
            return true;
        }

        private void ApplyStabilisation(Patient p, int amount)
        {
            p.Stability = Math.Min(p.Stability + amount, p.MaxStability);
        }

        private void ResolveSurgeries()
        {
            for (int i = Patients.Count - 1; i >= 0; i--)
            {
                Patient p = Patients[i];
                if (!p.InSurgery || p.Removed) continue;
                p.InSurgery = false;
                if (p.Symptoms.Count == 0) Discharge(p);
                else p.Location = Location.Ward;
            }
        }

        private void Discharge(Patient p)
        {
            p.Removed = true;
            ExecuteTriggers(p, TriggerCondition.OnCured);

            int modifier = 0;
            if (p.Stability >= p.MaxStability / 2f) modifier = 1;
            else if (p.Stability <= Config.BarelyStableThreshold) modifier = -1;

            int rep = Math.Max(0, Math.Min(p.Def.ReputationReward, Config.CureReputationCap) + modifier);
            int funding = Math.Max(0, Config.FundingRewardFor(p.Def.Phase) + modifier);
            AddReputation(rep, "cure");
            AddFunding(funding, "cure");
            observer?.OnPatientCured(this, p, rep, funding);

            // PatientDeckManager.PatientSuccessfullyCured
            PatientsCured++;
            if (PatientsCured >= Config.AmbulanceCureThreshold && !AmbulancesUnlocked)
            {
                activePatientDeck.AddRange(Data.AmbulancePool);
                AmbulancesUnlocked = true;
            }
            if (PatientsCured >= Config.TraumaFlightCureThreshold && !TraumaFlightsUnlocked)
            {
                activePatientDeck.AddRange(Data.TraumaFlightPool);
                TraumaFlightsUnlocked = true;
            }
            Patients.Remove(p);
        }

        /// <summary>Mirrors <c>PatientCardView.AdvanceShiftDecay</c>.</summary>
        private void AdvanceDecay(Patient p)
        {
            if (p.Removed) return;
            if (p.ClockPaused)
            {
                p.ClockPaused = false;
                return;
            }
            p.Stability -= p.HasWardBed ? Math.Max(1, (p.DecayRate + 1) / 2) : p.DecayRate;
            if (p.Stability <= 0)
            {
                p.Stability = 0;
                p.Removed = true;
                observer?.OnPatientDied(this, p);
                SpendReputation(Config.DeathReputationPenalty, "death");
                ExecuteTriggers(p, TriggerCondition.OnDeath);
                if (Config.DeathAddsRedTape) SpawnRandomRedTape(false);
                Patients.Remove(p);
                if (IsOver && LossCause.Length == 0) LossCause = "death";
                return;
            }
            ExecuteTriggers(p, TriggerCondition.OnTurnEnd);
        }

        private void ExecuteTriggers(Patient p, TriggerCondition condition)
        {
            foreach (PatientTriggerDef t in p.Def.Triggers)
            {
                if (t.Condition != condition) continue;
                string source = condition == TriggerCondition.OnDeath ? "death-trigger" : "patient-trigger";
                switch (t.Effect)
                {
                    case TriggerEffect.GainAP: GainAP(t.Value); break;
                    case TriggerEffect.LoseAP: SpendAP(t.Value); break;
                    case TriggerEffect.GainReputation: AddReputation(t.Value, source); break;
                    case TriggerEffect.LoseReputation: SpendReputation(t.Value, source); break;
                    case TriggerEffect.DrawCard: DrawCards(t.Value); break;
                    case TriggerEffect.ChangeStability: ApplyStabilisation(p, t.Value); break;
                    case TriggerEffect.AddCardToDiscard:
                        if (t.CardReference != null) DiscardPile.Add(t.CardReference);
                        else SpawnRandomRedTape(false);
                        break;
                    case TriggerEffect.DiscardCard:
                        for (int i = 0; i < t.Value && Hand.Count > 0; i++)
                        {
                            CardDef card = Hand[rng.Next(0, Hand.Count)];
                            Hand.Remove(card);
                            DiscardPile.Add(card);
                        }
                        break;
                    case TriggerEffect.AddCardToHand:
                        if (t.CardReference != null) Hand.Add(t.CardReference);
                        else SpawnRandomRedTape(true);
                        break;
                }
            }
        }

        // =====================================================================
        // Spawning (BoardManager.SpawnPatient / PatientDeckManager)
        // =====================================================================

        private void SpawnPatient()
        {
            if (TriageCount >= Config.TriageSlots)
            {
                observer?.OnSpawnSkipped(this, "triage full");
                return;
            }
            if (activePatientDeck.Count == 0)
            {
                observer?.OnSpawnSkipped(this, "patient deck empty");
                return;
            }

            int index = rng.Next(0, activePatientDeck.Count);
            PatientDef def = activePatientDeck[index];
            activePatientDeck.RemoveAt(index);

            var p = new Patient
            {
                Id = nextPatientId++,
                Def = def,
                Symptoms = GenerateSymptoms(def),
                MaxStability = def.StartingStability,
                Stability = def.StartingStability,
                DecayRate = def.DecayPerTurn,
                ArrivalShift = Shift,
                Location = Location.Triage
            };
            // InitialiseCard: stable bubble sort by tier, lowest first.
            for (int i = 0; i < p.Symptoms.Count - 1; i++)
                for (int j = 0; j < p.Symptoms.Count - i - 1; j++)
                    if (p.Symptoms[j].Tier > p.Symptoms[j + 1].Tier)
                        (p.Symptoms[j], p.Symptoms[j + 1]) = (p.Symptoms[j + 1], p.Symptoms[j]);
            p.InitialSymptomCount = p.Symptoms.Count;
            p.InitialMaxTier = p.Symptoms.Count == 0 ? 0 : p.Symptoms.Max(s => s.Tier);

            Patients.Add(p);
            observer?.OnPatientArrived(this, p);
            ExecuteTriggers(p, TriggerCondition.OnSpawn);
        }

        private List<Symptom> GenerateSymptoms(PatientDef patient)
        {
            var result = new List<Symptom>();
            var drawn = new List<SymptomDef>();
            for (int i = 0; i < patient.Categories.Count; i++)
            {
                SymptomDef symptom = DrawSymptom(patient.Categories[i], drawn, patient.Tags);
                if (symptom == null) continue;
                drawn.Add(symptom);
                // Only the primary symptom scales with triage severity.
                int tier = i == 0 ? TierFromTriage(patient.TriageLevel) : 1;
                result.Add(new Symptom { Def = symptom, Tier = tier });
            }
            return result;
        }

        private SymptomDef DrawSymptom(SymptomCategory category, List<SymptomDef> exclude, List<string> patientTags)
        {
            if (!Data.SymptomDecks.TryGetValue(category, out List<SymptomDef> deck) || deck.Count == 0) return null;
            List<SymptomDef> available = deck.Where(s => !exclude.Contains(s)).ToList();
            if (available.Count == 0) return null;
            if (patientTags != null && patientTags.Count > 0)
            {
                List<SymptomDef> tagged = available.Where(s => s.Tags.Any(patientTags.Contains)).ToList();
                if (tagged.Count > 0) return tagged[rng.Next(0, tagged.Count)];
            }
            return available[rng.Next(0, available.Count)];
        }

        public static int TierFromTriage(int triage) => triage == 1 ? 3 : (triage == 2 || triage == 3) ? 2 : 1;

        // =====================================================================
        // Shift transition (GameManager.StartNewShift)
        // =====================================================================

        private void StartNewShift()
        {
            Phase = Phase.Shift;
            Shift++;
            bestDoctorTier = bestNurseTier = PendingTeamAssessmentTier = 0;
            synergyUsedThisShift = false;
            AP = Config.BaseAP;

            // BoardManager.AdvanceShift: iterate backwards so deaths don't skip anyone.
            ResolveSurgeries();
            for (int i = Patients.Count - 1; i >= 0; i--)
                if (i < Patients.Count) AdvanceDecay(Patients[i]);
            usedDepartments.Clear();
            if (IsOver) return;

            DrawCards(Config.HandSize);
            SpawnPatient();
        }

        // =====================================================================
        // Team Assessment (StaffSynergyManager)
        // =====================================================================

        private void HandleSynergy(CardDef card)
        {
            if (synergyUsedThisShift) return;
            if (card.HasTag(CardTag.Doctor)) bestDoctorTier = Math.Max(bestDoctorTier, card.PowerTier);
            if (card.HasTag(CardTag.Nurse)) bestNurseTier = Math.Max(bestNurseTier, card.PowerTier);
            if (bestDoctorTier == 0 || bestNurseTier == 0) return;

            synergyUsedThisShift = true;
            int tier = Math.Clamp(Math.Min(bestDoctorTier, bestNurseTier), 1, 3);
            if (!ActivePatients.Any(p => p.CanTeamAssess(tier))) return; // wasted: nobody eligible
            PendingTeamAssessmentTier = tier;
            Phase = Phase.TeamAssessment;
        }

        private void ResolveTeamAssessment(Patient p)
        {
            int tier = PendingTeamAssessmentTier;
            PendingTeamAssessmentTier = 0;
            Phase = Phase.Shift;
            if (p == null || !p.CanTeamAssess(tier)) return;

            int lowest = p.Symptoms[p.DiagnosedCount].Tier;
            var candidates = new List<int>();
            for (int i = p.DiagnosedCount; i < p.Symptoms.Count; i++)
                if (p.Symptoms[i].Tier == lowest) candidates.Add(i);
            int pick = candidates[rng.Next(0, candidates.Count)];
            if (pick != p.DiagnosedCount)
                (p.Symptoms[pick], p.Symptoms[p.DiagnosedCount]) = (p.Symptoms[p.DiagnosedCount], p.Symptoms[pick]);
            observer?.OnTeamAssessment(this, tier, p);
            RevealNextSymptom(p);
        }

        // =====================================================================
        // Draft (DraftManager / DeckUpgradeManager)
        // =====================================================================

        private void OpenDraft()
        {
            pendingPurchases.Clear();
            MarketOffers.Clear();
            MarketPurchased.Clear();
            if (Data.DraftPool.Count == 0) return;
            for (int i = 0; i < Config.MarketSize; i++)
            {
                MarketOffers.Add(Data.DraftPool[rng.Next(0, Data.DraftPool.Count)]);
                MarketPurchased.Add(false);
            }
        }

        private void Buy(int slot)
        {
            if (Phase != Phase.Draft || slot < 0 || slot >= MarketOffers.Count || MarketPurchased[slot]) return;
            CardDef card = MarketOffers[slot];
            int cost = card.DraftValue;
            if (!TrySpendFunding(cost, "buy")) return;
            pendingPurchases.Add(card);
            MarketPurchased[slot] = true;
            observer?.OnPurchase(this, "buy:" + card.Name, card, cost);
        }

        private void UpgradeCard(CardDef card)
        {
            if (Phase != Phase.Draft || card.Upgrade == null) return;
            if (!DrawPile.Contains(card) && !DiscardPile.Contains(card) && !Hand.Contains(card)) return;
            if (!TrySpendFunding(card.UpgradeCost, "upgrade")) return;
            // The upgraded copy is appended to the same pile (so in the draw pile it goes to the bottom).
            if (DrawPile.Remove(card)) DrawPile.Add(card.Upgrade);
            else if (DiscardPile.Remove(card)) DiscardPile.Add(card.Upgrade);
            else if (Hand.Remove(card)) Hand.Add(card.Upgrade);
            observer?.OnPurchase(this, "upgrade:" + card.Name, card, card.UpgradeCost);
        }

        private void FinishDraft()
        {
            if (Phase != Phase.Draft) return;
            foreach (CardDef card in pendingPurchases) DiscardPile.Add(card);
            pendingPurchases.Clear();
            StartNewShift();
        }

        // =====================================================================
        // Economy (GameManager)
        // =====================================================================

        private void SpendAP(int amount) => AP = Math.Max(0, AP - amount);
        private void GainAP(int amount) => AP += amount;

        private void AddReputation(int amount, string source)
        {
            Reputation += amount;
            if (amount != 0) observer?.OnReputationChanged(this, amount, source);
        }

        private void SpendReputation(int amount, string source)
        {
            Reputation -= amount;
            observer?.OnReputationChanged(this, -amount, source);
            if (Reputation <= 0)
            {
                Reputation = 0;
                if (!IsOver)
                {
                    Phase = Phase.Lost;
                    LossCause = source;
                }
            }
        }

        private void AddFunding(int amount, string source)
        {
            Funding += amount;
            if (amount != 0) observer?.OnFundingChanged(this, amount, source);
        }

        private bool TrySpendFunding(int amount, string source)
        {
            if (amount > Funding) return false;
            Funding -= amount;
            observer?.OnFundingChanged(this, -amount, source);
            return true;
        }

        // =====================================================================
        // Deck (DeckManager)
        // =====================================================================

        public void DrawCards(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                if (DrawPile.Count == 0)
                {
                    if (DiscardPile.Count == 0) return;
                    DrawPile.AddRange(DiscardPile);
                    DiscardPile.Clear();
                    Shuffle(DrawPile);
                }
                CardDef card = DrawPile[0];
                DrawPile.RemoveAt(0);
                Hand.Add(card);
            }
        }

        private void DiscardHand()
        {
            foreach (CardDef card in Hand)
                if (!card.ExhaustsAtShiftEnd) DiscardPile.Add(card);
            Hand.Clear();
        }

        private void InitializeRedTapeDeck()
        {
            redTapeDrawPile.Clear();
            redTapeDrawPile.AddRange(Data.RedTapePool);
            Shuffle(redTapeDrawPile);
        }

        private void SpawnRandomRedTape(bool toHand)
        {
            if (Data.RedTapePool.Count == 0) return;
            if (redTapeDrawPile.Count == 0) InitializeRedTapeDeck();
            CardDef card = redTapeDrawPile[0];
            redTapeDrawPile.RemoveAt(0);
            if (toHand) Hand.Add(card);
            else DiscardPile.Add(card);
        }

        private void CleanseRedTape(int repPerCard)
        {
            int cleansed = DiscardPile.RemoveAll(c => c.Type == CardType.RedTape)
                         + DrawPile.RemoveAll(c => c.Type == CardType.RedTape)
                         + Hand.RemoveAll(c => c.Type == CardType.RedTape);
            if (cleansed > 0 && repPerCard > 0) AddReputation(repPerCard * cleansed, "cleanse");
        }

        private void Shuffle(List<CardDef> pile)
        {
            for (int i = 0; i < pile.Count; i++)
            {
                int j = rng.Next(i, pile.Count);
                (pile[i], pile[j]) = (pile[j], pile[i]);
            }
        }
    }
}
