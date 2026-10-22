# Office sunset lighting polish

Scene: `Assets/_Project/Scenes/Act1_Office_Blockout.unity`.

## Authoring

- `KuTy/Office/Polish Sunset Lighting` reapplies the balanced lighting and saves the office scene.
- `KuTy/Office/Capture Polished Lighting` renders four 1600×900 HDR/MSAA previews to `Assets/Screenshots/OfficePolished_*.png`.
- The first application saves `Assets/_Project/Scenes/Backups/Office_BeforeLightingPolish.unity` as a scene backup. Original shared URP and volume assets are retained.

## Rendering

The office uses `Assets/_Project/Art/Office/Lighting/OfficeCinematicURP.asset` with a dedicated renderer, HDR colour grading, MSAA 4×, additional-light shadows, four main-light cascades, and reflection-probe blending/box projection.

`OfficeRenderQuality` applies this asset at runtime while the lighting root is enabled and restores the previous quality pipeline and AA setting on disable. Edit Mode keeps the project's normal pipeline; use the capture menu or Play Mode to see the complete office render configuration.

Only the desk lamp and boss accent need additional-light shadow maps. Broad ceiling and bounce fills remain shadowless. Practical diffuser strips make the imported ceiling fixtures visibly luminous without adding more lights.

The office's dedicated volume uses ACES, restrained bloom, lower contrast, lighter vignette/grain and a subtle warm white balance. The legacy gameplay volume with missing component references is disabled. Glass materials retain their existing direct-specular suppression.

## Indirect illumination

The delivered scene uses art-directed realtime fills plus trilight ambient illumination. A CPU lightmap attempt reported out-of-memory failures and produced black illumination. That bake was rejected: its lighting data, temporary UV meshes, lightmap textures and probe group were removed; original architecture mesh references and static flags were restored from the backup. No successful baked GI is claimed.

For a future baked-GI pass, isolate interior geometry from the city backdrop and reduce texture/mesh memory before baking.

## Verification

- Unity script compilation completed successfully.
- Four dedicated HDR previews were visually inspected.
- A five-second Play Mode observation reported zero runtime errors. Automatic Game View screenshot capture timed out; the four camera-rendered previews are the visual evidence.
- A separate runtime read-back confirmed `OfficeCinematicURP`, MSAA 4× and additional-light shadows were active in Play Mode.
- Exiting Play Mode restored `Village_URP`; project QualitySettings have no content diff.
