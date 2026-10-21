# Thuận branch integration — 2026-10-22

Merged `origin/ThuanND_Scene_1` at `4a55a3c` into `main` with a merge commit. Git reported no conflicts. The branch includes Scene 3 village art, imported bamboo with smooth LOD transitions, sunset backdrop, and marble HUD updates.

Follow-up fixes:

- The vegetation renderer skips prototypes with an empty material array instead of indexing at `-1`. The rendering regression check exercises this case.
- The office smoke test now returns to the player's chair after using the real water dispenser before starting the rest sequence.
- Saved the already-authored Scene 1 appliance interaction zones, Excel background assignment and updated sleep-sequence serialization that were still pending in the open Editor scene. These scene settings were included in the successful story run.
- Added repeatable build-scene/reference and bamboo rendering checks that do not require the artist's untracked installation baselines.

Validation in Unity 2022.3.62f3:

- All five enabled build scenes: no missing script, missing scene GUID, broken serialized object reference, or Scene 3 shader compilation error. Scene 3 vegetation has 1,530 plants and 8 complete prototypes.
- Bamboo: LOD hysteresis/fade logic and 360 actual camera renders crossing both distance thresholds in both directions passed; empty-material regression passed.
- Scene 1 integration: **PASS (196 assertions)**, covering TTS/SFX, Excel, print collection, boss desk delivery, drinking and transition to Scene 2.
- Complete story flow: **PASS (52 assertions)**, including household progression, Scene 3, ending, pipeline restoration and replay reset.
- Editor returned to Edit Mode with no Console errors. The player's checkpoint backup was restored and removed after the tests.

Evidence: `Tools/ProjectAudit/merge-scene-validation.txt`, `merge-lod-validation.txt`, `merge-office-test.txt`, and `merge-story-test.txt`. This was Editor validation and Play Mode testing; a standalone player build was not produced.
