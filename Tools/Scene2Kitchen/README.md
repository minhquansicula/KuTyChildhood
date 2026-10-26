# Scene 2: hands-on rice washing and cooking

The existing household order, rewards and chapter checkpoints remain owned by `HouseholdChoreFlow`. Rice washing, lighting the fire and cooking now route from the normal E interaction into world-space actions, without the old action-board minigames.

## Try it

Stop Play Mode, then choose **KuTy → Scene 2 → Preview hands-on rice and cooking**. The preview starts after measuring rice, beside the rice-washing pot. It backs up the player's chapter checkpoint and restores it when Play Mode stops.

- Look at the pot/stove and press **E**.
- **Wash rice:** hold the left mouse button and circle around the water. When the water is cloudy, drag the wooden grip on the pot down to drain. Repeat for the second rinse.
- **Light fire:** drag the three sticks from their tray onto the hearth opening. Hold the bamboo blowing tube for 0.2–1.1 seconds, release, and repeat three gentle breaths. Very brief or prolonged breaths do not finish the task.
- **Cook rice:** drag the wood handle right to feed the fire, left to withdraw it. The volume/pitch of the simmer, lid movement and steam follow the heat. Maintain a gentle simmer, then drag the lid knob upward to check the rice. The next chore is setting the table.
- **Esc** steps away and restores the original camera/cursor. Partial progress survives stepping away during the current scene visit. Saved checkpoints record completed chores; an unfinished chore starts afresh after reloading the scene.

## Mother's dialogue and audio

`Assets/_Project/Generated/KitchenHandsOn/MotherKitchenLines.asset` contains 15 authored Vietnamese lines for instructions, encouragement, washing/draining, breathing mistakes, high/low heat and the finished meal. These new lines currently use subtitles; no unrelated existing recording is played over them. Assign a matching AudioClip to each line to enable the spatial kitchen voice. Reinstalling preserves assigned clips by line ID.

The three short work-sound loops are generated locally, without external downloads.

## Blender assets in the game

The 11 delivered FBX models and their PBR maps are imported under `Assets/_Project/Art/Scene2/BlenderKitchen`. URP Lit materials use the supplied base color, normal and metallic/smoothness maps. The source delivery under `Tools/BlenderKitchenDelivery` remains available for editing.

The wash pot uses its authored tilt pivot, water surface, drain and grip anchors. The cooking lid pivots independently; steam comes from its authored anchor. Separate firewood pieces, woven trays, a rice paddle and a bamboo blowing tube replace the earlier primitives, including the radio sequence's carried bundle. The washing pot sits on the floor beside the water jars. Stove lights now originate inside the new hearths and follow the fire/heat state.

**KuTy → Scene 2 → Use Blender kitchen assets in game** rebuilds this set and binds the existing gameplay controller. Original scene meshes are kept inactive for reference. The standard kitchen installer also uses these assets when available.

## Validation and reauthoring

- **KuTy → Tests → Check hands-on rice and cooking** checks actual E routing, world targets, camera/input restoration, chore order, rinsing, breath timing, heat regulation, lid checking, pause, duplicate rewards, checkpoints and chapter reset. See `verification.txt`.
- **KuTy → Scene 2 → Install hands-on rice and cooking** rebuilds `Scene2KitchenHandsOnWorld`, binds the controller and saves Scene 2. Run it with Scene 2 open and Play Mode stopped.
- `blender-*.png` files are current editor renders. `play-*.png` files are Play Mode captures; older `*-reference.png` files show the earlier prototype.
