# Cat animation assets

The V0.3 desktop shell loads the personalized PNG frame pack in
`cats/my-cat/` through its manifest and strict startup validation. The table
below records the older V0.2 placeholder frame keys that established the action
wiring before the bitmap pack replaced code-drawn cat frames.

| Action ID | Frame keys |
|---|---|
| `idle_sit` | `sit_open`, `sit_blink`, `sit_tail` |
| `rest_sleep` | `sleep_low`, `sleep_breathe`, `sleep_dream` |
| `walk_slow` | `walk_left`, `walk_right` |
| `wake_stretch` | `wake_low`, `wake_stretch`, `wake_settle` |
| `edge_stop` | `edge_brake`, `edge_watch` |
| `pet_react` | `pet_squish`, `pet_lift`, `pet_nuzzle` |
| `drag_settle` | `settle_drop`, `settle_blink` |
| `mouse_notice` | `notice_glance`, `notice_focus` |
| `window_linger` | `window_perch`, `window_watch` |
| observation responses | `note_yawn`, `note_perk`, `note_nuzzle` |

## Personalized art pack

`cats/my-cat/` contains the V0.3 personalized art pack built from the current
approved kitten sheets. The pack includes final PNG frame sequences for every
current action catalog entry. `walk_slow` is represented as separate left- and
right-facing art sequences so the renderer can preserve side-specific
appearance.

The WPF shell loads this pack directly through `manifest.json`. Startup
validation requires the full 13 clips, 16 RGBA PNG frames per clip, and a
512 x 512 canvas. Missing or malformed assets fail validation instead of
falling back to the older code-drawn placeholder cat.
