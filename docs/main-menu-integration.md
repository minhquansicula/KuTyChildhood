# Main-menu integration — 20/10/2026

Merged `origin/nhan/mainmenu` at `9608f2f` into `main`. The branch adds the nostalgic menu artwork, `MainMenu_Canvas.prefab` and the updated MainMenu scene. Git reported no content conflicts.

## Integration fixes

- The authored Continue button uses `MainMenuUI.OnContinueButtonClicked` directly. It is disabled without a checkpoint and does not create a duplicate button or move the vertical layout.
- Continue without a save no longer falls through into New Game. Home and outdoor checkpoints route to their existing resume scenes.
- New Game, Continue and Quit bind to a single controller on the prefab root; the duplicate controller on Quit was removed through Unity's PrefabUtility API.
- Menu captions use the project's dynamic Vietnamese font. Back buttons are labelled “Quay lại”. Artwork and layout remain as authored.
- Test checkpoints now have a disk backup under `Library/` as well as SessionState, with restoration on returning to Edit Mode or reopening after an interrupted test. This protects future test runs; the older backup from the first GPU crash was only in the lost Editor session.

## Verification

Unity 2022.3.62f3 compiled successfully. `KuTy/Tests/Check main menu and checkpoint navigation` passed 39 assertions, covering a single active controller, persistent button bindings, Vietnamese captions, no-checkpoint behavior, settings open/back, home resume at 4 completed chores, outdoor resume with 2,000 đồng, and New Game clearing the checkpoint before loading Scene 1. The checkpoint present in the reopened Editor was restored after this successful run.

The first navigation run hit an unrecoverable D3D11 device reset when entering the outdoor world. The later successful functional test suppresses world renderers only during test navigation; it verifies menu/state transitions rather than certifying gameplay GPU performance. Menu rendering itself was captured from Unity at 1366×768 and visually checked after the font fix. Normal gameplay rendering is unchanged.

The Settings panel currently contains its heading and Back button; audio/graphics configuration controls are not implemented by this menu branch.

Reapply bindings/font through `KuTy/Main menu/Integrate checkpoint buttons`; the command edits the prefab using Unity APIs.
