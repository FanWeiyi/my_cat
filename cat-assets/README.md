# Placeholder cat clips

V0.2 uses frame keys instead of final bitmap animation files. The WPF shell
draws simple transparent placeholder frames for these keys so animation wiring
can stay stable while cat art is still in progress.

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
