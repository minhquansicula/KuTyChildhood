# Scene 2 — Radio to kitchen prototype

The normal chapter still starts with arranging keepsakes. The radio chore now uses a physical dial attached to the existing radio, followed by carrying a wood bundle from the basket to the tray beside the stove. Delivering it continues the original fetch-water chore. The normal quest owner retains all rewards and checkpoint writes.

## Try it

In Unity, stop Play Mode and choose **KuTy → Scene 2 → Preview radio to kitchen**. This enters Play Mode beside the radio, with the keepsakes step already complete. The existing chapter checkpoint is backed up and restored when Play Mode stops.

- Look at the radio and press **E**. Hold the left mouse button on the brass dial and rotate around its center, or scroll over the dial for small adjustments.
- Hold the clear station briefly; there is no confirm button. **Esc** leaves the radio without completing it.
- After a short musical beat, the mother's voice comes from the kitchen. Walking remains available during her call.
- Find the wood bundle in the basket and press **E**. Walk to the round tray beside the stove, look down at it and press **E** to place the wood. **Q** returns it to the basket.
- **H** shows guidance; **Esc** pauses while walking.

The radio uses an original, generated pentatonic music-box phrase and static. The mother's call reuses `act2_mom_wakeup_001.wav`, with its existing Vietnamese text; this prototype does not add a new voice recording or character animation.

## Validate / reauthor

- **KuTy → Tests → Check Scene 2 radio to kitchen** runs scene integration checks, including real E targeting, cancellation, spatial audio, pause, delivery occlusion, rewards and chapter reset. Results: `verification.txt`.
- **KuTy → Scene 2 → Install radio to kitchen sequence** rebuilds only `Scene2RadioKitchenWorld` and binds the controller on the existing chore flow. Open Scene 2 first.
- `radio-reference.png` is an editor render; `play-radio.png` and `play-carry-wood.png` are Play Mode captures.
