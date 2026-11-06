# Scene 2 — Vietnamese bamboo flute soundtrack (2026-11-06)

## Installed

- User-provided MP3: Vietnam Bamboo Flute, VPRODMUSIC_Asia_BGM, Pixabay track 143601.
- Home BGM reference updated through Unity Skills and `KuTy/Audio/Install selected soundtrack`.
- Unity reports 152.87 seconds, stereo, 44100 Hz. Streaming/Vorbis quality 0.7, background loading, no preload. Original MP3 remains unchanged.
- Menu, village, letter and ending retain their existing tracks. Credits include the new title, contributor, source and license.
- No changes made by this task to the home scene, mother assets/integration, chore scripts, quest ordering/rewards or checkpoints.

## Mix

- Home music defaults to source gain 0.22 (existing BGM 0.4 multiplied by home gain 0.55). This is a starting mix, not a LUFS measurement or a guarantee of perceived loudness.
- Mother conversations, authored mother voice/captions and dialogue lower home music to 45% of its normal gain.
- Radio tuning, or a switched-on radio within 4 metres of the gameplay camera, lowers it to 25%. Leaving that area allows the background to recover.
- Strongest focus wins; overlapping radio and dialogue do not multiply attenuation.
- Gain changes use a short attack and slower recovery. Track fade envelopes are separate from focus gain so transitions and ducking do not fight over AudioSource volume.
- Pause freezes playback, fades and focus recovery. Full-song restarts use smooth fade-out/fade-in, not a seamless authored loop or simultaneous-source crossfade.
- SFX/voice gains are unchanged. Letter playback uses its original track and independent mix.

## Verification performed in this task

- Unity compiler: both project assemblies successful, no compiler errors.
- Soundtrack import/reference validation successful for all four clips.
- Fresh isolated Play Mode music checks passed: playback/routing, quiet home gain, focus multiplier rules, letter opening/completion/cancellation, village separation, pause/resume, song restart, ending priority, menu audio and credits.
- Report: `Tools/Music/playmode-checks.txt`. These are the music checks, NOT the 133 Scene 2 assertions from before the branch merge.
- Focus multiplier assertions verify the decision rules. They do not replace walking around the actual house, talking to mother and listening at the radio. No full home-scene playthrough or subjective listening-quality approval is claimed.

## Licensing and other developers

See `Assets/_Project/Audio/Music/README.md` for source hash and license links. The user supplied a download, not a license certificate. Retain a certificate if available; Content ID/streaming claims have not been tested.

The new loose MP3 and its Unity meta remain local/ignored to avoid publishing standalone music through the public repository. For another checkout, obtain the MP3 from the official page, place it under the documented local filename, then run `KuTy/Audio/Install selected soundtrack` to bind its local GUID. Missing local music must be resolved before building. No commit or push was performed.
