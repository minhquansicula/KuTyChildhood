# Đạt precise targeting integration — 2026-10-22

Merged `origin/dat-dev/feature/scene2` at `f2f62c2` into `main` without Git conflicts. Household tasks now require a camera ray hit; the proximity fallback is removed. Rice washing uses the imported pot's mesh colliders and retires the old floating trigger.

## Fix found in Unity

The rice pot was inside the dishwashing station's solid volume: its collider center was at world Y 0.38, while the station top was about Y 0.815. Camera rays hit the station before reaching the pot. The pot is now placed on the station, with its base at approximately Y 0.83, preserving its X/Z location, scale and mesh targeting.

Saving the scene also persists the existing wake-up field migration and the letter-placement component's automatic fit to its table. No proximity assistance was reintroduced.

## Checks

- Dedicated Play Mode spatial test: **PASS (66 assertions)**. Vo gạo, Dọn mâm and Mở thư each have multiple clear standing positions with successful production `PlayerInteraction` ray hits. Looking away, an intervening wall, and a five-metre distance do not select them. Opening the selected target and cancelling the minigame restores input without granting completion.
- Example verified feet positions: rice pot `(-4.300, 0.050, -1.647)`, serving table `(9.270, 0.050, -2.762)`, letter `(6.017, 0.050, -5.770)`. Aim at the visible object and press E once. The full report includes alternate positions and positions obstructed by existing scenery.
- All five enabled scenes and 32 minigame sprites passed the asset/reference checks.
- Scene 1 integration: **PASS (196 assertions)**.
- Complete story, Ending and replay: **PASS (100 assertions)**.
- Final Console: zero errors. The player checkpoint is restored after the tests. No standalone build or hardware E-key injection was performed.

Use `KuTy/Tests/Run precise Scene 2 targeting` for the spatial regression checks. Evidence is in `Tools/ProjectAudit/scene2-precise-targeting.txt`, `precise-story-test.txt`, and `precise-office-test.txt`.
