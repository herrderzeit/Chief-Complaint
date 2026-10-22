using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChiefComplaint.Sim.Data
{
    /// <summary>
    /// Reads game data straight from the Unity project on disk: the ScriptableObject assets under
    /// Assets/Data and the manager configuration serialized in the main scene. No Unity install needed,
    /// so balance runs always use exactly the content the game ships with.
    /// </summary>
    public sealed class ProjectLoader
    {
        public const string DefaultScene = "Assets/Scenes/MainBoard.unity";

        private readonly string projectRoot;
        private readonly Dictionary<string, string> pathByGuid = new Dictionary<string, string>();
        private readonly Dictionary<string, string> scriptByGuid = new Dictionary<string, string>();
        private readonly Dictionary<string, List<UnityDoc>> docsByGuid = new Dictionary<string, List<UnityDoc>>();
        private readonly Dictionary<string, CardDef> cardsByGuid = new Dictionary<string, CardDef>();
        private readonly Dictionary<string, SymptomDef> symptomsByGuid = new Dictionary<string, SymptomDef>();
        private readonly Dictionary<string, PatientDef> patientsByGuid = new Dictionary<string, PatientDef>();
        private GameData data;

        public ProjectLoader(string projectRoot)
        {
            this.projectRoot = Path.GetFullPath(projectRoot);
        }

        /// <summary>Walks up from <paramref name="start"/> until a folder containing Assets/ and ProjectSettings/ is found.</summary>
        public static string FindProjectRoot(string start)
        {
            var dir = new DirectoryInfo(Path.GetFullPath(start));
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "Assets")) && Directory.Exists(Path.Combine(dir.FullName, "ProjectSettings")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException($"No Unity project (Assets/ + ProjectSettings/) found above {start}. Pass --project <path>.");
        }

        public GameData Load(string scenePath = DefaultScene)
        {
            data = new GameData();
            IndexMetaFiles();

            string sceneFile = Path.Combine(projectRoot, scenePath);
            List<UnityDoc> scene = UnityYaml.ParseFile(sceneFile);

            UnityDoc gameManager = FindComponent(scene, "GameManager");
            UnityDoc patientDeck = FindComponent(scene, "PatientDeckManager");
            UnityDoc deckManager = FindComponent(scene, "DeckManager");
            UnityDoc draftManager = FindComponent(scene, "DraftManager");
            UnityDoc boardView = FindComponent(scene, "BoardView");

            GameConfig c = data.Config;
            if (gameManager != null)
            {
                c.BaseAP = gameManager.GetInt("baseAP", c.BaseAP);
                c.StartingReputation = gameManager.GetInt("currentReputation", c.StartingReputation);
                c.StartingFunding = gameManager.GetInt("currentFunding", c.StartingFunding);
                c.InitialPatientCount = gameManager.GetInt("initialPatientCount", c.InitialPatientCount);
                c.ExtraPatientEveryNShifts = gameManager.GetInt("extraPatientEveryNShifts", c.ExtraPatientEveryNShifts);
                c.MaxReputation = gameManager.GetInt("maxReputation", c.MaxReputation);
                c.DiversionReputationPenalty = gameManager.GetInt("diversionReputationPenalty", c.DiversionReputationPenalty);
                c.DeathReputationPenalty = gameManager.GetInt("deathReputationPenalty", c.DeathReputationPenalty);
                c.DeathAddsRedTape = gameManager.GetBool("deathAddsRedTape", c.DeathAddsRedTape);
                c.ShiftsToWin = gameManager.GetInt("shiftsToWin", c.ShiftsToWin);
                c.WalkInFundingReward = gameManager.GetInt("walkInFundingReward", c.WalkInFundingReward);
                c.AmbulanceFundingReward = gameManager.GetInt("ambulanceFundingReward", c.AmbulanceFundingReward);
                c.TraumaFlightFundingReward = gameManager.GetInt("traumaFlightFundingReward", c.TraumaFlightFundingReward);
                c.CureReputationCap = gameManager.GetInt("cureReputationCap", c.CureReputationCap);
            }
            else data.Warnings.Add("GameManager not found in scene; using code defaults.");

            if (patientDeck != null)
            {
                c.AmbulanceCureThreshold = patientDeck.GetInt("ambulanceCureThreshold", c.AmbulanceCureThreshold);
                c.AmbulanceShiftThreshold = patientDeck.GetInt("ambulanceShiftThreshold", c.AmbulanceShiftThreshold);
                c.TraumaFlightShiftThreshold = patientDeck.GetInt("traumaFlightShiftThreshold", c.TraumaFlightShiftThreshold);
                c.TraumaFlightCureThreshold = patientDeck.GetInt("traumaFlightCureThreshold", c.TraumaFlightCureThreshold);
                data.WalkInPool = patientDeck.GetRefList("walkInPool").Select(LoadPatient).Where(p => p != null).ToList();
                data.AmbulancePool = patientDeck.GetRefList("ambulancePool").Select(LoadPatient).Where(p => p != null).ToList();
                data.TraumaFlightPool = patientDeck.GetRefList("traumaFlightPool").Select(LoadPatient).Where(p => p != null).ToList();
                data.SymptomDecks[SymptomCategory.Cardiopulmonary] = LoadSymptoms(patientDeck, "cardiopulmonaryDeck");
                data.SymptomDecks[SymptomCategory.Infectious] = LoadSymptoms(patientDeck, "infectiousDeck");
                data.SymptomDecks[SymptomCategory.Internal] = LoadSymptoms(patientDeck, "internalDeck");
                data.SymptomDecks[SymptomCategory.Trauma] = LoadSymptoms(patientDeck, "traumaDeck");
            }
            else data.Warnings.Add("PatientDeckManager not found in scene; no patients loaded.");

            if (deckManager != null)
            {
                c.HandSize = deckManager.GetInt("handSize", c.HandSize);
                data.StartingDeck = deckManager.GetRefList("startingDeck").Select(LoadCard).Where(x => x != null).ToList();
                data.RedTapePool = deckManager.GetRefList("redTapePool").Select(LoadCard).Where(x => x != null).ToList();
            }
            else data.Warnings.Add("DeckManager not found in scene; empty starting deck.");

            if (draftManager != null)
            {
                foreach (var map in draftManager.GetMapList("supplyPiles"))
                {
                    if (!map.TryGetValue("card", out string cardRef) || !UnityRef.TryParse(cardRef, out UnityRef r)) continue;
                    CardDef card = LoadCard(r);
                    if (card == null) continue;
                    data.SupplyPiles.Add(new SupplyPileDef
                    {
                        Card = card,
                        UnlockAt = map.TryGetValue("unlockAt", out string u) && int.TryParse(u, out int ui) ? (EscalationType)ui : EscalationType.WalkIn,
                        MaxStock = map.TryGetValue("maxStock", out string m) && int.TryParse(m, out int mi) ? Math.Max(1, mi) : 3
                    });
                }
                if (data.SupplyPiles.Count == 0)
                {
                    // Older scenes: the random-roll pool, treated as open piles of 3.
                    foreach (CardDef card in draftManager.GetRefList("draftableCardsPool").Select(LoadCard).Where(x => x != null))
                        data.SupplyPiles.Add(new SupplyPileDef { Card = card });
                    if (data.SupplyPiles.Count > 0) data.Warnings.Add("DraftManager has no supplyPiles; using draftableCardsPool as open piles of 3.");
                }
                data.DraftPool = data.SupplyPiles.Select(sp => sp.Card).ToList();
            }

            if (boardView != null)
            {
                c.TriageSlots = boardView.GetRefList("triageSlots").Count;
                c.WardSlots = boardView.GetRefList("wardSlots").Count;
                int departments = boardView.GetRefList("departmentSlots").Count;
                if (departments != 4)
                    data.Warnings.Add($"Scene has {departments} department slots; the simulator models one department per specialty (4).");
            }

            // Load every action card asset too, so upgrade targets and patient card references resolve.
            foreach (var kv in pathByGuid)
                if (kv.Value.Replace('\\', '/').Contains("/Data/Actions/") && kv.Value.EndsWith(".asset", StringComparison.Ordinal))
                    LoadCard(new UnityRef(11400000, kv.Key));

            foreach (CardDef card in cardsByGuid.Values) data.AllCards[card.Id] = card;
            return data;
        }

        // -------------------------------------------------------------------------
        // Asset indexing
        // -------------------------------------------------------------------------

        private void IndexMetaFiles()
        {
            string assets = Path.Combine(projectRoot, "Assets");
            foreach (string meta in Directory.EnumerateFiles(assets, "*.meta", SearchOption.AllDirectories))
            {
                string guid = ReadGuid(meta);
                if (guid == null) continue;
                string target = meta.Substring(0, meta.Length - ".meta".Length);
                pathByGuid[guid] = target;
                if (target.EndsWith(".cs", StringComparison.Ordinal))
                    scriptByGuid[guid] = Path.GetFileNameWithoutExtension(target);
            }
        }

        private static string ReadGuid(string metaPath)
        {
            foreach (string line in File.ReadLines(metaPath))
                if (line.StartsWith("guid: ", StringComparison.Ordinal)) return line.Substring(6).Trim();
            return null;
        }

        private List<UnityDoc> DocsFor(string guid)
        {
            if (docsByGuid.TryGetValue(guid, out var docs)) return docs;
            if (!pathByGuid.TryGetValue(guid, out string path) || !File.Exists(path))
            {
                data.Warnings.Add($"Missing asset for guid {guid}.");
                return docsByGuid[guid] = new List<UnityDoc>();
            }
            return docsByGuid[guid] = UnityYaml.ParseFile(path);
        }

        private static UnityDoc MainDoc(List<UnityDoc> docs) => docs.FirstOrDefault(d => d.FileId == 11400000) ?? docs.FirstOrDefault();

        private string ScriptName(UnityDoc doc) => scriptByGuid.TryGetValue(doc.ScriptGuid, out string name) ? name : "";

        private static UnityDoc FindComponent(List<UnityDoc> scene, string className) =>
            scene.FirstOrDefault(d => d.ClassIdentifier.EndsWith("::" + className, StringComparison.Ordinal)
                                   || d.ClassIdentifier.EndsWith("." + className, StringComparison.Ordinal));

        private string AssetName(string guid) =>
            pathByGuid.TryGetValue(guid, out string path) ? Path.GetFileNameWithoutExtension(path) : guid;

        // -------------------------------------------------------------------------
        // Cards
        // -------------------------------------------------------------------------

        private CardDef LoadCard(UnityRef reference)
        {
            if (reference.IsNull || reference.Guid.Length == 0) return null;
            if (cardsByGuid.TryGetValue(reference.Guid, out CardDef cached)) return cached;

            List<UnityDoc> docs = DocsFor(reference.Guid);
            UnityDoc doc = MainDoc(docs);
            if (doc == null) return null;

            var card = new CardDef { Id = AssetName(reference.Guid) };
            // Register before resolving the upgrade so a cyclic chain cannot recurse forever.
            cardsByGuid[reference.Guid] = card;

            card.Name = doc.Get("cardName", card.Id);
            card.Type = (CardType)doc.GetInt("cardType");
            card.Tags = doc.GetPackedIntList("tags").Select(t => (CardTag)t).ToList();
            card.IsPlayable = doc.GetBool("isPlayable", true);
            card.SuccessChance = doc.GetFloat("successChance", 1f);
            card.ApCost = doc.GetInt("apCost");
            card.ReputationCost = doc.GetInt("reputationCost");
            card.CardsToDraw = doc.GetInt("cardsToDraw");
            card.PowerTier = doc.GetInt("powerTier", 1);
            card.ExhaustsOnUse = doc.GetBool("exhaustsOnUse");
            card.ExhaustsAtShiftEnd = doc.GetBool("exhaustsAtShiftEnd");
            card.UpgradeCost = doc.GetInt("draftUpgradeCost", 5);
            card.UpgradeTokenCost = doc.GetInt("upgradeTokenCost", 0);
            card.UpgradeTokenAnyColour = doc.GetBool("upgradeTokenAnyColour");
            card.UpgradeTokenColour = (SymptomCategory)doc.GetInt("upgradeTokenColour", 0);

            foreach (UnityRef effectRef in doc.GetRefList("effects"))
            {
                EffectDef effect = LoadEffect(effectRef, reference.Guid, card.Name);
                if (effect != null) card.Effects.Add(effect);
            }

            UnityRef upgrade = doc.GetRef("directDraftUpgrade");
            if (!upgrade.IsNull) card.Upgrade = LoadCard(upgrade);
            return card;
        }

        private EffectDef LoadEffect(UnityRef reference, string ownerGuid, string ownerName)
        {
            if (reference.IsNull) return null;
            string guid = reference.IsLocal ? ownerGuid : reference.Guid;
            List<UnityDoc> docs = DocsFor(guid);
            UnityDoc doc = reference.IsLocal ? docs.FirstOrDefault(d => d.FileId == reference.FileId) : MainDoc(docs);
            if (doc == null)
            {
                data.Warnings.Add($"{ownerName}: effect {reference.FileId} not found.");
                return null;
            }

            string script = ScriptName(doc);
            switch (script)
            {
                case "GainAPEffectSO": return new GainApEffect { Amount = doc.GetInt("apGenerated", 1) };
                case "GainReputationEffectSO": return new GainReputationEffect { Amount = doc.GetInt("reputationGenerated", 1) };
                case "DrawCardsEffectSO": return new DrawCardsEffect { Amount = doc.GetInt("cardsToDraw", 1) };
                case "DiagnoseEffectSO": return new DiagnoseEffect { MaxTier = doc.GetInt("diagnosesSymptomTier", 3) };
                case "CureSymptomEffectSO": return new CureEffect { MaxTier = doc.GetInt("curesSymptomTier", 3) };
                case "StabilisationHealEffectSO": return new HealEffect { Amount = doc.GetInt("healAmount", 1) };
                case "PauseStabilityEffectSO": return new PauseEffect();
                case "TrashCardEffectSO": return new TrashEffect();
                case "CleanseRedTapeEffectSO": return new CleanseRedTapeEffect { RepPerCard = doc.GetInt("repBonusPerRedTapeCleansed", 1) };
                case "EitherOrEffectSO":
                    return new EitherOrEffect { Reputation = doc.GetInt("reputationGenerated", 1), Ap = doc.GetInt("altApGenerated", 1) };
                case "WardWideEffectSO":
                    return new WardWideEffect { PatientEffect = LoadEffect(doc.GetRef("patientEffect"), guid, ownerName) };
                case "ChoiceEffectSO":
                    return new ChoiceEffect
                    {
                        LabelA = doc.Get("optionALabel"),
                        LabelB = doc.Get("optionBLabel"),
                        OptionA = LoadEffect(doc.GetRef("optionA"), guid, ownerName),
                        OptionB = LoadEffect(doc.GetRef("optionB"), guid, ownerName)
                    };
                default:
                    data.Warnings.Add($"{ownerName}: effect script '{script}' is not simulated; it will never apply.");
                    return new UnknownEffect { ScriptName = script };
            }
        }

        // -------------------------------------------------------------------------
        // Symptoms and patients
        // -------------------------------------------------------------------------

        private List<SymptomDef> LoadSymptoms(UnityDoc owner, string listName) =>
            owner.GetRefList(listName).Select(LoadSymptom).Where(s => s != null).ToList();

        private SymptomDef LoadSymptom(UnityRef reference)
        {
            if (reference.IsNull) return null;
            if (symptomsByGuid.TryGetValue(reference.Guid, out SymptomDef cached)) return cached;
            UnityDoc doc = MainDoc(DocsFor(reference.Guid));
            if (doc == null) return null;
            var symptom = new SymptomDef
            {
                Id = AssetName(reference.Guid),
                Category = (SymptomCategory)doc.GetInt("specialtyCategory"),
                Tags = doc.GetList("clinicalTags"),
                Tier1Name = doc.Get("tier1Name"),
                Tier2Name = doc.Get("tier2Name"),
                Tier3Name = doc.Get("tier3Name")
            };
            return symptomsByGuid[reference.Guid] = symptom;
        }

        private PatientDef LoadPatient(UnityRef reference)
        {
            if (reference.IsNull) return null;
            if (patientsByGuid.TryGetValue(reference.Guid, out PatientDef cached)) return cached;
            UnityDoc doc = MainDoc(DocsFor(reference.Guid));
            if (doc == null) return null;
            var patient = new PatientDef
            {
                Id = AssetName(reference.Guid),
                Name = doc.Get("patientName"),
                Complaint = doc.Get("chiefComplaint"),
                Phase = (EscalationType)doc.GetInt("escalationPhase"),
                TriageLevel = doc.GetInt("triageLevel", 3),
                // OnValidate caps the list at 3 categories.
                Categories = doc.GetPackedIntList("incomingCategories").Take(3).Select(x => (SymptomCategory)x).ToList(),
                Tags = doc.GetList("clinicalTags"),
                StartingStability = doc.GetInt("startingStability", 10),
                ArrivesWithHandover = doc.GetBool("arrivesWithHandover"),
                DecayPerTurn = doc.GetInt("stabilityDecayPerTurn", 1),
                ReputationReward = doc.GetInt("reputationReward", 2)
            };
            foreach (var map in doc.GetMapList("triggers"))
            {
                var trigger = new PatientTriggerDef
                {
                    Condition = (TriggerCondition)ParseInt(map, "condition"),
                    Effect = (TriggerEffect)ParseInt(map, "effect"),
                    Value = ParseInt(map, "effectValue")
                };
                if (map.TryGetValue("cardReference", out string refText) && UnityRef.TryParse(refText, out UnityRef cardRef) && !cardRef.IsNull)
                    trigger.CardReference = LoadCard(cardRef);
                patient.Triggers.Add(trigger);
            }
            return patientsByGuid[reference.Guid] = patient;
        }

        private static int ParseInt(Dictionary<string, string> map, string key) =>
            map.TryGetValue(key, out string v) && int.TryParse(v, out int i) ? i : 0;
    }
}
