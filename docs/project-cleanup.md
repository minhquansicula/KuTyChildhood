# Project cleanup — 2026-10-21

Removed 2,440.19 MiB of unused assets and disposable project output. A verified 389.23 MiB recovery archive is retained, giving **2.003 GiB net space recovered**, excluding any Unity cache changes.

## Removed

Six assets and their `.meta` files (315.32 MiB total), each with zero AssetDatabase referrers and checked against script references:

- `Assets/_Project/Art/Scene2/Models/Kitchen/SM_KitchenStructure_01.fbx`
- `Assets/_Project/Art/Scene2/Models/Architecture/SM_WallPanel_02.fbx`
- `Assets/_Project/Art/Scene2/Models/Kitchen/SM_FirewoodStove_03.fbx`
- `Assets/_Project/Art/Scene2/Models/Architecture/SM_TimberColumn.fbx`
- `Assets/_Project/Art/Scene2/Models/Architecture/SM_ImportedPanel_02.fbx`
- `Assets/_Project/UI/Sprites/bacground.png`

Disposable directories (2,124.87 MiB total): `.unity-validation`, `UnityChairValidation`, `Builds`, and `dumb`. The validation projects were closed. Their source files and older source backups, plus the drafts in `dumb`, were archived before deletion. Generated build outputs and validation caches were not archived.

## Recovery

`Tools/CleanupRecovery/cleanup-20261021.zip` contains 863 entries, checked for archive integrity and against source SHA256 hashes before deletion. Entries use project-relative paths; extract selected entries to restore assets together with their `.meta` files. Avoid overwriting newer sources. The archive is ignored by Git and sits outside `Assets`.

## Validation and scope

- All dependency paths remained identical for the five build scenes: MainMenu (23), Act1 (396), Act2 (307), Act3 (535), Ending (14). Every returned dependency under `Assets/` exists.
- SHA256 hashes of six scene files and four Resources assets remained unchanged.
- The active scene has zero missing scripts and zero missing references after cleanup. Unity is idle in Edit Mode and is not compiling. No Play Mode test or full build was run.
- Kept the main `Library`, `.git`, Blender sources, assets used by scenes, and assets referenced by Editor tools. Uncertain candidates were retained.
- This removes source/storage overhead; it does not reduce polygon counts of models still used in Scene 2.

Machine-readable evidence is in `Tools/ProjectAudit/cleanup-summary.json` and the associated before/after reports. The earlier `project-size-audit.md` describes the snapshot before this cleanup.
