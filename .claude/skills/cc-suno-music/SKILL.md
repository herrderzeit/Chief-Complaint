---
name: cc-suno-music
description: "Chief Complaint music with Suno (Songs tab). Writes ready-to-paste Suno Songs prompts for the game's music loops from the project's sound brief, and with 'import' brings downloaded tracks from Assets/Audio/Music/_Incoming into the game. Use when the user wants music, a theme or a loop. For sound effects and stings use cc-suno-sounds; for voice lines use cc-suno-speech."
argument-hint: "<cue or moment, e.g. 'Music_Shift_Calm' or 'staff room'>  |  import"
disable-model-invocation: true
---

# Suno Music (Songs tab)

Suno has no official API, so the user generates in their browser: you write the prompts, the user generates and downloads, and the team imports. Use Australian English. Read `docs/audio/sound-brief.md` first every time; its **Identity** and **Music cues** sections are the source of truth. Don't use unofficial Suno APIs.

## Mode 1: Write prompts (default)

1. Match the request to a music cue in the brief. For a new moment, propose a `Music_*` cue (when it plays, mood, tempo, length) and add it to the brief once the user agrees. Short stings belong to `/cc-suno-sounds`.
2. For each cue, give a block the user can paste into **Songs → Custom**:

   ```
   Title:    Chief Complaint – <Cue name>
   Style:    <max ~200 characters: genre, instruments, mood, BPM, key, "instrumental">
   Lyrics:   [Instrumental]
             [Intro] ... [Loop A] ... [Loop B] ... [Outro: return to the Intro feel]
   Settings: Instrumental ON
   ```
   - Put the BPM and key in the style (e.g. "104 BPM, D minor"). Paired cues (Calm and Tense) must share both.
   - Use structure tags in the lyrics box to steer the form. For loops, ask for an ending that returns to the opening feel, and say the cut point will be trimmed afterwards.
   - Leave out anything the brief says to avoid; no vocals.
3. Give **two or three variations** per cue (e.g. sparser and busier), each labelled with what it changes.
4. Remind the user:
   - download as **WAV** if their plan allows, otherwise MP3
   - put the files in `Assets/Audio/Music/_Incoming/`
   - commercial use needs a paid Suno plan
5. Stop. Don't create assets in this mode.

## Mode 2: Import (`/cc-suno-music import`)

Load `unity:audio-setup-mixers` and `unity:optimize-audio` (and tell cc-audio-specialist to load them too), then follow `docs/audio/import-guide.md` for `Assets/Audio/Music/_Incoming/`: delegate the move, settings, mixer and `MusicDirector` wiring to **cc-audio-specialist**, then do the lead's check and log each track in `docs/audio/suno-log.md`. Report the tracks imported, where they play, what you checked and anything not verified. Don't commit unless asked.
