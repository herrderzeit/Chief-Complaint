# Chief Complaint sound brief

The shared sound identity for every Suno prompt and audio decision. Change this file, not individual prompts, when the direction changes. Suno has three modes, each with its own workflow:

| Suno tab | Workflow | Used for |
|---|---|---|
| **Songs** | `/cc-suno-music` | Music loops |
| **Sounds** | `/cc-suno-sounds` | Sound effects, stings, ambience loops |
| **Speech** | `/cc-suno-speech` | Voice lines and announcements |

Importing works the same for all three: see `docs/audio/import-guide.md`.

## Identity

- **Setting:** an overstretched modern emergency department. Clinical, fluorescent, always slightly behind.
- **Tone:** tense medical drama with a dry, satirical edge (bureaucracy, Red Tape, "Page the on-call team"). Serious stakes, never grim; wry, never goofy.
- **Palette:** analogue and modern synths, pulsing arpeggios, soft electric piano, muted plucks, light percussion (rimshots, shakers, brushed kits), sub bass. Heartbeat-like kick patterns and monitor-like bleeps are welcome **as musical elements**.
- **Avoid:** vocals in music (instrumental only), epic orchestral or trailer drums, horror stabs, comedy kazoo or cartoon cues. Don't copy a real hospital alarm exactly; stylise it, so players never mistake game audio for a real device.
- **Mix:** music sits under gameplay. Leave space in the 1–4 kHz range for UI sounds and voice, and keep dynamics gentle so loops don't jump in volume.

## Music cues (Songs → `/cc-suno-music`)

| Cue | When | Mood | Tempo | Length / form |
|---|---|---|---|---|
| `Music_Menu` | Title and tutorial | Calm, curious, a little ironic | 90–100 BPM | 1:30–2:30, seamless loop |
| `Music_Shift_Calm` | Shift phase, board under control | Focused, steady, busy-but-fine | 100–110 BPM | 2:00–3:00, seamless loop |
| `Music_Shift_Tense` | Shift phase, a patient at low stability or Code Blue | Same key and tempo as Calm, more urgent layers | Same as Calm | Same length, so it can crossfade |
| `Music_Draft` | Draft and upgrade screens | Reflective, planning, lighter | 80–90 BPM | 1:30–2:00, seamless loop |

Calm and Tense are a pair: generate them in the same key and BPM so the game can crossfade between them.

## Sound cues (Sounds → `/cc-suno-sounds`)

Short, dry and clean, with no reverb tails longer than the sound itself. One-shots should start instantly: no silence at the front.

| Cue | When | Character | Type |
|---|---|---|---|
| `SFX_Card_Draw` | A card enters the hand | Soft paper flick | One-Shot |
| `SFX_Card_Play` | A card is played | Firm paper slap with a short click | One-Shot |
| `SFX_Card_Invalid` | Illegal drop or not enough AP | Muted low buzz, not harsh | One-Shot |
| `SFX_Patient_Diagnosed` | A symptom is diagnosed | Bright two-note monitor blip | One-Shot |
| `SFX_Patient_Cured` | A patient is discharged | Warm rising three-note chime | One-Shot |
| `SFX_Patient_Died` | A patient dies | Stylised flatline tone that fades out | One-Shot |
| `SFX_Funding_Gain` | Funding earned | Light coin and register tick | One-Shot |
| `SFX_Page_OnCall` | Page the on-call team | Retro pager beep, two pulses | One-Shot |
| `SFX_RedTape` | Red Tape card added | Rubber stamp thud plus paper shuffle | One-Shot |
| `SFX_UI_Click` | Buttons | Tiny soft click | One-Shot |
| `Sting_Victory` | Run won | Relief, warm, a little triumphant, 5–10 s | One-Shot |
| `Sting_GameOver` | Reputation hits 0 | Deflated, sardonic, 5–10 s | One-Shot |
| `Sting_Escalation` | Ambulances or Trauma Flights unlock | Rising alarm, 3–6 s | One-Shot |
| `Amb_ED_Background` | Under the shift music | Distant ward hum, muffled chatter, far-off monitors | Loop |

## Voice (Speech → `/cc-suno-speech`)

- **Voices:** a tired-but-kind triage nurse (tutorial, hints); a crisp hospital PA announcer (escalations, Code Blue); a brisk paramedic (Trauma Flight handover). Neutral, globally understood English accents; no regional slang.
- **Delivery:** short lines (under 4 seconds for barks), clear diction, dry humour. Never mock patients.
- **Use globally understood medical terms** (e.g. "Emergency Department", "Registered Nurse"), as on the cards.

| Cue | When | Voice | Example line |
|---|---|---|---|
| `VO_PA_CodeBlue` | A Code Blue starts | PA announcer | "Code Blue, Emergency Department. Code Blue." |
| `VO_PA_Ambulances` | Ambulances unlock | PA announcer | "Attention staff: ambulance arrivals incoming." |
| `VO_PA_TraumaFlight` | Trauma Flights unlock | PA announcer | "Trauma team to the helipad. Trauma Flight inbound." |
| `VO_Paramedic_Handover` | A Trauma Flight arrives | Paramedic | "Handover: primary's confirmed, the rest is yours." |
| `VO_Nurse_Tutorial_*` | Tutorial steps | Triage nurse | One line per tutorial step |

## Files and licence

- Downloads go in the matching drop folder: `Assets/Audio/Music/_Incoming/`, `Assets/Audio/SFX/_Incoming/` or `Assets/Audio/Voice/_Incoming/`. After import they are renamed to their cue name and moved out of `_Incoming`.
- Every kept file is recorded in `docs/audio/suno-log.md`: cue, date, Suno tab, the prompt or script, and the Suno plan it was made on. Commercial release needs audio made on a paid Suno plan.
