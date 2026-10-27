# Importing Suno audio

Used by `/cc-suno-music import`, `/cc-suno-sounds import` and `/cc-suno-speech import`. The lead delegates the import to **cc-audio-specialist**, then checks the result in the Editor.

## 1. Match files to cues
List the relevant `_Incoming` folder and match each file to a cue in `docs/audio/sound-brief.md`. Ask the user if a match isn't obvious, or before replacing a cue that already has a file. Add new cues to the brief once the user agrees.

## 2. Rename and move (in the Editor)
Use `AssetDatabase.MoveAsset` so `.meta` files and GUIDs move with the audio.

| Kind | Destination |
|---|---|
| `Music_*` | `Assets/Audio/Music/` |
| `Sting_*` | `Assets/Audio/Stings/` |
| `SFX_*`, `Amb_*` | `Assets/Audio/SFX/` |
| `VO_*` | `Assets/Audio/Voice/` |

## 3. Import settings

| Kind | Load Type | Format | Other |
|---|---|---|---|
| Music loops | Streaming | Vorbis ~70% | Load In Background on, Preload off |
| Ambience loops | Streaming | Vorbis ~60% | Force To Mono unless clearly stereo |
| Stings | Compressed In Memory | Vorbis ~70% | |
| Short SFX (< 1 s) | Decompress On Load | ADPCM | Force To Mono |
| Longer SFX | Compressed In Memory | Vorbis ~70% | Force To Mono |
| Voice | Compressed In Memory | Vorbis ~60% | Force To Mono |

Trim leading silence on one-shots and voice lines, and check that loops cut cleanly. If a file needs editing outside Unity (trim, loop point, normalise), tell the user exactly what to do rather than guessing.

## 4. Mixer
`Assets/Audio/Mixers/MainMixer.mixer`: Master → **Music**, **SFX**, **UI**, **Voice**, each with an exposed volume parameter. Duck Music by about 6 dB while a Voice line plays. Mixer authoring is visual, so if a command can't do it, give the user exact manual steps and wait.

## 5. Wiring (events, never polling)
Audio scripts live in `Assets/Scripts/Audio/` (`ChiefComplaint.Audio`) and only listen to game events (see the event list in `CLAUDE.md`). They never drive game state.
- **MusicDirector:** plays one loop at a time with a crossfade of about 1.5 s. It reacts to `GameManager.OnPhaseChanged` (Shift, StaffRoom, GameOver) and to `PatientCardView.OnStabilityChanged` (switching between Calm and Tense).
- **SfxPlayer:** plays pooled one-shots on events such as `DraggableCard.OnCardPlayed`, the diagnose, cure and death events, `OnFundingChanged`, and button clicks. It adds a little pitch and volume variation to repeated sounds.
- **VoicePlayer:** plays one line at a time, queues or drops lines that overlap, ducks the music, and has a cooldown per cue so announcements don't repeat.
- If an event a cue needs doesn't exist, report it to the lead; don't add it yourself.

## 6. Check (lead)
- Read the diff and confirm the `.meta` files moved with the audio.
- Recompile, check the console and run the EditMode tests.
- Play mode: trigger each new cue (`unity command script eval`), then exit play mode.
- Ask before saving the scene if it may hold the user's own edits.

## 7. Log
Append each imported file to `docs/audio/suno-log.md`: cue, date, Suno tab, prompt or script, and Suno plan. Create the file if it doesn't exist.
