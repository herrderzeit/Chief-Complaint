# Papers, Please (2013): how entrant validity is modelled and checked

Source-access note for the report writer: the Papers, Please Fandom wiki (the most complete reference) returned HTTP 402 on every direct fetch, the Wayback Machine could not be fetched, the BreezeWiki mirror served a bot check (not bypassed), and StrategyWiki, TIGSource (Lucas Pope's devlog), TV Tropes and Steam guides returned 403/429. Wiki content below therefore comes from **search-result snippets of the wiki pages** (cited to the wiki URL), plus one fully fetched third-party guide (sort-the-court.com), Wikipedia, and a Game Developer feature with Pope's own comments. The day-by-day schedule rests mainly on one third-party guide and should be treated as **medium confidence**; contradictions are flagged inline.

## 1. Documents: fields and introduction days

### Takeaway
An entrant is a record of true attributes (name, sex, DOB, nationality, face, height, weight, fingerprints, purpose, duration, items carried) plus a set of documents that each carry a **subset of those attributes, plus issuer metadata** (issuing city, seal, expiry, number). Validity means each document being present when the current rules require it, internally valid (current date, real issuing city or district, genuine seal), and matching the other documents and the physical entrant. New document types join the requirement set over about 31 days.

### Cited Findings
- Passport: photo, name, sex, date of birth, issuing city, expiry date, passport number. There are 7 issuing nations: Antegria, Arstotzka, Impor, Kolechia, Obristan, Republia, United Federation. — [Fandom: Discrepancy / Fake passport via search](https://papersplease.fandom.com/wiki/Discrepancy); [Fandom: Fake passport](https://papersplease.fandom.com/wiki/Fake_passport)
- Each nation has a fixed list of valid issuing cities in the rulebook (Arstotzka's: Orvech Vonor, East Grestin, Paradizna). "Invalid issuing city" is a passport discrepancy. Generated misspellings of city names are "not generated completely randomly", i.e. they are plausible near-misses. — [Fandom: Arstotzka](https://papersplease.fandom.com/wiki/Arstotzka); [Fandom: Discrepancy](https://papersplease.fandom.com/wiki/Discrepancy)
- Document schedule, per one third-party guide (fully fetched). — [sort-the-court.com documents guide](https://www.sort-the-court.com/games/papers-please/documents-guide)
  - **Passport** (Day 1): nation, issuing city, expiry, photo, name, sex, number.
  - **Entry Ticket** (Day 3, gone after Day 3): a date that must equal today's date.
  - **Entry Permit** (Day 4, used until Day 26): name, passport number, purpose (Visit / Transit / Work / Immigrate), duration, expiry, Ministry of Admission seal.
  - **Arstotzkan ID Card** (Day 4, citizens only): name, DOB, district, photo, height, weight. The district must be one listed in the rulebook.
  - **Work Pass** (Day 6): name, field of work, work-until date, Ministry of Labor seal. The work-until date must cover the stay. The Fandom wiki also says work passes are "first seen on day 6". — [Fandom: Work Pass](https://papersplease.fandom.com/wiki/Work_Pass)
  - **Diplomatic Authorization** (Day 8): name, passport number, issuing country, access list (must include Arstotzka), diplomatic seal. It replaces the entry permit, ID supplement and access permit.
  - **ID Supplement** (Days 13–26, foreigners): height, weight, a text description of appearance, expiry, thumbprint.
  - **Grant of Asylum**: fingerprints, passport number, nationality, height, weight, expiry, seal. The guide says Day 21. A search snippet tied it to Day 14. **Unresolved.** The bulletin says holders do not need an entry permit or an ID supplement. — [Fandom: Official Bulletin via search](https://papersplease.fandom.com/wiki/Official_Bulletin)
  - **Certificate of Vaccination** (Day 26, all entrants): name, passport number, vaccines with dates. It must list the required vaccine (polio) and be current.
  - **Access Permit** (Days 27–31): merges the entry permit and ID supplement fields (name, number, nationality, purpose, duration, height, weight, appearance, expiry, seal). Workers still need a work pass. This is corroborated by [ludo.guide: access permit](https://www.ludo.guide/guide/papers-please/access-permit).
  - Non-entrant artefacts: the Wanted Criminals list in the daily bulletin (Day 14), the Reason-for-Denial stamp (Day 18), and passport seizure slips (Days 24 and 28).
- Day-by-day rulebook changes (same guide, medium confidence):
  - D1: Arstotzkans only, passport required.
  - D2: foreigners allowed; all documents must be current. The "documents must be current from Day 2, permanent" rule is corroborated by a [Steam guide snippet](https://steamcommunity.com/sharedfiles/filedetails/?id=2539652296).
  - D3: entry ticket.
  - D4: entry permit replaces the ticket; citizens need an ID card.
  - D6: work pass.
  - D7: search all Kolechians.
  - D8: diplomats.
  - D11: forged seals become common.
  - D13: ID supplement.
  - D14: wanted list.
  - D18: reason-for-denial stamp.
  - D19: no entry from Impor.
  - D24: confiscate passports from the Altan district.
  - D25: no entry from the United Federation.
  - D26: polio vaccination.
  - D27: access permit.
  - D28: confiscate all Arstotzkan passports.
  - — [sort-the-court.com](https://www.sort-the-court.com/games/papers-please/documents-guide)
- Requirement matrix by entrant class (same guide):
  - Citizen: passport, plus ID card, plus vaccination.
  - Visitor / transit / immigrant: passport, plus ticket or permit, plus supplement, then access permit plus vaccination.
  - Worker: the visitor set plus a work pass.
  - Diplomat: passport plus diplomatic authorisation (plus vaccination).
  - Asylum seeker: passport plus grant of asylum (plus vaccination).
  - — [sort-the-court.com](https://www.sort-the-court.com/games/papers-please/documents-guide)

### Inferences
- Model: `Entrant { trueAttributes; DocumentInstance[] }`, with `DocumentInstance { type, fields{}, issuer, seal, expiry }`. The requirement set is a function of (day, nationality, entrant class), where the class comes from purpose and the documents carried.
- Seals and issuing cities are **enumerations defined by reference data in the rulebook**. Forgeries are values outside the valid set, or near-miss variants of valid ones.

### Gaps
- The exact field list of each document (for example, whether the entry permit carries an expiry date as well as a duration) could not be confirmed from the primary wiki pages, which were blocked.
- The grant-of-asylum introduction day conflicts between sources (Day 14 or Day 21). The Day 7 "search Kolechians" and Day 11 "forged seals" entries come from a single source and may be misdated. The Reason-for-Denial requirement from Day 18 *is* corroborated (see section 4).

## 2. How the rulebook works

### Takeaway
Rules are **cumulative and declarative**. A morning bulletin announces the day's additions and removals, and the rulebook holds the reference data (nation and city lists, districts, seal examples, document specimens) that checks are made against. Pope hand-authored the schedule because rules and scripted encounters depend heavily on each other.

### Cited Findings
- Each day sets entry requirements. New rules appear as relations with neighbours worsen, for example nationality bans or new document requirements. — [Wikipedia](https://en.wikipedia.org/wiki/Papers,_Please)
- Rules are announced in the Official Bulletin. The bulletin also instructs the player to use INSPECT mode, and it can carry requirements that are not written into the rulebook. — [Fandom: Official Bulletin](https://papersplease.fandom.com/wiki/Official_Bulletin); [Fandom: Day 31](https://papersplease.fandom.com/wiki/Day_31)
- Pope (Game Developer):
  - Any attribute of a scripted entrant that is not specified "is filled in randomly under the current rules", so scripted and random entrants interleave.
  - Adding one encounter or rule could disrupt the rest of the schedule, so he laid it out by hand.
  - He avoided announcing rules on days when another character was also speaking to the player, because testers ignored the bulletin.
  - — [Game Developer: Designing the bleak genius of Papers, Please](https://www.gamedeveloper.com/design/designing-the-bleak-genius-of-i-papers-please-i-)
- Pope wrote validation procedures that generate thousands of immigrants at once to test the generator. — [Game Developer](https://www.gamedeveloper.com/design/designing-the-bleak-genius-of-i-papers-please-i-)

### Inferences
- An implementation needs: a `RuleSet` per day (a list of active predicates plus reference tables), a generator that samples entrants *consistent with the active rules*, and an automated validator that checks the generator's ground-truth "should approve?" flag against the rule evaluation.

### Gaps
- No primary source gives the internal rule representation. The TIGSource devlog could not be fetched.

## 3. Inspect mode and discrepancy types

### Takeaway
In inspect mode the player highlights **two facts** (two document fields, or a field and a physical fact such as the scale reading or the entrant's face). If the facts conflict, the game confirms a discrepancy and unlocks an interrogation line. If they are consistent, or a discrepancy has been resolved, the game reports it as cleared.

### Cited Findings
- Enter INSPECT with the red desk button, then highlight two pieces of discrepant information "for further options". — [Fandom: Official Bulletin](https://papersplease.fandom.com/wiki/Official_Bulletin)
- When the highlighted items resolve a discrepancy, the game shows "Discrepancy cleared". — [Fandom: Inspection mode](https://papersplease.fandom.com/wiki/Inspection_mode); [Fandom: Discrepancy](https://papersplease.fandom.com/wiki/Discrepancy)
- Discrepancies come in two classes:
  - Minor: incorrect spoken statements or missing documents, resolvable through interrogation.
  - Severe: discrepancies that warrant detention.
  - — [Fandom: Discrepancy](https://papersplease.fandom.com/wiki/Discrepancy)
- Discrepancy types named across the sources:
  - Expired document. Expiry alone allows an immediate denial without highlighting or interrogating.
  - Invalid issuing city.
  - Forged or missing seal (entry permit, access permit, work pass).
  - Name, passport number or sex mismatch across documents.
  - Photo not resembling the entrant.
  - Weight on the document against the booth scale. Contraband smugglers are a few kg heavier than their documents state.
  - Height against the booth backdrop.
  - Purpose or duration against the spoken answers.
  - Entrant on the wanted list.
  - Banned nationality.
  - Wrong date on the entry ticket.
  - Wrong vaccine.
  - Diplomatic access list missing Arstotzka.
  - — [Fandom: Discrepancy](https://papersplease.fandom.com/wiki/Discrepancy); [Fandom: Search](https://papersplease.fandom.com/wiki/Search); [Fandom: Detention](https://papersplease.fandom.com/wiki/Detention); [sort-the-court.com](https://www.sort-the-court.com/games/papers-please/documents-guide)
- For weight, highlight the scale reading (bottom right of the booth) and the weight on the document. For dates, highlight the date and the relevant field. — search summary of [Fandom: Discrepancy](https://papersplease.fandom.com/wiki/Discrepancy)
- Fingerprinting (available from about Day 4 per one snippet) resolves identity mismatches. A body scan is used for weight mismatches (suspected contraband) and for a sex mismatch. — [Fandom: Search](https://papersplease.fandom.com/wiki/Search); [Fandom: Discrepancy](https://papersplease.fandom.com/wiki/Discrepancy)
- Originally (in development) the denial stamp could be used only after a discrepancy had been found. — [TV Tropes: Trivia (search snippet)](https://tvtropes.org/pmwiki/pmwiki.php/Trivia/PapersPlease)

### Inferences
- Model the check as a **typed pair relation**. Each discrepancy type is defined by a pair of field kinds (`Doc.A.field` ↔ `Doc.B.field`, `Doc.field` ↔ `Physical.x`, or `Doc.field` ↔ `Rulebook.table`) plus a comparison (equality, membership, date ≤ today, |Δ| > tolerance).

### Gaps
- No complete per-document discrepancy list was retrievable (the wiki's "List of discrepancies by document" was blocked).
- The weight and height tolerances are not documented in the accessible sources.

## 4. Interrogation, fingerprinting, search and detention

### Takeaway
A flagged discrepancy unlocks **Interrogate**. The entrant then does one of three things:
- produces the missing document (a minor discrepancy is resolved);
- gives an answer that clears the discrepancy;
- leaves it standing, which unlocks Search, Fingerprint, Detain and the Reason-for-Denial option.

Detention generally requires a prior interrogation.

### Cited Findings
- The entrant must be interrogated before they can be detained, with two scripted exceptions. An entrant missing a document (except a diplomatic authorisation) may present it when asked. — [Fandom: Discrepancy / Detention (search snippet)](https://papersplease.fandom.com/wiki/Discrepancy)
- Interrogation enables these actions: Search, Fingerprint (alias test), Detainment (in various cases), and Reason for denial. — [Fandom: Inspection mode / Discrepancy](https://papersplease.fandom.com/wiki/Inspection_mode)
- After a search or fingerprint check, the discrepancy either persists, in which case the entrant can be detained, or is cleared, in which case the entrant cannot be denied on that basis. — [Fandom: Search](https://papersplease.fandom.com/wiki/Search)
- The player is not required to search and may simply deny the entrant without a citation. — [Fandom: Search (search snippet)](https://papersplease.fandom.com/wiki/Search)
- If an entrant has no passport and does not produce one under interrogation, a temporary visa slip appears. Approving it lets the entrant through but earns a citation. — [Fandom: Frequent sticking points (search snippet)](https://papersplease.fandom.com/wiki/Frequent_sticking_points)
- Detain reasons include a wanted criminal, forged or stolen documents, and contraband or weapons found by the scanner. — [Wikipedia](https://en.wikipedia.org/wiki/Papers,_Please); [Fandom: Detention](https://papersplease.fandom.com/wiki/Detention)
- A guard (Calensk) later offers a per-detention kickback ("detain bonus"). Before that, denying is more time-efficient than detaining. — [TV Tropes](https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/PapersPlease)

### Inferences
- This works as a small state machine: `Presented → (Flagged) → Interrogated → {Resolved | Persisting → {Search/Fingerprint → Cleared | Confirmed → Detain}} → Stamp`.

### Gaps
- The exact rules for when Detain becomes available for each discrepancy type, and the full list of entrant responses, were not retrievable.

## 5. Consequences: stamps, citations, pay and time

### Takeaway
Each entrant ends in Approve, Deny (from Day 18 with a reason stamp) or Detain. A wrong outcome produces a **citation by fax** soon after the entrant leaves. The first two citations each day are warnings, later ones cost money, and every cited entrant forfeits that entrant's pay. Pay is 5 credits per correctly processed entrant, and the real-time shift clock caps throughput.

### Cited Findings
- The player stamps to admit or deny, or orders an arrest. A citation arrives by in-game fax shortly after a protocol violation. At the end of the day the player is paid per properly processed entrant (plus bribes), minus penalties. Each shift has a limited amount of real time. — [Wikipedia](https://en.wikipedia.org/wiki/Papers,_Please)
- Citation schedule:
  - The first two citations each day carry no penalty ("WARNING ISSUED - NO PENALTY"), but the entrant's pay is still lost.
  - Per the wiki, the next two cost 5 credits each, and each later citation that day adds 5 credits more. — [Fandom: Citation / General tips (search snippets)](https://papersplease.fandom.com/wiki/Citation)
  - A Vita walkthrough describes it as "next two 5 credits, next two after that 10". — [TrueTrophies Vita walkthrough (snippet)](https://www.truetrophies.com/game/Papers-Please-Vita/walkthrough/3)
  - **Minor conflict on the escalation step.**
- Citations have specific texts tied to rules, for example "No entry from Kolechia/Republia" (Day 1 only) and "Passport not confiscated" (Days 24–31). — [Fandom: Citation](https://papersplease.fandom.com/wiki/Citation)
- Salary is 5 credits per entrant processed without a citation. An entrant counts as processed when they leave the booth before a scripted event or the end of time. — [Fandom: Credits / End of day screen (snippet)](https://papersplease.fandom.com/wiki/Credits)
- Before Day 18 a flagged entrant can be denied without interrogation. From Day 18 a denial needs a reason stamp. — [Fandom: Discrepancy (snippet)](https://papersplease.fandom.com/wiki/Discrepancy)
- Economy pressure:
  - Rent 20 credits, rising to 25 after Day 5.
  - Food 5 credits per family member.
  - Heat 10 credits.
  - Debt ends the game.
  - — [Fandom: Bills](https://papersplease.fandom.com/wiki/Bills); [Wikipedia](https://en.wikipedia.org/wiki/Papers,_Please)
  - Sources differ slightly on the early rent figure.

### Inferences
- The incentive loop is: speed (more entrants × 5 credits) against accuracy (a lost wage plus an escalating fine), with two free mistakes a day as slack. This is a clean template for "throughput against correctness" pressure.

### Gaps
- The exact shift length in real seconds and the in-game clock range were not confirmed (6:00 to 18:00 was not verified).
- Whether interrogating costs time beyond the dialogue animation was not confirmed.

## 6. How errors are seeded into generated entrants

### Takeaway
Entrants are a mix of scripted characters, partly specified entrants and fully random ones. According to the fan wiki, a random entrant has **at most one discrepancy** (at most one per category), which comes from a fixed per-entrant chance. The exact probability is not public.

### Cited Findings
- Scripted entrants specify only some attributes, and everything else is randomised "under the current rules". — [Game Developer (Pope)](https://www.gamedeveloper.com/design/designing-the-bleak-genius-of-i-papers-please-i-)
- Each procedurally generated entrant can have at most one error per category. The General tips page says one discrepancy per entrant, and notes that more errors would make the game easier, since any single flag justifies action. — [Fandom: Discrepancy](https://papersplease.fandom.com/wiki/Discrepancy); [Fandom: General tips](https://papersplease.fandom.com/wiki/General_tips)
- There is "a set chance in the game's coding" that information is flat-out wrong or slightly incorrect (a near-miss). — [Fandom: Discrepancy (snippet)](https://papersplease.fandom.com/wiki/Discrepancy)
- Speedrun guides say that from about entrant 10 of each day (entrant 6 in some walkthroughs), everyone is random. — [Speed Demos Archive](https://speeddemosarchive.com/PapersPlease.html); [TrueTrophies (snippet)](https://www.truetrophies.com/game/Papers-Please-Vita/walkthrough/3)

### Inferences
- Generation is likely: sample a valid entrant for today's rules, then with probability p choose one discrepancy type from the types active today, and apply either a "blatant" or a "subtle" mutation (for example a misspelled city or a slightly off date).

### Gaps
- The bad-entrant probability p, how it varies by day, and the weighting of discrepancy types were not found in any accessible source. The TIGSource devlog may contain them but could not be fetched.

## Version notes
- The PC version moved from OpenFL/Haxe to Unity in line with the mobile ports. The Vita port needed interface changes. The iOS version censored the body scanner (later a "nudity" option). The 2022 mobile versions were redesigned for small screens. No sourced change to the validity rules themselves. — [Wikipedia](https://en.wikipedia.org/wiki/Papers,_Please)
