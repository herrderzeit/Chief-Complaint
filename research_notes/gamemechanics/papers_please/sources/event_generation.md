# Papers, Please (Lucas Pope, 2013): event generation, day sequencing and meta-game pressure

Scope note: the papersplease.fandom.com wiki (and its BreezeWiki/antifandom mirrors) returned HTTP 402 or bot-checks during this research, so wiki-only facts (exact day-by-day rules, exact costs) could not be verified directly. The strongest primary sources found were (a) Lucas Pope's own TIGSource devlog, mirrored month by month on dukope.com, and (b) a community modding reference documenting the game's data files (Days.csv, Travelers.txt, Errors.txt, Facts.xml and others). **Caveat on (b):** the documented Days.csv covers only days 1–10 plus a "Test day" 100, which matches the ~10-day public beta (Pope said the beta was about a third of the game and that beta assets were editable and moddable). The data-driven *architecture* is very likely the same in the release, but the specific values may be beta-era.

## 1. How are ordinary entrants generated, and how are scripted entrants interleaved with random ones? How did Pope implement it?

### Takeaway
Each day is a data row that mixes a **count of random filler entrants** with a **list of named, hand-authored "traveler" templates**. A traveler template fixes only the attributes the story needs (name, face, nation, items, a specific error, dialogue, flag logic) and leaves the rest to the same generator that builds filler entrants. Faces are assembled from part sprites, names come from mined name lists, and each entrant gets **at most one deliberate discrepancy**, with errors defined as data rules over a fact model rather than as code.

### Cited Findings
**Procedural filler entrants**
- The first prototype build was "100 procedurally-generated immigrants and their passports", with 7 passport styles and 8 male plus 8 female original faces; Pope planned 32 faces per gender to cut repetition — [Pope, TIGSource devlog, early posts (#33)](https://dukope.com/devlogs/papers-please/tig-01)
- Names were random first and last names harvested from Wikipedia pages about names — [Pope devlog (#33, #35)](https://dukope.com/devlogs/papers-please/tig-01)
- Faces are drawn whole, then split into parts and recombined by the game; he marks the parts with colour masks and anchor dots — [Pope devlog (#232)](https://dukope.com/devlogs/papers-please/tig-03)
- He planned to vary facial features by nationality so countries look different (instead of colour-coding clothes) — [Pope devlog (#197)](https://dukope.com/devlogs/papers-please/tig-03)
- In the data files a face is encoded `X-A-B-C-D-E` (gender plus five part-sprite indices from the `faces` folder); nationality may be given as weighted `Name:weight` pairs to randomise filler; name parts can be left out (e.g. `-Martin`) to randomise the rest — [Papers Please modding doc: Travelers.txt](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Travelers/)

**Error (discrepancy) model**
- "all errors are due to correlation (2 pieces of the same info don't match)"; every logical correlation should work for any error — [Pope devlog (#179)](https://dukope.com/devlogs/papers-please/tig-03)
- He estimated about 35 unique pieces of information, roughly 600 fact pairs; inspect mode works by highlighting two pieces of information, and a rule error pairs a document field with a rulebook line — [Pope devlog (#62, #69)](https://dukope.com/devlogs/papers-please/tig-01)
- "more errors makes the game easier, not harder", so "each immigrant will have at most one thing wrong" (with exceptions for predictable cases such as a newly handed-over document) — [Pope devlog (#539)](https://dukope.com/devlogs/papers-please/tig-06)
- He wanted a good share of entrants with no errors at all — [Pope devlog (#152)](https://dukope.com/devlogs/papers-please/tig-03)
- Subtlety controls: a library of slightly-off seals appearing at random; partial-name errors; ID-number errors limited to a single-digit difference; expiry dates could be programmatically guaranteed to extend past the stay — [Pope devlog (#192)](https://dukope.com/devlogs/papers-please/tig-03)
- Errors.txt defines each error as a data block: a requirement (a paper from Papers.xml, or an expression such as `Day/Features has FINGERPRINT`, `Day/Rules has RuleX`, `Traveler/Nationality == Kolechia`), a fact check from Facts.xml (`!`, with `~` marking a "clearable" error the entrant can explain away), and the interrogation response (speech IDs, actions such as `Fingerprint`, `Detain`, `Search`, items added, values set). Tags `<generic>`, `<contraband>`, `<missing>` classify errors. Example IDs: `Passport-WrongFaceClear`, `Passport-WrongFaceError`, `Passport-Missing`, `Passport-Hidden`, `Rules-DetainKolechia`, `Bulletin-News0`, `Misc-BoothCounter` — [Modding doc: Errors.txt](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Errors/)
- Errors are gated by day state: e.g. `Passport-Missing` needs the INSPECT feature to be enabled, `Rules-DetainKolechia` needs the day's rule set to contain RuleDetainKolechia — [Modding doc: Errors.txt](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Errors/)

**Scripted travelers and interleaving**
- Story-critical entrants are scripted per day so that they match that day's bulletin — [Pope devlog (#160)](https://dukope.com/devlogs/papers-please/tig-03)
- Predefined entrants often fix only some traits; face, nationality and possibly errors vary between playthroughs — [Pope devlog (#163)](https://dukope.com/devlogs/papers-please/tig-03)
- "The main story mode will only have a small amount of randomness" — [Pope devlog (#483)](https://dukope.com/devlogs/papers-please/tig-06)
- Days.csv per day: `COUNT` = number of random entrants (5, 7, 8, 9, 9 … across days 1–10) and `TRAVELERS` = list of named traveler IDs from Travelers.txt (day 1: FINALLY_RETURN_HOME, SMALL_CHECKPOINT, WAITED_FOREVER, FIRST_DAY, KOLECHIAN_HATER, FIRST_DAY) — [Modding doc: Days.csv](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Days/)
- Travelers.txt template language: `INCLUDE <ID>` inheritance; `= nation/name/gender/face/purpose/duration/job`; `= error <ErrorName>` to force a specific error, or weighted `x <weight> <option>` lines to randomise whether/which error; `MANUALPAPERS` to override the day's standard documents; `+ Item` carried items, `= want Item` items the player can hand over; dialogue lines; actions `@ Leave`, `@ Detain`, `@ News <headline \ subtitle>` (sets next day's newspaper); hooks `Intro-Before/After/Replace`, `ShutterClose`, `Interrogate-Replace`, `Detain-Replace/After`, `Approved`, `Denied`, `Leave`, `Give-<Item>` — [Modding doc: Travelers.txt](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Travelers/)
- Global story flags: `Game/Ezic`, `Game/Ezic-MetCorman`, `Game/MrPersistent`, `Game/Brothel`, `Game/HusbandWifeTeam`, `Game/FirstSuicideBomber`; templates test them (`? Game/X == "v"`) and set them (`= Game/X v`); `fallback GENERIC` turns a traveler into an ordinary random entrant when its thread is finished — [Modding doc: Travelers.txt](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Travelers/)
- Documented cross-day chains: **Jorji Costava** (MR_PERSISTENT) recurs until approved, then approval sets the flag to `done` and later slots fall back to GENERIC; **husband/wife team**: the husband's approve/deny sets a flag, and the next day the wife's intro and outcome branch on it; **EZIC**: the first agent hands over `EzicIntro` and sets `Game/Ezic = waiting-for-corman`, Corman Drex then sets `MetCorman`, after which EZIC slots become generic Arstotzkans; **Dari Ludum brothel thread** (BROTHEL2/3) gives a `BrothelHelp` item and exposes `PimpName` for cross-referencing; **killer athlete Vince Lestrade** carries a `Bulletin-News0News1` error, so he is caught by matching his face and name against the bulletin or newspaper — [Modding doc: Travelers.txt](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Travelers/)
- Pope described the story as "pieces" (encounters, characters, documents, puzzles, attacks) that he arranged on a grid of days: "Most of the story threads are done so now the task is to arrange them" — [Pope devlog (#535)](https://dukope.com/devlogs/papers-please/tig-06)
- Thread sizes: many threads of 1–3 encounters, 2–3 longer threads, and one thread spanning the whole game; threads may carry custom documents, special headlines or custom travellers — [Pope devlog (#237)](https://dukope.com/devlogs/papers-please/tig-03)
- Pacing intent: open with hooks, expand the longer stories mid-game, "ramp things up to a climax at the end" — [Pope devlog (#535)](https://dukope.com/devlogs/papers-please/tig-06)
- Some scripted events play out the same regardless of player choice — [Pope devlog (#342)](https://dukope.com/devlogs/papers-please/tig-05)

**Implementation and tooling**
- Built in Haxe/NME (no Flixel), chosen to target several platforms; a reflection plus first-class-function state machine drives game objects' animation and state — [Pope devlog (#75, #92)](https://dukope.com/devlogs/papers-please/tig-01); later PC builds used OpenFL, and later still the game was moved to Unity to match the mobile versions — [Wikipedia](https://en.wikipedia.org/wiki/Papers,_Please)
- He split the game logic from the UI and built a separate fast-play UI on the same logic engine, planning scripted "play style" bots for full autoplay to test "progression, story elements, and money balancing" — [Pope devlog (#535)](https://dukope.com/devlogs/papers-please/tig-06)
- Beta assets were editable; he first built encryption, then chose to support mods via override assets in a single `.ppmod` file — [Pope devlog (#403)](https://dukope.com/devlogs/papers-please/tig-05)
- Data files documented by the modding community: XML (Facts, Papers, Faces, Bodies, Emblems, News, Intro, Paths) and TXT/CSV (Days.csv, Travelers.txt, Errors.txt, Speeches.txt) — [Modding doc index](https://paperspleasemoddingdoc.readthedocs.io/en/latest/)
- Character names were partly crowd-sourced from the public — [Wikipedia](https://en.wikipedia.org/wiki/Papers,_Please)

### Inferences
- The architecture is a **fact model + rule predicates + templated entrants**: the fact graph (Facts.xml) defines every comparable field; rules and errors are predicates over it; a traveler is a partial override of the random generator. That lets one generator serve filler, story characters and endless mode.
- "At most one error per entrant", weighted error choice and "subtle" error variants are the main difficulty dials, separate from the rule list.
- The story is held together by a handful of string-valued global flags tested by later templates, not by a branching narrative graph. Threads "finish" by falling back to the generic generator, so the daily entrant count stays stable whatever the player chose.

### Gaps
- No primary source found on the exact random-error probability per day or the weight values in the release build.
- Whether the release build's data matches the beta-era files documented by the modding wiki is unconfirmed (the doc lists days 1–10 only).
- No GDC talk by Pope specifically on Papers, Please's content pipeline was located; the Edge "Making Of" and the Gamasutra article cited by Wikipedia were not fetched.

## 2. Day structure: bulletin, newspaper, new rules, clock, end-of-day summary

### Takeaway
Each day is one data row: rules in force, enabled mechanics ("features"), newspaper headlines, bulletin text plus tutorial pages, entrant mix, shift length, attack flag and guard placement. The morning gives the newspaper and bulletin, the shift runs on a clock with a minimum quota, and the night screen settles money and family needs.

### Cited Findings
- Days.csv fields: DAY, DESC, RULES (rule IDs from Facts.xml, e.g. day 1: RuleNeedPassport, RuleArstotzkaOnly), COUNT, TRAVELERS, FEATURES, NEWS (`T <title> S <subtitle>`), BULLETIN, BPAGES (tutorial pages appended to the bulletin), DURATION (2, 4, 6, 7, 7, 7, 7, 7, 5, 5 across days 1–10), RUNNER (a wall-climbing attacker, YES on day 2 in that data), LAMP, GUARD (guard positions) — [Modding doc: Days.csv](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Days/)
- Day 1 described in data as "Arstotzkans only, No inspection, No errors" — [Modding doc: Days.csv](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Days/)
- Bulletin tutorial pages by day: day 1 passport and booth tutorials; day 2 correlation tutorial; day 3 missing-document tutorial; day 7 rule tutorial — [Modding doc: Days.csv](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Days/)
- Newspaper headlines can also be pushed by a traveler's action (`@ News`), so player choices show up in the next morning's paper — [Modding doc: Travelers.txt](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Travelers/)
- "The plan is to break the game up into days"; mornings were to bring a stack of letters (Ministry reprimands, thanks, hate mail) — [Pope devlog (#92)](https://dukope.com/devlogs/papers-please/tig-01)
- There is a minimum number of people to process each day; leftover entrants were processed unpaid — [Pope devlog (#160)](https://dukope.com/devlogs/papers-please/tig-03)
- Mistakes are flagged straight away with a citation naming the error (alpha citations did not, beta ones did, and he called immediate citations essential); warnings carry a money penalty — [Pope devlog (#424)](https://dukope.com/devlogs/papers-please/tig-05); [Pope devlog (#174, #200)](https://dukope.com/devlogs/papers-please/tig-03)
- The bulletin date was hooked into correlations (expiry checks against today's date) — [Pope devlog (#200)](https://dukope.com/devlogs/papers-please/tig-03)
- Pay is deliberately vague; a processed-count on the night screen lets players deduce it — [Pope devlog (#424)](https://dukope.com/devlogs/papers-please/tig-05)
- Rules get progressively more complex each day within a limited real-time shift — [Wikipedia](https://en.wikipedia.org/wiki/Papers,_Please)
- Days can be replayed easily, so the early game works as an implicit tutorial — [Pope devlog (#572)](https://dukope.com/devlogs/papers-please/tig-07)

### Inferences
- Shift length (DURATION) is a separate pacing dial from entrant count: short early days teach, long mid-game days stress, and the slightly shorter days later (5 in the beta data) raise pressure as rules pile up.
- Tutorial content is attached to the day row, not to the mechanic, so introducing a mechanic and teaching it are a single authored step.

### Gaps
- Exact release-build day lengths, quota and payment per entrant could not be confirmed from a reachable primary source.

## 3. Endless mode, rule sets, seed and day scripts

### Takeaway
Endless mode is a separate, purely procedural mode (no story) with three scoring types; it was the designed outlet for replay value and was unlocked by the loyal ending. No source found describes a player-visible seed; the story is fixed day scripts with small random variation.

### Cited Findings
- Endless mode was planned separately from story mode to improve replayability; Pope agreed with the idea of random daily rules there — [Pope devlog (#342, #438)](https://dukope.com/devlogs/papers-please/tig-05)
- Endless mode was his answer to replay demand instead of difficulty options — [Pope devlog (#483)](https://dukope.com/devlogs/papers-please/tig-06)
- Endless mode offers an endless flow of procedurally generated entrants in one of three types, Timed, Perfection and Endurance, each scored differently; the unlock code 62131 is shared by all copies and comes with the loyal (no-EZIC) ending — [search summary of fandom "Game modes" page and guides](https://papersplease.fandom.com/wiki/Game_modes?oldid=9027) (page itself not directly fetchable; treat as secondary)
- Endless mode is limited only by error count — [Wikipedia](https://en.wikipedia.org/wiki/Papers,_Please)
- Days.csv included a "Test day" 100 with COUNT 100 and DURATION 500, i.e. a long procedural test day — [Modding doc: Days.csv](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Days/)

### Inferences
- Because story days are just rows of rules, features and traveler lists, endless mode can reuse the same generator with an all-features-on rule set and no TRAVELERS list.

### Gaps
- Endless mode's exact rule sets (which documents and rules, whether they change during a session) not confirmed from a primary source.
- No evidence of a seed system; variation between playthroughs comes from randomised traits inside scripted travelers (#163).

## 4. Family economy and scarcity

### Takeaway
Income is per correctly processed entrant (plus bribes and minus citations); fixed rent plus optional food, heat and medicine create a nightly budget the player can rarely cover in full, which makes bribes, confiscation side-deals and rule-bending tempting. Family members get sick from unpaid needs, sickness leads to death, and debt ends the game.

### Cited Findings
- Pay per correctly processed entrant plus bribes, minus citation penalties; the player budgets rent, food, heat and other necessities; debt ends the game; accepting bribes risks discovery — [Wikipedia](https://en.wikipedia.org/wiki/Papers,_Please)
- Rent and penalties are mandatory; food and heat optional; medicine 5 credits per sick family member; penalties apply only beyond two citations in a day — [ludo.guide family expenses](https://www.ludo.guide/guide/papers-please/family-expenses)
- Sickness triggers: missing food or heat two days running, or paying neither on the same day; the son gets sick after missing food or heat for one day — [ludo.guide](https://www.ludo.guide/guide/papers-please/family-expenses)
- Community strategy: alternate food and heat payments; skipping one day and paying the next can be more profitable — [ludo.guide](https://www.ludo.guide/guide/papers-please/family-expenses); [search summary of fandom Heat page](https://papersplease.fandom.com/wiki/Heat)
- Food 5 credits per living family member per day; heat 10 credits in the class-8 apartment, varying by class; rent depends on apartment class — [search summary of fandom wiki](https://papersplease.fandom.com/wiki/Heat) (secondary, unverified directly)
- Pope: a line about feeding the family was added to day 1's night screen; medicine "doesn't immediately cure sickness"; he was "80% set" on the family setup; reduced food costs were planned; "lots of scripted night-time events affecting your finances" — [Pope devlog (#424)](https://dukope.com/devlogs/papers-please/tig-05); [Pope devlog (#483, #506)](https://dukope.com/devlogs/papers-please/tig-06)
- Early design tied money, bribes and wealth brackets to progression: "The story and game progresses as you move up the brackets" — [Pope devlog](https://dukope.com/devlogs/papers-please/tig-01)
- Moral side-channels designed in: entrants offering money for a passport in the confiscation drawer; a request-a-bribe option — [Pope devlog (#192, #200)](https://dukope.com/devlogs/papers-please/tig-03)
- Escalating penalties: about 5 citations in a day ends that night; he fixed the "deny everyone" / "approve everyone" exploits — [Pope devlog (#381, #424)](https://dukope.com/devlogs/papers-please/tig-05)

### Inferences
- The family economy turns speed and accuracy into survival: every mistake has a delayed, human cost on the night screen, and medicine's delay makes neglect hard to undo.
- Optional needs (food, heat) are the slack the player can trade against moral risk (bribes) or story choices that pay (EZIC money).

### Gaps
- Exact salary per entrant (commonly cited as 5 credits) and the rent figure for each class were not confirmed from a reachable source.
- Exact sickness-to-death timing not confirmed.

## 5. Branching: EZIC, flags, the 20 endings, delayed consequences

### Takeaway
Branching is a small set of global flags plus counters (EZIC tasks done, money, family alive, items such as Obristan passports) checked at fixed points, mostly days 29–31. Most of the 20 endings are early fail states (debt, family death, arrests); the story endings depend on how many EZIC tasks were done and what the player does in the day 31 attack. Consequences arrive later through follow-up travelers, Ministry inspections and newspaper headlines.

### Cited Findings
- 20 story endings; EZIC is a group plotting a coup, and how the player treats it shapes the ending; escape to Obristan with or without family is possible — [Wikipedia](https://en.wikipedia.org/wiki/Papers,_Please)
- Ending triggers (one guide's grouping): debt at day end; family dies; fired for unauthorised wall decorations (warning day 20, inspector every 10 days, fired day 30); detaining Dimitri's friend Shae; shooting or tranquillising an innocent or a guard (tranquilliser from day 16, rifle from day 23); handing EZIC documents from days 8–10 to the Ministry inspector (arrested day 12); keeping EZIC money from days 11–12 then failing the day 14 investigator; following or half-following the EZIC order to kill the Man in Red; day 31 outcomes by EZIC tasks done (4+ and holding fire = EZIC wins; 4 and shooting = executed; 3 or fewer with the wall blown = executed); killing both attackers with zero EZIC tasks = commendation and endless mode; escaping to Obristan days 29–31 alone (25+ credits) or with family (an Obristan passport confiscated from entrants plus 25 credits per family member) — [Pocket Gamer endings guide](https://www.pocketgamer.com/papers-please/endings/)
- Numbering conflict: Pocket Gamer numbers the loyal ending 18 and the Obristan escapes 19–20, while other guides call the loyal ending "Ending 20" (the one that unlocks endless mode) — [Pocket Gamer](https://www.pocketgamer.com/papers-please/endings/) vs [search summary of guides incl. speedrun.com](https://speedrun.com/papers_please/guides/qgmep). The guides also disagree on the EZIC threshold ("four" vs "4–5").
- If the player neither escapes nor ends the game on day 31, play continues to day 32 with three possible endings; with at least four EZIC tasks done, a further EZIC ending unlocks — [search summary of fandom Day 32 page and guides](https://papersplease.fandom.com/wiki/Day_32)
- EZIC thread start in data: first agent gives `EzicIntro` and sets a flag; Corman Drex follows; the flag then drives later dialogue — [Modding doc: Travelers.txt](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Travelers/)
- Delayed consequence examples in data: husband approved or denied changes the wife's next-day scene; travelers can push next-day newspaper headlines — [Modding doc: Travelers.txt](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Travelers/)
- Pope: if you don't arrest the pimp, his associates kill people (consequence off-screen, reported later); he would not push every story's moral consequences that far — [Pope devlog (#381, #424)](https://dukope.com/devlogs/papers-please/tig-05)
- Story arc "deeply integrated with the sniping"; "lots of cool attacks planned" — [Pope devlog (#483, #506)](https://dukope.com/devlogs/papers-please/tig-06)
- Hidden "tokens" (collectibles) were left mostly unexplained — [Pope devlog (#307)](https://dukope.com/devlogs/papers-please/tig-05)

### Inferences
- Endings are evaluated as a checklist over a few counters at fixed checkpoints, not a tree; most narrative variety comes from many small 1–3 encounter threads whose outcome only shows in the next newspaper or a returning traveler.
- Failure endings (debt, family, arrest) do double duty as the pressure system: they are always reachable, so every day carries the risk of an "ending".

### Gaps
- Exact ending numbering and EZIC thresholds conflict between guides; the fandom wiki (the usual reference) could not be fetched.
- No primary source found for the full list of EZIC tasks per day.

## 6. Escalating complexity and special events

### Takeaway
New mechanics are switched on per day through the FEATURES and RULES fields and taught by tutorial pages in that morning's bulletin: first a single rule with no inspection and no errors, then correlation, missing papers, rules, and later fingerprints, detain, search, contraband, the tranquilliser and the rifle. Attacks, border events and ministry visits are authored day events that interrupt the procedural flow.

### Cited Findings
- Day 1 "Arstotzkans only, No inspection, No errors"; day 2 introduces correlation; day 3 missing documents; day 7 a rule tutorial — [Modding doc: Days.csv](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Days/)
- Feature switches: FREEDENIAL (deny without interrogation), INSPECT, FINGERPRINT, DETAIN, SEARCH, SNIPING (unused in that data) — [Modding doc: Days.csv](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Days/)
- An early bulletin restricted entry to Arstotzkan citizens because of a security alert — [Pope devlog (#174)](https://dukope.com/devlogs/papers-please/tig-03)
- "I gotta make the game easier to play at the start"; difficulty to scale beyond the first few days — [Pope devlog (#185, #192)](https://dukope.com/devlogs/papers-please/tig-03)
- The step from stamping to inspection on day 3 (missing papers) was "too big" in testing — [Pope devlog (#367, #370)](https://dukope.com/devlogs/papers-please/tig-05)
- Goal of escalation: "force the player to think long and hard about ignoring any piece of information" — [Pope devlog (#551)](https://dukope.com/devlogs/papers-please/tig-07)
- Document security arms race: every official document gets a seal listed in the rulebook, later forged seals, a possible UV seal; luggage and visa-page flipping cut for scope — [Pope devlog (#62)](https://dukope.com/devlogs/papers-please/tig-01); [Pope devlog (#424)](https://dukope.com/devlogs/papers-please/tig-05); [Pope devlog (#483, #506)](https://dukope.com/devlogs/papers-please/tig-06)
- Attacks: wall runners (RUNNER flag per day); options to do nothing, tranquillise or snipe, with bonus pay for stopping them yourself; suicide bombers you let through give "no chance to react"; tranquilliser chosen before the rifle to limit darkness — [Pope devlog (#212, #223, #236)](https://dukope.com/devlogs/papers-please/tig-03); [Modding doc: Days.csv](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Days/)
- Rule-driven events in data: e.g. `Rules-DetainKolechia` fires only when the day's rules contain RuleDetainKolechia and the entrant is Kolechian — [Modding doc: Errors.txt](https://paperspleasemoddingdoc.readthedocs.io/en/latest/txt-csv/Errors/)
- Body-scan searches can show weapons and contraband — [Pope devlog (#228)](https://dukope.com/devlogs/papers-please/tig-03)
- Tranquilliser available from day 16 and the rifle from day 23 — [Pocket Gamer](https://www.pocketgamer.com/papers-please/endings/)

### Inferences
- Layering pattern: (1) a new rule or feature appears, (2) the bulletin teaches it the same morning, (3) errors that need it unlock through their `Requirement` on that feature, (4) a scripted traveler shows it off. Old rules mostly stay, so the cognitive load builds up over the days.
- Special events are bulletin- or news-coupled errors (e.g. a wanted killer whose face is in the bulletin), which turns the morning reading into a mid-shift test.

### Gaps
- Full day-by-day release schedule (when work permits, ID cards, diplomatic authorisations, the Kolechian search requirement and the border closure arrive, days 11–31) could not be verified from a reachable source; the fandom day pages were blocked and other day-guide sites found looked unreliable.
