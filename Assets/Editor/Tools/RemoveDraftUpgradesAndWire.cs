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
    public class RemoveDraftUpgradesAndWire
    {
        [MenuItem("Chief Complaint/Data Tools/Wire Remaining Upgrades and Cleanup")]
        public static void DoWire()
        {
            // 1. Delete the old "isUpgradeCard" cards from the Draft pool.
            string[] oldUpgradeCards = new string[]
            {
                "Assets/Data/Actions/Draft/Card_PromoteToChief.asset",
                "Assets/Data/Actions/Draft/Card_PromoteToAttending.asset",
                "Assets/Data/Actions/Draft/Card_ProcureDiagnostics.asset",
                "Assets/Data/Actions/Draft/Card_OpenSurgicalWard.asset",
                "Assets/Data/Actions/Draft/Card_HireSpecialist.asset",
                "Assets/Data/Actions/Draft/Card_ContinuingEducation.asset"
            };

            foreach (string path in oldUpgradeCards)
            {
                if (AssetDatabase.LoadAssetAtPath<ActionCardSO>(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                    Debug.Log("Deleted old upgrade card: " + path);
                }
            }

            // 2. Wire up First-Year Resident -> Attending Physician -> Chief of Medicine
            var resident = AssetDatabase.LoadAssetAtPath<ActionCardSO>("Assets/Data/Actions/Starting/Card_FirstYearResident.asset");
            var attending = AssetDatabase.LoadAssetAtPath<ActionCardSO>("Assets/Data/Actions/Upgrade/Card_AttendingPhysician.asset");
            var chief = AssetDatabase.LoadAssetAtPath<ActionCardSO>("Assets/Data/Actions/Upgrade/Card_ChiefOfMedicine.asset");

            if (resident != null && attending != null) { resident.directDraftUpgrade = attending; resident.draftUpgradeCost = 5; EditorUtility.SetDirty(resident); }
            if (attending != null && chief != null) { attending.directDraftUpgrade = chief; attending.draftUpgradeCost = 10; EditorUtility.SetDirty(attending); }

            // 3. Wire up Standard Care -> Specialised Care -> Surgical Intervention
            var standard = AssetDatabase.LoadAssetAtPath<ActionCardSO>("Assets/Data/Actions/Starting/Card_StandardCare.asset");
            var specialised = AssetDatabase.LoadAssetAtPath<ActionCardSO>("Assets/Data/Actions/Upgrade/Card_SpecialisedCare.asset");
            var surgical = AssetDatabase.LoadAssetAtPath<ActionCardSO>("Assets/Data/Actions/Upgrade/Card_SurgicalIntervention.asset");

            if (standard != null && specialised != null) { standard.directDraftUpgrade = specialised; standard.draftUpgradeCost = 5; EditorUtility.SetDirty(standard); }
            if (specialised != null && surgical != null) { specialised.directDraftUpgrade = surgical; specialised.draftUpgradeCost = 10; EditorUtility.SetDirty(specialised); }

            // 4. Wire up Initial Assessment -> Advanced Triage -> Consulting Specialist
            var initial = AssetDatabase.LoadAssetAtPath<ActionCardSO>("Assets/Data/Actions/Starting/Card_BasicTriage.asset");
            var advanced = AssetDatabase.LoadAssetAtPath<ActionCardSO>("Assets/Data/Actions/Upgrade/Card_AdvancedTriage.asset");
            var consulting = AssetDatabase.LoadAssetAtPath<ActionCardSO>("Assets/Data/Actions/Upgrade/Card_ConsultingSpecialist.asset");

            if (initial != null && advanced != null) { initial.directDraftUpgrade = advanced; initial.draftUpgradeCost = 5; EditorUtility.SetDirty(initial); }
            if (advanced != null && consulting != null) { advanced.directDraftUpgrade = consulting; advanced.draftUpgradeCost = 10; EditorUtility.SetDirty(advanced); }

            AssetDatabase.SaveAssets();
            Debug.Log("Wired all remaining starter cards to their tier 2 and tier 3 forms in the Draft Upgrades system!");
        }
    }

}
