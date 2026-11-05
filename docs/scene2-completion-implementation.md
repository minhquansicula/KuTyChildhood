# Scene 2 — Household flow completion

## Scope

The implementation preserves `Act2_MemoryWorld_Home`'s authored layout, props, character assets and serialized enum IDs. No scene or character asset is rewritten. Physical radio/wood/kitchen interactions and illustrated 2D mini-games remain complementary, not competing progression systems.

## Changes

- Twelve required chores award 120 pieces; two optional chores award five each. The board separates essential progress from optional bonuses instead of implying that 130 pieces are required to finish.
- Removed the gameplay P shortcut that erased all household progress. P still restarts only the active 2D mini-game. Starting a new game remains the explicit way to clear a chapter checkpoint.
- Added chapter headings and concrete instructions for every required chore. Mother conversations and H guidance share the same instructions, including the current wood-carrying destination.
- Refreshed HUD targets after completion/world-state changes, prefers physical interaction targets, and can find enabled child colliders for imported props.
- Added a larger household task board with optional tasks and save information; retained outdoor board sizing.
- Mother conversation uses a dedicated readable text area and three vertically arranged choices. Irrelevant progress/restart controls are hidden and restored when returning to a mini-game.
- Optional rewards now display +5 rather than +10. Shared theme fonts work even without the legacy HUD font reference.
- Radio/music/mother audio explicitly pause and resume. Music ducks during other narration/conversations. Obsolete kitchen calls are discarded after wood delivery; text-only mother speech also reserves its speaking interval.
- Radio cleanup uses Unity's destroyed-object checks, avoiding `MissingReferenceException` when its audio rig unloads before the sequence owner.
- Physical interactions release held mouse gestures on pause/focus loss. Radio and kitchen cancel restore the camera and release input. World restoration uses completed chore IDs rather than hard-coded order indices.
- Incomplete physical rigs report a warning and disable themselves rather than throwing during initialization.

## Regression checks

Menu: **KuTy → Tests → Check Scene 2 complete household flow**.

The suite backs up the player's checkpoint, opens the actual home scene and checks runtime task bindings, wake-up, 2D cancellation/restart, rewards, radio tuning/cancellation/pause, wood delivery from a clear physical approach, rice washing, fire lighting, rice cooking, checkpoint reconstruction, conversation layout state and the final story handoff. It also starts another home visit to check completion without optional tasks. Checkpoint and previous open scenes are restored on returning to Edit Mode. Unsaved scenes are refused, never discarded.

Latest report: `Library/Scene2CompletionChecks.txt` (local, not committed).

Verified in Unity 2022.3.62f3: compilation succeeded and **133 runtime assertions passed**, including a second home-scene load and the required-only route. Editor returned to Edit Mode and the original checkpoint was restored.

The checks drive the interaction methods and gesture inputs programmatically. They are **not** a substitute for manual mouse playtesting, visual review or an actual end-to-end outdoor scene transition. Mother-model rigging/animation review, environmental art and lighting are unchanged and still require a separate visual pass.

The Editor still reports existing negative-scale BoxColliders on several `Act2_RusticWalls/Wall_01_AgedPlaster/TimberPost_Right` objects. Their authored geometry was not altered in this code pass; collision/scale cleanup should be part of the environment review before declaring the scene finished.
