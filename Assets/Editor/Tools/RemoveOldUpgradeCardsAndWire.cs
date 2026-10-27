using UnityEngine;
using UnityEditor;
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
    public class RemoveOldUpgradeCardsAndWire
    {
        [MenuItem("Chief Complaint/Data Tools/Wire Remaining Upgrades and Cleanup")]
        public static void DoWire()
        {
            // 1. Delete the old "isUpgradeCard" cards from the market pool.
            string[] oldUpgradeCards = new string[]
            {
                "Assets/Data/Actions/Market/Card_PromoteToChief.asset",
                "Assets/Data/Actions/Market/Card_PromoteToAttending.asset",
                "Assets/Data/Actions/Market/Card_ProcureDiagnostics.asset",
                "Assets/Data/Actions/Market/Card_OpenSurgicalWard.asset",
                "Assets/Data/Actions/Market/Card_HireSpecialist.asset",
                "Assets/Data/Actions/Market/Card_ContinuingEducation.asset"
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

            if (resident != null && attending != null) { resident.upgradesTo = attending; resident.upgradeCost = 5; EditorUtility.SetDirty(resident); }
            if (attending != null && chief != null) { attending.upgradesTo = chief; attending.upgradeCost = 10; EditorUtility.SetDirty(attending); }

            // 3. Wire up Standard Care -> Specialised Care -> Surgical Intervention
            var standard = AssetDatabase.LoadAssetAtPath<ActionCardSO>("Assets/Data/Actions/Starting/Card_StandardCare.asset");
            var specialised = AssetDatabase.LoadAssetAtPath<ActionCardSO>("Assets/Data/Actions/Upgrade/Card_SpecialisedCare.asset");
            var surgical = AssetDatabase.LoadAssetAtPath<ActionCardSO>("Assets/Data/Actions/Upgrade/Card_SurgicalIntervention.asset");

            if (standard != null && specialised != null) { standard.upgradesTo = specialised; standard.upgradeCost = 5; EditorUtility.SetDirty(standard); }
            if (specialised != null && surgical != null) { specialised.upgradesTo = surgical; specialised.upgradeCost = 10; EditorUtility.SetDirty(specialised); }

            // 4. Wire up Initial Assessment -> Advanced Triage -> Consulting Specialist
            var initial = AssetDatabase.LoadAssetAtPath<ActionCardSO>("Assets/Data/Actions/Starting/Card_BasicTriage.asset");
            var advanced = AssetDatabase.LoadAssetAtPath<ActionCardSO>("Assets/Data/Actions/Upgrade/Card_AdvancedTriage.asset");
            var consulting = AssetDatabase.LoadAssetAtPath<ActionCardSO>("Assets/Data/Actions/Upgrade/Card_ConsultingSpecialist.asset");

            if (initial != null && advanced != null) { initial.upgradesTo = advanced; initial.upgradeCost = 5; EditorUtility.SetDirty(initial); }
            if (advanced != null && consulting != null) { advanced.upgradesTo = consulting; advanced.upgradeCost = 10; EditorUtility.SetDirty(advanced); }

            AssetDatabase.SaveAssets();
            Debug.Log("Wired all remaining starter cards to their tier 2 and tier 3 forms in the Upgrades system!");
        }
    }

}
