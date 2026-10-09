using UnityEngine;
using UnityEditor;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Editor.Tools
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class GenerateUpgradeCards
    {
        [MenuItem("Chief Complaint/Data Tools/Generate Tier 2 and 3 Cards")]
        public static void Generate()
        {
            // 1. Create IV Saline T3
            var iv3 = ScriptableObject.CreateInstance<ActionCardSO>();
            iv3.cardName = "Broad Spectrum Stabiliser";
            iv3.cardType = ActionCardType.Treatment;
            iv3.apCost = 1;
            iv3.cardsToDraw = 1;
            iv3.powerTier = 3;
            AssetDatabase.CreateAsset(iv3, "Assets/Data/Actions/Upgrade/Card_IVSaline_T3.asset");
            
            var iv3Pause = ScriptableObject.CreateInstance<ChiefComplaint.Cards.Effects.PauseStabilityEffectSO>();
            iv3Pause.name = "PauseStabilityEffect";
            AssetDatabase.AddObjectToAsset(iv3Pause, iv3);
            iv3.effects.Add(iv3Pause);
            
            var iv3Heal = ScriptableObject.CreateInstance<ChiefComplaint.Cards.Effects.StabilisationHealEffectSO>();
            iv3Heal.name = "StabilisationHealEffect";
            iv3Heal.healAmount = 6;
            AssetDatabase.AddObjectToAsset(iv3Heal, iv3);
            iv3.effects.Add(iv3Heal);

            // 2. Create IV Saline T2
            var iv2 = ScriptableObject.CreateInstance<ActionCardSO>();
            iv2.cardName = "Fluid Resuscitation";
            iv2.cardType = ActionCardType.Treatment;
            iv2.apCost = 1;
            iv2.powerTier = 2;
            iv2.directDraftUpgrade = iv3;
            iv2.draftUpgradeCost = 10;
            AssetDatabase.CreateAsset(iv2, "Assets/Data/Actions/Upgrade/Card_IVSaline_T2.asset");

            var iv2Pause = ScriptableObject.CreateInstance<ChiefComplaint.Cards.Effects.PauseStabilityEffectSO>();
            iv2Pause.name = "PauseStabilityEffect";
            AssetDatabase.AddObjectToAsset(iv2Pause, iv2);
            iv2.effects.Add(iv2Pause);
            
            var iv2Heal = ScriptableObject.CreateInstance<ChiefComplaint.Cards.Effects.StabilisationHealEffectSO>();
            iv2Heal.name = "StabilisationHealEffect";
            iv2Heal.healAmount = 3;
            AssetDatabase.AddObjectToAsset(iv2Heal, iv2);
            iv2.effects.Add(iv2Heal);

            // Link IV Saline T1 to T2
            var iv1 = AssetDatabase.LoadAssetAtPath<ActionCardSO>("Assets/Data/Actions/Starting/Card_IVSaline.asset");
            if (iv1 != null)
            {
                iv1.directDraftUpgrade = iv2;
                iv1.draftUpgradeCost = 5;
                EditorUtility.SetDirty(iv1);
            }

            // 3. Create Bedside Manner T3
            var bm3 = ScriptableObject.CreateInstance<ActionCardSO>();
            bm3.cardName = "Miracle Worker";
            bm3.cardType = ActionCardType.Staff;
            bm3.apCost = 0;
            bm3.powerTier = 3;
            AssetDatabase.CreateAsset(bm3, "Assets/Data/Actions/Upgrade/Card_BedsideManner_T3.asset");

            var bm3Rep = ScriptableObject.CreateInstance<ChiefComplaint.Cards.Effects.GainReputationEffectSO>();
            bm3Rep.name = "GainReputationEffect";
            bm3Rep.reputationGenerated = 8;
            AssetDatabase.AddObjectToAsset(bm3Rep, bm3);
            bm3.effects.Add(bm3Rep);

            // 4. Create Bedside Manner T2
            var bm2 = ScriptableObject.CreateInstance<ActionCardSO>();
            bm2.cardName = "Reassuring Presence";
            bm2.cardType = ActionCardType.Staff;
            bm2.apCost = 0;
            bm2.powerTier = 2;
            bm2.directDraftUpgrade = bm3;
            bm2.draftUpgradeCost = 8;
            AssetDatabase.CreateAsset(bm2, "Assets/Data/Actions/Upgrade/Card_BedsideManner_T2.asset");

            var bm2Rep = ScriptableObject.CreateInstance<ChiefComplaint.Cards.Effects.GainReputationEffectSO>();
            bm2Rep.name = "GainReputationEffect";
            bm2Rep.reputationGenerated = 4;
            AssetDatabase.AddObjectToAsset(bm2Rep, bm2);
            bm2.effects.Add(bm2Rep);

            // Link Bedside Manner T1 to T2
            var bm1 = AssetDatabase.LoadAssetAtPath<ActionCardSO>("Assets/Data/Actions/Starting/Card_BedsideManner.asset");
            if (bm1 != null)
            {
                bm1.directDraftUpgrade = bm2;
                bm1.draftUpgradeCost = 4;
                EditorUtility.SetDirty(bm1);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Generated Tier 2 and Tier 3 upgrade cards and linked them to the base cards!");
        }
    }

}
