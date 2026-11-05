# Mother — áo bà ba review

## Files and preservation

- Working file: `Source/Mother_AoBaBa_Review.blend`.
- Source retained: `Source/Mother.blend`; pre-edit snapshot: `Source/Mother_PreAoBaBa.blend`.
- Hidden independent object/mesh/armature/material backup: `BACKUP_Mother_Original`.
- All 20 original actions, both armature rest configurations, and original texture files are fingerprinted. See `Source/ao_ba_ba_preservation_manifest.json` and `Source/ao_ba_ba_validation.json`.
- No FBX export was performed for this revision. Older exports do **not** represent this revision.

## Object changes

| Object | Changes |
|---|---|
| `Mother_Body` | Existing mesh/UVs/weights/facial keys retained; added reversible low-amplitude middle-age facial corrective. WarmSmile reduced to 0.55 for a calmer resting expression. |
| `Mother_Shirt` | Existing garment retained; minimal neck and shallow central opening, eased waist, hip-length hem, locally split side-slit edges, restrained elbow/waist/hem folds. Original texture nodes retained behind a faded-brown palette node targeting sRGB `#795A48`; matte cloth response. |
| `Mother_Pants` | Existing mesh retained with reversible straight/loose-cut corrective; more volume at thighs, knees and ankles, subtle folds, ankle-length openings. Matte palette targeting sRGB `#252525`. |
| `Mother_Hair` | Existing scalp and bun retained; reversible bun flattening, small organic asymmetry and closer nape placement. Bun remains an intentionally overlapping weighted island, not a destructive scalp remesh. |
| `Mother_Footwear` | Existing plain sandals retained; reversible contact/clearance fit of soles and straps, no added decoration. |
| `Mother_Eyes` | Renamed only; existing mesh, texture, UVs and weights preserved. |
| `Mother_NeckContinuity` | Small additive patch of missing lower-neck skin from original source UV/topology/weights, revealed by lowering the old collar. Body was not replaced or merged with it. A non-applied normal-transfer modifier softens the junction. |

`Mother_AoBaBa_Relaxed_Idle` is a **new** action: arms lowered at the sides, slightly clear of the torso, wrists following the forearm, restrained finger curl. Original idle/walk/household actions were not edited. This review action is not a replacement for testing the existing animation library with the revised clothes.

## Inspection outputs

`Preview/AoBaBaReview/` contains before/after comparisons and front, back, left, right and three-quarter views in solid and material-lit modes, plus close-ups. Captures are Blender renders, not claimed as viewport screenshots. Neutral white area lighting and a gray environment were used; material color changes with lighting/AgX and is not a promise of an exact screen pixel hex.

Approximate visible character budget: **34,406 triangles**, including the neck patch, excluding the hidden backup and studio. No subdivision or destructive global merge was added.

## Validation and unresolved work

- Original action curves, original texture file bytes and both rest skeletons verified unchanged.
- No invalid skin weights, loose vertices, wire edges, greater-than-two-face mesh edges or inverted closed components were found in the inspected active meshes. Existing garment/skin boundaries are not all intended to be watertight.
- Neutral-pose tests found no surface crossings between trousers and shirt, body or footwear. Foot/sandal clearance was also checked after fitting.
- **Not certified intersection-free:** more detailed tests still flag garment self-crossings and shirt/skin contact around hem/hand and neckline regions. Some shirt results involve deliberately separate buttons/placket, but not every result has been classified. They are recorded rather than silently dismissed.
- Neck patch is separate and a subtle normal/color seam may remain at close distance. It is not a welded production topology repair.
- Bun/scalp meet by overlap; physically continuous shared topology was deliberately not forced onto the skinned mesh.
- Full-frame sitting, walking, household gestures, facial animation, self-collision and Unity import have **not** been re-certified. Clothing shape keys include neutral-pose corrections; animation-specific correctives may still be needed.
- No SDK/MCP export or unavailable UI capability is claimed. All edits and renders were executed via Blender MCP Python.

This is a preserved, inspectable **art-review revision**, not a final game-delivery certification.
