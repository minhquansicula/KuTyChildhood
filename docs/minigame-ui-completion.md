# Mini-game UI completion

## Delivered

- **Office spreadsheet:** centered blocking modal above the exploration HUD, larger readable cells, clear workbook header, formula hint, progress, timer/relaxed state, and keyboard submission. The first-run guide fits narrow displays. Boss messages are centered above a full-screen input blocker.
- **Excel results:** explicit timeout actions for a fresh timed attempt, relaxed play, and closing. Both retries retain entered values. A success card confirms the report before the existing print handoff. Repeated retry clicks cannot start parallel retry coroutines.
- **15 household tasks:** preserved existing authored illustrations and gestures, corrected dark-on-dark instructions and low-contrast feedback, added numeric progress, and connected mouse-accessible restart/leave buttons to the same owners as P/Escape. No progress or reward ownership moved into UI.
- **Marbles:** shallow top HUD keeps the playing circle exposed, with instructions, shots used/remaining, scoring progress, numeric shot power, and a leave button. The existing automatic retry and completion flows remain the game-state owners.
- **Shared hold-task/dish progress:** larger wrapping instruction card, percent readout, and a noninteractive progress bar without a misleading slider handle.
- `MiniGamePanelFit` fits fixed-design cards to their parent canvas while preserving aspect ratio. Existing Vietnamese fonts, parchment/green/brass palette, button sound effects, and input locks are reused.

## Verification

- Unity Skills instance `ChiaKhoaKyUc_B92B4C12`, Unity 2022.3.62f3: final compilation succeeded with zero errors.
- `KuTy/Tests/Review mini game UI after`: **PASS**, 43 rendered screenshots, no TMP text-overflow findings. Covers all 15 household tasks at 1280×720 and 1440×1080; office workbook additionally at 2560×1080; tutorial, validation, chat, timeout, success, dish progress, and marble aiming/retry states.
- The Play Mode review invokes each household restart/leave button and verifies task activation/deactivation. It checks both Excel retry modes, input retention, and acceptance of the two correct workbook values.
- `KuTy/Tests/Review all household artwork`: **PASS**, 15 task surfaces plus existing keepsake, rice, letter, wood, dish-stage and cancellation checks.
- Review output and detailed report: `Library/MiniGameUiReview/After/`. Before captures: `Library/MiniGameUiReview/Before/`. Selected final captures: `Assets/Screenshots/MiniGameUi_*.png`.
- Review tools back up and restore the player's chapter checkpoint using `KuTyCheckpointTestScope` and exit Play Mode automatically.

The UI is configured at runtime; no scene rebake or manual reference wiring is required.
