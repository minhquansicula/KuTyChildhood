# Household art and wood update — 2026-10-22

Merged Đạt's branch through `cda1b68` (including `51de83e`) into `main` without conflicts. This brings the new household task presentation, wood stacking/tying sequence, dish sponge/rack, letter page, worktop and served-rice artwork.

The rice-pot aiming assist is retained, with an added solid-collider visibility check. Nearby players can aim slightly beside the pot, but a wall or another prop between the camera and the pot blocks the assistance. The query uses a reusable hit buffer. Existing registration of initially inactive proximity targets was retained.

Spatial regression tests now use the actual gameplay camera for both ray targeting and facing assistance, move the player's body as well as the camera for the distance check, and verify that a 20-degree aim offset can still select the initially hidden rice pot.

Validation:

- Five enabled scenes and **42** task sprites pass reference/import checks.
- Vo gạo, Dọn mâm, Đọc thư: **PASS (67 assertions)**, including nearby aim assistance, looking away, solid obstruction, distance, opening and cancelling.
- All **15** household action surfaces pass the artwork/state-gate review, including wood tying, rice draining, letter reading and dish washing/rinsing/racking. This checks UI/state behavior rather than solving all mouse gestures.
- Scene 1 integration: PASS (196 assertions). Complete story and replay: PASS (101 assertions).
- Final Console: zero errors. Checkpoint restored, Editor returned to Scene 1 in Edit Mode.

No standalone player build was produced. Test evidence is under `Tools/ProjectAudit/household-art-*` and `scene2-precise-targeting.txt`.
