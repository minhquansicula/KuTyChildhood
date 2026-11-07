# Selected music — updated 2026-11-06

Original MP3 downloads are retained as Unity source assets; Unity encodes build audio as Vorbis (quality 0.7), Streaming, stereo preserved, source sample rate preserved, Load In Background on, Preload Audio Data off. No unnecessary MP3 → OGG → Vorbis recompression step. Audio plays locally without network access. Full songs fade out/in when restarting; these are not seamless authored loops.

## Attribution

'Childhood' by Scott Buckley — released under CC-BY 4.0. www.scottbuckley.com.au

- Source: https://www.scottbuckley.com.au/library/childhood/
- License: https://creativecommons.org/licenses/by/4.0/
- Used for main menu.

'Reverie' by Scott Buckley — released under CC-BY 4.0. www.scottbuckley.com.au

- Source: https://www.scottbuckley.com.au/library/reverie/
- License: https://creativecommons.org/licenses/by/4.0/
- Used for mother's letter and ending.
- Both Scott Buckley tracks also require attribution in the description of YouTube uploads featuring them. In-game attribution is accessible through “Nhạc / Ghi công” in the main menu. Playback gain and fades are applied; the compositions are unchanged.

'田舎の夏休み' (Inaka no Natsuyasumi / Countryside Summer) by Yoshinori Tanaka (田中芳典).

- Source and official download: https://opentracks.com/bgm/detail/19313
- License: https://opentracks.com/help/articles/license/
- Composer conditions: https://opentracks.com/en/creator/detail/130 (site license).
- Used as background music for village. Home now uses Vietnam Bamboo Flute.
- This is NOT CC BY. Game background use and format conversion are permitted under the site's terms; standalone audio redistribution is prohibited. Distribute the compiled Unity game, not the loose MP3. The source MP3 and its generated meta are ignored by Git to avoid redistributing the loose material in repository clones. Each developer obtains it through the official download form and runs `KuTy/Audio/Install selected soundtrack` to bind its local GUID.

## Setup and validation

### Vietnam Bamboo Flute — Scene 2

- Artist: VPRODMUSIC_Asia_BGM, via Pixabay.
- Source: https://pixabay.com/music/world-vietnam-bamboo-flute-143601/
- License: Pixabay Content License, NOT CC0 or CC BY.
- Terms: https://pixabay.com/service/terms/ (section 5); summary: https://pixabay.com/service/license-summary/
- Provided by the user as `vprodmusic_asia_bgm-vietnam-bamboo-flute-143601.mp3` on 2026-11-06.
- Local asset: `Assets/_Project/Audio/Music/VietnamBambooFlute_VPRODMUSIC.mp3`.
- SHA-256: `F31C2DEF5E5A911804335FDAA06AB8457EEBACC83FF9CA136A40B825C5BA4D40`.
- License permits incorporation into a larger creative work, subject to its restrictions; do not redistribute this MP3 as standalone content. The MP3 and its generated meta stay local/ignored, like the OpenTracks source. Other developers must download from the official page, rename the local copy as above, and run the installer to bind their local GUID. Ship the built game, not loose music downloads.
- Keep the downloader's license certificate if available. No certificate was supplied here. Content ID status has NOT been verified; videos/streams may require a certificate to resolve claims. This is not a guarantee against third-party claims.
- Original source audio is preserved. No pitch/time stretching, artificial instruments or destructive processing.
- Home uses 0.55 times the existing BGM setting (default 0.4 -> 0.22 source gain, not a measured loudness percentage). Mother dialogue/voice ducks home music to 45% of that; radio tuning or an active radio within 4m of the camera ducks to 25%. Leaving the radio restores the background instead of suppressing music for the rest of the chapter. Overlapping focus uses the stronger reduction. Attack 0.35s, recovery 1.8s; SFX levels unchanged. Letter music keeps its own existing track and mix.
- Smooth fade-out/fade-in separates full-song repeats; this is NOT a seamless loop or overlapping two-source crossfade.

`KuTy/Audio/Install selected soundtrack` configures importers and the existing SoundLibrary asset. `KuTy/Audio/Validate selected soundtrack` checks all three clips, import settings, audio bootstrap references and routing; it writes `Tools/Music/validation.txt`.

Act 1 keeps its existing office sound design; menu music stops when entering it. Letter music begins with the close-up letter interaction and returns to the current scene's background when that interaction closes. State changes take priority over letter music. SFX volume and gameplay/save data are unchanged.
