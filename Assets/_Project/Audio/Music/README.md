# Selected music — installed 2026-10-21

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
- Used as background music for home and village.
- This is NOT CC BY. Game background use and format conversion are permitted under the site's terms; standalone audio redistribution is prohibited. Distribute the compiled Unity game, not the loose MP3. The source MP3 and its generated meta are ignored by Git to avoid redistributing the loose material in repository clones. Each developer obtains it through the official download form and runs `KuTy/Audio/Install selected soundtrack` to bind its local GUID.

## Setup and validation

`KuTy/Audio/Install selected soundtrack` configures importers and the existing SoundLibrary asset. `KuTy/Audio/Validate selected soundtrack` checks all three clips, import settings, audio bootstrap references and routing; it writes `Tools/Music/validation.txt`.

Act 1 keeps its existing office sound design; menu music stops when entering it. Letter music begins with the close-up letter interaction and returns to the current scene's background when that interaction closes. State changes take priority over letter music. SFX volume and gameplay/save data are unchanged.
