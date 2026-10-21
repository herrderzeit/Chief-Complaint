using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using Object = UnityEngine.Object;

namespace ChiefComplaint.Editor.Tools
{
    /// <summary>
    /// Records a captioned, scripted tour of the game to an MP4 (Unity Recorder) for sharing with collaborators,
    /// and saves a still of each step. Menu: Chief Complaint/Showcase/Record Gameplay Tour. Output goes to
    /// Recordings/ in the project root (outside Assets, ignored by git).
    /// </summary>
    [InitializeOnLoad]
    public static class ShowcaseRecorder
    {
        private const string PendingKey = "CC_Showcase_Pending";
        private const string FolderKey = "CC_Showcase_Folder";

        /// <summary>Seconds each step stays on screen.</summary>
        private const float StepSeconds = 3.2f;

        private static RecorderController recorder;
        private static List<(string caption, Action action)> steps;
        private static int stepIndex;
        private static float nextStepAt;
        private static TextMeshProUGUI captionText;
        private static GameObject captionCanvas;
        private static string outputFolder;

        static ShowcaseRecorder()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Chief Complaint/Showcase/Record Gameplay Tour")]
        public static void RecordTour()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Showcase: stop play mode first.");
                return;
            }
            string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Recordings",
                "Showcase_" + DateTime.Now.ToString("yyyy-MM-dd_HHmm"));
            Directory.CreateDirectory(folder);
            SessionState.SetBool(PendingKey, true);
            SessionState.SetString(FolderKey, folder);
            EditorApplication.EnterPlaymode();
            // If play mode can't start (e.g. compile errors), drop the request so a later Play press isn't recorded.
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && SessionState.GetBool(PendingKey, false))
                {
                    SessionState.SetBool(PendingKey, false);
                    Debug.LogWarning("Showcase: play mode didn't start, so the tour was cancelled. Fix any compile errors and try again.");
                }
            };
        }

        /// <summary>Folder of the most recent recording, for scripts and the cc-showcase skill.</summary>
        public static string LastOutputFolder => SessionState.GetString(FolderKey, "");

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false);
            outputFolder = SessionState.GetString(FolderKey, "");
            try
            {
                BuildSteps();
                StartRecorder();
            }
            catch (Exception e)
            {
                Debug.LogError($"Showcase: couldn't start the recording, so play mode is stopping. {e.Message}");
                recorder = null;
                EditorApplication.ExitPlaymode();
                return;
            }
            stepIndex = 0;
            // Let the board settle before the first step.
            nextStepAt = Time.time + 1.5f;
            EditorApplication.update += Tick;
        }

        // -------------------------------------------------------------------------
        // Recorder
        // -------------------------------------------------------------------------

        private static void StartRecorder()
        {
            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = "Showcase Movie";
            movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High
            };
            movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = 1920, OutputHeight = 1080 };
            movie.CaptureAudio = false;
            movie.OutputFile = Path.Combine(outputFolder, "ChiefComplaint_Tour");

            controllerSettings.AddRecorderSettings(movie);
            controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRate = 30f;
            controllerSettings.CapFrameRate = true;

            recorder = new RecorderController(controllerSettings);
            recorder.PrepareRecording();
            recorder.StartRecording();
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            try { recorder?.StopRecording(); } catch (Exception e) { Debug.LogException(e); }
            recorder = null;
            Debug.Log($"Showcase: recording saved to {outputFolder}");
            EditorApplication.ExitPlaymode();
        }

        // -------------------------------------------------------------------------
        // Step runner
        // -------------------------------------------------------------------------

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
            if (Time.time < nextStepAt) return;

            if (stepIndex >= steps.Count) { Finish(); return; }

            var (caption, action) = steps[stepIndex];
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Showcase step {stepIndex + 1} ({caption}) failed: {e.Message}");
            }
            SetCaption(caption);

            // Save a still a moment after the step has drawn.
            int still = stepIndex + 1;
            string path = Path.Combine(outputFolder, $"{still:00}.png");
            EditorApplication.delayCall += () => ScreenCapture.CaptureScreenshot(path);

            stepIndex++;
            nextStepAt = Time.time + StepSeconds;
        }

        private static void SetCaption(string text)
        {
            if (captionText == null)
            {
                captionCanvas = new GameObject("Showcase_Caption", typeof(Canvas), typeof(CanvasScaler));
                var canvas = captionCanvas.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32000;
                var scaler = captionCanvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                var strip = new GameObject("Strip", typeof(RectTransform), typeof(Image));
                strip.transform.SetParent(captionCanvas.transform, false);
                var srt = (RectTransform)strip.transform;
                srt.anchorMin = new Vector2(0.15f, 0f);
                srt.anchorMax = new Vector2(0.85f, 0f);
                srt.pivot = new Vector2(0.5f, 0f);
                srt.anchoredPosition = new Vector2(0f, 150f);
                srt.sizeDelta = new Vector2(0f, 70f);
                strip.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.08f, 0.88f);

                var label = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI));
                label.transform.SetParent(strip.transform, false);
                var lrt = (RectTransform)label.transform;
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(20f, 6f);
                lrt.offsetMax = new Vector2(-20f, -6f);
                captionText = label.GetComponent<TextMeshProUGUI>();
                captionText.alignment = TextAlignmentOptions.Center;
                captionText.enableAutoSizing = true;
                captionText.fontSizeMin = 18;
                captionText.fontSizeMax = 30;
                captionText.color = Color.white;
                captionText.raycastTarget = false;
                strip.GetComponent<Image>().raycastTarget = false;
            }
            captionText.text = text;
            captionCanvas.SetActive(!string.IsNullOrEmpty(text));
        }

        // -------------------------------------------------------------------------
        // The tour
        // -------------------------------------------------------------------------

        private static void BuildSteps()
        {
            PatientCardView ward = null;
            steps = new List<(string, Action)>
            {
                ("<b>Chief Complaint</b> - gameplay tour", null),
                ("Patients arrive in Triage. Each hides 1-3 symptoms; Stability decays every shift.", null),
                ("Admit a patient to a Ward bed: Ward patients decay at half rate.", () =>
                {
                    ward = Patients()[0];
                    Admit(ward, Board().wardSlots[0]);
                    Hurt(ward, 3);
                }),
                ("Drag preview: the PlayZone shows what a card gives; valid patients glow.", () =>
                {
                    StartDrag(AddToHand("Starter/Card_NursingStudent"));
                }),
                ("Diagnose: Initial Assessment reveals the next hidden symptom.", () =>
                {
                    EndDrag();
                    Drop(AddToHand("Starter/Card_InitialAssessment"), false, ward, null);
                }),
                ("Team Assessment: a Doctor and a Nurse card in one shift diagnose a symptom for free.", () =>
                {
                    Admit(Patients()[1], Board().wardSlots[1]);
                    Drop(AddToHand("Starter/Card_MedicalStudent"), true, null, null);
                    Drop(AddToHand("Starter/Card_NursingStudent"), true, null, null);
                }),
                ("Pick a highlighted patient to complete the Team Assessment.", () =>
                {
                    if (PatientPicker.IsPicking) PatientPicker.TryPick(Patients()[1]);
                }),
                ("Click a revealed symptom to zoom in on it.", () => ToggleZoom(ward)),
                ("Treat in the matching department: the main symptom always sits in the centre.", () =>
                {
                    ToggleZoom(ward);
                    MoveToDepartment(ward);
                    Drop(AddToHand("Starter/Card_StandardCare"), false, ward, null);
                }),
                ("Treating earns an upgrade token in that department's colour (top bar).", null),
                ("End Shift: the Supply Room opens. Piles have limited stock and unlock as the run escalates.", () =>
                {
                    Gm().AddFunding(15);
                    Gm().EnterDraftPhase();
                }),
                ("Buy copies into the cart; Undo refunds them before the next shift.", () =>
                {
                    Draft().Buy(1);
                    Draft().Buy(1);
                }),
                ("Upgrades now cost Funding plus department-coloured upgrade tokens.", () =>
                {
                    Draft().Undo(1);
                    Gm().AddUpgradeTokens(SymptomCategory.Internal, 1);
                    Upgrades().SwitchTab(1);
                }),
                ("View Deck groups your cards by type.", () => Upgrades().SwitchTab(2)),
                ("Next shift. Harder patients unlock by cures or by time: Ambulances from shift 5.", () =>
                {
                    Upgrades().SwitchTab(0);
                    Draft().CloseDraftAndStartShift();
                    while (Gm().CurrentShift < 5 && Gm().CurrentPhase != GamePhase.GameOver) Gm().StartNewShift();
                }),
                ("If Triage is full, arriving patients are diverted and cost Reputation.", () =>
                {
                    while (Board().triageSlots.Length > TriageCount()) Bm().SpawnPatient();
                    Bm().SpawnPatient();
                }),
                ("Thanks for watching! Feedback welcome.", null),
            };
        }

        // -------------------------------------------------------------------------
        // Helpers (drive the real game code, as a player's input would)
        // -------------------------------------------------------------------------

        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private static DraggableCard dragging;

        private static GameManager Gm() => GameManager.Instance;
        private static BoardManager Bm() => GameManager.Instance.boardManager;
        private static BoardView Board() => BoardView.Instance;
        private static DraftManager Draft() => Object.FindFirstObjectByType<DraftManager>(FindObjectsInactive.Include);
        private static DeckUpgradeManager Upgrades() => Object.FindFirstObjectByType<DeckUpgradeManager>(FindObjectsInactive.Include);
        private static IReadOnlyList<PatientCardView> Patients() => Bm().ActivePatients;

        private static int TriageCount()
        {
            int n = 0;
            foreach (var p in Patients())
                if (p != null && p.transform.parent != null && p.transform.parent.CompareTag("TriageSlot")) n++;
            return n;
        }

        private static DraggableCard AddToHand(string assetPath)
        {
            var card = AssetDatabase.LoadAssetAtPath<ActionCardSO>($"Assets/Data/Actions/{assetPath}.asset");
            DeckManager.Instance.AddCardToHand(card);
            var active = HandView.Instance.activeCards;
            return active[active.Count - 1].GetComponent<DraggableCard>();
        }

        private static void StartDrag(DraggableCard card)
        {
            dragging = card;
            typeof(DraggableCard).GetMethod("OnMouseDown", Private).Invoke(card, null);
        }

        private static void EndDrag()
        {
            if (dragging == null) return;
            typeof(DraggableCard).GetMethod("OnMouseUp", Private).Invoke(dragging, null);
            dragging = null;
        }

        private static void Drop(DraggableCard card, bool onPlayZone, PatientCardView patient, ActionCardView targetCard)
        {
            var t = typeof(DraggableCard);
            t.GetField("isDragging", Private).SetValue(card, true);
            t.GetField("hoveredPlayZone", Private).SetValue(card, onPlayZone ? Object.FindFirstObjectByType<PlayZoneView>() : null);
            t.GetField("hoveredPatient", Private).SetValue(card, patient);
            t.GetField("hoveredCard", Private).SetValue(card, targetCard);
            t.GetMethod("OnMouseUp", Private).Invoke(card, null);
        }

        private static void Admit(PatientCardView patient, Transform bed)
        {
            patient.transform.position = new Vector3(bed.position.x, bed.position.y, -1f);
            patient.transform.SetParent(bed);
            patient.assignedWardSlot = bed;
        }

        private static void Hurt(PatientCardView patient, int amount)
        {
            patient.currentStability = Mathf.Max(1, patient.currentStability - amount);
            patient.UpdateVisuals();
        }

        private static void MoveToDepartment(PatientCardView patient)
        {
            if (patient.assignedSymptoms.Count == 0) return;
            var category = patient.assignedSymptoms[0].specialtyCategory;
            foreach (var dept in Board().departmentSlots)
            {
                if (dept == null || dept.category != category) continue;
                patient.transform.position = new Vector3(dept.transform.position.x, dept.transform.position.y, -1f);
                patient.transform.SetParent(dept.transform);
                patient.SpawnGhostInWard();
                return;
            }
        }

        private static void ToggleZoom(PatientCardView patient)
        {
            var sub = patient != null ? patient.GetComponentInChildren<SymptomSubCardView>() : null;
            if (sub == null) sub = Object.FindFirstObjectByType<SymptomSubCardView>();
            if (sub != null) typeof(SymptomSubCardView).GetMethod("OnMouseUp", Private).Invoke(sub, null);
        }
    }
}
