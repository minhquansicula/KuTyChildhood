# Game UI presentation

The gameplay HUD now uses warm ivory text, muted green panels and brass accents alongside the existing main menu's parchment artwork. The shared Vietnamese font remains `Resources/GameUiTheme`'s font; the main menu illustration is preserved.

## Presentation changes

- `GameUiPresentation` contains the small set of canvas, text, panel and button helpers used across screens. `GameUiSafeArea` keeps gameplay cards inside the display safe area with 32 reference pixels of padding and reacts to window resizing.
- The memory collection and wallet have dedicated cards. Old standalone memory captions are hidden. Currency uses Vietnamese thousands separators. Collection indicators continue to subscribe to the existing game events.
- Scene 1 and memory chapter objectives have clearer title/action/location hierarchy. Scene 2's task list is behind Tab; reset instructions stay in that list. Interaction prompts and controls occupy separate bottom rows, away from the crosshair.
- Notifications appear in a bottom toast and hide during blocking interactions. Dialogue uses the existing speaker, typewriter and voice timing with clearer padding and a gently pulsing Space instruction.
- The shop and journal share the menu's parchment frame. Shop items have individual cards and small procedural illustrations when their authored icon is absent. Closing screens still releases their existing input owner.
- Pause keeps the shared menu artwork and fits both viewport dimensions. The marble status strip hides the exploration HUD while active.
- Settings completes the existing menu panel with master volume and mouse sensitivity sliders. Values persist under `KuTy.Settings.MasterVolume` and `KuTy.Settings.LookSensitivity`; both the player controller and office opening use the sensitivity multiplier. Defaults preserve previous behavior.
- Ending text now binds the previously unassigned message field to the authored body label; the prototype controls caption is hidden. Office report choices and Excel/tutorial fonts use the same Vietnamese typeface.

## Review

`KuTy/Tests/Review all game UI and screen ratios` exercises the actual screens in Play Mode, backs up the player's checkpoint, checks critical text overflow and writes screenshots/results to `Library/UiPresentationReview`. Coverage includes 1280×720, 1920×1080, 1440×1080 (4:3) and 2560×1080 (wide) for the HUD, plus the task list, pause, dialogue, journal, shop, household interaction, village, marbles, main menu, settings, office and ending screens. Settings checks restore the prior preference values.

This presentation work does not change chore rewards, quest progression, voice recordings or the Scene 2 window-lighting configuration.
