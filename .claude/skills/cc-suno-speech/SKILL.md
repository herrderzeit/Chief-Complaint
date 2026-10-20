---
name: cc-suno-speech
description: "Chief Complaint voice lines with Suno (Speech tab). Writes scripts and voice directions for PA announcements, paramedic handovers and tutorial lines from the project's sound brief, and with 'import' brings downloaded files from Assets/Audio/Voice/_Incoming into the game. Use when the user wants voice-over, barks, announcements or narration."
argument-hint: "<cue or moment, e.g. 'VO_PA_CodeBlue' or 'tutorial step 3'>  |  import"
disable-model-invocation: true
---

# Suno Speech (Speech tab)

Suno's **Speech** tab turns a script into spoken audio. Suno has no API, so the user generates in the browser; you write the scripts and voice directions and the team imports. Use Australian English for notes, and globally understood English in the lines themselves. Read `docs/audio/sound-brief.md` first; its **Identity** and **Voice** sections are the source of truth. Don't use unofficial Suno APIs.

I don't know the Speech tab's exact fields. The first time this skill runs, ask the user what it offers (for example voice choice, style or emotion, speed) and record it at the end of this file under **Speech tab fields**, so later prompts match the UI.

## Mode 1: Write scripts (default)

1. Match the request to a voice cue in the brief (`VO_*`). For a new line, propose the cue, the voice (one of the brief's characters) and when it plays, and add it to the brief once the user agrees. Check the triggering game event exists; if not, note that the lead must add one.
2. For each line, give:

   ```
   Cue:       VO_<...>
   Voice:     <character from the brief + direction, e.g. "PA announcer: calm, clipped, slightly bored; light tannoy feel">
   Script:    <the exact words, short; under 4 seconds for barks>
   Variants:  <2–3 alternative wordings, so repeats don't grate>
   ```
   - Keep lines short and clear: players hear them mid-turn. Use punctuation to steer pacing (commas for pauses, full stops for clipped delivery).
   - Use the globally understood medical terms from the cards. No regional slang, and never mock patients.
   - Spell out anything a voice might misread (e.g. "A P" not "AP", "Code Blue" not "CB").
3. Remind the user:
   - generate each line two or three times and keep the clearest
   - download as WAV if possible
   - put the files in `Assets/Audio/Voice/_Incoming/`
   - check Suno's terms for voice use in a commercial game
4. Stop. Don't create assets in this mode.

## Mode 2: Import (`/cc-suno-speech import`)

Follow `docs/audio/import-guide.md` for `Assets/Audio/Voice/_Incoming/`. Delegate to **cc-audio-specialist**: the move to `Voice/`, voice import settings, Voice mixer routing with music ducking, and `VoicePlayer` wiring with a cooldown per cue. Then do the lead's check, playing each line in play mode, and log each file in `docs/audio/suno-log.md` with the script used. If a line repeats the on-screen popup text word for word, consider shortening one of them. Report what was imported, what triggers each line, what you checked and anything not verified. Don't commit unless asked.

## Speech tab fields

(Not yet recorded. Fill in on first use.)
