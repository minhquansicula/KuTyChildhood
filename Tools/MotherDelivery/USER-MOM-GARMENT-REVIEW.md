# Mother garment repair — work in progress

Working file: `Source/Mom_User_GarmentRepair.blend`.
Original input `C:/Users/LENOVO/Downloads/mom.fbx` is unchanged.

## Review in Blender

The selected rig is `Mom_User_Rig_Repaired`. Press Space in the 3D viewport to play the 461-frame review at 30 FPS:

| Frames | Motion |
| --- | --- |
| 1–61 | Standing idle |
| 62–146 | Sit down |
| 147–267 | Seated idle |
| 268–352 | Stand up |
| 353–461 | Call child / wave |

`PREVIEW_MomUser_GarmentRepair_Review` is for review only, not an eighth delivery clip. Seven individual copied clips have the `_GarmentRepair` suffix. Source actions and old character objects remain separate.

## Preserved and changed

- All 50,002 original outer faces, neutral vertex positions and UVs retained.
- Shirt/body and trousers/feet split at the waist; hidden overlap geometry added.
- 28-bone copied rig, including five baked cloth helpers; maximum four skin influences.
- 50,662 triangles across the two repaired meshes; Armature modifiers only.
- Sampled loop/transition checks pass, with no floor penetration beyond 2 mm.

## Unresolved — not final/game-ready

- Seated shirt/trouser probe reports 725 intersecting triangle pairs (not independent defect count).
- Strong dark/deformed region remains on rear upper trousers while sitting. Recomputed-normal, disconnected-normal-map and flat-material diagnostic renders do not remove it. Further geometry/weight investigation is required; changing textures alone is not a demonstrated fix.
- Checks sample selected frames, not continuous collision certification.
- No FBX exported; Unity import/animation has not been tested.

Machine-readable results are in `Source/user_mom_garment_repair_validation.json`, `Source/user_mom_garment_motion_checks.json`, and `Source/user_mom_garment_review_status.json`. Images are in `Preview/UserMomAnimation/GarmentRepair/`; files prefixed `Diagnostic_` are diagnostic renders, not the final appearance.
