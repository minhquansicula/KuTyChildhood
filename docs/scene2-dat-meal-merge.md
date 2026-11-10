# Dat meal integration — 11/11/2026

Merged `origin/dat-dev/feature/scene2` at `91e1d58` into `main`, following the user's explicit merge/resolve/push request.

## Resolution

- Preserved pre-existing local five-task gameplay, table UX and source kitchen in commit `e9f9899` before merging. Unrelated Blender/Tools working files were not staged.
- Resolved Home YAML conflicts by preserving local scene objects and adding Dat's meal. Checked unique document IDs, local reference integrity and preservation of all 1,777 pre-merge serialized objects.
- Reused Dat's original canh/thịt kho/rau xào meshes and materials; fitted their scene-instance footprints to the existing table lanes. Source assets/prefabs were not regenerated.
- `Scene2DatMealIntegration` binds those three objects to the existing `HouseholdChoreInteractable` / `Scene2HouseholdSimulator` flow. The incoming standalone `Scene2FoodInteractable` components are removed from these scene instances, so they cannot bypass task gating, checkpoints or rewards. Their source scripts remain available.
- Retained task order, enum IDs, three placement bits, camera/input lease, partial save/reopen, completion beat, one-time reward, washing, letter and ending route.
- Runtime meal copies preserve source orientation and materials. A separate table-source visibility cache prevents duplicate meal visuals and avoids interference with washing-source restoration.
- Former table mesh references were borrowed from washing props: their original positions and active state are retained. They are not retired/deleted when selecting Dat's art.
- Incoming home hands, reach layer and cutscene changes were merged as well.

## Verification on merged scene

- Unity compilation: successful, zero C# errors.
- Four activities + UI + table interactions: **200 assertions PASS**, including Dat-source lifecycle/reset and continued washing-source availability.
- Original source kitchen pointer/lifecycle: **81 assertions PASS**.
- Authored dishwashing pointer/lifecycle: **90 assertions PASS** after correcting the shared-source visibility integration.
- Chapter handoff / ending: **43 assertions PASS**, including natural final-card reveal and 50/50 pieces without Scene 3.
- Five-task checkpoint migration: **78 assertions PASS**.
- Table UX recording suite: **144 assertions passed before a native VideoPlayer playback failure** at decoded sample frame 35. This is NOT an overall passing suite. Gameplay, three aspect ratios, complete dish bounds, material/orientation reuse, click/drag placement, cancel, partial checkpoint/reopen, one-time reward and camera/source restoration passed; metadata preparation succeeded, but full movie playback was not verified. No assertion was removed or relaxed to hide this failure.
- Inspected actual `idle.png` and `valid.png` Game View captures: Dat's food is visible with the current Vietnamese task UI and matching placement preview.
- These are scripted Editor checks, not an independent human mouse-feel/standalone acceptance test. No new player build was produced.

Evidence remains local under `Tools/TableUxSlice`, `Tools/Scene2FourActivities`, `Tools/DishwashingSimulator`, `Tools/ExistingAssetMinigames/SourceArticulation`, `Tools/Scene2FiveTasks` and `Builds/Integration-20261109/merged-journey.txt`. Previous affected Tools evidence was copied to `Builds/DatMealValidation-20261111/*-before` before test runs. The automatic multi-suite queue did not complete; only the individually verified fresh reports above are claimed.
