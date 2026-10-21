using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TMPro;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;
using Object = UnityEngine.Object;

namespace ChiefComplaint.Editor.Tools
{
    /// <summary>
    /// Records a captioned, scripted tour of the game to an MP4 (Unity Recorder) for sharing with collaborators,
    /// and saves a still of each step. Player actions are performed with simulated Input System mouse events
    /// (real drags and clicks through the game's own input handling), queued one per frame. Menu: Chief Complaint/Showcase/Record Gameplay Tour. Output goes to
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
        private static string pendingStillPath;
        private static readonly Queue<Action> inputQueue = new Queue<Action>();
        private static int lastInputFrame = -1;
        private static Vector2 pointer = new Vector2(960f, 540f);
        private static bool pointerPressed;

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
            pendingStillPath = null;
            inputQueue.Clear();
            pointerPressed = false;
            // Input respects Game view focus in the Editor, so make sure the Game view has it.
            EditorApplication.ExecuteMenuItem("Window/General/Game");
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

            // Once per rendered frame, re-assert the simulated pointer: otherwise the real mouse device (or a
            // Game view focus change) resets its position and button between our moves, even during a hold.
            if (Time.frameCount == lastInputFrame) return;
            lastInputFrame = Time.frameCount;
            SendPointer(pointer, pointerPressed);

            // Play queued input, one action per frame, so the game sees smooth drags.
            if (inputQueue.Count > 0)
            {
                try { inputQueue.Dequeue()?.Invoke(); }
                catch (Exception e) { Debug.LogWarning($"Showcase step {stepIndex} input failed: {e.Message}"); }
                return;
            }

            // The step's input has finished: save its still, then hold it on screen.
            if (pendingStillPath != null)
            {
                ScreenCapture.CaptureScreenshot(pendingStillPath);
                pendingStillPath = null;
                nextStepAt = Time.time + StepSeconds;
                return;
            }

            if (Time.time < nextStepAt) return;
            if (stepIndex >= steps.Count) { Finish(); return; }

            var (caption, action) = steps[stepIndex];
            SetCaption(caption);
            try { action?.Invoke(); }
            catch (Exception e) { Debug.LogWarning($"Showcase step {stepIndex + 1} ({caption}) failed: {e.Message}"); }

            stepIndex++;
            pendingStillPath = Path.Combine(outputFolder, $"{stepIndex:00}.png");
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
            PatientCardView second = null;
            steps = new List<(string, Action)>
            {
                ("<b>Chief Complaint</b> - gameplay tour", null),
                ("Patients arrive in Triage. Each hides 1-3 symptoms; Stability decays every shift.", null),
                ("Admit a patient to a Ward bed: Ward patients decay at half rate.", () =>
                {
                    ward = Patients()[0];
                    DragTo(() => Header(ward), () => At(Board().wardSlots[0]));
                    Do(() => Hurt(ward, 3));
                }),
                ("Drag preview: the PlayZone shows what a card gives; valid patients glow.", () =>
                {
                    AddToHand("Starter/Card_NursingStudent");
                    DragTo(() => HandCard("Nursing Student"), () => PlayZoneTop(), release: false);
                }),
                ("Diagnose: drag Initial Assessment onto a Ward patient to reveal a hidden symptom.", () =>
                {
                    // Cancel the held card by letting go over the hand.
                    MoveTo(() => new Vector2(pointer.x, 20f));
                    Press(false);
                    AddToHand("Starter/Card_InitialAssessment");
                    DragTo(() => HandCard("Initial Assessment"), () => At(ward));
                }),
                ("Team Assessment: a Doctor and a Nurse card in one shift diagnose a symptom for free.", () =>
                {
                    second = Patients()[1];
                    DragTo(() => Header(second), () => At(Board().wardSlots[1]));
                    AddToHand("Starter/Card_MedicalStudent");
                    DragTo(() => HandCard("Medical Student"), () => At(PlayZone()));
                    AddToHand("Starter/Card_NursingStudent");
                    DragTo(() => HandCard("Nursing Student"), () => At(PlayZone()));
                }),
                ("Pick a highlighted patient to complete the Team Assessment.", () => Click(() => At(second))),
                ("Click a revealed symptom to zoom in on it.", () => Click(() => At(Symptom(ward)))),
                ("Treat in the matching department: the main symptom always sits in the centre.", () =>
                {
                    Click(() => At(Symptom(ward)));
                    DragTo(() => Header(ward), () => At(DepartmentFor(ward)));
                    AddToHand("Starter/Card_StandardCare");
                    DragTo(() => HandCard("Standard Care"), () => At(ward));
                }),
                ("Treating earns an upgrade token in that department's colour (top bar).", null),
                ("End Shift: the Supply Room opens. Piles have limited stock and unlock as the run escalates.", () =>
                {
                    Do(() => Gm().AddFunding(15));
                    Click(() => AtUi(Named("EndShift_Button")));
                }),
                ("Buy copies into the cart; Undo refunds them before the next shift.", () =>
                {
                    Click(() => AtUi(MarketCard(1).transform));
                    Click(() => AtUi(MarketCard(1).transform));
                }),
                ("Upgrades now cost Funding plus department-coloured upgrade tokens.", () =>
                {
                    Click(() => AtUi(MarketCard(1).undoButton.transform));
                    Do(() => Gm().AddUpgradeTokens(SymptomCategory.Internal, 1));
                    Click(() => AtUi(Upgrades().upgradesTabButton.transform));
                }),
                ("View Deck groups your cards by type.", () => Click(() => AtUi(Upgrades().deckViewTabButton.transform))),
                ("Next shift. Harder patients unlock by cures or by time: Ambulances from shift 5.", () =>
                {
                    Click(() => AtUi(Upgrades().marketTabButton.transform));
                    Click(() => AtUi(Named("NextShift_Button")));
                    Wait(10);
                    Do(() =>
                    {
                        while (Gm().CurrentShift < 5 && Gm().CurrentPhase != GamePhase.GameOver) Gm().StartNewShift();
                    });
                }),
                ("If Triage is full, arriving patients are diverted and cost Reputation.", () => Do(() =>
                {
                    while (Board().triageSlots.Length > TriageCount()) Bm().SpawnPatient();
                    Bm().SpawnPatient();
                })),
                ("Thanks for watching! Feedback welcome.", null),
            };
        }

        // -------------------------------------------------------------------------
        // Simulated input (Input System mouse events, as Pipeline's runtime simulate_pointer sends)
        // -------------------------------------------------------------------------

        private static void SendPointer(Vector2 screen, bool? leftPressed)
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            pointer = screen;
            if (leftPressed.HasValue) pointerPressed = leftPressed.Value;
            using (StateEvent.From(mouse, out var eventPtr))
            {
                mouse.position.WriteValueIntoEvent(screen, eventPtr);
                if (leftPressed.HasValue) mouse.leftButton.WriteValueIntoEvent(leftPressed.Value ? 1f : 0f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
            InputSystem.Update();
        }

        /// <summary>Runs <paramref name="action"/> in order with the queued input (for setup that isn't player input).</summary>
        private static void Do(Action action) => inputQueue.Enqueue(action);

        private static void Wait(int frames)
        {
            for (int i = 0; i < frames; i++) inputQueue.Enqueue(null);
        }

        /// <summary>
        /// Glides the pointer to <paramref name="target"/> over a few frames. The target is re-read every frame,
        /// so the pointer tracks things that are still moving (e.g. a card sliding into the hand).
        /// </summary>
        private static void MoveTo(Func<Vector2> target, int frames = 14)
        {
            Vector2 from = default;
            for (int i = 1; i <= frames; i++)
            {
                int step = i;
                inputQueue.Enqueue(() =>
                {
                    if (step == 1) from = pointer;
                    SendPointer(Vector2.Lerp(from, target(), step / (float)frames), null);
                });
            }
        }

        private static void Press(bool pressed)
        {
            inputQueue.Enqueue(() => SendPointer(pointer, pressed));
            Wait(4);
        }

        /// <summary>
        /// Moves onto <paramref name="target"/>, lets hover effects settle (a hovered hand card grows and lifts),
        /// then snaps back onto the target, so the press lands on what was aimed at.
        /// </summary>
        private static void Settle(Func<Vector2> target)
        {
            MoveTo(target);
            Wait(8);
            MoveTo(target, 4);
            Wait(2);
        }

        private static void Click(Func<Vector2> at)
        {
            Settle(at);
            Press(true);
            Press(false);
        }

        private static void DragTo(Func<Vector2> from, Func<Vector2> to, bool release = true)
        {
            Settle(from);
            Press(true);
            MoveTo(to, 26);
            Wait(6);
            if (release) Press(false);
        }

        // -------------------------------------------------------------------------
        // Targets (screen points of things on the board and in the UI)
        // -------------------------------------------------------------------------

        private static GameManager Gm() => GameManager.Instance;
        private static BoardManager Bm() => GameManager.Instance.boardManager;
        private static BoardView Board() => BoardView.Instance;
        private static DraftManager Draft() => Object.FindFirstObjectByType<DraftManager>(FindObjectsInactive.Include);
        private static DeckUpgradeManager Upgrades() => Object.FindFirstObjectByType<DeckUpgradeManager>(FindObjectsInactive.Include);
        private static PlayZoneView PlayZone() => Object.FindFirstObjectByType<PlayZoneView>();
        private static IReadOnlyList<PatientCardView> Patients() => Bm().ActivePatients;

        private static Vector2 At(Component c) => At(c.transform.position);

        /// <summary>A point on a patient card's name header, clear of the symptom card in the middle.</summary>
        private static Vector2 Header(PatientCardView patient)
        {
            var col = patient.GetComponent<Collider2D>();
            if (col == null) return At(patient);
            Bounds b = col.bounds;
            return At(new Vector3(b.center.x, b.max.y - b.size.y * 0.15f, b.center.z));
        }

        /// <summary>
        /// A point just above the PlayZone. The zone's label shows for the whole drag, so holding the card here
        /// leaves the label (and the glowing patients) visible instead of covered by the card.
        /// </summary>
        private static Vector2 PlayZoneTop()
        {
            var zone = PlayZone();
            var col = zone.GetComponent<Collider2D>();
            if (col == null) return At(zone);
            Bounds b = col.bounds;
            return At(new Vector3(b.center.x, b.max.y + b.size.y * 0.55f, b.center.z));
        }
        private static Vector2 At(Vector3 world) => CameraUtil.Main.WorldToScreenPoint(world);

        private static Vector2 AtUi(Transform t)
        {
            var canvas = t.GetComponentInParent<Canvas>().rootCanvas;
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return RectTransformUtility.WorldToScreenPoint(cam, t.position);
        }

        private static Transform Named(string name)
        {
            var go = GameObject.Find(name);
            if (go == null) throw new InvalidOperationException($"No active object named {name}.");
            return go.transform;
        }

        /// <summary>A point on the visible top edge of the hand card named <paramref name="cardName"/> (hand cards only peek up).</summary>
        private static Vector2 HandCard(string cardName)
        {
            var active = HandView.Instance.activeCards;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var view = active[i].GetComponent<ActionCardView>();
                if (view == null || view.currentCardData == null || view.currentCardData.cardName != cardName) continue;
                var col = active[i].GetComponent<Collider2D>();
                Bounds b = col != null ? col.bounds : new Bounds(active[i].transform.position, Vector3.one);
                Vector2 top = At(new Vector3(b.center.x, b.max.y, b.center.z));
                return new Vector2(top.x, Mathf.Max(8f, top.y - 30f));
            }
            throw new InvalidOperationException($"{cardName} isn't in the hand.");
        }

        private static DraftMarketCardView MarketCard(int index) =>
            Draft().marketContainer.GetChild(index).GetComponent<DraftMarketCardView>();

        private static SymptomSubCardView Symptom(PatientCardView patient)
        {
            var sub = patient.GetComponentInChildren<SymptomSubCardView>();
            return sub != null ? sub : Object.FindFirstObjectByType<SymptomSubCardView>();
        }

        private static DepartmentSlot DepartmentFor(PatientCardView patient)
        {
            var category = patient.assignedSymptoms[0].specialtyCategory;
            foreach (var dept in Board().departmentSlots)
                if (dept != null && dept.category == category) return dept;
            throw new InvalidOperationException($"No department for {category}.");
        }

        private static int TriageCount()
        {
            int n = 0;
            foreach (var p in Patients())
                if (p != null && p.transform.parent != null && p.transform.parent.CompareTag("TriageSlot")) n++;
            return n;
        }

        /// <summary>Adds a card to the hand, then waits for the hand layout to settle before it is grabbed.</summary>
        private static void AddToHand(string assetPath)
        {
            Do(() => DeckManager.Instance.AddCardToHand(AssetDatabase.LoadAssetAtPath<ActionCardSO>($"Assets/Data/Actions/{assetPath}.asset")));
            Wait(60);
        }

        private static void Hurt(PatientCardView patient, int amount)
        {
            patient.currentStability = Mathf.Max(1, patient.currentStability - amount);
            patient.UpdateVisuals();
        }
    }
}
