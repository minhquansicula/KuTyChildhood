# Scene 1 interaction checks

The visible water dispenser was offset by about 0.5 m from its old interaction collider. The printer's old colliders ended below its visible scanner section. The utility clock had no interaction collider. The scene now has interaction triggers fitted to the visible models, with physical appliance collisions aligned to their bodies. The art was not repositioned.

## How to interact

All actions use one press of **E**. The interaction text remains hidden in Scene 1; the crosshair highlight and objective provide feedback.

| Object | Position and aim |
| --- | --- |
| Water dispenser | Stand in the open aisle in front, roughly 0.85–2.1 m from its center; looking at the body, tap area or bottle selects it. A comfortable spot is world `(5.00, 0.08, 3.14)`, about 1.1 m in front. Press E once. After delivering the report, this advances the water-break objective to rest. |
| Photocopier | Stand in front, roughly 1.15–2.2 m from its center, and aim at its body or scanner. E checks it before printing finishes, and collects the report when ready. |
| Player chair | Approach within 1.6 m horizontally. E sits; a near chair remains reachable even if the ray passes above its seat. |
| Computer | Sit at the player's chair, aim at the monitor, then E opens the Excel task. |
| Boss desk | Enter the room and approach the visitor side, within 2.2 m of the seated boss. E delivers the printed report. The door only opens/closes. |
| Clock | Look at the actual utility clock from the aisle, about 2–2.6 m in front; the closer position beside the trash bin is obstructed. E reads the time. |
| Coffee | Aim at the visible mug from the front edge of the desk and press E. |

## Verification

Latest focused run: **PASS (153 assertions)** on 2026-10-21. Unity returned to Edit Mode after completion.

The spatial smoke test samples front and left/right standing positions, verifies the player capsule is unobstructed, uses the production `PlayerInteraction.HandleRaycast` target selection, and checks distance and wall occlusion. The drinking step interacts with the selected target after report delivery and verifies the transition to `Rest`. E key hardware input is not injected by this test.

Run `KuTy/Tests/Prepare office spatial interactions only`, then enter Play Mode. Results are written to `Assets/Screenshots/OfficeBlockoutGameplay_Integration.txt`. The geometry audit is in `Tools/ProjectAudit/office-interaction-geometry.txt`.

The full Excel task-flow run encountered a null reference in `ExcelDataAuditorGame.CreateHeaders` while opening the spreadsheet. The spatial run is separate from that UI check.
