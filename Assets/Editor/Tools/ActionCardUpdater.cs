using UnityEngine;
using UnityEditor;
using System.Text;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.StaffRoom;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Editor.Tools
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class ActionCardUpdater
    {
        [MenuItem("Chief Complaint/Data Tools/Update All Action Cards Rules Text")]
        public static void DoUpdate()
        {
            string[] guids = AssetDatabase.FindAssets("t:ActionCardSO", new[] { "Assets/Data/Actions" });
            int updatedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ActionCardSO card = AssetDatabase.LoadAssetAtPath<ActionCardSO>(path);

                StringBuilder sb = new StringBuilder();

                if (!card.isPlayable)
                {
                    sb.AppendLine("Unplayable.");
                }

                if (card.successChance < 1.0f)
                {
                    sb.Append($"{Mathf.RoundToInt(card.successChance * 100)}% chance to: ");
                }

                foreach (var effect in card.effects)
                {
                    if (effect is ChiefComplaint.Cards.Effects.DiagnoseEffectSO diag)
                    {
                        sb.AppendLine($"Unmask 1 hidden symptom (up to Tier-{(int)diag.diagnosesSymptomTier}).");
                    }
                    else if (effect is ChiefComplaint.Cards.Effects.CureSymptomEffectSO cure)
                    {
                        sb.AppendLine($"Cure 1 symptom (up to Tier-{(int)cure.curesSymptomTier}).");
                    }
                    else if (effect is ChiefComplaint.Cards.Effects.PauseStabilityEffectSO)
                    {
                        sb.AppendLine("Pause patient's stability decay.");
                    }
                    else if (effect is ChiefComplaint.Cards.Effects.StabilisationHealEffectSO heal)
                    {
                        sb.AppendLine($"Restore {heal.healAmount} Stability.");
                    }
                    else if (effect is ChiefComplaint.Cards.Effects.TrashCardEffectSO)
                    {
                        sb.AppendLine("Target a card in your hand to permanently remove it from your deck.");
                    }
                    else if (effect is ChiefComplaint.Cards.Effects.GainAPEffectSO ap)
                    {
                        sb.AppendLine($"Gain {ap.apGenerated} AP.");
                    }
                    else if (effect is ChiefComplaint.Cards.Effects.GainReputationEffectSO rep)
                    {
                        if (rep.reputationGenerated > 0)
                            sb.AppendLine($"Gain {rep.reputationGenerated} Reputation.");
                        else if (rep.reputationGenerated < 0)
                            sb.AppendLine($"Lose {Mathf.Abs(rep.reputationGenerated)} Reputation.");
                    }
                    else if (effect is ChiefComplaint.Cards.Effects.CleanseRedTapeEffectSO tape)
                    {
                        sb.AppendLine($"Exhaust all Red Tape cards in hand/discard (+{tape.repBonusPerRedTapeCleansed} Rep per card).");
                    }
                    else if (effect is ChiefComplaint.Cards.Effects.EitherOrEffectSO either)
                    {
                        sb.AppendLine($"Choose: Gain {either.reputationGenerated} Rep OR Gain {either.altApGenerated} AP.");
                    }
                }
                if (card.cardsToDraw > 0)
                {
                    sb.AppendLine($"Draw {card.cardsToDraw} card(s).");
                }

                if (card.exhaustsOnUse)
                {
                    sb.AppendLine("Card exhausts on use.");
                }
                if (card.exhaustsAtShiftEnd)
                {
                    sb.AppendLine("Card exhausts if in hand at end of shift.");
                }

                // Trim the final newline
                string newRules = sb.ToString().TrimEnd();

                if (string.IsNullOrEmpty(newRules) && card.isPlayable)
                {
                    newRules = "No effect.";
                }

                if (card.rulesText != newRules)
                {
                    card.rulesText = newRules;
                    EditorUtility.SetDirty(card);
                    updatedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Finished updating rules text for {updatedCount} action cards.");
        }
    }

}
