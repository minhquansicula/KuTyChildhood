# Scene 3 art direction — first implementation

Date: 21 October 2026. Implements the first review slice of [the art direction plan](scene3-village-art-direction-plan.md), not the complete village redesign.

## Saved changes

`Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity` now contains `VillageArtDirection_Sample` around the existing marble yard:

- Three children in baked crouching poses and one standing spectator replace the visible twelve-child shoulder crowd. The original group remains inactive for restoration. Source FBX files and textures are unchanged; the sample adds no Animator, runtime script, or NPC collider.
- Two low lime-coloured boundary walls with brick bases/caps, two gateposts, an open grey-green wooden gate, and three shared-mesh earthenware jars establish an initial yard edge. These are provisional simple meshes/materials, not finished weathered assets.
- The existing sun uses a 32° elevation, yaw −48°, colour `(1, .91, .77)` and intensity `1.15`. This is a lighting trial; the existing sky, fog, pipeline, shadow distance, water, rice instancing and tree LOD configuration are retained.

The sample uses the actual marble ring and playing surface as anchors. The yard's mesh contains an offset: its Transform at `(2, .125, 0)` is not its visible location. The ring is around `(-13, .225, 44)`. The yard, minigame, shop, houses and quest objects have not been relocated. Moving the yard towards the entrance belongs to a later composition decision with its complete gameplay hierarchy.

## Editor workflow

Implementation: `Assets/_Project/Editor/Scene3VillageComposition.ArtDirection.cs`, a separate partial of the existing composition tool. It never invokes the full scene rebuild.

Open Scene 3 in Edit Mode, then use **KuTy > Scene 3 > Art direction**:

1. **Capture baseline** saves the original scene, reads checkpoint values without modifying them, inventories existing assets, and renders four 1920 × 1080 views.
2. **Apply marble-yard sample** adds the sample once, verifies original gameplay component serialization and walking routes, then saves. Reapplication is rejected to avoid duplicate props and cumulative transforms. Failed validation reverts the operation through Undo before saving.
3. **Validate and capture comparison** repeats grounding/access checks and renders the same four cameras with the same post-processing settings. Wait for the environment probe to update after changing lighting before capturing.
4. **Refine sample props and wall shadows** updates only the generated jars and boundary wall shadows, with the same validation and Undo protection.

Local evidence is in `Tools/Scene3/VillageComposition/ArtDirection/` (ignored generated output):

- `before.unity`: original scene safety copy. Open this copy and save it over the original scene only when intentionally restoring the whole baseline; do not replace the original scene's `.meta` or GUID. Later unrelated scene edits would also be reverted by a full restore.
- `Before/` and `After/`: entrance, marble-yard, village-end and overview captures.
- `inventory.txt`, `checkpoint-before.txt`, `checkpoint-after.txt`, `sample-validation.txt`, `sample-walking.txt`: inspection and validation evidence.

## Verification and review status

Unity 2022.3.62f3 imported and compiled the tool. Applying the sample verified all 45 existing runtime script components against their serialized state before the edit. The reference walking checks passed, including the three crossings and access to the marble yard. New checks passed through the south approach, east approach and west gate in both directions with a 1.6 m tall, 0.24 m radius CharacterController. Child scale and grounding passed. The final comparison was inspected visually in the Editor renders.

This is **stage 1 evidence collection and a stage 2 yard blockout**, not approval of the whole 12–15 m house–lane sample. The frontage still needs a closer house relationship, weathering, a softer dirt-to-grass transition and a clearer connection to the lane. The tree canopy is still too dense relative to the target. The end-of-village camera remains a composition check, not proof of a completed gate.

No new Play Mode gameplay test was run. Shop purchases, minigame completion, journal/key/door progression and a complete two-way village traversal remain pending. No test progress was created or cleared. The art pass only reads checkpoint keys and records whether they still match the baseline.

The GPU is an NVIDIA Quadro T2000. The raw `editor-stats-before.json` sample was taken during the existing Play Mode session, before it was stopped; its instantaneous timings are unsuitable for an FPS claim or before/after comparison. Warmed-up 1080p build CPU/GPU frame times, shadow cost and traversal stability are still pending. The plan's 60 FPS target has not been demonstrated. CLI build/run remain disabled in this project's existing configuration.

Next review: assess the yard poses, boundary proportions and afternoon colours from the actual player and marble cameras, then finish the nearby house–lane connection before expanding the art treatment to the rest of the village.
