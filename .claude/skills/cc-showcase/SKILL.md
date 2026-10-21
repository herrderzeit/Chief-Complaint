---
name: cc-showcase
description: "Chief Complaint showcase for collaborators: records a captioned MP4 tour of the current game (mechanics and UI) with Unity Recorder, then writes a Google Doc explaining every change since the last showcase (or a given commit), shared view-only, and returns the share links. Use when the user wants to show the project lead or friends what changed, a progress update, a gameplay video, or a changelog document."
argument-hint: "[since <commit-or-tag>]  (default: the last showcase-* tag, else the last 20 commits)"
disable-model-invocation: true
---

# Showcase: video + changes document

You are the lead (see `CLAUDE.md`). Produce a gameplay video and an explanatory Google Doc that the user can send to their project lead for feedback. Use Australian English; write for a reader who knows the game's idea but not the recent work.

## Step 0: Preconditions
1. `unity status` must be "ready". Ask the user before saving MainBoard if it has unsaved edits (the tour plays the open scene, saved or not, but the doc should describe committed work).
2. Note the range: `since` argument, else the most recent tag `showcase-*` (`git tag --list "showcase-*" --sort=-creatordate`), else the last 20 commits. Record `git log --oneline <range>`.
3. Don't run while subagents are running Unity tests (play mode interrupts them).

## Step 1: Record the tour
1. If the game has new mechanics or screens since the tour was last updated, extend the steps in `Assets/Editor/Tools/ShowcaseRecorder.cs` (`BuildSteps`): one short caption per mechanic, driving the real game code the way existing steps do. Recompile and check the console.
2. Run it: `unity command menu --path "Chief Complaint/Showcase/Record Gameplay Tour"` (check the exact `menu` parameter name with `unity command menu --help`). It enters play mode, records `Recordings/Showcase_<date>/ChiefComplaint_Tour.mp4` (1920×1080, 30 fps) plus one PNG still per step (`01.png`, `02.png`, …), then exits play mode by itself.
3. Wait until play mode has stopped (`unity command editor_status`), then check the console for "Showcase step … failed" warnings. Look at the stills (Read the PNGs): if a step shows the wrong thing, fix the step and record again.

## Step 2: Write the changes document
1. Gather the changes: the commit range from Step 0, hand-off files in `docs/handoffs/` touched in that range, and simulator numbers (re-run `dotnet run --project Tools/BalanceSim -c Release -- report --runs 3000` if economy changed). Group by theme, not by commit.
2. Structure:
   - **Summary**: three to five bullets on what's new and why.
   - **How to watch the video**: link to the video, plus what each part shows.
   - **New mechanics**: for each one, what it is, how it plays, and why it was added. Use screenshots from the stills.
   - **Cards and content**: new cards (name, cost, effect), changed cards, Reputation and economy changes.
   - **UI and quality of life**
   - **Balance**: a before/after table of simulator win rates, with one line on what the bots are.
   - **Known issues and next steps**
   - **Questions for feedback**: three to five specific questions for the project lead.
3. Before creating the file, read the `anthropic-skills:google-workspace` skill. Create a Google Doc with the Google Drive connector, titled "Chief Complaint – Update <date>". Upload the MP4 and the stills you use to the same Drive folder.
4. Share the Doc and the video so that **anyone with the link can view only** (not comment or edit). Confirm the permissions read back as viewer.

## Step 3: Finish
1. Tag the commit you described: `git tag showcase-<yyyy-mm-dd>`. It's a local tag; push it only if the user asks.
2. Give the user both view-only links, the Doc and the video, plus the local video path, and say what wasn't verified.
3. Don't commit or push anything else unless asked.
