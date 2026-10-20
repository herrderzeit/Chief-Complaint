using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Editor.Inspectors
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class PatientCardViewer : EditorWindow
    {
        private List<PatientCardSO> allPatients = new List<PatientCardSO>();
        private Vector2 scrollPos;
        private GUIStyle headerStyle;

        [MenuItem("Chief Complaint/Windows/Patient Card Viewer")]
        public static void ShowWindow()
        {
            GetWindow<PatientCardViewer>("Patient Card Viewer");
        }

        private void OnEnable()
        {
            LoadPatients();
        }

        private void LoadPatients()
        {
            allPatients.Clear();
            string[] guids = AssetDatabase.FindAssets("t:PatientCardSO");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PatientCardSO patient = AssetDatabase.LoadAssetAtPath<PatientCardSO>(path);
                if (patient != null) allPatients.Add(patient);
            }

            // Sort by escalation, then triage level, then by name
            allPatients = allPatients
                .OrderBy(p => p.escalationPhase)
                .ThenBy(p => p.triageLevel)
                .ThenBy(p => p.patientName)
                .ToList();
        }

        private void OnGUI()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel);
                headerStyle.alignment = TextAnchor.MiddleLeft;
            }

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh Data", EditorStyles.toolbarButton)) LoadPatients();
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{allPatients.Count} Patient Cards Found", EditorStyles.label);
            GUILayout.EndHorizontal();

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            DrawHeader();

            foreach (var patient in allPatients)
            {
                DrawPatientRow(patient);
            }

            GUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("Patient & Complaint", headerStyle, GUILayout.Width(160));
            GUILayout.Label("Escalation", headerStyle, GUILayout.Width(90));
            GUILayout.Label("Triage", headerStyle, GUILayout.Width(50));
            GUILayout.Label("Stab/Decay", headerStyle, GUILayout.Width(80));
            GUILayout.Label("Rep", headerStyle, GUILayout.Width(40));
            GUILayout.Label("Categories", headerStyle, GUILayout.Width(120));
            GUILayout.Label("Clinical Tags", headerStyle, GUILayout.Width(120));
            GUILayout.Label("Triggers", headerStyle, GUILayout.Width(250));
            GUILayout.EndHorizontal();
        }

        private void DrawPatientRow(PatientCardSO patient)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            // Make the patient name clickable so it highlights in the project window
            GUIStyle linkStyle = new GUIStyle(EditorStyles.label);
            linkStyle.normal.textColor = new Color(0.3f, 0.6f, 1f); // light blue

            if (GUILayout.Button($"{patient.patientName}\n({patient.chiefComplaint})", linkStyle, GUILayout.Width(160), GUILayout.Height(35)))
            {
                Selection.activeObject = patient;
                EditorGUIUtility.PingObject(patient);
            }

            // Escalation Phase
            GUILayout.Label(patient.escalationPhase.ToString(), GUILayout.Width(90));

            // Triage Level coloring
            GUIStyle triageStyle = new GUIStyle(EditorStyles.label);
            if (patient.triageLevel <= 2) triageStyle.normal.textColor = new Color(1f, 0.4f, 0.4f); // Red/Critical
            else if (patient.triageLevel == 3) triageStyle.normal.textColor = new Color(1f, 0.8f, 0.4f); // Yellow/Urgent
            else triageStyle.normal.textColor = Color.green; // Green/Less Urgent
            GUILayout.Label($"Level {patient.triageLevel}", triageStyle, GUILayout.Width(50));

            // Stability & Decay
            GUILayout.Label($"{patient.startingStability} (-{patient.stabilityDecayPerTurn}/t)", GUILayout.Width(80));

            // Reputation Reward
            GUILayout.Label($"+{patient.reputationReward}", GUILayout.Width(40));

            // Categories
            string categoriesStr = string.Join(", ", patient.incomingCategories);
            if (string.IsNullOrEmpty(categoriesStr)) categoriesStr = "-";
            GUILayout.Label(categoriesStr, GUILayout.Width(120));

            // Tags
            string tagsStr = patient.clinicalTags != null && patient.clinicalTags.Count > 0 
                ? string.Join(", ", patient.clinicalTags) 
                : "-";
            GUILayout.Label(tagsStr, GUILayout.Width(120));

            // Triggers formatting
            string triggersStr = "-";
            if (patient.triggers != null && patient.triggers.Count > 0)
            {
                List<string> triggerList = new List<string>();
                foreach (var t in patient.triggers)
                {
                    if (t.condition == PatientTriggerCondition.None || t.effect == PatientTriggerEffect.None) continue;

                    string cardRef = t.cardReference != null ? $" ({t.cardReference.cardName})" : "";
                    triggerList.Add($"[{t.condition}] {t.effect} {t.effectValue}{cardRef}");
                }
                if (triggerList.Count > 0)
                {
                    triggersStr = string.Join("\n", triggerList);
                }
            }

            GUIStyle wrapStyle = new GUIStyle(EditorStyles.label);
            wrapStyle.wordWrap = true;
            GUILayout.Label(triggersStr, wrapStyle, GUILayout.Width(250));

            GUILayout.EndHorizontal();
        }
    }

}
