# Supply room market + 8 new cards + Reputation fixes

## Plan (lead: cc-card-engine-specialist)

**Goal:** replace the random 5-card draft roll with a "supply room": every draftable card has its own pile, always visible, with limited stock; piles unlock as the run escalates; stock restocks +1 per pile each draft (up to its max). Add 8 new market cards, reduce Reputation income, and add an all-or-nothing Reputation cost to cards.

**Decisions already made (user approved):**
- 12 piles in three stages: Open (shift 1), Ambulance (after 3 cures), Trauma (after 8 cures). Stock and stages per the table below.
- Restock +1 per unlocked pile at each draft, capped at the pile's max. Newly unlocked piles start full.
- The cart stays: buying reserves a copy (stock −1) and spends Funding; clicking again refunds it (stock +1). Purchases go to the discard pile when the next shift starts.
- Upgrades tab unchanged.
- No Reputation ceiling for now (one mechanic at a time); re-check Rep in the simulator afterwards.
- New `ActionCardSO.reputationCost` (int, default 0): the card can't be played unless current Reputation is **above** the cost, and Rep is only paid if the card's effect lands. Double Shift uses this field instead of a negative GainReputation effect.
- Market price stays `CalculateDraftValue()` (unchanged formula).

| Stage | Pile | Max stock |
|---|---|---|
| Open | Sharps Bin (new) | 3 |
| Open | Agency Nurse (new) | 4 |
| Open | Shift Handover (new) | 3 |
| Open | Ward Round (new) | 2 |
| Ambulance | Stat Labs | 3 |
| Ambulance | Grateful Patient | 2 |
| Ambulance | Compliance Audit | 2 |
| Ambulance | Blood Bank (new) | 3 |
| Ambulance | Double Shift (new) | 3 |
| Trauma | Crash Cart | 2 |
| Trauma | Trauma Team (new) | 2 |
| Trauma | Damage Control Surgery (new) | 2 |

**Steps and owners:**
1. Lead: `ActionCardSO.reputationCost`; Rep-cost gate in `DraggableCard`; `PatientDeckManager.CurrentEscalation`; `SupplyRoom` logic class + `SupplyPile` config on `DraftManager`; `DraftMarketCardView` stock/locked API; `DraftManager.OnSupplyChanged` event; mirror everything in `Tools/BalanceSim`.
2. cc-medical-data-scraper: the 8 new card assets (+ effect assets) in `Assets/Data/Actions/Draft/`; Reputation fixes on existing assets.
3. cc-ui-ux-specialist: supply-room layout (scrolling grid, stage headings), stock badge and locked state on `Draft_Market_Card`.
4. cc-unity-qa-tester: tests for `SupplyRoom`, Reputation cost, `PatientCardView.SubCardSlots`, simulator parity.
5. Lead: wire the 12 piles on DraftManager in `MainBoard.unity`, review, recompile, tests, simulator, play-mode check.

**Done when:** the Market tab shows 12 piles (locked ones greyed with their unlock stage), buying/refunding moves stock, restock and unlocks work across shifts, all tests pass, and the simulator reports win rates and Rep at win for the new setup.

## Follow-ups (not in this change)
- **Next: drag preview** (user request). While dragging a card:
  - **Play zone:** show what the card gives on the play zone, e.g. "+3 AP · −2 Rep" or "+2 Cards". Use a per-effect `Describe()` on `CardEffectSO`, plus `cardsToDraw` and `reputationCost`.
  - **Patient glow:** give every patient the card's patient effect can apply to a soft "targetable" glow, separate from the hover highlight. Cards with an alternative (e.g. Nursing Student: draw on the play zone, heal on a patient) show both at once.
  - **Who builds what:** the lead does the effect descriptions and `PatientCardView.SetTargetable`; cc-ui-ux-specialist does the PlayZone label and the glow visual.
- **Symptom upgrade tokens** (user idea): treating symptoms earns tokens in that symptom's colour (Cardiopulmonary, Infectious, Internal, Trauma); upgrades cost tokens of specific colours plus Funding, so how a deck upgrades depends on what the player has treated. Build next with `/cc-new-feature`, after the supply room; decide which colours feed which upgrade lines.
- Re-check Reputation with the simulator after this change; consider a Reputation ceiling if it's still too high.

## Also in this change
- `PatientCardView.SubCardSlots`: the highest-tier symptom always sits in the centre slot (fixes handover patients and 2-symptom patients).

## cc-medical-data-scraper: step 2 done (2026-10-21)

All changes were made through the connected Editor (`unity command eval_file` using AssetDatabase.CreateAsset / AddObjectToAsset, then SaveAssets). No YAML was hand-edited. Scene, prefabs, C# and tests were not touched.

**Convention followed:** effect SOs are **sub-assets** of the card file (as in Card_CrashCart / Card_T3_ChiefNurse). Single top-level effects use the class-default name (`TrashCardEffect`, `GainAPEffect`, `CureSymptomEffect`), and nested trees use the Chief Nurse naming (`Choice`, `WardHeal`, `WardPause`, `WardDiagnose`, `Heal3`, `Pause`, `Diagnose`). All new cards: isPlayable true, successChance 1, exhaustsAtShiftEnd false, cardArt none (no existing draft card has art), directDraftUpgrade none, draftUpgradeCost 0.

**Created (Assets/Data/Actions/Draft/):**

| File | Type / Tier / AP | Other | Effects | Tags | Price |
|---|---|---|---|---|---|
| Card_SharpsBin.asset | Supply / 1 / 1 | | TrashCardEffect | none | 3 |
| Card_AgencyNurse.asset | Staff / 1 / 1 | | Choice("Agency Nurse": A "Restore 3 Stability" = Heal3; B "Pause decay" = Pause) | none | 5 |
| Card_ShiftHandover.asset | Administration / 1 / 1 | cardsToDraw 2 | none | Management | 8 |
| Card_WardRound.asset | Diagnostic / 2 / 2 | | WardDiagnose(Diagnose Tier1) | none | 6 |
| Card_BloodBank.asset | Supply / 2 / 1 | exhausts | WardHeal(Heal3) | Emergency | 6 |
| Card_DoubleShift.asset | Staff / 2 / 0 | reputationCost 2 | GainAPEffect +3 | none | 10 |
| Card_TraumaTeam.asset | Staff / 3 / 2 | exhausts | WardPause(Pause), WardHeal(Heal3) | Emergency | 12 |
| Card_DamageControlSurgery.asset | Treatment / 3 / 1 | exhausts | CureSymptomEffect Tier3 | Surgical, Emergency (both exist) | 12 |

Rules and flavour text are exactly as briefed. Every price matches the expected CalculateDraftValue().

**Changed:**
- `Assets/Data/Actions/Draft/Card_GratefulPatient.asset`: apCost 0 to 1, exhaustsOnUse true, rulesText "Gain 3 Reputation. Exhausts." (+3 Rep kept). **Price 10 to 8.** Its upgrade link to Philanthropist (cost 10) is unchanged.
- `Assets/Data/Actions/Upgrade/Draft/Card_Philanthropist.asset`: GainReputationEffect 6 to 4, exhaustsOnUse true, rulesText "Gain 4 Reputation. Exhausts." Price 13 (unchanged).
- The re-save removed stale legacy fields (apGenerated, isDiagnostic, etc.) from both cards' YAML. Those fields are no longer in ActionCardSO, so this is harmless.
- `reputationReward` on all 45 patients in `Assets/Data/Patients/`: 15 Walk-In (T4/T5) to 0; Ambulance T3 (8) 4-5 to 1; Ambulance T2 (7) 6 to 2; Trauma Flight T2 (6) 6 to 2; Trauma Flight T1 (9) 6-9 to 3. Every patient fit the mapping.
- Re-save side effects: 30 patient files gained `arrivesWithHandover: 0` (the lead's new field, written with its default). 3 older files had their `incomingCategories` hex rewritten to Unity 6.6's `...i` format, which the other 42 already used. No values changed.

**Verified:** independent read-back through eval (fields, nested effect trees, sub-asset status, prices), on-disk YAML diff, and the console (0 asset or compile errors).

**Not verified / for others:**
- No play-mode check of the new cards. Things for the lead and QA to watch: Sharps Bin drop-on-hand-card targeting; Ward Round when no Ward patient has a hidden Tier 1 symptom (WardWide CanApply); Double Shift's Rep gate.
- The simulator (`Tools/BalanceSim`) has not been re-run with the new Rep rewards. That is the lead's job.
- The 12 supply piles are not wired on DraftManager (step 5, lead).

## QA (cc-unity-qa-tester)

**Files added or changed (tests only):**
- `Assets/Tests/EditMode/SupplyRoomTests.cs` (new): Walk-In piles unlock full on the first draft and Ambulance/Trauma stay locked; Ambulance unlocks full (not +1); Trauma unlocks every stage; restock +1 capped at max (parameterised 0–3 bought, plus repeated restocks); locked piles never restock; reserve/unreserve stock and cart; locked, empty and out-of-range piles refused; unreserve with an empty cart fails; CommitCart gives one entry per copy and empties the cart; no unreserve after commit; OpenDraft clears a leftover cart; Price = CalculateDraftValue; null configs and configs without a card are skipped.
- `Assets/Tests/EditMode/ReputationCostTests.cs` (new): `DraggableCard.CanAffordReputation` (8 cases: cost 0 or negative always true; cost 2 at Rep 0/1/2 false, 3/20 true).
- `Assets/Tests/EditMode/SubCardSlotsTests.cs` (new): `PatientCardView.SubCardSlots` (the 6 briefed cases plus tied tiers [T2,T2] → [-1,0] and 5 symptoms → [-1,1,-2,2,0]).
- `Tools/BalanceSim.Tests/EngineTests.cs`: `Doctor_plus_nurse_grants_one_team_assessment_per_shift` now admits the patient first.
- `Tools/BalanceSim.Tests/SupplyRoomEngineTests.cs` (new): first draft unlocks only Walk-In piles; buying decrements stock and charges Funding until empty (forced buy on an empty pile does nothing; purchases join the deck); restock +1 per draft capped at max; reaching Ambulances unlocks those piles full; Reputation-cost card refused at Rep 1 and 2 (cost 2); Rep only charged when the card lands (refused on a Triage patient, then charged after admitting); handover patient has the primary first with DiagnosedCount 1 (plus a non-handover control); Team Assessment wasted when the only patient is in Triage; only Ward patients offered, and a forced pick on a Triage patient does nothing.

**Results:**
- Unity EditMode: 130/130 passed.
- Simulator: 35/36 passed. **1 failure, not caused by these tests:** `ProjectDataTests.Every_card_effect_is_understood`. `Card_SharpsBin.asset` uses `TrashCardEffectSO`, and `Tools/BalanceSim/src/Data/ProjectLoader.cs` doesn't map it, so it loads as `UnknownEffect` ("not simulated"). **For cc-card-engine-specialist:** add a Trash effect to the simulator (EffectDef plus a loader mapping) that mirrors `TrashCardEffectSO`. Until then the simulator never applies Sharps Bin's effect.

**Testability notes (no action needed):** `SupplyRoom` is plain C# and easy to test. The simulator's `Reputation` has a private setter, so the Rep-cost tests use `Config.StartingReputation` and can't cover Rep 0. Rep 0 is covered in EditMode instead.

## cc-ui-ux-specialist: supply-room layout and market card (done 2026-10-21)

All changes made through `unity command eval_file` (no YAML hand-edits). No C#, data or test files touched. **Scene not saved** (MainBoard is dirty; the lead saves).

**Prefab `Assets/Prefabs/UI/Draft_Market_Card.prefab`** (saved)
- Root: 190 x 294 (RectTransform and LayoutElement min/preferred).
- `Card` (UI_ActionCard): scale 0.75 → **0.62** (186 x 260), anchored top-centre.
- `InCartOverlay` renamed **`InCartBadge`**: no longer covers the card; a 174 x 30 green strip (0.08, 0.42, 0.24, 0.95) over the card's bottom (flavour-text line), raycastTarget off. Old "IN CART / click to undo" text removed. Children:
  - `InCartText` (TMP, Inter Bold 15, left) → `inCartText`.
  - `UndoButton` (58 x 22, own Image + Button, dark navy; child `Text` "Undo", Inter Bold 13) → `undoButton`. Being the first click handler under the pointer, its click does not reach the root buy Button.
  - Inactive by default.
- `PriceTag`: now a full-width 30 px strip at the bottom of the root (raycastTarget off). `PriceText` left-aligned, size 15; new **`StockText`** (TMP Inter Bold 14, right-aligned, light grey) → `stockText`.
- New **`LockedOverlay`** (186 x 260 over the card, colour (0.02, 0.03, 0.05, 0.8), blocks raycasts) with its own `CanvasGroup` (`ignoreParentGroups = true`) so the label stays full-strength while the root CanvasGroup fades the card to `lockedAlpha`. Child `LockedText` (TMP Inter Bold 20, centred, wraps) → `lockedText`. Inactive by default.
- Sibling order: Card, InCartBadge, LockedOverlay, PriceTag.

**Scene `MainBoard` → `Main_Canvas/DraftPhase_Panel/DraftMarketContainer`** (unsaved)
- HorizontalLayoutGroup removed. RectTransform: centre anchors, pos (0, -78), size **1230 x 600** (sits under the tab buttons, which end at y≈230, and left of NEXT SHIFT, which starts at x=660).
- Added transparent `Image` (raycast catcher) + **`ScrollRect`** (vertical only, Clamped, scroll sensitivity 30). With 12 piles the content is exactly 600 tall, so it doesn't scroll; more piles later would scroll.
- New child `Viewport` (stretch, `RectMask2D`) → `Content` (top-stretch, pivot top) with **`GridLayoutGroup`** (cell 190 x 294, spacing 14 x 12, Fixed Column Count 6, Upper Centre) + `ContentSizeFitter` (vertical Preferred).
- **`DraftManager.marketContainer` now points at `DraftMarketContainer/Viewport/Content`**. `DeckUpgradeManager.draftMarketContainer` (tab toggle) is unchanged and still the `DraftMarketContainer` object.

**Checked:** spawned 12 temporary prefab copies into Content via eval (Setup + SetPileState with a mix of in cart ×1/×2, out of stock, unaffordable and locked), captured the Game view, then destroyed them (0 stray objects; the tab containers' active states restored to how they were). The grid fits in 6 x 2 without covering the tabs or NEXT SHIFT. Console: no errors other than my own tool-call mistakes. To capture the screen-space UI I had to focus the Game view window.

**Open issues / for the lead**
- No stage headings: pile order (4 Open, 5 Ambulance, 3 Trauma) doesn't line up with 6-column rows, and headings would need a 3-row layout that doesn't fit without scrolling. The locked overlay text already names the stage. If headings are wanted, the options are a scrolling 3-section layout (one grid per stage) or 5 columns.
- At 0.62 scale the rules text is about 9 px at 1080p: readable but small. A hover zoom (enlarge the hovered card, or show it at `PreviewCardSpawnPoint`) would help. It needs a small UI script; ask me if you want it.
- `SetPileState` leaves the price visible on locked piles (it fades with the card). Consider clearing or hiding `priceText` while locked (C#, lead).
- `×` in "In cart ×N" is not in the static Inter Bold atlas. TMP falls back to LiberationSans SDF (the default font asset), so it renders but in a different typeface. Either add U+00D7 to the Inter atlas or use "x".
- Note: `GameOverScreen` is active in the open scene (it shows over the draft panel in the editor capture). I didn't touch it.

## Drag preview (QA)

**File added (tests only):** `Assets/Tests/EditMode/DragPreviewTests.cs` (36 cases, category `DragPreview`). No production code touched. All SOs via `CreateInstance` and all GameObjects destroyed in TearDown.

- **PlayZonePreview:** Medical Student "+1 AP"; Double Shift "+3 AP\n-2 Rep"; Nursing Student Choice(heal 2 | draw 1) "+1 Card"; Shift Handover (cardsToDraw 2) "+2 Cards"; Blood Bank WardWide(heal 3) + exhaust "Ward: +3 Stability each\nExhausts"; Stat Labs Diagnose at 50% "" (no lone % line); Initial Assessment "". Extras: "Exhausts" alone is not shown; "+2 AP\n75% chance"; a null effect entry is skipped; DrawCards Describe singular/plural; GainReputation Describe "+2"/"+0"/"-2 Rep"; Choice Describe "+2 Stability or +1 Card".
- **IsValidTarget:** Diagnose T1 valid on a Ward patient with a hidden Tier 1 next; invalid in Triage (untagged parent), with no parent, when fully diagnosed, when the next hidden tier is Tier 2 (T2 diagnose valid as a control), and for null. Heal invalid at full Stability, valid when hurt. Choice(heal | draw) invalid at full, valid when hurt. GainAP never valid (hurt patient or null). CureSymptom follows CanCureWith (tier and used department).
- **CanCureWith:** `DepartmentSlot` builds fine in EditMode (AddComponent; Awake/Start don't run, `isAvailable` defaults true, `MarkAsUsed` works). Valid when fully diagnosed in a matching available department at or below the tier; invalid when the tier is too low, the department is used, not fully diagnosed, the category doesn't match, the patient is in surgery, or the parent is a Ward bed.
- **Outline:** SetTargetable(true) shows it in `targetableOutlineColour`; SetHighlight(true) restores the outline's original colour; SetHighlight(false) keeps it shown in the targetable colour; SetTargetable(false) hides it. Highlight alone uses the original colour; a null `highlightOutline` doesn't throw.

**Results:** Unity EditMode 166/166 passed (130 before + 36 new). Console: no compile errors (2 unrelated errors from someone's `unity command capture` call, which doesn't exist).

**Not covered:** `WardWideEffectSO.CanApply` / Ward Round glow (needs `GameManager.Instance.boardManager`), and the `DraggableCard` code that calls `SetTargetable` on each patient during a drag (PlayMode / UI). No PlayMode tests run.

## Drag preview (UI) — cc-ui-ux-specialist (2026-10-21)

**Changed**
- `Assets/Scripts/UI/PlayZoneView.cs`: new `[SerializeField] TextMeshPro previewText`, `previewColor` (white) and `warningColor` (1, 0.45, 0.4). When a drag starts and the zone shows (no-target card), the label shows `card.PlayZonePreview()`, or "Not enough AP" (`CurrentAP < apCost`) / "Not enough Reputation" (`!DraggableCard.CanAffordReputation`) in the warning colour. Text alpha follows the zone's fade as `clamp01(zoneAlpha / normalColor.a)`, so it is at full strength once the zone reaches its normal alpha, and it snaps to the target alpha once the fade settles. Cleared (empty, alpha 0) in `Awake` and on drag end. Existing sprite/highlight behaviour is unchanged.
- Scene `MainBoard` (through `unity command eval_file`, **scene not saved**): new child `Core/PlayZone/PreviewText` (TextMeshPro, Inter_18pt-Bold SDF with the "Inter_18pt-Bold SDF - Outline" material, size 3 with auto-size 1.6 to 3 like the patient Stability text, centred, wrapping on, rect 2.3 x 3.0, raycast off). It cancels the PlayZone's rotation (z 90) and its non-uniform scale (local rotation 270, local scale 0.5 x 0.6), so the world scale is 1 with no skew. Sorting is "Player Hand" order -9: above the zone sprite (-10) and below hand cards (SortingGroup 0+, +100 while dragged). Assigned to `PlayZoneView.previewText`. Left with empty text and alpha 0.

**Checked**
- Recompiled with no errors or warnings.
- In edit mode, rendered the board with a temporary camera using "+3 AP / -2 Rep / Exhausts" (white) and "Not enough Reputation" (warning). Both read clearly and sit centred in the zone; the long warning wraps to two lines. Then restored the empty, invisible state.

**Not verified**
- Not tested in play mode with a real drag: the fade timing, the clear on drop, and how it looks beside a dragged card over the zone. Lead: please check this in play mode after saving the scene.
