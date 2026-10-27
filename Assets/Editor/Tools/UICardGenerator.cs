
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
    #if UNITY_EDITOR
    using UnityEngine;
    using UnityEditor;
    using UnityEngine.UI;
    using TMPro;

    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class UICardGenerator : EditorWindow
    {
        private const float CardW = 300f;
        private const float CardH = 420f;

        [MenuItem("Chief Complaint/Tools/Generate UI Action Card Prefab")]
        public static void GenerateUICard()
        {
            // Try to borrow the original card's background sprite/colors and font
            Sprite bgSprite = null;
            Color bgColor = new Color(0.93f, 0.9f, 0.82f, 1f);
            TMP_FontAsset font = null;

            string[] guids = AssetDatabase.FindAssets("t:Prefab Action Card");
            if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:Prefab ActionCard");
            if (guids.Length > 0)
            {
                var original = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (original != null)
                {
                    SpriteRenderer best = null;
                    foreach (var sr in original.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if (sr.sprite == null) continue;
                        if (best == null || sr.sprite.bounds.size.x * sr.sprite.bounds.size.y > best.sprite.bounds.size.x * best.sprite.bounds.size.y)
                            best = sr;
                    }
                    if (best != null) { bgSprite = best.sprite; bgColor = best.color; }
                    var t = original.GetComponentInChildren<TextMeshPro>(true);
                    if (t != null) font = t.font;
                }
            }

            // Root
            GameObject root = new GameObject("UI_ActionCard", typeof(RectTransform));
            RectTransform rootRT = root.GetComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(CardW, CardH);

            // Outline (drawn first = behind, shown larger as a highlight)
            var outline = MakeImage(root.transform, "HighlightOutline", new Color(1f, 0.8f, 0.2f, 1f), null);
            Stretch(outline.rectTransform, -8f);
            outline.gameObject.SetActive(false);

            // Background
            var bg = MakeImage(root.transform, "Background", bgColor, bgSprite);
            Stretch(bg.rectTransform, 0f);

            // Name bar
            var nameText = MakeText(root.transform, "NameText", font, 30, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
            Place(nameText.rectTransform, 15, -12, CardW - 30, 50);
            nameText.enableAutoSizing = true; nameText.fontSizeMin = 16; nameText.fontSizeMax = 30;

            // Rules text
            var rulesText = MakeText(root.transform, "RulesText", font, 22, FontStyles.Normal, TextAlignmentOptions.TopLeft, Color.black);
            Place(rulesText.rectTransform, 20, -75, CardW - 40, 220);
            rulesText.enableAutoSizing = true; rulesText.fontSizeMin = 12; rulesText.fontSizeMax = 22;

            // Flavor text
            var flavorText = MakeText(root.transform, "FlavorText", font, 16, FontStyles.Italic, TextAlignmentOptions.BottomLeft, Color.black);
            Place(flavorText.rectTransform, 20, -305, CardW - 40, 95);
            flavorText.enableAutoSizing = true; flavorText.fontSizeMin = 10; flavorText.fontSizeMax = 16;

            // Cost badge (top-right corner)
            var badge = MakeImage(root.transform, "CostBadge", new Color(0.12f, 0.3f, 0.8f, 1f), null);
            badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(1, 1);
            badge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            badge.rectTransform.sizeDelta = new Vector2(60, 60);
            badge.rectTransform.anchoredPosition = new Vector2(-10, -10);

            var costText = MakeText(badge.transform, "CostText", font, 36, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            Stretch(costText.rectTransform, 0f);

            // View script
            var view = root.AddComponent<UI_ActionCardView>();
            view.nameText = nameText;
            view.rulesText = rulesText;
            view.flavorText = flavorText;
            view.costText = costText;
            view.highlightOutline = outline.gameObject;

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI")) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            string path = "Assets/Prefabs/UI/UI_ActionCard.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);

            Debug.Log("Successfully generated UI_ActionCard prefab at: " + path);
        }

        private static Image MakeImage(Transform parent, string name, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI MakeText(Transform parent, string name, TMP_FontAsset font, float size, FontStyles style, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = color;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.text = name;
            return t;
        }

        // Stretch to parent with an inset (negative = outset)
        private static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        // Place relative to top-left of the card
        private static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }
    }
    #endif

}
