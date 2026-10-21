---
name: cc-suno-sounds
description: "Chief Complaint sound effects with Suno (Sounds tab). Writes ready-to-paste Suno Sounds prompts (description, One-Shot or Loop, BPM, Key) for UI and gameplay SFX, stings and ambience loops from the project's sound brief, and with 'import' brings downloaded files from Assets/Audio/SFX/_Incoming into the game. Use when the user wants a sound effect, sting, jingle or ambience."
argument-hint: "<cue or moment, e.g. 'SFX_Patient_Cured' or 'card draw'>  |  import"
disable-model-invocation: true
---

# Suno Sounds (Sounds tab)

Suno's **Sounds** tab makes short samples: a text description plus **Type** (One-Shot or Loop), **BPM** (Auto or a number) and **Key** (Any or a key). Suno has no API, so the user generates in the browser; you write the prompts and the team imports. Use Australian English. Read `docs/audio/sound-brief.md` first; its **Identity** and **Sound cues** sections are the source of truth. Don't use unofficial Suno APIs.

## Mode 1: Write prompts (default)

1. Match the request to a sound cue in the brief (`SFX_*`, `Sting_*`, `Amb_*`). For a new moment, propose a cue (when it plays, character, type) and add it to the brief once the user agrees. Check the game event it will hang off exists (see `CLAUDE.md`); if not, note that the lead must add one.
2. For each cue, give a block matching the Sounds tab:

   ```
   Sound:  <one or two plain sentences: the source, texture, length, start/end, e.g.
            "Short warm three-note chime rising upward, soft synth bell, clean and dry,
             starts instantly, about 0.8 seconds, no reverb tail">
   Type:   One-Shot | Loop
   BPM:    Auto | <number>   (set it for musical stings and loops so they sit with the music)
   Key:    Any | <key>       (match the music cues' key for stings and musical blips)
   ```
   - Describe what the sound **is**, not what it means ("rubber stamp thud and paper shuffle", not "bureaucracy").
   - For one-shots say "starts instantly" and give an approximate length. For loops say "seamless loop, steady, no build".
   - Keep SFX dry and short. Ask for "no music, no voice" when a description could drift into a song.
3. Give **three variations** per cue (e.g. softer, brighter, shorter); sound effects need more picking than music.
4. Remind the user:
   - generate several takes and keep the best
   - download as WAV if possible
   - put the files in `Assets/Audio/SFX/_Incoming/`
   - commercial use needs a paid Suno plan
5. Stop. Don't create assets in this mode.

## Mode 2: Import (`/cc-suno-sounds import`)

Load `unity:audio-setup-mixers` and `unity:optimize-audio` (and tell cc-audio-specialist to load them too), then follow `docs/audio/import-guide.md` for `Assets/Audio/SFX/_Incoming/`. Delegate to **cc-audio-specialist**:
- the move, to `SFX/` or `Stings/`
- import settings for short SFX, longer SFX, stings or ambience
- mixer routing: SFX, UI for clicks, or Music for stings
- `SfxPlayer` or `MusicDirector` wiring

Then do the lead's check, playing each cue in play mode, and log each file in `docs/audio/suno-log.md`. Report what was imported, what triggers each sound, what you checked and anything not verified. Don't commit unless asked.
