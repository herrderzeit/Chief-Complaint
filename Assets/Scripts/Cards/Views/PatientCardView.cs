using UnityEngine;
using System.Collections.Generic;
using TMPro;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Cards.Views
{
    /// <summary>
    /// Presentation and runtime-state layer for a single patient card on the board.
    /// Manages stability, diagnosis progression, symptom sub-cards, the highlight outline,
    /// and the ghost-in-ward mechanic used when a patient visits a specialty department.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class PatientCardView : MonoBehaviour
    {
        [Header("Assigned Data")]
        public PatientCardSO patientData;
        public List<SymptomCardSO> assignedSymptoms = new List<SymptomCardSO>();
        public List<SymptomTier> assignedTiers = new List<SymptomTier>();

        [Header("Runtime State")]
        public Transform assignedWardSlot;
        public int diagnosedCount = 0;
        public int currentStability;
        public int maxStability;
        public int decayRate;

        /// <summary><c>true</c> when every assigned symptom has been diagnosed.</summary>
        public bool isFullyDiagnosed => diagnosedCount >= assignedSymptoms.Count;
        public bool isCritical = false;
        public bool stabilityClockPaused = false;

        /// <summary>
        /// <c>true</c> after a symptom is treated: the patient stays in the department (keeping it
        /// "In Use") and cannot be moved until <see cref="ResolveSurgery"/> runs when the shift ends.
        /// </summary>
        public bool isInSurgery = false;

        /// <summary><c>true</c> when the patient is in surgery with every symptom treated, waiting to be discharged at End Shift.</summary>
        public bool isAwaitingDischarge => isInSurgery && assignedSymptoms.Count == 0;

        /// <summary>Set once this patient has died or been discharged and is about to be destroyed.</summary>
        public bool isRemoved { get; private set; }

        /// <summary>Raised when this patient dies (stability reached zero), before the card is destroyed.</summary>
        public event System.Action<PatientCardView> OnDied;

        /// <summary>Raised when this card is destroyed for any reason (death, discharge or scene teardown).</summary>
        public event System.Action<PatientCardView> OnRemoved;

        /// <summary>Raised after stability is restored by a heal, so listeners (e.g. the Code Blue counter) can refresh.</summary>
        public event System.Action<PatientCardView> OnStabilityChanged;

        /// <summary>Stability at or below this value is shown in red and triggers a Code Blue alert.</summary>
        public const int LowStabilityThreshold = 3;

        /// <summary>Discharging at or below this stability reduces the Reputation reward.</summary>
        private const int BarelyStableThreshold = 2;

        [Header("Visual References")]
        public TextMeshPro nameText;
        public TextMeshPro complaintText;
        public TextMeshPro triageNumberText;
        public TextMeshPro stabilityText;
        public SpriteRenderer triageIcon;
        public TextMeshPro decayText;
        public TextMeshPro rewardText;
        public TextMeshPro tagsText;
        public Transform subCardSpawnParent;

        [Header("UI Feedback")]
        /// <summary>
        /// A child GameObject containing the outline graphic.
        /// Toggled active/inactive by <see cref="DraggableCard"/> to indicate a valid target.
        /// </summary>
        public GameObject highlightOutline;
        [Tooltip("Outline colour while this patient is a valid target for the card being dragged (the hover colour is the outline's own).")]
        public Color targetableOutlineColour = new Color(0.4f, 1f, 0.7f, 0.35f);
        private bool hoverHighlighted;
        private bool targetable;
        private Color? outlineBaseColour;
        private SpriteRenderer outlineRenderer;

        [Header("Visual Feedback")]
        /// <summary>
        /// A semi-transparent clone of this patient card placed in the ward slot while the
        /// original is visiting a specialty department. Destroyed when the patient returns.
        /// </summary>
        public GameObject ghostInstance;

        private List<GameObject> activeSubCards = new List<GameObject>();

        // -------------------------------------------------------------------------
        // Initialisation
        // -------------------------------------------------------------------------

        /// <summary>
        /// Initialises the patient card with the given data, spawns symptom sub-cards,
        /// and refreshes all visuals. Call this immediately after instantiation.
        /// </summary>
        /// <param name="patient">The patient's static data asset.</param>
        /// <param name="symptoms">The symptom cards assigned to this encounter.</param>
        /// <param name="tiers">Severity tiers corresponding 1-to-1 with <paramref name="symptoms"/>.</param>
        /// <param name="subCardPrefab">Prefab used to instantiate each <see cref="SymptomSubCardView"/>.</param>
        public void InitialiseCard(PatientCardSO patient, List<SymptomCardSO> symptoms, List<SymptomTier> tiers, GameObject subCardPrefab)
        {
            patientData = patient;
            assignedSymptoms = symptoms;
            assignedTiers = tiers;
            diagnosedCount = 0;
            isInSurgery = false;

            // Sort symptoms mechanically so lower tiers must be diagnosed first.
            // Simple bubble sort is fine for 1-4 items.
            for (int i = 0; i < assignedSymptoms.Count - 1; i++)
            {
                for (int j = 0; j < assignedSymptoms.Count - i - 1; j++)
                {
                    if (assignedTiers[j] > assignedTiers[j + 1])
                    {
                        // Swap tiers
                        var tempTier = assignedTiers[j];
                        assignedTiers[j] = assignedTiers[j + 1];
                        assignedTiers[j + 1] = tempTier;

                        // Swap symptoms
                        var tempSym = assignedSymptoms[j];
                        assignedSymptoms[j] = assignedSymptoms[j + 1];
                        assignedSymptoms[j + 1] = tempSym;
                    }
                }
            }

            // Paramedic handover: the primary (highest-tier) symptom arrives already diagnosed.
            // Moving it to the front keeps the remaining hidden symptoms sorted by tier.
            if (patientData.arrivesWithHandover && assignedSymptoms.Count > 0)
            {
                int last = assignedSymptoms.Count - 1;
                var primarySym = assignedSymptoms[last];
                var primaryTier = assignedTiers[last];
                assignedSymptoms.RemoveAt(last);
                assignedTiers.RemoveAt(last);
                assignedSymptoms.Insert(0, primarySym);
                assignedTiers.Insert(0, primaryTier);
                diagnosedCount = 1;
            }

            maxStability = patientData.startingStability;
            decayRate = patientData.stabilityDecayPerTurn;
            currentStability = maxStability;
            isRemoved = false; // Reset for pooling
            isCritical = false;
            // A pooled card may still carry an unused pause from its previous patient.
            stabilityClockPaused = false;
            assignedWardSlot = null;

            SpawnSubCards(subCardPrefab);
            UpdateVisuals();

            // Triggers run last so effects such as ChangeStability act on fully initialised state.
            ExecuteTriggers(PatientTriggerCondition.OnSpawn);
        }

        /// <summary>
        /// Horizontal slot (in sub-card widths) for each symptom: the primary (highest-tier) symptom always
        /// sits in the centre slot (0), and the others fill the left (-1) then right (+1) slots in list order.
        /// Placement follows tier rather than list position, so a paramedic handover (which moves the
        /// primary to the front of the list) or a 2-symptom patient still shows the primary in the middle.
        /// </summary>
        public static int[] SubCardSlots(IReadOnlyList<SymptomTier> tiers)
        {
            int count = tiers.Count;
            var slots = new int[count];
            if (count == 0) return slots;

            // Last occurrence of the highest tier is the primary (the sort keeps it last unless a handover moved it).
            int primary = 0;
            for (int i = 1; i < count; i++)
                if (tiers[i] >= tiers[primary]) primary = i;

            int side = -1;
            for (int i = 0; i < count; i++)
            {
                if (i == primary) continue;
                slots[i] = side;
                // Left, right, then further out if a patient ever has more than 3 symptoms.
                side = side < 0 ? -side : -side - 1;
            }
            return slots;
        }

        /// <summary>
        /// Instantiates one <see cref="SymptomSubCardView"/> per assigned symptom in a row beneath the
        /// patient card, with the primary (highest-tier) symptom in the centre (see <see cref="SubCardSlots"/>).
        /// </summary>
        /// <param name="prefab">The symptom sub-card prefab to instantiate.</param>
        private void SpawnSubCards(GameObject prefab)
        {
            if (subCardSpawnParent == null) return;

            float spacing = 0.9f;
            int count = assignedSymptoms.Count;
            int[] slots = SubCardSlots(assignedTiers);
            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPos = subCardSpawnParent.position + new Vector3(slots[i] * spacing, 0f, -0.05f);

                SymptomSubCardView subView = null;
                if (BoardView.Instance != null)
                {
                    subView = BoardView.Instance.GetSymptomSubCard(subCardSpawnParent);
                    subView.transform.position = spawnPos;
                    subView.transform.rotation = Quaternion.identity;
                }
                else if (prefab != null)
                {
                    GameObject subCardObj = Instantiate(prefab, spawnPos, Quaternion.identity, subCardSpawnParent);
                    subView = subCardObj.GetComponent<SymptomSubCardView>();
                }

                if (subView != null)
                {
                    subView.parentPatient = this;
                    subView.SetupSubCard(assignedSymptoms[i], assignedTiers[i], false);
                    activeSubCards.Add(subView.gameObject);
                }
            }
        }

        public void ReleaseAllSubCards()
        {
            foreach (var subCard in activeSubCards)
            {
                if (subCard != null)
                {
                    SymptomSubCardView subView = subCard.GetComponent<SymptomSubCardView>();
                    if (subView != null && BoardView.Instance != null)
                    {
                        BoardView.Instance.ReleaseSymptomSubCard(subView);
                    }
                    else
                    {
                        Destroy(subCard);
                    }
                }
            }
            activeSubCards.Clear();
        }

        // -------------------------------------------------------------------------
        // Diagnosis
        // -------------------------------------------------------------------------

        /// <summary>
        /// Attempts to reveal the next hidden symptom. The patient must be in a ward slot.
        /// </summary>
        /// <param name="maxDiagnosticTier">The maximum tier the played card can reveal.</param>
        /// <param name="rngFailed">If true, validates the play but skips the effect, returning true to consume the card.</param>
        /// <returns><c>true</c> if a symptom was successfully diagnosed (or consumed by RNG); <c>false</c> otherwise.</returns>
        public bool TryDiagnoseNext(SymptomTier maxDiagnosticTier, bool rngFailed = false)
        {
            if (isFullyDiagnosed)
            {
                GameFeedback.WarnAt("Patient is already fully diagnosed!", transform.position);
                return false;
            }

            if (transform.parent == null || !transform.parent.CompareTag("WardSlot"))
            {
                GameFeedback.WarnAt("Move to the Ward first!", transform.position);
                return false;
            }

            if (assignedTiers[diagnosedCount] > maxDiagnosticTier)
            {
                GameLog.Info($"Diagnosis Failed: This symptom is Tier {assignedTiers[diagnosedCount]} and requires a more advanced diagnostic card.");
                GameFeedback.WarnAt("Needs a stronger diagnostic card!", transform.position);
                return false;
            }

            if (rngFailed)
            {
                GameLog.Info("Diagnosis failed due to card RNG!");
                GameFeedback.WarnAt("Lab Error!", transform.position);
                return true;
            }

            RevealNextSymptom();
            return true;
        }

        /// <summary>Reveals the symptom at <see cref="diagnosedCount"/> and fires the diagnosis feedback and triggers.</summary>
        private void RevealNextSymptom()
        {
            diagnosedCount++;
            int revealed = diagnosedCount - 1;
            GameFeedback.ShowAt($"{SymptomName(revealed)} diagnosed", transform.position, GameFeedback.CareColour);
            ExecuteTriggers(PatientTriggerCondition.OnDiagnosed);
            UpdateVisuals();
        }

        /// <summary>
        /// <c>true</c> if a Team Assessment of up to <paramref name="maxTier"/> can diagnose this patient: they are
        /// in a Ward bed and their lowest hidden symptom is within the tier.
        /// </summary>
        public bool CanTeamAssess(SymptomTier maxTier) =>
            !isRemoved && !isFullyDiagnosed &&
            transform.parent != null && transform.parent.CompareTag("WardSlot") &&
            assignedTiers[diagnosedCount] <= maxTier;

        /// <summary>
        /// Team Assessment: diagnoses one hidden symptom picked at random among the lowest-tier hidden ones,
        /// so Tier 1 symptoms are still found before the main (higher-tier) symptom.
        /// </summary>
        public bool TeamAssess(SymptomTier maxTier, System.Random rng = null)
        {
            if (!CanTeamAssess(maxTier)) return false;

            // Symptoms are kept sorted by tier, so the lowest hidden tier is at diagnosedCount.
            SymptomTier lowest = assignedTiers[diagnosedCount];
            var candidates = new List<int>();
            for (int i = diagnosedCount; i < assignedSymptoms.Count; i++)
                if (assignedTiers[i] == lowest) candidates.Add(i);

            int pick = candidates[rng != null ? rng.Next(candidates.Count) : Random.Range(0, candidates.Count)];
            if (pick != diagnosedCount)
            {
                (assignedSymptoms[pick], assignedSymptoms[diagnosedCount]) = (assignedSymptoms[diagnosedCount], assignedSymptoms[pick]);
                (assignedTiers[pick], assignedTiers[diagnosedCount]) = (assignedTiers[diagnosedCount], assignedTiers[pick]);
            }

            RevealNextSymptom();
            return true;
        }

        // -------------------------------------------------------------------------
        // Stability
        // -------------------------------------------------------------------------

        /// <summary>
        /// Applies one turn of stability decay. If the stability clock was paused by a card
        /// effect, the pause is consumed instead of applying decay. Ward patients receive
        /// halved decay (minimum 1). Death triggers a Reputation penalty and destroys the card.
        /// </summary>
        public void AdvanceShiftDecay()
        {
            // A patient that has already died or been discharged this frame must not decay again.
            if (isRemoved) return;

            if (stabilityClockPaused)
            {
                stabilityClockPaused = false;
                GameLog.Info($"{patientData.patientName}'s stability clock was paused! No decay this shift.");
                // A pause stops decay, not the patient's own end-of-shift effects.
                ExecuteTriggers(PatientTriggerCondition.OnTurnEnd);
                UpdateVisuals();
                return;
            }

            int appliedDecay = decayRate;
            if (assignedWardSlot != null)
            {
                // Ward patients decay at half rate (rounded up), minimum 1.
                appliedDecay = Mathf.Max(1, Mathf.CeilToInt(decayRate / 2f));
                GameLog.Info($"{patientData.patientName} is stabilised in a Ward. Decay slowed to {appliedDecay}.");
            }

            currentStability -= appliedDecay;
            if (currentStability <= 0)
            {
                currentStability = 0;
                isCritical = true;
                isRemoved = true;

                // A patient with its own OnDeath Reputation trigger pays that instead of the flat penalty.
                int penalty = GameManager.Instance != null && !patientData.HasOwnDeathPenalty ? GameManager.Instance.deathReputationPenalty : 0;
                GameLog.Warn($"PATIENT LOST: {patientData.patientName} has died!" + (penalty > 0 ? $" You lost {penalty} Reputation!" : ""));

                // Raised before the penalty so listeners (e.g. BoardManager's Code Blue counter) are up to date
                // even if the penalty ends the run.
                OnDied?.Invoke(this);

                if (GameManager.Instance != null && penalty > 0)
                {
                    // SpendReputation automatically triggers TriggerGameOver if reputation hits zero.
                    GameManager.Instance.SpendReputation(penalty);
                }

                ExecuteTriggers(PatientTriggerCondition.OnDeath);

                // A death generates incident paperwork: a random Red Tape card joins the discard pile.
                if (GameManager.Instance != null && GameManager.Instance.deathAddsRedTape && DeckManager.Instance != null)
                {
                    DeckManager.Instance.SpawnRandomRedTape();
                }

                OnRemoved?.Invoke(this);
                if (BoardView.Instance != null) BoardView.Instance.ReleasePatientCard(this);
                else DestroyCard();
                return;
            }

            ExecuteTriggers(PatientTriggerCondition.OnTurnEnd);
            UpdateVisuals();
        }

        /// <summary>
        /// Restores <paramref name="amount"/> stability, clamped to <see cref="maxStability"/>.
        /// </summary>
        /// <param name="amount">Stability points to restore.</param>
        public void ApplyStabilisation(int amount)
        {
            int before = currentStability;
            currentStability = Mathf.Min(currentStability + amount, maxStability);
            UpdateVisuals();

            int gained = currentStability - before;
            if (gained > 0) GameFeedback.ShowAt($"+{gained} Stability", transform.position, GameFeedback.CareColour);

            // BoardManager listens to this so the HUD updates immediately if they are healed out of the red.
            OnStabilityChanged?.Invoke(this);
        }

        /// <summary>
        /// Prevents the next turn's stability decay from being applied.
        /// The pause is automatically consumed at the start of the following turn.
        /// </summary>
        public void PauseStabilityClock()
        {
            stabilityClockPaused = true;
            UpdateVisuals();
            GameFeedback.ShowAt("Decay paused", transform.position, GameFeedback.CareColour);
        }

        // -------------------------------------------------------------------------
        // Treatment
        // -------------------------------------------------------------------------

        /// <summary>
        /// Attempts to cure the first symptom that matches the patient's current department
        /// and is within the treatment card's <paramref name="maxTier"/>.
        /// On success, removes the symptom, marks the department as used, and triggers
        /// discharge if no symptoms remain.
        /// </summary>
        /// <param name="maxTier">The highest symptom tier the treatment card can cure.</param>
        /// <param name="rngFailed">If true, validates the play but skips the effect, returning true to consume the card.</param>
        /// <returns><c>true</c> if a symptom was cured (or consumed by RNG); <c>false</c> otherwise.</returns>
        public bool TryCureSymptom(SymptomTier maxTier, bool rngFailed = false)
        {
            if (transform.parent == null) return false;

            // 0. Already treated this shift: stays in surgery until the shift ends.
            if (isInSurgery)
            {
                GameFeedback.WarnAt("Patient is in surgery until the shift ends!", transform.position);
                return false;
            }

            // 1. Must be fully diagnosed before ANY treatment can happen
            if (!isFullyDiagnosed)
            {
                GameFeedback.WarnAt("Patient needs full diagnosis!", transform.position);
                return false;
            }

            // 2. Must be in a Specialty Ward to receive treatment
            DepartmentSlot deptZone = transform.parent.GetComponent<DepartmentSlot>();
            SymptomCategory? currentDepartment = deptZone != null ? deptZone.category : (SymptomCategory?)null;

            if (currentDepartment == null)
            {
                GameFeedback.WarnAt("Move to a department first!", transform.position);
                return false;
            }

            // 3. Department must still be available this shift
            // deptZone was resolved above.
            if (deptZone != null && !deptZone.isAvailable)
            {
                GameFeedback.WarnAt("Department used this shift!", transform.position);
                return false;
            }

            int targetIndex = -1;
            for (int i = 0; i < assignedSymptoms.Count; i++)
            {
                if (assignedSymptoms[i].specialtyCategory == currentDepartment && assignedTiers[i] <= maxTier)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex != -1)
            {
                if (rngFailed)
                {
                    GameLog.Info("Treatment failed due to card RNG!");
                    GameFeedback.WarnAt("Treatment Failed!", transform.position);
                    ReturnToWard();
                    return true;
                }

                string treatedName = SymptomName(targetIndex);
                assignedSymptoms.RemoveAt(targetIndex);
                assignedTiers.RemoveAt(targetIndex);

                // Reduce diagnosedCount so remaining hidden symptoms don't auto-reveal.
                if (diagnosedCount > 0)
                {
                    diagnosedCount--;
                }

                UpdateVisuals();

                if (deptZone != null)
                {
                    deptZone.MarkAsUsed();
                }

                // Treating a symptom earns an upgrade token in that department's colour.
                if (GameManager.Instance != null && currentDepartment.HasValue)
                    GameManager.Instance.AddUpgradeTokens(currentDepartment.Value);

                GameFeedback.ShowAt(assignedSymptoms.Count == 0 ? $"{treatedName} treated - ready for discharge" : $"{treatedName} treated",
                    transform.position, GameFeedback.CareColour);

                // Stay in the department for the rest of the shift. When the shift ends the patient
                // is discharged (all symptoms treated) or sent back to their bed.
                isInSurgery = true;

                return true;
            }

            GameLog.Info("Treatment Failed: No matching symptom found for this tier and department.");
            GameFeedback.WarnAt("Symptom tier too high or wrong department!", transform.position);
            return false;
        }

        /// <summary>
        /// Ends a surgery stay when the shift ends: discharges the patient and pays their rewards if
        /// every symptom has been treated, otherwise sends them back to their reserved ward bed.
        /// </summary>
        public void ResolveSurgery()
        {
            if (!isInSurgery || isRemoved) return;
            isInSurgery = false;

            if (assignedSymptoms.Count == 0) DischargePatient();
            else ReturnToWard();
        }

        /// <summary>
        /// Instantly returns this patient from a specialty department back to their reserved ward bed.
        /// </summary>
        public void ReturnToWard()
        {
            if (assignedWardSlot != null && transform.parent != assignedWardSlot)
            {
                transform.position = new Vector3(assignedWardSlot.position.x, assignedWardSlot.position.y, -1f);
                transform.SetParent(assignedWardSlot);
                ClearGhost();
            }
        }

        /// <summary>
        /// Finds the lowest tier symptom on this patient that matches the given department.
        /// Used by the Department fallback button to determine AP costs.
        /// </summary>
        public SymptomTier? GetLowestMatchingSymptomTier(SymptomCategory department)
        {
            if (!isFullyDiagnosed) return null;

            SymptomTier? lowest = null;
            for (int i = 0; i < assignedSymptoms.Count; i++)
            {
                if (assignedSymptoms[i].specialtyCategory == department)
                {
                    if (lowest == null || assignedTiers[i] < lowest)
                    {
                        lowest = assignedTiers[i];
                    }
                }
            }
            return lowest;
        }

        /// <summary>
        /// Executes all triggers on this patient matching the specified condition.
        /// </summary>
        /// <param name="condition">The condition event (e.g. OnSpawn, OnDeath).</param>
        private void ExecuteTriggers(PatientTriggerCondition condition)
        {
            if (patientData == null || patientData.triggers == null) return;

            foreach (var trigger in patientData.triggers)
            {
                if (trigger.condition == condition)
                {
                    GameLog.Info($"Trigger Executed: {patientData.patientName} [{condition}] -> {trigger.effect}");
                    // Gain/Lose triggers only move in their named direction: a negative value does nothing.
                    int amount = Mathf.Max(0, trigger.effectValue);
                    switch (trigger.effect)
                    {
                        case PatientTriggerEffect.GainAP:
                            if (GameManager.Instance != null) GameManager.Instance.GainAP(amount);
                            break;
                        case PatientTriggerEffect.LoseAP:
                            if (GameManager.Instance != null) GameManager.Instance.SpendAP(amount);
                            break;
                        case PatientTriggerEffect.GainReputation:
                            if (GameManager.Instance != null) GameManager.Instance.AddReputation(amount);
                            break;
                        case PatientTriggerEffect.LoseReputation:
                            if (GameManager.Instance != null) GameManager.Instance.SpendReputation(amount);
                            break;
                        case PatientTriggerEffect.DrawCard:
                            if (DeckManager.Instance != null) DeckManager.Instance.DrawCards(amount, reshuffleDiscard: false);
                            break;
                        case PatientTriggerEffect.ChangeStability:
                            ApplyStabilisation(trigger.effectValue);
                            break;
                        case PatientTriggerEffect.AddCardToDiscard:
                            if (DeckManager.Instance != null)
                            {
                                if (trigger.cardReference != null) DeckManager.Instance.AddCardToDiscardPile(trigger.cardReference);
                                else DeckManager.Instance.SpawnRandomRedTape(false);
                            }
                            break;
                        case PatientTriggerEffect.DiscardCard:
                            DiscardRandomCards(trigger.effectValue);
                            break;
                        case PatientTriggerEffect.AddCardToHand:
                            if (DeckManager.Instance != null)
                            {
                                if (trigger.cardReference != null) DeckManager.Instance.AddCardToHand(trigger.cardReference);
                                else DeckManager.Instance.SpawnRandomRedTape(true);
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the final Reputation reward, grants it to the player, notifies
        /// <see cref="PatientDeckManager"/> of the cure, and destroys this card.
        /// <para>Awards Activity Funding by escalation type and a small Reputation reward (capped by
        /// <see cref="GameManager.cureReputationCap"/>). Bonus +1 if the patient retained ≥50% stability;
        /// −1 penalty at ≤2 stability. The modifier applies to both rewards.</para>
        /// </summary>
        private void DischargePatient()
        {
            isRemoved = true;
            ExecuteTriggers(PatientTriggerCondition.OnCured);
            GameLog.Info($"{patientData.patientName} has been fully cured and discharged!");

            if (GameManager.Instance != null)
            {
                int modifier = 0;

                if (currentStability >= (maxStability / 2f))
                {
                    modifier = 1;
                    GameLog.Info("Bonus awarded for fast stabilisation!");
                }
                else if (currentStability <= BarelyStableThreshold)
                {
                    modifier = -1;
                    GameLog.Warn("Reward reduced for discharging a barely-stable patient.");
                }

                int reputationReward = Mathf.Min(patientData.reputationReward, GameManager.Instance.cureReputationCap);
                int repGained = Mathf.Max(0, reputationReward + modifier);
                GameManager.Instance.AddReputation(repGained);

                int fundingReward = GameManager.Instance.GetFundingReward(patientData.escalationPhase);
                int fundingGained = Mathf.Max(0, fundingReward + modifier);
                GameManager.Instance.AddFunding(fundingGained);
                GameFeedback.ShowAt($"{patientData.patientName} cured!", transform.position, GameFeedback.RewardColour);
                GameFeedback.ShowAt($"+{fundingGained} Funding  +{repGained} Rep", transform.position, GameFeedback.RewardColour);
            }

            if (PatientDeckManager.Instance != null)
            {
                PatientDeckManager.Instance.PatientSuccessfullyCured(patientData);
            }

            OnRemoved?.Invoke(this);
            if (BoardView.Instance != null) BoardView.Instance.ReleasePatientCard(this);
            else DestroyCard();
        }

        /// <summary>Destroys this card when there is no pool to return it to (Destroy in play, DestroyImmediate in the Editor).</summary>
        private void DestroyCard()
        {
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        /// <summary>Display name of the symptom at <paramref name="index"/> at its assigned tier.</summary>
        private string SymptomName(int index)
        {
            if (index < 0 || index >= assignedSymptoms.Count || assignedSymptoms[index] == null) return "Symptom";
            return assignedSymptoms[index].GetNameByTier(assignedTiers[index]);
        }

        /// <summary>Moves <paramref name="count"/> random cards from the hand to the discard pile.</summary>
        private static void DiscardRandomCards(int count)
        {
            var deck = DeckManager.Instance;
            if (deck == null) return;
            for (int i = 0; i < count && deck.handData.Count > 0; i++)
            {
                ActionCardSO card = deck.handData[Random.Range(0, deck.handData.Count)];
                deck.RemoveCardFromHand(card);
                if (HandView.Instance != null) HandView.Instance.RemoveCardVisualFor(card);
            }
        }

        // -------------------------------------------------------------------------
        // Visuals
        // -------------------------------------------------------------------------

        /// <summary>
        /// Refreshes all text labels, the triage icon colour, and the symptom sub-card
        /// visibility/reveal state based on current runtime values.
        /// Sub-card slots beyond the remaining symptom count are hidden.
        /// </summary>
        public void UpdateVisuals()
        {
            if (nameText != null && patientData != null)
                nameText.text = $"{patientData.patientName.ToUpper()}\n<size=75%>{patientData.age} Y/O</size>";

            if (complaintText != null && patientData != null)
                complaintText.text = patientData.chiefComplaint;

            if (triageNumberText != null && patientData != null)
                triageNumberText.text = patientData.triageLevel.ToString();

            if (stabilityText != null)
            {
                stabilityText.text = $"{currentStability}/{maxStability}";
                stabilityText.color = currentStability <= LowStabilityThreshold ? Color.red : Color.white;
            }

            if (decayText != null)
            {
                decayText.text = $"-{decayRate} / shift";
            }

            if (rewardText != null && patientData != null)
            {
                var gm = GameManager.Instance;
                int funding = gm != null ? gm.GetFundingReward(patientData.escalationPhase) : 0;
                int rep = gm != null ? Mathf.Min(patientData.reputationReward, gm.cureReputationCap) : patientData.reputationReward;
                rewardText.text = $"+{funding} Funding\n+{rep} Rep";
            }

            if (tagsText != null && patientData != null)
            {
                List<string> tagStrings = new List<string>();
                foreach (var t in patientData.clinicalTags) tagStrings.Add($"[{t}]");
                tagsText.text = string.Join(" ", tagStrings);
            }

            if (triageIcon != null && patientData != null)
            {
                triageIcon.color = GetTriageColor(patientData.triageLevel);
            }

            for (int i = 0; i < activeSubCards.Count; i++)
            {
                if (i < assignedSymptoms.Count)
                {
                    activeSubCards[i].SetActive(true);

                    bool isRevealed = i < diagnosedCount;
                    SymptomSubCardView subView = activeSubCards[i].GetComponent<SymptomSubCardView>();

                    if (subView != null)
                    {
                        subView.SetupSubCard(assignedSymptoms[i], assignedTiers[i], isRevealed);
                    }
                }
                else
                {
                    // Symptom list is shorter than the physical UI slots — hide the empty slot.
                    activeSubCards[i].SetActive(false);
                }
            }

            // If this patient has a ghost in the ward, re-clone it so visual changes (like removed symptoms) sync instantly.
            if (ghostInstance != null)
            {
                Transform currentWard = assignedWardSlot;
                ClearGhost();
                assignedWardSlot = currentWard;
                SpawnGhostInWard();
            }
        }

        /// <summary>
        /// Maps a triage level (1–5) to a colour for the triage icon.
        /// 1 = Red (Resuscitation), 2 = Orange (Emergent), 3 = Yellow (Urgent), 4 = Green (Less-urgent), 5 = White.
        /// </summary>
        /// <param name="tier">ESI triage level (1–5).</param>
        /// <returns>The corresponding <see cref="Color"/>.</returns>
        private Color GetTriageColor(int tier)
        {
            return tier switch
            {
                1 => new Color(0.85f, 0.15f, 0.15f), // Red
                2 => new Color(1f,    0.5f,  0f),     // Orange
                3 => new Color(0.95f, 0.85f, 0.1f),  // Yellow
                4 => new Color(0.2f,  0.75f, 0.2f),  // Green
                _ => Color.white
            };
        }

        /// <summary>
        /// Activates or deactivates the highlight outline to indicate targeting status.
        /// </summary>
        /// <param name="isHighlighted"><c>true</c> to show the outline; <c>false</c> to hide it.</param>
        public void SetHighlight(bool isHighlighted)
        {
            hoverHighlighted = isHighlighted;
            RefreshOutline();
        }

        /// <summary>
        /// Soft glow marking this patient as a valid target for the card being dragged. Separate from
        /// <see cref="SetHighlight"/> (hover / picking), which shows the full outline on top.
        /// </summary>
        public void SetTargetable(bool isTargetable)
        {
            targetable = isTargetable;
            RefreshOutline();
        }

        private void RefreshOutline()
        {
            if (highlightOutline == null) return;
            if (outlineRenderer == null)
            {
                outlineRenderer = highlightOutline.GetComponent<SpriteRenderer>();
                if (outlineRenderer != null) outlineBaseColour = outlineRenderer.color;
            }

            highlightOutline.SetActive(hoverHighlighted || targetable);
            if (outlineRenderer != null) outlineRenderer.color = hoverHighlighted ? outlineBaseColour.Value : targetableOutlineColour;
        }

        /// <summary><c>true</c> if a diagnosis of up to <paramref name="maxTier"/> would reveal a symptom now (mirrors <see cref="TryDiagnoseNext"/>).</summary>
        public bool CanDiagnoseWith(SymptomTier maxTier) =>
            !isRemoved && !isFullyDiagnosed &&
            transform.parent != null && transform.parent.CompareTag("WardSlot") &&
            assignedTiers[diagnosedCount] <= maxTier;

        /// <summary><c>true</c> if a treatment of up to <paramref name="maxTier"/> would cure a symptom now (mirrors <see cref="TryCureSymptom"/>).</summary>
        public bool CanCureWith(SymptomTier maxTier)
        {
            if (isRemoved || isInSurgery || !isFullyDiagnosed || transform.parent == null) return false;
            DepartmentSlot dept = transform.parent.GetComponent<DepartmentSlot>();
            if (dept == null || !dept.isAvailable) return false;
            for (int i = 0; i < assignedSymptoms.Count; i++)
                if (assignedSymptoms[i].specialtyCategory == dept.category && assignedTiers[i] <= maxTier) return true;
            return false;
        }

        // -------------------------------------------------------------------------
        // Ghost-in-Ward Mechanic
        // -------------------------------------------------------------------------

        /// <summary>
        /// Instantiates a semi-transparent clone of this patient card into the assigned
        /// ward slot so the bed appears occupied while the patient is in a department.
        /// The ghost has its drag script destroyed and its view script disabled to freeze it.
        /// </summary>
        public void SpawnGhostInWard()
        {
            if (assignedWardSlot == null || ghostInstance != null) return;

            ghostInstance = Instantiate(gameObject, assignedWardSlot.position, Quaternion.identity, assignedWardSlot);
            ghostInstance.transform.localPosition = Vector3.zero;

            // Freeze the ghost by removing interactive scripts.
            DraggablePatient dragScript = ghostInstance.GetComponent<DraggablePatient>();
            if (dragScript != null) Destroy(dragScript);

            PatientCardView viewScript = ghostInstance.GetComponent<PatientCardView>();
            if (viewScript != null) viewScript.enabled = false;

            // Disable colliders so cards and drags can never target the clone instead of the real patient.
            foreach (Collider2D col in ghostInstance.GetComponentsInChildren<Collider2D>())
            {
                col.enabled = false;
            }

            // Make the ghost translucent (alpha 40%).
            SpriteRenderer[] sprites = ghostInstance.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in sprites)
            {
                Color c = sr.color;
                c.a = 0.4f;
                sr.color = c;
            }

            TextMeshPro[] texts = ghostInstance.GetComponentsInChildren<TextMeshPro>();
            foreach (var tm in texts)
            {
                Color c = tm.color;
                c.a = 0.4f;
                tm.color = c;
            }
        }

        /// <summary>Destroys the ghost instance if one exists and clears the reference.</summary>
        public void ClearGhost()
        {
            if (ghostInstance != null)
            {
                Destroy(ghostInstance);
                ghostInstance = null;
            }
        }

        private void OnDestroy()
        {
            isRemoved = true;
            OnRemoved?.Invoke(this);
            ClearGhost();
        }
    }
}
