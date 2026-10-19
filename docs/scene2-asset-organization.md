# Scene 2 asset organization

Scene: `Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity`.

All 174 assets under `Assets/_Project/Art/Scene2` were renamed/moved through
Unity's AssetDatabase. Their `.meta` GUIDs were preserved. Imported FBX mesh,
material-slot and node identities remain intact.

## Folders and names

Models, Materials and Textures each use the same three categories:

- `Architecture`: house interior, facade, roof, timber, wall panels and ground.
- `Kitchen`: stoves, pots, rice jar, dish cabinet, dishwashing station and fittings.
- `Bedroom`: bed, bedroom shell, wall art, television stand and storage chest.

| Type | Pattern | Example |
| --- | --- | --- |
| Model or generated mesh | `SM_<Object>[_Variant]` | `SM_RiceJar.fbx` |
| Material | `MAT_<Object>[_Variant]` | `MAT_RiceJar.mat` |
| Texture | `T_<Object>_<Channel>` | `T_RiceJar_BaseColor.png` |
| Prefab | `PF_<Object>` | `PF_BedroomWall.prefab` |
| Shader file | `SH_<Purpose>` | `SH_KitchenAgedWalls.shader` |

Channels are `BaseColor`, `Normal`, `Metallic` and `Roughness`. Different assets
with the same role use numbered variants (`_01`, `_02`). House atlas fragments
whose function is unclear retain source part numbers. `SM_ImportedPanel_02`
retains a neutral descriptive name until its exact role is identified.

## Scene hierarchy

37 scene objects were renamed in place, including their visual groups:

| Old name | New name |
| --- | --- |
| `lunuoc` | `TraditionalHouseInterior` |
| Its interactive `tripo_part_10` | `WaterJarVisual` |
| `CaiLu` | `RiceJar` |
| `GocRuaChen` | `DishwashingStationVisual` |
| `TuChen` | `DishCabinetVisual` |
| `NoiBepLua` | `StovePotVisual` |
| `Decor_Textured` | `BedroomDecor` |
| `godownbed` | `BedExitPlatform` |
| Date/hash panel group | `PlasterPanels` with numbered children |

Setup scripts now use the new asset and object paths. Transforms, components,
quest bindings and the internal names of unidentified imported mesh parts were
preserved. The shader's internal ShaderLab name was preserved as well.

The pre-organization saved scene and complete old/new mappings are retained in
`.unity-validation/scene2-organization-20261019-132519/`. This backup includes
the Editor changes that were already unsaved when this task started.

## Verification

- All 174 destination assets exist and retain their original GUIDs.
- Unity reports zero missing scene references and zero C# compilation errors.
- All 777 serialized scene objects remain present. Transform values, mesh and
  script references, enabled states and serialized chore fields match the backup.
- The only additional external-reference entry is the prefab name override for
  `WaterJarVisual`; no existing external references were removed.
- The scene is saved. Nineteen obsolete empty folder trees were removed after
  checking that they contained no remaining assets. Their folder metadata can be
  restored from Git if needed; no art asset was deleted.

## Texture recovery after organization

Moving and renaming the extracted house textures caused the FBX importer's
filename-based texture lookup to leave its embedded materials' Base Map empty.
The earlier missing-reference check did not detect those empty texture slots.

`Scene2TextureRecovery` restores 35 original material/texture bindings for the
traditional house interior and rustic bedroom. The materials are now standalone
`MAT_TraditionalHouse_*` and `MAT_RusticBedroom` assets with explicit texture GUID
references, and both ModelImporters remap their original material identifiers to
these assets. Existing scene material overrides are updated as well. No texture
image, UV layout, transform or lighting value is changed by this recovery.

The Editor menu `KuTy/Scene 2/Verify restored house textures` checks the persistent
model bindings. A recovery backup of the scene and both importer metadata files
is in `.unity-validation/scene2-texture-recovery-20261019-133623/`.

## Window village backdrop

The heavy imported village model was replaced by a lightweight, non-interactive
window view under the scene root `WindowVillageBackdrop`. Its two children keep
their hand-placed transforms and use shallow curved cyclorama meshes so the view
continues below the window instead of ending in empty space.

The retained assets are:

- `Textures/Architecture/T_WindowVillage_Backdrop.png`
- `Materials/Architecture/MAT_WindowVillageBackdrop.mat`
- `Models/Architecture/SM_WindowVillageCyclorama_01.asset`
- `Models/Architecture/SM_WindowVillageCyclorama_02.asset`

The Editor utility `Scene2WindowVillageBackdropSetup` can regenerate the curved
meshes without changing the placed transforms. The obsolete `SM_Village.fbx`,
`PF_VillageExterior.prefab`, their 20 materials/textures, and the extracted
`SourceAssets/Village` copies were removed. Generated preview PNGs remain local
under ignored temporary folders and are not versioned.
