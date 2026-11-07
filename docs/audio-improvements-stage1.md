# Audio improvements — stage 1 (2026-11-06)

## Implemented

- `Resources/KuTyMix.mixer`: Master with Music, SFX, Voice and Ambience groups; four exposed category volume parameters.
- Master volume retains the existing `AudioListener.volume` behavior so legacy/unrouted sources still obey it. Master is NOT attenuated a second time in the mixer.
- `KuTyAudioMix`: cached runtime group lookup and routing using public Unity APIs. User gains live in the mixer; source volume remains owned by fades, gameplay and existing per-cue mixing.
- Settings now expose master, music, effects (including UI/radio), voice, ambience and mouse sensitivity. New audio categories default to unity, so existing saves/preferences retain their mix. Changes are stored in separate PlayerPrefs keys; closing settings saves preferences.
- Routed common AudioManager sources/pool, DialogueVoicePlayer, Scene 2 radio/mother/kitchen work, fallback 2D radio, and the inspected office atmosphere/opening/intro/door sources. Routing changes only; no office narrative timing, camera or scene changes.
- DialogueVoicePlayer tracks actual playback for music ducking, including mother's letter. Voice stop/finish allows music to recover smoothly; mute/disabled voice players do not report active playback.
- Missing ambient clips stop the previous ambient instead of silently leaving an unrelated scene's ambience playing. Re-selecting the same playing ambient no longer restarts it.

## Authoring

`KuTy/Audio/Configure category mixer` is an idempotent Editor-only configuration adapter for Unity 2022.3. The mixer was created with Unity Skills, then configured via this menu through Unity Skills. Internal authoring signatures were checked against UnityCsReference 2022.3; this adapter may need updating for another Unity version. Runtime audio has no dependency on UnityEditor or reflection.

`KuTy/Audio/Validate category mixer` checks category groups and exposed parameters. The existing isolated music test also validates this mixer before entering Play Mode.

## Verified in this implementation

- Unity compilation succeeded for the runtime and Editor assemblies.
- Saved mixer read-back contains five groups, the four exposed parameters and an Editor view.
- Fresh isolated Play Mode run: **45 assertions PASS**, including independent category gains, runtime routing, actual voice-driven ducking during letter music, recovery, six settings sliders, existing music routing/pause/repeat and menu audio.
- Tests restore the pre-existing category preferences. No checkpoint writes or home gameplay scene navigation.
- Reports: `Tools/Music/mixer-validation.txt`, `Tools/Music/playmode-checks.txt`.
- These are audio checks, NOT a rerun of the historic 133 Scene 2 assertions. No subjective sound-quality, clipping/LUFS, full-house listening or settings-layout screenshot approval is claimed.

## Not implemented yet

- Real house ambience, outdoor birds/wind recordings and footsteps.
- Replacement recordings for procedural kitchen water/fire/simmer, radio content, and material-specific mini-game sounds.
- New mother voice recordings or TTS replacement. Existing missing voice clips remain missing, with caption fallbacks unchanged.
- A global voice-priority arbiter or a full audit of every legacy/prologue audio path. New/third-party audio sources must explicitly route to the appropriate bus.

No new placeholder tones were generated to pretend these recordings exist. Next stage should acquire/record properly licensed assets, document their provenance, integrate them one group at a time and listen in the real house scene. Do not ship loose third-party recordings when their license prohibits standalone redistribution.

## Preserved

No mother model/animation integration, source TTS files, household enum IDs, quest order/rewards, checkpoints, house layout or connecting cutscene was changed by this stage. Existing concurrent local edits remain in place. No commit or push was performed.
