# Integrating nhan-dev Scene 3 — 20/10/2026

Merged `origin/nhan-dev/feature/scene3-shop` at `9535a75` into `main` after saving the current office, audio, checkpoint and household work in `19b4a5d`. Git merged the shared marble gameplay automatically; no conflict markers required resolution.

## Integration fixes

- `RiceCardMeshGenerator` no longer generates assets, overwrites field settings or saves the open scene on every script reload. Applying card meshes is an explicit `KuTy/Rice/Apply Rice Card Meshes to open village` command, available only for the outdoor village in Edit Mode. The user reviews and saves the result.
- The village URP asset is no longer the global quality override for unrelated scenes. `VillageVisualIntegration` makes a runtime copy for resolution adjustment, restores the previous pipeline when leaving Scene 3, and destroys the copy. Play Mode does not modify the shared URP asset.
- The letter transition fallback waits while the dialogue is open, preserving unlimited reading time.
- Story smoke checks follow all 14 household chores, verify the one-time 140 memory pieces and 2,000 đồng allowance, load the actual outdoor village, then test purchases, marble cancellation/camera restoration, winning, key crafting, ending and replay. The former test incorrectly expected a journal-first dish-only sequence within Scene 2.
- Checkpoint smoke tests back up all chapter PlayerPrefs and restore them on returning to Edit Mode, including early cancellation.
- Prototype validation finds ItemData under the project root instead of the obsolete `ScriptableObjects/Items` folder, so moved item assets remain valid.
- Full story checks reuse the current office test runner, whose completion event can hand off to the household chapter without stopping Play Mode; the standalone office test still stops normally.

## Verification

Unity 2022.3.62f3 compiled the merged project successfully. The complete story run passed 89 office assertions and 52 story assertions, including pipeline cloning/shared-asset preservation/restoration, the real Scene 2 → Scene 3 handoff, ending and replay. The household interaction suite also passed all 70 assertions on the merged project. The earlier memory-only run passed 47 assertions before adding render regression checks. The test deliberately completes chores and moves target marbles through code to verify story state and event wiring; it does not simulate human mouse gestures or measure frame rate.

Re-run from the Editor:

- `KuTy/Tests/Run story smoke test`: office → household → outdoor village → ending → replay, including village pipeline isolation.
- `KuTy/Tests/Run merged memory chapter smoke test`: the same household/outdoor checks without the office opening.
- `KuTy/Tests/Check household interactions`: gesture surface construction, cancellation and completion invariants for all 14 chores.
- `KuTy/Tests/Run Act1 office task flow + edge cases`: detailed office task, audio and input checks.
