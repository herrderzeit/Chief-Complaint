using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.StaffRoom;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Editor.Inspectors
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class CardBalanceViewer : EditorWindow
    {
        private List<ActionCardSO> allCards = new List<ActionCardSO>();
        private Vector2 scrollPos;
        private GUIStyle headerStyle;

        [MenuItem("Chief Complaint/Windows/Card Balance Viewer")]
        public static void ShowWindow()
        {
            GetWindow<CardBalanceViewer>("Card Balance Viewer");
        }

        private void OnEnable()
        {
            LoadCards();
        }

        private void LoadCards()
        {
            allCards.Clear();
            string[] guids = AssetDatabase.FindAssets("t:ActionCardSO");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ActionCardSO card = AssetDatabase.LoadAssetAtPath<ActionCardSO>(path);
                if (card != null) allCards.Add(card);
            }

            // Sort by folder location, then card type, then power tier
            allCards = allCards
                .OrderBy(c => GetLocationPriority(GetLocation(c)))
                .ThenBy(c => c.cardType)
                .ThenBy(c => c.powerTier)
                .ToList();
        }

        private string GetLocation(ActionCardSO card)
        {
            string path = AssetDatabase.GetAssetPath(card);
            if (path.Contains("/Starting/")) return "Starting Deck";
            if (path.Contains("/Market/")) return "Market Pool";
            if (path.Contains("/Upgrade/")) return "Upgraded Target";
            if (path.Contains("/RedTape/")) return "Red Tape Pool";
            return "Uncategorised";
        }

        private int GetLocationPriority(string loc)
        {
            return loc switch
            {
                "Starting Deck" => 0,
                "Market Pool" => 1,
                "Upgraded Target" => 2,
                "Red Tape Pool" => 3,
                _ => 4
            };
        }

        private void OnGUI()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel);
                headerStyle.alignment = TextAnchor.MiddleLeft;
            }

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh Data", EditorStyles.toolbarButton)) LoadCards();
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{allCards.Count} Action Cards Found", EditorStyles.label);
            GUILayout.EndHorizontal();

            DrawEconomyAnalysis();

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            DrawHeader();

            string currentLocation = "";

            foreach (var card in allCards)
            {
                string loc = GetLocation(card);
                if (loc != currentLocation)
                {
                    currentLocation = loc;
                    GUILayout.Space(15);

                    // Draw a nice dark header for the category
                    Rect rect = EditorGUILayout.GetControlRect(false, 20f);
                    EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
                    EditorGUI.LabelField(rect, $"   === {currentLocation.ToUpper()} ===", EditorStyles.boldLabel);
                }

                DrawCardRow(card);
            }

            GUILayout.EndScrollView();
        }

        private void DrawEconomyAnalysis()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Economy & Curve Balancing Analysis", EditorStyles.boldLabel);
            GUILayout.BeginVertical(EditorStyles.helpBox);

            var marketCards = allCards.Where(c => GetLocation(c) == "Market Pool").ToList();
            var starting = allCards.Where(c => GetLocation(c) == "Starting Deck").ToList();

            float startingAPCost = starting.Count > 0 ? (float)starting.Average(c => c.apCost) : 0f;
            float marketAPCost = marketCards.Count > 0 ? (float)marketCards.Average(c => c.apCost) : 0f;
            int totalAPGen = starting.Sum(c => c.effects.OfType<ChiefComplaint.Cards.Effects.GainAPEffectSO>().Sum(e => e.apGenerated));

            GUILayout.Label($"Average Starting Deck AP Cost: {startingAPCost:F2} AP");
            GUILayout.Label($"Average Market Pool AP Cost: {marketAPCost:F2} AP");
            GUILayout.Label($"Total Base AP Generation in Starting Deck: {totalAPGen} AP");

            GUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("Card Name", headerStyle, GUILayout.Width(200));
            GUILayout.Label("Type", headerStyle, GUILayout.Width(80));
            GUILayout.Label("AP Cost", headerStyle, GUILayout.Width(60));
            GUILayout.Label("P. Tier", headerStyle, GUILayout.Width(50));
            GUILayout.Label("Med Effect (Diag/Cure)", headerStyle, GUILayout.Width(180));
            GUILayout.Label("Economy (AP / Rep)", headerStyle, GUILayout.Width(130));
            GUILayout.Label("Upgrades To", headerStyle, GUILayout.Width(250));
            GUILayout.EndHorizontal();
        }

        private void DrawCardRow(ActionCardSO card)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            // Make the card name clickable so it highlights in the project window
            GUIStyle linkStyle = new GUIStyle(EditorStyles.label);
            linkStyle.normal.textColor = new Color(0.3f, 0.6f, 1f); // light blue

            if (GUILayout.Button(card.cardName, linkStyle, GUILayout.Width(200)))
            {
                Selection.activeObject = card;
                EditorGUIUtility.PingObject(card);
            }

            GUILayout.Label(card.cardType.ToString(), GUILayout.Width(80));

            // AP Cost coloring
            GUIStyle costStyle = new GUIStyle(EditorStyles.label);
            if (card.apCost == 0) costStyle.normal.textColor = Color.green;
            else if (card.apCost > 1) costStyle.normal.textColor = new Color(1f, 0.5f, 0.3f);
            GUILayout.Label(card.apCost.ToString(), costStyle, GUILayout.Width(60));

            GUILayout.Label(card.powerTier.ToString(), GUILayout.Width(50));

            // Med Effects formatting
            string medicalStr = "-";
            var diag = card.effects.OfType<ChiefComplaint.Cards.Effects.DiagnoseEffectSO>().FirstOrDefault();
            var cure = card.effects.OfType<ChiefComplaint.Cards.Effects.CureSymptomEffectSO>().FirstOrDefault();
            
            if (diag != null) medicalStr = $"Diagnoses up to {diag.diagnosesSymptomTier.ToString().Replace("Tier", "Tier ")}";
            else if (cure != null) medicalStr = $"Cures up to {cure.curesSymptomTier.ToString().Replace("Tier", "Tier ")}";
            GUILayout.Label(medicalStr, GUILayout.Width(180));

            // Economy formatting
            string ecoStr = "-";
            var ap = card.effects.OfType<ChiefComplaint.Cards.Effects.GainAPEffectSO>().FirstOrDefault();
            var rep = card.effects.OfType<ChiefComplaint.Cards.Effects.GainReputationEffectSO>().FirstOrDefault();
            var either = card.effects.OfType<ChiefComplaint.Cards.Effects.EitherOrEffectSO>().FirstOrDefault();

            if (either != null)
            {
                ecoStr = $"+{either.altApGenerated} AP or +{either.reputationGenerated} Rep";
            }
            else if (ap != null || rep != null)
            {
                ecoStr = $"{(ap != null ? $"+{ap.apGenerated} AP " : "")}{(rep != null ? $"+{rep.reputationGenerated} Rep" : "")}";
            }
            GUILayout.Label(ecoStr, GUILayout.Width(130));

            // Upgrades formatting
            string upgradeStr = "-";
            if (card.upgradesTo != null)
            {
                upgradeStr = $"Upgrades to: {card.upgradesTo.cardName} ({card.upgradeCost} Rep)";
            }
            GUILayout.Label(upgradeStr, GUILayout.Width(250));

            GUILayout.EndHorizontal();
        }
    }

}
