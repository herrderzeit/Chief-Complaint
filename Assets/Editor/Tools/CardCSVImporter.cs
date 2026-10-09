using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
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
    public class CardCSVImporter : EditorWindow
    {
        private string csvData = "";
        private int importCount = 0;

        [MenuItem("Chief Complaint/Data Tools/CSV Data Importer")]
        public static void ShowWindow()
        {
            GetWindow<CardCSVImporter>("CSV Data Importer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Automated Data Ingestion (CSV)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Paste your master balancing CSV here. Expected format:\nCardName,APCost,Tier,RulesText", MessageType.Info);

            csvData = EditorGUILayout.TextArea(csvData, GUILayout.Height(300));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Dry Run (Validate)", GUILayout.Height(30)))
            {
                ParseCSV(true);
            }

            if (GUILayout.Button("Import Data", GUILayout.Height(30)))
            {
                ParseCSV(false);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            GUILayout.EndHorizontal();

            if (importCount > 0)
            {
                GUILayout.Label($"Last Action: Processed {importCount} rows.", EditorStyles.boldLabel);
            }
        }

        private void ParseCSV(bool isDryRun)
        {
            if (string.IsNullOrEmpty(csvData)) return;

            string[] rows = csvData.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            importCount = rows.Length;

            // Note: A full implementation would map headers to specific ScriptableObject fields
            // using reflection or switch statements (e.g., matching "Tier" to powerTier).
            // For this architectural foundation, we log the parsed structure.

            foreach (string row in rows)
            {
                string[] columns = row.Split(',');
                if (columns.Length == 0) continue;

                string cardName = columns[0].Trim();
                if (isDryRun)
                {
                    Debug.Log($"[Dry Run] Found Card Entry: {cardName} with {columns.Length} fields.");
                }
                else
                {
                    Debug.Log($"[Import] Processed Data for: {cardName}. (Waiting for schema mapping).");
                }
            }
        }
    }

}
