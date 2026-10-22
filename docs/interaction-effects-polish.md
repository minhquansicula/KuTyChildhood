# Scene 1 and Scene 2 interaction effects

Effects follow materials and actions rather than using a coloured particle burst for every task.

Office: water uses a short stream and tiny droplets beneath the nearest visible cooler's cold tap. Printing feeds a textured report onto the scanner output surface; collecting it lifts the paper away. Delivery slides a report onto the boss's desk. Desk impact adds four small dust particles at the existing animation contact event. Printer status light is local, subtle and shadowless.

Household: cooking emits translucent expanding steam; kindling uses sparse embers, smoke and a low amber flicker. Water tasks emit small droplets, dishwashing uses hollow bubbles, measuring and milling use small grains. Fabric, wood and keepsakes use brief dust feedback. Radio tuning, reading and placing clean dishes do not emit decorative particles. Existing physical and minigame actions remain responsible for progress.

`InteractionVfx` supplies six cached 96px alpha masks and bounded particle presets (28 steam / 48 other particles per system). The resource shader uses alpha blending, fog, near-camera fading and optional depth intersection fading when the URP depth texture is enabled. It is included through Resources for builds. No extra package is required. UI steam uses the same soft mask; soap bubbles have a translucent rim and highlight; the hearth's rectangular ember was replaced with a soft glow. These UI effects do not receive paper shadows.

Particles use scaled time. Office coroutines also explicitly respect pause. Disabling feedback clears unfinished water, and closing/restarting a chore disposes its temporary lights and particles. Voice timing, animation contact timing, reward ownership, sleep and wake-up transitions are preserved.

Run `KuTy > Tests > Review Scene 1 and 2 interaction effects` after saving the current scene. It preserves the player's Scene 2 checkpoint, renders office and home close-ups, and checks idle, repeated triggering, pause, cancellation, particle budgets, shader compilation and cleanup across scenes. Output: `Library/EffectsReview`. Run the existing story smoke test for gameplay regression coverage. Preview captures and test output are generated locally, not committed as game assets.
