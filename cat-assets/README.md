# Placeholder cat clips

M0-M1 uses frame keys instead of final bitmap animation files. The WPF shell
draws simple transparent placeholder frames for these keys so animation wiring
can stay stable while cat art is still in progress.

| Action ID | Frame keys |
|---|---|
| `idle_sit` | `sit_open`, `sit_blink` |
| `rest_sleep` | `sleep_low`, `sleep_breathe` |
| `walk_slow` | `walk_left`, `walk_right` |
| `pet_react` | `pet_squish`, `pet_lift` |

