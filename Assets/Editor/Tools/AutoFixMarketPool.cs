using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.StaffRoom;

namespace ChiefComplaint.Editor.Tools
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class AutoFixMarketPool
    {
        /// <summary>
        /// Adds a supply-room pile for every card in Assets/Data/Actions/Market that doesn't have one yet
        /// (Walk-In stage, 3 stock). Existing piles, with their stages and stock, are left untouched.
        /// </summary>
        [MenuItem("Chief Complaint/Data Tools/Add Missing Supply Piles")]
        public static void FixPool()
        {
            var staffRoomManager = Object.FindAnyObjectByType<StaffRoomManager>();
            if (staffRoomManager == null) return;

            Undo.RecordObject(staffRoomManager, "Add Missing Supply Piles");
            int added = 0;
            string[] guids = AssetDatabase.FindAssets("t:ActionCardSO", new[] { "Assets/Data/Actions/Market" });
            foreach (string guid in guids)
            {
                ActionCardSO card = AssetDatabase.LoadAssetAtPath<ActionCardSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (card == null || staffRoomManager.supplyPiles.Exists(p => p != null && p.card == card)) continue;
                staffRoomManager.supplyPiles.Add(new SupplyPileConfig { card = card });
                added++;
            }
            EditorUtility.SetDirty(staffRoomManager);
            EditorSceneManager.MarkSceneDirty(staffRoomManager.gameObject.scene);
            Debug.Log($"Added {added} supply pile(s). Set each new pile's stage and stock on StaffRoomManager.");
        }
    }
}
