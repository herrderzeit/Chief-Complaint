# Symptom upgrade tokens

## Plan (lead: cc-card-engine-specialist)

**Goal:** card upgrades cost department-coloured *upgrade tokens* as well as Funding, so how a deck upgrades depends on which symptoms the player treats.

**Decisions (user approved):**
- **Earning:** treating a symptom in a department earns 1 token of that department's colour (`SymptomCategory`):
  - Cardiology (`Cardiopulmonary`), blue
  - Internal Med (`Internal`), orange
  - Isolation (`Infectious`), green
  - Trauma, red
- **Cost:** a Tier 1→2 upgrade costs **1 token + 3 Funding**; a Tier 2→3 upgrade costs **2 tokens + 5 Funding**. Funding costs are halved from 5 / 10.
- **Colour per line:**

  | Line | Token colour |
  |---|---|
  | Initial Assessment → Comprehensive Workup → Consulting Specialist (diagnosis) | Internal |
  | Standard Care → Specialised Care → Surgical Intervention (treatment) | Trauma |
  | IV Saline → Fluid Resuscitation → Broad Spectrum Stabiliser (fluids) | Cardiopulmonary |
  | Nursing Student → Registered Nurse → Chief Nurse | Infectious |
  | Medical Student → Licensed Doctor → Chief of Medicine | any colour |
  | Grateful Patient → Philanthropist | any colour (2 tokens, a T2→T3 step) |

- **Any-colour costs** are paid from the colour the player holds most of.

**Built by the lead (done, compiles):**
- `Core/UpgradeTokenWallet.cs`: `Add`, `Get`, `Total`, `CanPay(cost, anyColour, colour)`, `TryPay`, `ColourName`.
- `GameManager`: `UpgradeTokens`, `AddUpgradeTokens(colour, n)`, `CanAffordUpgrade(card)`, `TryPayForUpgrade(card)`, event `OnUpgradeTokensChanged(SymptomCategory, int)`.
- `ActionCardSO`: `upgradeTokenCost` (default 0 = no tokens), `upgradeTokenAnyColour`, `upgradeTokenColour`.
- `PatientCardView.TryCureSymptom` awards the token, so both cure cards and paging the on-call team earn one.
- `DeckUpgradeManager` checks and pays tokens and Funding; `UpgradeCostText(card)` gives text like "3 Funding + 1 Trauma token".
- `Tools/BalanceSim` mirrors all of the above.

**Steps:**
1. cc-medical-data-scraper: set the token fields and halved Funding on every card that has a `directDraftUpgrade`.
2. cc-ui-ux-specialist: token counter in the HUD, and the tokens held shown on the Upgrades tab.
3. cc-unity-qa-tester: tests for the wallet, GameManager upgrade payment, the cost text, and simulator parity.
4. Lead: review, simulator balance run, play-mode check.

## Step 1 done (cc-medical-data-scraper)

I changed the assets through the connected Editor (`eval_file` with SerializedObject, then `AssetDatabase.SaveAssets`). No YAML was edited by hand. I found 11 cards under `Assets/Data/Actions` with a `directDraftUpgrade`, and all of them fitted the tier rule.

| Card | Tier | Upgrades to | Funding | Tokens | Colour |
|---|---|---|---|---|---|
| Initial Assessment | 1 | Comprehensive Workup | 3 | 1 | Internal |
| Comprehensive Workup | 2 | Consulting Specialist | 5 | 2 | Internal |
| Standard Care | 1 | Specialised Care | 3 | 1 | Trauma |
| Specialised Care | 2 | Surgical Intervention | 5 | 2 | Trauma |
| IV Saline | 1 | Fluid Resuscitation | 3 | 1 | Cardiopulmonary |
| Fluid Resuscitation | 2 | Broad Spectrum Stabiliser | 5 | 2 | Cardiopulmonary |
| Nursing Student | 1 | Registered Nurse | 3 | 1 | Infectious |
| Registered Nurse | 2 | Chief Nurse | 5 | 2 | Infectious |
| Medical Student | 1 | Licensed Doctor | 3 | 1 | any |
| Licensed Doctor | 2 | Chief of Medicine | 5 | 2 | any |
| Grateful Patient | 2 | Philanthropist | 5 | 2 | any |

- The other 25 action cards already had `upgradeTokenCost` 0, so nothing changed there. Their stale `draftUpgradeCost` values are untouched.
- Unity also re-serialised the files it saved. Some diffs show `reputationCost: 0` written out and long `flavorText` lines unwrapped. Neither changes any value.
- Console: 0 errors, 0 warnings.
- Simulator (`report --runs 3000`): smart 96.7%, randombuy 82.5%, stall 87.9%, random 61.0%, nobuy 43.3%. Single-focus win rates: Initial Assessment line 87.1%, Nursing line 77.2%, IV Saline line 62.9%, Medical Student line 52.1%, Standard Care line 43.1% (baseline 41.7%). The Grateful Patient > Philanthropist focus bought 0 items. That bot never owns a Grateful Patient, so the row only repeats the baseline.
- For the lead: the Standard Care line barely beats the baseline, and Initial Assessment ties for the strongest purchase. Consider this when you do the balance pass.

## QA (cc-unity-qa-tester)

**Files added (tests only, no production code touched):**
- `Assets/Tests/EditMode/UpgradeTokenTests.cs` (55 cases, category `Economy`)
- `Tools/BalanceSim.Tests/UpgradeTokenEngineTests.cs` (13 cases)

**EditMode coverage:**
- `UpgradeTokenWallet`: empty wallet, `Add` default/one-colour, `Add` ignores 0 and negatives, `CanPay` boundaries (cost ≤ 0 always true, exact colour ignores other colours, any-colour uses the total), `TryPay` exact colour, short of colour / short in total deducts nothing, zero cost, any-colour drains the largest pile, re-checks the largest pile per token, ties go to the lowest enum value (Trauma < Cardiopulmonary < Infectious < Internal), `ColourName` for all four.
- `GameManager`: `AddUpgradeTokens` + event, `CanAffordUpgrade` (null card, no `directDraftUpgrade`, Funding boundary 6/7, wrong colour), `TryPayForUpgrade` short on Funding → nothing paid and no events, short on tokens → nothing paid and no events, both → both paid and both events, any-colour from the largest pile, token cost 0 → Funding only, card asset not mutated.
- `DeckUpgradeManager.UpgradeCostText`: all formats incl. Cardiology/Isolation names and "5 Funding" when the token cost is 0 even with any-colour set.
- `PatientCardView.TryCureSymptom`: feasible in EditMode. The test registers its GameManager as `Singleton<GameManager>.Instance` via reflection on the private setter (Awake does not run in EditMode) and restores the previous value in TearDown. Success awards one token of the department's colour (all four); RNG failure, wrong department, department already used, tier too high and undiagnosed award none.

**Simulator coverage:** treating with a card earns a token of the department's colour (all four); paging also earns one; RNG failure earns none; an upgrade with a token cost is not offered without the right colour, is refused (no Funding taken) when applied anyway, is not offered / keeps tokens when short on Funding, pays tokens and Funding when both are held; any-colour pays from the most plentiful colour and the tie rule matches the wallet; token cost 0 upgrades on Funding alone.

**Results (2026-10-21):** Unity EditMode 221/221 passed (includes the 55 new cases); `dotnet test Tools/BalanceSim.Tests` 49/49 passed (includes the 13 new).

**Notes for the lead / testability gaps:**
- `Singleton<T>.Instance` has a private setter, so any EditMode test that needs `GameManager.Instance` must use reflection. A small test seam (for example an `internal` setter with `InternalsVisibleTo("EditModeTests")`) would make this less brittle. Owner: cc-card-engine-specialist.
- `GameManager.TryPayForUpgrade` raises `OnUpgradeTokensChanged` for all four colours even when the token cost is 0; harmless, but listeners refresh four times per upgrade.
- No loader test reads `upgradeTokenCost` / `AnyColour` / `Colour` from the real assets yet, because the data pass (step 1) was in progress. Once it lands, a `ProjectDataTests` case could assert that every card with a `directDraftUpgrade` has a token cost of 1 (T1→T2) or 2 (T2→T3) and Funding 3 / 5.
- PlayMode tests were not run.

## UI (cc-ui-ux-specialist)

**Done (compiles, console clean; scene changed in the Editor but NOT saved; the lead saves `MainBoard.unity`):**
- New `Assets/Scripts/UI/UpgradeTokenDisplay.cs` (`ChiefComplaint.UI`): a serialised `Badge[]` (colour, pip `Image`, count `TextMeshProUGUI`). Subscribes to `GameManager.OnUpgradeTokensChanged` in `OnEnable` (retried in `Start`, since the GameManager singleton may not have run `Awake` yet), unsubscribes in `OnDisable`/`OnDestroy`, and re-reads every colour from `GameManager.UpgradeTokens` whenever it is enabled, so the hidden Upgrades tab is never stale. Pip colours come from `DepartmentFallbackButton.DepartmentColour`, so they match the department plates. A colour the player holds none of is dimmed to 35% alpha so held colours stand out. `ShowCount(colour, n)` is public for previews. `MainHUD.cs` is unchanged.
- **HUD:** `Main_Canvas/HUD_Panel/UpgradeTokens_Display` (sibling index 3, between Funding and Shift, grouped with the economy readouts): "Tokens:" label plus four badges, `Token_Cardiology`, `Token_InternalMed`, `Token_Isolation`, `Token_Trauma` (a 16 px circle from the built-in `UI/Skin/Knob` sprite plus the count). The labels use the HUD font (ShareTechMono, 20). It fits the top bar: with worst-case strings ("Escalation: Trauma Flights", "CODE BLUE: 2 Critical!", "Patients Cured: 25", two-digit counts) every item still gets at least its preferred width, with about 40 px spare each.
- **Upgrades tab:** `DraftPhase_Panel/UpgradesContainer/UpgradeTokens_Display`: a dark strip ("Tokens held:" plus the same four badges) anchored top-centre just above the list, in the 33 px gap below the Market/Upgrades/View Deck tabs. It is a child of the container, so it shows and hides with the tab. `DeckUpgradeManager` only clears `Content`, so the strip is safe from list rebuilds.

**Checked:** recompile was clean. In edit mode I set temporary values (worst-case HUD strings; tokens 12/0/3/7; the Upgrades tab shown; `GameOverScreen` hidden), captured the Game view, then restored all of it (texts, the active states of the draft containers, `GameOverScreen` active, counts back to 0) and deleted the temporary capture assets.

**Not verified:** live updates in play mode (lead's play-mode check: cure a symptom and confirm that colour's count goes up in both places; buy an upgrade and confirm the counts go down).

**Notes for the lead:** `GameOverScreen` is active in the saved scene (it covers the board in edit-mode Game view captures), and the console shows pre-existing "GameManager: No GameOverScreen assigned in the Inspector!" warnings, which may be worth a look. Neither was changed here.
