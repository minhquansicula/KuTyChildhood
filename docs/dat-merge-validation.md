# Đạt branch integration — 2026-10-22

Merged `origin/dat-dev/feature/scene2` at `c8333c5` into `main`. The only Git conflict was `BedWakeUpController.cs`.

Conflict resolution combines Đạt's 2.25-second opening/blink, steady head movement, 0.62-second rise and hidden cursor with the existing sleep handoff. It retains waiting for the persistent scene-cover overlay, eyelid rendering, configurable sleeping/awake camera angles, wake light, bed exit positioning and input cleanup. `FormerlySerializedAs("wakingDuration")` preserves the timing stored in the incoming scene.

Integration fixes:

- Runtime automatic collider creation skips orphan single-triangle/zero-area imported fragments. `tripo_part_33` previously caused PhysX mesh-cleaning errors whenever Scene 2 loaded. Other visible props keep their colliders.
- Test checkpoint backup now includes `Optional`, while still accepting older backup files with fewer keys.
- Story checks cover the incoming design of 12 required chores and 2 optional chores (130 total memory pieces), version 1 checkpoint migration, optional rewards and duplicate-completion guards.
- Every household action board is opened, restarted and closed without granting progress. These are UI/lifecycle checks, not mouse-gesture solutions for every minigame.

Validation in Unity 2022.3.62f3:

- Five enabled build scenes: no missing script, scene asset GUID, serialized reference or Scene 3 shader compilation issue; 32 Scene 2 minigame PNGs load as sprites.
- Sleep-transition integration: PASS, including full final voice playback, pause/resume, delayed activation of Scene 2, input ownership, hidden cursor, bed prompt/exit cleanup and normal direct home loading.
- Scene 1 integration: PASS (196 assertions).
- Complete story through Ending and replay: PASS (100 assertions), including all action-board lifecycle checks and checkpoint migration checks above.
- Final Unity Console: zero errors; Editor is out of Play Mode. Player checkpoint backup restored and removed.

Evidence is retained under `Tools/ProjectAudit/dat-*-test.txt` and `dat-scene-validation.txt`. No standalone player build was produced.
