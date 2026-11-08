# Scene 2 household simulator — first playable implementation

## Installed activities

The home scene now has seven authored world-space stations, routed from the existing
`HouseholdChoreInteractable` objects:

- Arrange keepsakes: pick up three objects, rotate and place in matching zones.
- Fetch water: fill a scoop at the source, carry it and tilt over the receiving pot.
- Measure rice: repeat three measured scoops; moving without tilting does not pour.
- Wash dishes: wet each bowl, apply soap, scrub three patches on each face, rinse and rack.
- Set the table and put away dishes: place three physical objects in their matching zones.
- Read mother's letter: open envelope, unfold, read, then explicitly continue into the existing story handoff.

The existing radio, wood delivery and imported Blender kitchen are retained. Kitchen mouse
gestures are now the default; F1 explicitly toggles the existing keyboard assistance.
Optional chores and the outdoor marble game retain their existing implementations.

## Controls

- E on an existing chore target: enter its activity.
- Hold left mouse: pick up / move / scrub.
- Mouse wheel while holding a bowl: switch between its inside and outside.
- Mouse wheel while holding a scoop: tilt / level it; pour only above the receiver.
- Mouse wheel while placing an item: rotate it.
- Click soap after wetting the bowl. Scrub the actual dirt patches with the sponge.
- F during dishwashing: equip the sponge and view the bowl close up / return to overview.
  Carrying the bowl temporarily returns to overview so basin and rack remain reachable.
- Right mouse: safely return held items, retaining earned work.
- Esc: leave, restore camera and input, and persist unfinished work.

## Ownership and persistence

`HouseholdWorkSession` captures and restores the camera parent/local pose, lens, HUD and
input lease. It does not reset another owner's input lock.

`HouseholdSimulatorState` contains gesture-independent rules. The simulator controller
maps ray hits and gestures to those rules; the visuals partial creates reusable runtime
meshes and materials with cleanup on scene unload. Manipulation targets are triggers;
external solid geometry blocks picking. Workstands have a solid tabletop.

`HouseholdChoreFlow` remains the only owner of order, rewards and chapter completion.
No enum IDs were changed. The original final-letter dialogue/outdoor transition is kept.

`Scene2Checkpoint.Work` and `WorkChore` store unfinished current activity data. They are
cleared when advancing to another required chore, starting a new game, or leaving home.
Kitchen work also serializes wash rounds, placed wood, embers, heat, fuel and cooking time.
Test scopes back up and restore both integer checkpoint keys and the string work payload.
Restarting household chores clears cached simulator states and generated finished props.

## Editor tools

- `KuTy > Scene 2 > Install household simulator`: additive, repeatable station authoring;
  does not rebuild the house, player, mother, or imported kitchen.
- `KuTy > Tests > Check household simulator`: rules plus real camera-ray gesture tests,
  pause, partial save/reopen, completion/rewards and camera/lens restoration.
  Uses checkpoint backup and restores it when leaving Play Mode.
- `KuTy > Tests > Check hands-on rice and cooking`: existing kitchen regression suite.

Local reports/screenshots live in ignored `Tools/ProjectAudit/Simulator` and
`Tools/ProjectAudit/simulator-checks.txt`.

## Deliberate limits

This is a simulator-lite gameplay implementation, not the final art/animation pass.
Work surfaces remain lightweight procedural meshes except the reused washing fixture.
Dishwashing, water scooping and rice measuring use their delivered Blender sets (see below);
placement activities share the imported bowl/plate.
Dedicated grasping hand animations, articulated chest and
cabinet doors, physically simulated liquids/cloth, spill damage, additional mother voice
recordings, optional-chore conversions and marble/office redesign are not implemented.
Placed items use supported zones rather than unconstrained Rigidbody stacking.

Automated ray/gesture tests are not a substitute for a human mouse-feel/playability review.

## Validation on 2026-11-08

- Household simulator: PASS, 717 assertions (including camera rays, external occlusion,
  16:9 / 21:9 framing, partial checkpoint reload, pause and chapter-reset cleanup).
- Existing imported kitchen: PASS, 39 assertions.
- Child wake-up: PASS, including feet on the floor rather than the mattress.
- Latest Windows development build: succeeded, 0 errors / 0 warnings, all six enabled scenes.
  Output: `Builds/Simulator-20261108/KuTy.exe` (local, ignored).
- Direct3D 11 player startup observed for 35 seconds with no startup exceptions; the test
  process was explicitly stopped afterward. This is not an end-to-end player playtest.
- A preliminary null-graphics/headless startup probe is not a supported URP visual test;
  its shader warnings were superseded by the real Direct3D startup observation above.

Unity-generated build prefilter/font changes and overwritten legacy preview PNGs were
restored to their pre-validation versions; only intended simulator/source/scene changes remain.

## Blender dishwashing integration — 2026-11-08

### Latest layout: preserve the legacy washing area

**Latest gameplay pass — authored transforms, not fixed positions:**

- The user has since arranged the individual props on/around the legacy stone fixture.
  Runtime now reads `WashBasin`, `DryingRack`, `DishBowl`, `ScrubSponge`, `SoapBar` from
  `Station.idleDressing`, preserving their authored world poses and scales. Do not rerun
  the original placement menu to bind gameplay: it is an initial-layout tool.
- Use `KuTy > Scene 2 > Bind authored dishwashing gameplay (keep layout)` only when
  repairing the entry binding. It does not reposition, rotate or scale any props.
- The interaction owner now lives on the persistent station, not the idle visual group:
  hiding idle visuals must not invoke OnDisable and cancel the task that just began.
- Bowl/sponge/soap query shapes follow mesh bounds; basin wetting radius, water level,
  rack drop region and pointer limits follow the authored geometry. Rack slots follow
  the enlarged/rotated rack, but clean bowls retain their own size rather than inheriting
  the rack's scale. Camera overview fits the real work bounds; HUD replaces fixed floor labels.
- The original fixture BoxCollider remains solid. Only dishwashing pointer occlusion uses
  a temporary exact mesh query for this fixture, so its broad box cannot block visible
  props sitting on a ledge. Actual mesh surfaces and other walls still obstruct picking.
  Basin/rack drop volumes no longer steal clicks from a visible bowl or sponge.
- F equips the sponge and transitions to close view. Hold left mouse over stains to scrub;
  F returns the tool safely and restores overview. Carrying a bowl uses overview automatically.
- **Latest validation: simulator PASS 754 assertions; kitchen PASS 39.** Tests include real
  player E focus from an unobstructed standing position, actual Interact/cancel/resume and
  completion callback, authored pose/scale preservation, zoom/tool toggle, both-face wash
  loop, checkpoint recovery and duplicate-reward rejection. Editor ends out of Play Mode,
  saved Home scene, no Console errors. No new standalone build in this pass.
- Camera readbacks: `WashDishes-CloseView.png`, `WashDishes-Blender-16x9.png`,
  `WashDishes-CleanRack.png` under `Tools/ProjectAudit/Simulator/`.

The following notes describe the earlier initial placement; the user's later arrangement
takes precedence over those initial coordinates:

- The original `HouseLayout/KitchenArea/Sink/DishwashingStationVisual` and its fused
  pots/basins remain in place and visible. The new working set occupies free floor beside
  its edge, at station world position approximately `(-1.727, 0.012, -0.494)`, yaw -90°.
- WashDishes no longer creates a procedural tabletop, cloth or legs. Other activities keep
  their existing supports. Serialized idle props make the new set visible before interaction;
  they hide while the real runtime objects are manipulated, return on cancellation/restart,
  and stay hidden behind the completed runtime set after completion.
- Authoring menu: `KuTy > Scene 2 > Place dishes beside existing washing area`.
  It checks for solid blockers and retains the original fixture, rather than overwriting it.
- All seven new normal maps were reimported and verified as NormalMap, linear, with no
  grayscale conversion. Import ordering now sets texture types before model import.
- Latest regression: **simulator PASS 745 assertions**, **kitchen PASS 39 assertions**.
  The simulator total is lower because the removed tabletop/legs no longer contribute
  per-renderer material assertions; added checks cover the retained fixture and idle lifecycle.
- The rice pot's currently authored position differs from the previous handoff. Its position
  was preserved; only washView was aligned to its current water bounds to fix the stale camera.
  Menu: `KuTy > Scene 2 > Align rice view to current pot`.
- Visual review: `Tools/ProjectAudit/Simulator/LegacyWashing-Idle.png` and the latest
  `WashDishes-Blender-16x9.png`. The earlier Windows executable predates this layout;
  this layout has been verified in the Editor, not rebuilt for standalone.

- Source delivery under `Tools/SimulatorAssets/Dishwashing_Set` is preserved unchanged.
  Six individual FBXs and 21 authored texture maps are copied into
  `Assets/_Project/Art/Scene2/Dishwashing`; the combined presentation FBX and `.blend` are not imported.
- Six standalone prefabs retain the attachment markers, with marker axes converted into
  engine-native coordinates. Rendered proxy meshes are stripped from the prefabs; the
  original exports are untouched. Runtime uses five query triggers, not 105 physics proxies.
- Seven URP Lit materials use sRGB albedo, imported tangent normals and generated linear
  metallic/smoothness maps (R = metallic, A = 1 - roughness). Basin metallic is 1; others 0.
- Imported bowl bounds verified in Unity: 0.14 × 0.065 × 0.14 metres. Six dirt contacts and
  their normals are sampled from the imported mesh during authoring, not from the former
  28 cm procedural bowl. The sponge projects onto the exposed face and cleans only near
  an actual stain; stationary movement, distant strokes and pointer jumps do not clean it.
- The green sponge face turns toward the bowl while scrubbing. Flipping updates both
  geometry and contacts immediately. Rinsing lowers the bowl into translucent water;
  lightweight ripples and shrinking foam supply feedback (not fluid simulation).
- Completed bowls are placed upside down at the three exported rack slots. Applying soap
  again resets rinsing. Wetting, soap, dirt, orientation, rinsing and delivered count keep
  using the existing partial-work checkpoint and reward owner.
- The imported bowl and plate are also used for setting the table / putting dishes away.
- `KuTy > Scene 2 > Import Blender dishwashing set` is repeatable and refuses to overwrite
  independently modified copied FBX/source maps. It requires a saved Home scene.
- **Quick human review:** open the saved Home scene, then
  `KuTy > Scene 2 > Preview Blender dishwashing (temporary checkpoint)`.
  This starts the real activity without replaying preceding chores. Stop Play Mode to
  restore the original checkpoint; the preview does not save the scene.

### Related kitchen access correction

Regression testing found that the existing ground-level rice pot was inside the legacy
sink's broad solid bounds. Only the rice-washing cluster and its close-view camera were
repositioned, clear of both the sink and the nearby chair. The house, sink solids, stove,
player/mother scales and chore IDs are unchanged. Repeatable repair menu:
`KuTy > Scene 2 > Clear imported rice washing approach`.
Kitchen targeting tests now aim at the actual imported interaction bounds, not a guessed
offset from the FBX pivot; they still require an unobstructed real E-interaction ray.

### Integration validation

- Household simulator after the layout correction: **PASS 758 assertions** (including
  imported art/material settings, five triggers, rack orientation, measured-face gestures,
  stationary/out-of-bowl rejection, partial dish save/reload, soap reapplication, existing
  placement/transfer/letter activities, pause, rewards and camera restoration).
- Imported rice/fire/cooking regression: **PASS 39 assertions**.
- Windows development build: `Builds/Dishwashing-20261108/KuTy.exe`, succeeded in ~76 s,
  **0 errors / 1 warning**. The warning is the pre-existing environment `Mesh_0` with more
  than 2,097,152 triangles using Fast Midphase; this integration does not modify that mesh.
  All six existing enabled scenes are included (Act3 is not removed).
- Direct3D11 standalone startup observed for 35 seconds without errors/exceptions in the
  startup log, then only the spawned test process was stopped. This is not an end-to-end
  standalone dishwashing playtest. Log: `Builds/Dishwashing-20261108/startup.log`.
- One-click preview verified entering the real imported activity (`IsWorking=true`), then
  exited; checkpoint test backup restored. Human mouse-feel review remains necessary.
- Import report: `Tools/ProjectAudit/Simulator/dishwashing-import.txt`.
- Render readbacks: `Tools/ProjectAudit/Simulator/WashDishes-Blender-16x9.png`,
  `WashDishes-InteriorFoam.png`, `WashDishes-ExteriorFoam.png`, `WashDishes-CleanRack.png`.
  Latest kitchen regression report/screenshots are retained under its `Kitchen/` subfolder;
  legacy tracked `Tools/Scene2Kitchen` outputs were restored to avoid generated diff noise.
  These 1600×900 camera renders exclude screen-space captions; `WashDishes.png` includes
  the user's docked Game View and UI. Automated gestures are not a human mouse-feel review.
- Scene 3 removal is still a separate, unimplemented scope change. Nothing here changes
  the existing ending/outdoor route or enabled scenes.

## WaterRice_Set integration — 2026-11-08

### Art and import

- Original delivery `Tools/SimulatorAssets/WaterRice_Set/` is untouched. Eight FBXs and
  25 PNGs are copied byte-for-byte into `Assets/_Project/Art/Scene2/WaterRice/`.
  No `.blend` or preview/studio scene is imported into Assets.
- Eight isolated prefabs retain 16 attachment markers. All 130 `COL_*` proxies are removed
  from the gameplay prefabs, not from the source exports. Gameplay uses three query triggers.
- Eight URP Lit materials use authored albedo/normal maps and generated linear metallic
  smoothness maps (R = metallic, A = 1 - roughness). Aluminum uses metallic 1; others 0.
  Water uses transparent URP Lit, not Blender's transmission nodes.
- Unity bounds verified: ladle `.320 × .055 × .110`, cup `.130 × .100 × .090`, bucket
  `.320 × .360 × .320`, basket `.380 × .200 × .380` metres (Unity XYZ).
- FBX Empty axes require semantic conversion after the mesh-root corrective rotation.
  Marker positions are preserved; ContentsRoot/FillCenter forward points physically upward,
  Grip forward outward (-Z), PourSpout forward toward -X and slightly up.
- The receiving pot prefab is derived from the **existing** Blender kitchen washing pot:
  keep Pot_Body/Handle_Left/Handle_Right, exclude original rice/water/paddle/colliders,
  normalize its bottom-center origin. The original imported model and scene pot are unchanged.
- Import/bind menu: `KuTy > Scene 2 > Import Blender water and rice set`.
  It requires the saved Home scene and refuses to overwrite independently modified copied
  source files. No station or prop transforms are changed: scene diff adds only the art reference.

### Runtime behavior and limits

- FetchWater uses coconut ladle + bucket; MeasureRice uses aluminum cup + bamboo basket.
  Existing workstands and chore targets/order/rewards/checkpoints are retained.
- Picking follows the real tool bounds. Filling tests the exported FillCenter against the
  source opening; pouring tests the actual PourSpout against the receiver opening. Rotation
  is around the exported grip pivot and lowers the -X pouring lip. Broad drop queries do
  not steal tool clicks. Dragging alone or tilting away from the receiver cannot transfer.
- The four delivered contents prefabs are retained as reference meshes. Runtime contents
  are **adaptively generated** using the handoff's inner-wall profiles and authored water/rice
  materials, not simple vertically scaled copies of those reference meshes.
- `WaterRiceFill` clips an inset cavity to a world-horizontal plane and triangulates a closed
  cap; this preserves the tapered cross-section and prevents star-shaped cap gaps. Empty
  contents hide. No rigidbody/physics collider is added to contents. Meshes are cached and
  destroyed with their runtime objects; geometry updates only when level/orientation changes.
- This is visual clipping, **not a fluid, exact-volume or granular solver**. At strong tilt
  the retained visual pool is capped at the lowest rim; logical fill stays governed by the
  existing chore rules. There is no spill-loss penalty or source inventory depletion. Rice
  also uses a level surface, not individual moving grains. Pour stream is a lit line effect.
- Both incomplete carried fill and receiver progress survive closing/reopening the activity.
  FetchWater still requires its existing water target; rice still requires three measured cups.

### Preview and validation

- `KuTy > Scene 2 > Preview Blender water scooping (temporary checkpoint)`.
- `KuTy > Scene 2 > Preview Blender rice measuring (temporary checkpoint)`.
- Hold left mouse to pick up the tool, put its bowl into the source, carry to the pot and
  use the wheel to tilt its lip over the opening. Right-click returns it safely; Esc leaves
  and saves partial work. Stop preview Play Mode to restore the original player checkpoint.
- Latest full simulator suite: **PASS 865 assertions**, including the authored dishwashing
  setup, profile containment at three fill levels, horizontal cap coverage, mild-tilt surface,
  real-ray tool pickup, wrong-place pour rejection, save/reopen, lip-driven stream, both
  transfer completions, pause, camera restoration and existing chore/reward checks.
- Kitchen regression: **PASS 39 assertions**. No standalone build was made in this pass.
- Both quick-preview menus were verified to enter their respective real activity
  (`IsWorking=true`, WaterRiceArt bound), then stopped. Checkpoint backup restored;
  final Editor is out of Play Mode, Home scene saved, Console has no errors.
- Import report: `Tools/ProjectAudit/Simulator/waterrice-import.txt`.
- Render readbacks there: `FetchWater-Blender-16x9.png`, `FetchWater-Pouring.png`,
  `FetchWater-Received.png`, `MeasureRice-Blender-16x9.png`, `MeasureRice-Pouring.png`,
  `MeasureRice-Received.png`. These camera renders omit the screen-space instructions.
- Human mouse-feel review and any subsequent in-room arrangement remain to be approved.
  The user's dishwashing arrangement and both Blender deliveries are preserved.
