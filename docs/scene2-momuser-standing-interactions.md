# Scene 2 — MomUser standing interaction integration (2026-11-07)

## Verified input

The current home scene binds `Scene2MotherDemo/MomUser_Model` to the verified MomUser prefab/controller under `Assets/_Project/Characters/MomUserDemo/`. The four existing state names beginning with `Mother_` are compatibility aliases for the NEW MomUser clips, not references to the retired Mother model.

Read `Tools/MotherDelivery/UNITY-MOMUSER-INSTALL.md` for the latest installation details. The current scene-instance height override is approximately 1.65m. This task changes no scene placement, model/FBX scale, player height, rig, material, animation import settings or controller asset.

## Implemented behavior

- **Idle:** standing wait/default state, no root motion or autonomous navigation.
- **CallChild:** one shot from the existing radio sequence's `OnMotherCalled` event. Repeated requests cannot continually restart it; a cooldown prevents repeated waving. Opening conversation no longer calls this animation.
- **TalkIdle:** while the companion presents a reply, or kitchen/radio mother speech/caption windows are active. Returns to Idle when waiting for the player's next choice. Missing optional Talk state falls back to Idle rather than abusing CallChild.
- **Nod:** listens to the real `HouseholdChoreFlow.OnChoreCompleted` event. Requests a single nod when the camera is within 6m and mother is not occluded by solid scenery. Does not acknowledge ReadLetter, reward anything or change quests. Requests wait for higher-priority call/speech/active conversations, expire after 10 seconds, and use a 6-second cooldown.
- **Pause:** explicitly holds Animator speed and restores the previous speed on resume. Event subscriptions are removed on disable and rebound once on re-enable.

`Scene2MotherCompanion.IsPresentingReply` describes a bounded caption-presentation window (1.8–9 seconds estimated from text). It is NOT generated voice, lip sync, or evidence that missing voice clips are now recorded. Existing TTS files and dialogue content are unchanged.

## Code

- `Assets/_Project/Scripts/Systems/Scene2MotherDemo.cs`: four-state presentation and event lifecycle. Existing serialized bindings and public `Configure`, `Animator`, `IsCalling`, `Greet` compatibility retained.
- `Assets/_Project/Scripts/Systems/Scene2MotherCompanion.cs`: reply presentation window; existing conversation choices and input ownership retained.
- `Assets/_Project/Editor/Scene2MotherPresentationChecks.cs`: isolated runtime checks with the real MomUser prefab and inactive flow/radio event fixtures. Fixture events are emitted without running chore completion/save logic.

## Verification in this task

- Unity project compiled successfully after correcting an Editor test API typo; the final test domain loaded successfully.
- Fresh MomUser isolated Play Mode suite: **27 assertions PASS**. Includes correct real rig, TalkIdle conversation, waiting/closing behavior, actual imported hand motion during call, repeated-call/approval suppression, event-driven Nod, distance/wall guards, pause/resume, subscription cleanup, unchanged transforms/progress/checkpoints.
- Fresh audio regression suite: **45 assertions PASS**, including voice ducking and settings.
- The home scene is back in Edit Mode and was not saved/rewritten by this implementation. The tests use generated isolated scenes and restore the previous Play Mode start scene.
- Reports: `Tools/MotherDelivery/Source/mother-presentation-checks.txt` and `Tools/Music/playmode-checks.txt`.

These tests do not claim a full 12-chore house playthrough, mouse-driven visual acceptance, elimination of all garment deformation, or a rerun of the historic Scene 2 assertion suite.

## Still excluded

- Walk, turning/navigation, sitting/standing transitions and cooking actions with unverified clips.
- New mother voice recordings/TTS replacements and lip sync.
- Any action that automatically performs chores, grants pieces, changes checkpoint data or advances the story.
- Old Mother prefab/tool installation; do not run `Scene2MotherDemoSetup.Install` to reinstall the retired model.

Existing user deletions of retired Mother assets and concurrent cutscene/project changes are preserved. No commit or push performed.
