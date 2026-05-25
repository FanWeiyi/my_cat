# My Cat Art Guide

This directory contains the V0.3 personalized art pack for the photographed
cat. The first three pilot actions established the approved identity and the
remaining sequences extend that same visual lock across the current action
catalog before program integration.

## Action Coverage

| Action | Frames | Playback intent |
|---|---:|---|
| `idle_sit` | 16 | Quiet seated loop with blink, breath, and tail-tip motion |
| `rest_sleep` | 16 | Compact sleep loop with breathing and a tiny dream twitch |
| `walk_slow_left` | 16 | Slow left-facing walk loop |
| `walk_slow_right` | 16 | Slow right-facing walk loop |
| `wake_stretch` | 16 | Wake from rest, stretch, and settle |
| `edge_stop` | 16 | Slow walk deceleration and watchful stop |
| `pet_react` | 16 | Gentle touched response that settles back toward idle |
| `drag_settle` | 16 | Soft placement settle after a drag |
| `mouse_notice` | 16 | Brief curious pointer notice |
| `window_linger` | 16 | Quiet watchful linger loop |
| `observation_rest` | 16 | Drowsy response to a rest note |
| `observation_activity` | 16 | Perked response to an activity note |
| `observation_accompany` | 16 | Affectionate accompany response |

Final frame assets are stored as `512 x 512` RGBA PNGs named
`frame_000.png` through `frame_015.png` inside each action directory.
`reference/identity-basis.png` and the `source/` sheets preserve the visual
basis used to split the frame sequences.
`reference/pilot-desktop-preview.png` shows selected pilot frames reduced to
the current desktop cat window scale.
`reference/full-pack-desktop-preview.png` checks the same scale across the
complete V0.3 frame set.

## Identity Lock

- Keep the face round and calm, with large green eyes that read at desktop-pet
  scale.
- Keep the nose warm pink-brown and the muzzle, chin, chest, and belly lighter
  than the surrounding golden-brown fur.
- Preserve the forehead stripe rhythm and warmer golden eye surrounds.
- Preserve the shaded gray-brown back and rounded short-haired body mass.
- Preserve the dark ringed tail and blackish tail tip as a strong silhouette
  cue.
- Prioritize facial likeness first, then stable marking blocks, then body
  proportions and mood.

## Animation Guardrails

- Keep the full cat visible in every frame. Do not crop ears, paws, whiskers,
  or tail.
- Keep a stable ground line and body scale inside a `512 x 512` transparent
  canvas so future rendering work can choose one anchor policy.
- Keep each sequence's visual center and bottom baseline stable. The WPF shell
  draws the full `512 x 512` frame into a fixed desktop window, so frame-level
  anchor drift appears as unwanted sliding.
- Keep the visible body size stable inside each action without distorting the
  cat. Stabilization may scale a whole frame uniformly, but it must never force
  both width and height independently because that stretches the body, face, and
  tail.
- For quiet loops, keep the visible body size very stable. For transition poses
  such as stretch, settle, and activity response, allow natural width changes
  as long as the ground line and visual center remain steady.
- For `idle_sit`, keep the torso, head, chest, paws, and tail locked unless a
  clean tail-only replacement is available. Blink frames should be drawn or
  masked only inside the eye region; do not paste eye or tail patches from
  unmatched generated frames.
- Keep motions contained. The pack should feel like the same cat breathing,
  blinking, sleeping, moving, and leaning into affection rather than swapping
  identities.
- Avoid accessories, environmental props, cast shadows, visible hands, text,
  and action-specific special effects in the frame assets.

## Future Reference Gaps

The current photos were enough for this stylized V0.3 pass. A future polish
pass would still benefit from clearer references for:

- left and right full-body side views;
- standing or walking side views;
- full-body sleeping poses;
- the tail fully visible away from the body;
- back, leg, and paw marking detail.

## QA Workflow

Run the stabilizer from the repository root after any art change:

```powershell
python .\scripts\stabilize-cat-art.py --check
```

Run without `--check` only when deliberately rewriting the PNG frame anchors
and per-action visual scale.

The stabilizer rebuilds frames from the source alpha sheets and applies
equal-aspect scaling only. If a sequence appears too wide or too tall, adjust
its single target dimension in `scripts/stabilize-cat-art.py`; do not add a
separate width and height target for the same action.
