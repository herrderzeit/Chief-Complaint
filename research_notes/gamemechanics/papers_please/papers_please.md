# Papers, Please hides its pressure in data rows

Papers, Please runs on a small, data-driven engine. Each day is one row of settings: active rules, unlocked mechanics, shift length, a count of random entrants and a short list of hand-written "traveller" templates. A traveller fixes only what its story needs, and everything else is generated "under the current rules". Every generated entrant has **at most one deliberate error**, and every error is a mismatch between two facts. Pressure comes from five stacked layers. Rules arrive one at a time and stay. A shift clock caps throughput. Wages are lost to citations. A family budget turns those wages into survival. Consequences come back later, through returning travellers and the next morning's newspaper.

For Chief Complaint, the important finding is that the game's weight comes from one **speed-against-accuracy decision**, repeated many times, not from its many systems. Chief Complaint does not have that decision yet. Full diagnosis is mandatory before a patient can enter a department, so the player can never choose to check less. The roadmap below therefore starts with one mechanic that creates that choice: an **Early Discharge** card whose risk is a delayed bounce-back. It needs no new screen, dialogue or shift phase. Each later Papers, Please idea is a separate step that can be playtested alone. Each step maps to a piece of the shelved `experimental/bureaucracy-update` branch, which failed because seven of these steps arrived together.

## One generator serves filler, story characters and endless mode

The strongest evidence comes from Lucas Pope's own development log and from the community's documentation of the game's data files. The first prototype was **"100 procedurally-generated immigrants"**, built from 7 passport styles and 8 faces per gender. Names were harvested from Wikipedia name lists ([Pope devlog #33, #35](https://dukope.com/devlogs/papers-please/tig-01)). Faces are drawn whole, cut into colour-masked parts and recombined at runtime ([Pope devlog #232](https://dukope.com/devlogs/papers-please/tig-03)). In the data, a face is a gender plus five part indices, nationality can be a weighted list, and any name part left blank is filled at random ([modding doc: Travelers.txt](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Travelers/)).

The day schedule lives in `Days.csv`. Each row holds `RULES`, `FEATURES`, `COUNT` (random entrants: 5, 7, 8, 9, 9 … over days 1–10), `TRAVELERS` (named templates), `NEWS`, `BULLETIN`, tutorial pages, `DURATION` (2, 4, 6, 7, 7, 7, 7, 7, 5, 5) and a `RUNNER` attack flag ([modding doc: Days.csv](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Days/)). Three things follow from that structure. Shift length is a pacing dial separate from crowd size. The tutorial is attached to the day, not to the mechanic. The quieter, shorter late days raise pressure, because the rulebook keeps growing while the time shrinks.

Scripted and random entrants come from the same generator. A traveller template overrides only some fields (face, nation, a forced error, items, dialogue), and Pope told Game Developer that any attribute left unspecified "is filled in randomly under the current rules" ([Game Developer, 2013](https://www.gamedeveloper.com/design/designing-the-bleak-genius-of-i-papers-please-i-)).

Story continuity rests on a handful of **string flags**, such as `Game/Ezic` and `Game/MrPersistent`. Later templates test these flags and set them. When a thread is finished, its slot uses `fallback GENERIC` and becomes an ordinary random entrant ([modding doc: Travelers.txt](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Travelers/)). So the daily entrant count stays stable whatever the player chose. Jorji Costava recurs until he is approved. A husband's outcome changes his wife's scene the next day. A traveller's `@ News` action writes the next morning's headline.

Pope described the story as "pieces" arranged on a grid of days. Most threads are 1–3 encounters long, two or three run longer, and one spans the whole game ([Pope devlog #237](https://dukope.com/devlogs/papers-please/tig-03); [#535](https://dukope.com/devlogs/papers-please/tig-06)). He later regretted how rigid that grid was. Adding one encounter could break the rest of the schedule, and in hindsight he would have expressed dependencies so that events could move ([Game Developer, 2013](https://www.gamedeveloper.com/design/designing-the-bleak-genius-of-i-papers-please-i-)).

Reigns, a later card game, solves exactly that problem with a weighted "bag". Cards that don't fit the current state, or were seen recently, are removed from the bag. Active storylines get heavier weights. A sub-system can narrow the bag down to a single card for a linear moment ([Game Developer, Alliot 2016](https://gamedeveloper.com/design/game-design-deep-dive-creating-an-adaptive-narrative-in-i-reigns-i-)).

Errors are data rather than code. Pope's rule was that **"all errors are due to correlation"**: two pieces of the same information that don't match. He estimated about 35 comparable facts and roughly 600 possible fact pairs ([Pope devlog #62, #69](https://dukope.com/devlogs/papers-please/tig-01); [#179](https://dukope.com/devlogs/papers-please/tig-03)).

`Errors.txt` defines each error as a requirement, a fact check and an interrogation response. Requirements gate an error on the day's features or rules: `Passport-Missing`, for example, needs INSPECT to be enabled ([modding doc: Errors.txt](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Errors/)).

Pope capped each entrant at **one wrong thing**, because "more errors makes the game easier, not harder": any single flag justifies a denial ([Pope devlog #539](https://dukope.com/devlogs/papers-please/tig-06)). He also wanted many entrants to have no error at all ([#152](https://dukope.com/devlogs/papers-please/tig-03)). Difficulty is tuned by subtlety, not by error count. The game uses near-miss seals, partial-name errors and passport numbers that differ by a single digit ([#192](https://dukope.com/devlogs/papers-please/tig-03)).

Pope tested all of this by generating thousands of entrants at once ([Game Developer, 2013](https://www.gamedeveloper.com/design/designing-the-bleak-genius-of-i-papers-please-i-)). He also planned scripted "play style" bots on a separate fast-play front end, to test progression and money balance ([Pope devlog #535](https://dukope.com/devlogs/papers-please/tig-06)). That is the same role `Tools/BalanceSim` plays for Chief Complaint.

Endless mode reuses the generator with no traveller list and three scoring types ([Pope devlog #342](https://dukope.com/devlogs/papers-please/tig-05); [#483](https://dukope.com/devlogs/papers-please/tig-06); [Wikipedia](https://en.wikipedia.org/wiki/Papers,_Please)).

## Validity is a two-fact comparison against a rulebook on the desk

An entrant is a record of true attributes: name, sex, date of birth, nationality, face, height, weight, fingerprints, purpose and duration. The entrant also carries documents, and each document holds a subset of those attributes plus issuer metadata: issuing city, seal, expiry date and document number.

The rulebook holds the **reference tables** used for the checks: valid issuing cities, districts and seal specimens. Generated misspellings of city names are plausible near-misses, not random noise ([Fandom: Discrepancy](https://papersplease.fandom.com/wiki/Discrepancy); [Fandom: Arstotzka](https://papersplease.fandom.com/wiki/Arstotzka)).

Documents join the requirement set over about 31 days, in this order according to one third-party guide:

| Day | Change |
|---|---|
| 1 | Passport only |
| 3 | Entry ticket |
| 4 | Entry permit and ID card |
| 6 | Work pass |
| 8 | Diplomatic authorisation |
| 13 | ID supplement |
| 14 | Wanted list |
| 18 | Reason-for-denial stamp |
| 26 | Polio vaccination |
| 27 | Access permit, which merges two older documents |

([sort-the-court.com documents guide](https://www.sort-the-court.com/games/papers-please/documents-guide)). The rules are **cumulative and declarative**. The morning bulletin announces what changed, and the rulebook keeps the whole set available to look up. Pope stopped announcing rules on days when another character was also talking to the player, because testers ignored the bulletin ([Game Developer, 2013](https://www.gamedeveloper.com/design/designing-the-bleak-genius-of-i-papers-please-i-)).

The check itself is a verb. In INSPECT mode the player highlights **two facts**. These can be two document fields, a field and a rulebook line, or a field and a physical reading such as the booth scale or the face. If the facts conflict, the game confirms a discrepancy and unlocks interrogation. If they agree, or the conflict has been explained away, it reports "Discrepancy cleared" ([Fandom: Inspection mode](https://papersplease.fandom.com/wiki/Inspection_mode); [Fandom: Discrepancy](https://papersplease.fandom.com/wiki/Discrepancy)).

Interrogation drives a small state machine. A flagged entrant either produces the missing document, gives an answer that clears the discrepancy, or leaves it standing. A standing discrepancy unlocks fingerprinting, a body search and detention. Detention generally requires interrogation first ([Fandom: Search](https://papersplease.fandom.com/wiki/Search); [Fandom: Detention](https://papersplease.fandom.com/wiki/Detention)). Discrepancies fall into two classes. **Minor** ones can be resolved by talking. **Severe** ones justify detention ([Fandom: Discrepancy](https://papersplease.fandom.com/wiki/Discrepancy)).

Wrong outcomes are punished quickly and in proportion. A **citation arrives by fax** moments after the entrant leaves and names the rule that was broken. Pope called that immediacy essential ([Pope devlog #424](https://dukope.com/devlogs/papers-please/tig-05)). The first two citations each day are warnings with no fine, but the entrant's 5-credit wage is still lost. Later citations cost money ([Fandom: Citation](https://papersplease.fandom.com/wiki/Citation); [Wikipedia](https://en.wikipedia.org/wiki/Papers,_Please)). About five citations in one day ends that night, and Pope deliberately closed the "deny everyone" and "approve everyone" exploits ([Pope devlog #381, #424](https://dukope.com/devlogs/papers-please/tig-05)).

Translated into Chief Complaint's terms, the model is simple. A patient is a set of true facts. The card face shows a subset of them. Diagnosis is the tool that brings a hidden fact into view. A rule is a predicate checked against facts the player can always see.

## Five stacked pressures, and why follow-on games went shallow when they removed one

The pressure in Papers, Please is a triangle of throughput, accuracy and mercy, held in place by money. Pay is per correctly processed entrant within a real-time shift. Rent is mandatory, while food and heat are optional. Unpaid needs make family members sick, sickness can kill them, and debt ends the game ([Wikipedia](https://en.wikipedia.org/wiki/Papers,_Please); [ludo.guide family expenses](https://www.ludo.guide/guide/papers-please/family-expenses); [Fandom: Bills](https://papersplease.fandom.com/wiki/Bills)). Medicine "doesn't immediately cure sickness", so neglect is slow to undo ([Pope devlog #424](https://dukope.com/devlogs/papers-please/tig-05)).

Bribes and side deals become tempting exactly because the budget rarely closes. Pope kept the family screen to a few lines of text so that players would project their own family onto it ([Game Developer, 2013](https://www.gamedeveloper.com/design/designing-the-bleak-genius-of-i-papers-please-i-)).

Mechanics are introduced one at a time. Day 1 in the data is "Arstotzkans only, No inspection, No errors". Correlation follows on day 2, missing papers on day 3 and a rule tutorial on day 7. Fingerprinting, search and detention are switched on per day through `FEATURES` ([modding doc: Days.csv](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Days/)). Even so, Pope found the step from stamping to inspection on day 3 "too big" in testing ([Pope devlog #367, #370](https://dukope.com/devlogs/papers-please/tig-05)).

He cut features freely, including baggage inspection, UV seals and gunfights. One cut is especially relevant here: a light showing when the supervisor was watching, removed because players were "clicking it like a button" ([Kill Screen, 2013](https://www.killscreen.com/peek-behind-scenes-papers-please-reveals-baggage-search-and-gun-fights/)). A status display with no decision behind it turned into a toy.

Consequences arrive late. Most of the 20 endings are fail states that are always within reach, such as debt, a dead family or arrest. The story endings are checked against a few counters at fixed late-game points ([Pocket Gamer endings guide](https://www.pocketgamer.com/papers-please/endings/)). Smaller threads pay off through returning travellers and headlines. For example, if the player does not arrest the pimp, his associates kill people off-screen, and the player learns about it later ([Pope devlog #381](https://dukope.com/devlogs/papers-please/tig-05)).

Later games that copied the formula show which of these legs carry the weight. Death and Taxes dropped the clock, the household and rule layering. Reviewers called it shallow, with "little reason to spend money" ([Gameluster](https://gameluster.com/review-death-and-taxes-afterlife-bureaucracy/); [Destructoid](https://www.destructoid.com/reviews/review-death-and-taxes/)). Quarantine Zone's medical examination tools were praised, but PC Gamer scored it 60/100 because the management layer around them is thin ([PC Gamer](https://pcgamer.com/games/sim/quarantine-zone-the-last-check-review)).

War Hospital is the closest analogue to Chief Complaint, because it presents patients as triage cards. Its developers cut the card down to key stats at the top. They also found that labelling some patients as bad people made players treat everyone as a resource. They answered with neutral humanising details instead, such as a pet rat or a waiting spouse ([Game Developer, 2024](https://www.gamedeveloper.com/pc/what-devs-tackling-morality-can-learn-from-war-hospital-s-card-system)).

Critics generally read Papers, Please's tedium as intentional procedural rhetoric ([Cleger, *Digital Culture & Education* 2015](https://doaj.org/article/b390103419504ed29881ff6dea18e143)). Chief Complaint's fantasy is competence under pressure, not dehumanisation, so it should borrow the tension and drop the tedium.

## Mapping each concept onto Chief Complaint's existing systems

Chief Complaint already has most of the Papers, Please skeleton. It lacks the core decision. Symptoms are dealt face down. `DiagnoseEffectSO` reveals them one at a time, with tier gating (`PatientCardView.TryDiagnoseNext`), which is the INSPECT step. `PatientCardSO.chiefComplaint` and `clinicalTags` play the part of the "stated" facts. The drawn symptoms play the part of the "true" facts, and `PatientDeckManager.DrawSymptomForCategory` already prefers symptoms whose tags overlap the patient's.

There is a decisive difference, though. `DraggablePatient` refuses to place a patient in a department until `isFullyDiagnosed` is true ("Patient needs full diagnosis!"), and it blocks the wrong department outright. Inspection in Chief Complaint is therefore **mandatory and cannot fail**. The player never weighs checking more against moving faster. That missing choice is the most important finding of this mapping. It is also why the first step below differs from a "spot the mismatch" mechanic: a mismatch the game forces you to uncover anyway carries no decision. It would repeat the supervisor-light mistake.

| Papers, Please concept | Chief Complaint mechanic | Existing hook in this repo | Shelved-branch piece | Depends on |
|---|---|---|---|---|
| Approve without full inspection; wrong approvals are punished later | **Early Discharge**: an action card played on a Ward patient (a low-acuity Walk-In) who has at least one symptom diagnosed and only Tier 1 symptoms revealed. It discharges them at once with a reduced payout, skipping the department. Any symptom still face down returns next shift as a bounce-back patient | Ward-only diagnosis in `PatientCardView.TryDiagnoseNext`; the cure payout in `PatientCardView` (which already applies a −1 modifier to barely-stable discharges); `PatientDeckManager.activePatientDeck`; a new `CardEffectSO` subclass | `IntakeManager` Admit stamp (M3), without the form or the dialog | Nothing |
| At most one error per entrant; most entrants clean; difficulty from subtlety | **Atypical presentation**: at most one hidden symptom per patient sits outside the chief complaint's category or is a higher tier than the triage level suggests; most patients are clean | `GenerateSymptomsForPatient`, `incomingCategories`, `DetermineTierFromTriage`, `clinicalTags` | `IntakeForm.ComplaintMismatch`, `IntakeFormGenerator.DiscrepancyChance` (20% rising to 35%) | Step 1 (otherwise the mismatch is invisible) |
| Rules switched on per day through `FEATURES`/`RULES`, always in the rulebook | **Standing clinical protocols**, one introduced at each escalation boundary and shown permanently on the HUD | `ambulanceShiftThreshold` (5), `traumaFlightShiftThreshold` (10), `shiftsToWin` (15), `OnEscalationPhaseChanged` | `DirectiveSO`/`DirectiveManager` (M2), but persistent rather than one-shift, and with no `MorningBriefing` dialog | Steps 1–2 for most protocol ideas |
| Citation by fax: two free warnings, then penalties | **Protocol breaches**: an immediate on-board notice; two per shift are free, then a Red Tape card goes into the deck | `ActionCardType.RedTape`, `DeckManager.SpawnRandomRedTape`, `CleanseRedTapeEffectSO`, `NotificationManager` | `CitationManager`/`CitationLedger` (M1), with the Funding fine tier removed | Step 3 (needs a rule to break) |
| Travellers carrying flags; `fallback GENERIC`; Reigns weighted bag | **Recurring patients and short threads**: conditional, weighted patient cards that fall back to generic patients | `DrawRandomPatient` (currently uniform, without replacement), `PatientTrigger` (`OnCured`, `OnDeath`, `AddCardToDiscard`) | `HeadlineGenerator`'s per-shift event record (M6) | Step 1's bounce-back insertion |
| Morning newspaper reflecting yesterday's choices | **Staff room noticeboard**: one line about the last shift, shown on the existing staff room screen | `StaffRoomManager.OpenStaffRoom`, `OnShiftChanged` | `HeadlineWriter`, `HeadlineGenerator` (M6) | Step 5 for content worth reporting |
| Family budget and the bribe temptation | **Optional staff room upkeep and an occasional single-choice offer**, both inside the staff room | `GameManager.TrySpendFunding`, `CurrentFunding`, market and upgrades | `UpkeepManager`/`UpkeepRules` (M5), `DilemmaSO` (M6) | Steps 1–4 stable in BalanceSim |
| Attacks, runners and authored day events | Already present as **Code Blue** pressure | `BoardManager.OnCodeBlueCountChanged` | none | none |
| Endings checked against counters; inspections | Department accreditation and Board inspections as a win condition | `shiftsToWin` timer win in `GameManager` | `AccreditationManager`, `InspectionRules` (M4) | Keep shelved |
| Endless mode unlocked by an ending | **Do not port.** A reward that unlocks across runs edges into roguelike meta-progression | none | none | none |

Three cautions apply to the content of these steps. First, the shelved intake form checked **insurance expiry**. That is a region-specific idea, foreign to a Medicare-style system, and it should not return. A globally understood equivalent is an allergy band or a patient ID wristband that doesn't match the chart. Second, protocol and card text should use generic titles only: Doctor, Nurse, Surgeon, Paramedic. Third, following War Hospital, a returning patient's card should carry neutral human detail (who is waiting for them), never a "frequent flyer" or "drug seeker" label. The shelved `ComplaintMismatch` comment used "a possible drug-seeker", and that framing should not carry over.

## A seven-step roadmap where each step ships and is playtested alone

The shelved branch did not fail on any single idea. It added a `ShiftSequence` of start-of-shift and End Shift dialog steps (`BureaucracyDialog`, `MorningBriefing`) on top of seven rule sets at once. Every shift gained screens to click through before play could resume. Papers, Please introduces one feature at a time, and the shift-to-inspection jump still felt "too big" to Pope's testers.

Each step below therefore follows four rules. It adds **no new modal phase**. It reuses an existing hook. It can be modelled in `Tools/BalanceSim` before anyone playtests it. And it lives on its own branch until the user signs it off.

One more pacing point. With 15 shifts, "one new rule per shift" would rebuild the overload. New protocols belong at the existing escalation boundaries (shifts 5 and 10), the same way `FEATURES` switches on mechanics per day.

| Step | Mechanic | Main files touched | Playtest signal | BalanceSim change |
|---|---|---|---|---|
| **1 (first)** | Early Discharge card with bounce-back | New `EarlyDischargeEffectSO`; `PatientCardView` (a new Ward-side discharge trigger, plus the cure payout moved into a method shared with it); `PatientDeckManager` (insert a bounce-back patient at the next shift); one `ActionCardSO` asset | Players sometimes use it and sometimes don't; bounce-backs feel fair because the face-down count was visible | Add a "fast-track" bot policy; compare cure rate, Reputation and Funding against the never-buy bot |
| 2 | Atypical presentation (at most one per patient, roughly a fifth of patients) | `PatientDeckManager.GenerateSymptomsForPatient`; content tags from cc-medical-data-scraper | Diagnosing a risky-looking Walk-In feels like a judgement call, not a chore | Seed the rate; check that the fast-track bot's bounce-back rate rises as expected |
| 3 | First standing protocol at shift 5, second at shift 10, always shown on the HUD | Small `ProtocolSO` plus a manager listening to `OnEscalationPhaseChanged`; one HUD line (cc-ui-ux-specialist) | Players can name the active protocol without opening anything | Encode both protocols as predicates; check no shift becomes unwinnable |
| 4 | Protocol breaches: two warnings per shift, then Red Tape | Port `CitationLedger`; drop the Funding fine | Breaches read as immediate and specific, like the fax | Count breaches per bot; Red Tape stays within what `CleanseRedTapeEffectSO` can handle |
| 5 | Recurring patients and short threads through a weighted, conditional draw | Replace uniform `DrawRandomPatient` with a weighted bag plus a fallback to generic patients | A returning patient is recognised and remembered | Bag weights reproducible from a seed |
| 6 | Staff room noticeboard line | Staff room UI only | Read, not skipped; adds no clicks | None |
| 7 | Optional upkeep or one staff room offer per shift, never mandatory bills | Port a softened `UpkeepRules`, with a Funding floor | Spending Funding feels like a real trade-off, not a tax | Check that one bad shift cannot snowball into a lost game |

**Why Early Discharge comes first.** It is the only candidate that creates Papers, Please's central decision: inspect thoroughly at an AP cost, or approve quickly and risk a late penalty. It needs neither a new rule nor a citation system to mean something.

The mechanic has to live in the Ward. Diagnosis happens only there, and a department will not accept a patient until every symptom is diagnosed, so hidden symptoms never exist anywhere else. The card adds a new discharge trigger in the Ward. It reuses the existing cure payout, which already scales the reward down for barely-stable patients, and the face-down symptom count the player can already see. The risk is visible, but how bad it will be is not, the Reigns principle of showing which stakes a choice touches without showing their size ([TechRaptor](https://techraptor.net/content/reigns-review-addicted-death)). The bounce-back delivers the delayed consequence the way a returning traveller or the next morning's paper does. And it is a single `CardEffectSO` subclass, which is how this project's engineering rules say new behaviour should arrive.

If playtests show nobody uses the card, the cost is too high or the bounce-back too harsh. That is a tuning question, not a structural one.

**Things to keep shelved.** Steps 6 and 7 are optional, and accreditation and inspections should stay shelved unless the user revisits the 15-shift win timer. Mandatory bills recreate the punishment spiral the notes warn about: in Papers, Please, a fine cuts the heat money, a sick family member makes the next day harder, and a bad day snowballs. In Chief Complaint, Reputation already plays the role of life, so a second compounding drain on Funding would double that pressure.

## Where the evidence is thin or contested

Several sources were blocked or only partly readable, and the confidence varies by topic.

**The fan wiki.** papersplease.fandom.com returned HTTP 402 on every direct fetch, and its mirrors served bot checks. Every fact cited to the Fandom wiki above comes from search-result snippets of its pages, not from full reads.

**Pope's devlog.** The original TIGSource thread could not be fetched, but Pope's own mirror of it on dukope.com was read. That mirror is the primary source for the generation section.

**The data files.** The `Days.csv`, `Travelers.txt` and `Errors.txt` documentation covers only days 1–10 plus a test day. That matches the roughly 10-day public beta. The architecture very likely carried over to the release, but specific values such as entrant counts and durations may be beta-era.

**The document schedule.** The day-by-day schedule rests on one third-party guide, so treat it as medium confidence. Grant of Asylum is dated Day 14 in one source and Day 21 in another. The Day 7 "search Kolechians" and Day 11 "forged seals" entries come from that single source.

**Contested figures.** The citation escalation step is disputed: one source says each later citation adds another 5 credits, another says "5, 5, then 10, 10". Early rent figures differ between sources. Ending numbering and the EZIC threshold ("four" or "4–5" tasks) conflict between guides.

**One error per what?** Pope's "at most one thing wrong" per immigrant is a slightly stronger claim than the wiki's "at most one per category". The devlog is the primary source, but the wiki may describe later behaviour.

**Unknown values.** No reachable source gives the per-entrant error probability or how it changes over the days. The weight and height tolerances are also unknown. The 2015 GDC Game Narrative Review PDF could not be text-extracted.

**Wording and recommendations.** The "player as a cog" reading is critics' language. Pope's own word is the "dehumanising" side of bureaucracy. The applied lessons in this report are design judgement drawn from the notes, not sourced findings: one rule per escalation phase, scannable cards, avoiding the punishment spiral, and retiring checks once they become rote. They should be validated in BalanceSim and in playtests.

## Conclusion

Papers, Please feels oppressive mainly because of one decision, repeated under a clock, rather than because of how much bureaucracy it simulates. Strip the game down and what remains is a two-fact check, at most one error per person, a rulebook that is always to hand, and a penalty that lands just after the decision. Chief Complaint built the inspection tool (face-down symptoms and tiered diagnosis) and then made it compulsory. That removed the decision the tool exists to serve. The shelved branch then added the surrounding systems (forms, directives, bills, inspections) before the core decision existed, which explains why it felt like paperwork rather than pressure.

The way forward reverses that order: make checking optional and risky first, prove in BalanceSim and one playtest that the choice is interesting, and only then let protocols, breaches and recurring patients give that choice more texture. Pope's own regret also points to a structural advantage Chief Complaint can use. A deck is already a weighted, shuffleable bag, so story beats can be written as conditional patient cards rather than fixed days. That gives the game Papers, Please's threaded storytelling without the fragile day grid its author wished he could rearrange.
