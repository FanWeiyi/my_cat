from __future__ import annotations

import argparse
import json
from dataclasses import dataclass
from pathlib import Path

import numpy as np
from scipy import ndimage
from PIL import Image, ImageDraw


FRAME_SIZE = 512
ACTIONS = (
    "idle_sit",
    "rest_sleep",
    "walk_slow_left",
    "walk_slow_right",
    "wake_stretch",
    "edge_stop",
    "pet_react",
    "drag_settle",
    "mouse_notice",
    "window_linger",
    "observation_rest",
    "observation_activity",
    "observation_accompany",
)
TARGETS = {
    "idle_sit": {"height": 412},
    "rest_sleep": {"width": 434},
    "walk_slow_left": {"height": 254},
    "walk_slow_right": {"height": 252},
    "wake_stretch": {"height": 292},
    "edge_stop": {"height": 338},
    "pet_react": {"height": 421},
    "drag_settle": {"height": 334},
    "mouse_notice": {"height": 391},
    "window_linger": {"height": 356},
    "observation_rest": {"height": 300},
    "observation_activity": {"height": 340},
    "observation_accompany": {"height": 292},
}
QA = {
    "center_max": 2.0,
    "bottom_max": 1,
    "locked_width_delta": 92,
    "locked_height_delta": 28,
    "locked_aspect_ratio_max_delta": 0.10,
}
STRICT_SIZE_ACTIONS = {
    "idle_sit",
    "rest_sleep",
    "mouse_notice",
    "window_linger",
}


@dataclass(frozen=True)
class FrameMetric:
    path: Path
    box: tuple[int, int, int, int]
    center_x: float
    bottom: int
    width: int
    height: int
    area: int


def load_rgba(path: Path) -> Image.Image:
    return Image.open(path).convert("RGBA")


def alpha_bbox(image: Image.Image) -> tuple[int, int, int, int]:
    box = image.getchannel("A").getbbox()
    if box is None:
        raise ValueError(f"{image.filename} has no visible alpha.")
    return box


def visible_area(image: Image.Image) -> int:
    return int(np.count_nonzero(np.array(image.getchannel("A")) > 8))


def frame_metrics(action_dir: Path) -> list[FrameMetric]:
    metrics: list[FrameMetric] = []
    for path in sorted(action_dir.glob("frame_*.png")):
        image = load_rgba(path)
        left, top, right, bottom = alpha_bbox(image)
        metrics.append(
            FrameMetric(
                path=path,
                box=(left, top, right, bottom),
                center_x=(left + right) / 2,
                bottom=bottom,
                width=right - left,
                height=bottom - top,
                area=visible_area(image),
            )
        )
    return metrics


def paste_scaled_subject(
    image: Image.Image,
    target: dict[str, int],
    target_center_x: int,
    target_bottom: int,
) -> Image.Image:
    image = zero_transparent_pixels(image)
    left, top, right, bottom = alpha_bbox(image)
    subject = image.crop((left, top, right, bottom))
    source_width = right - left
    source_height = bottom - top
    if "width" in target:
        scale = target["width"] / source_width
    elif "height" in target:
        scale = target["height"] / source_height
    else:
        scale = 1.0
    target_width = max(1, round(source_width * scale))
    target_height = max(1, round(source_height * scale))
    resized = subject.resize((target_width, target_height), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (FRAME_SIZE, FRAME_SIZE), (0, 0, 0, 0))
    paste_x = round(target_center_x - (target_width / 2))
    paste_y = round(target_bottom - target_height)
    canvas.alpha_composite(resized, (paste_x, paste_y))
    return canvas


def zero_transparent_pixels(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    pixels = np.array(image)
    transparent = pixels[:, :, 3] == 0
    pixels[transparent, :3] = 0
    return Image.fromarray(pixels, mode="RGBA")


def rebuild_frames_from_sources(root: Path) -> None:
    for action in ACTIONS:
        sheet_path = root / "source" / f"{action}-sheet-alpha.png"
        if not sheet_path.exists():
            continue
        sheet = load_rgba(sheet_path)
        width, height = sheet.size
        x_edges = [round(width * index / 4) for index in range(5)]
        y_edges = [round(height * index / 4) for index in range(5)]
        output_dir = root / action
        output_dir.mkdir(exist_ok=True)
        for index in range(16):
            row, column = divmod(index, 4)
            tile = sheet.crop(
                (x_edges[column], y_edges[row], x_edges[column + 1], y_edges[row + 1])
            )
            tile = tile.resize((480, 480), Image.Resampling.LANCZOS)
            canvas = Image.new("RGBA", (FRAME_SIZE, FRAME_SIZE), (0, 0, 0, 0))
            canvas.alpha_composite(tile, (16, 16))
            canvas.save(output_dir / f"frame_{index:03}.png")


def normalize_action(root: Path, action: str) -> None:
    metrics = frame_metrics(root / action)
    target = TARGETS[action]
    target_center_x = round(sum(metric.center_x for metric in metrics) / len(metrics))
    target_bottom = max(metric.bottom for metric in metrics)
    for metric in metrics:
        image = load_rgba(metric.path)
        normalized = paste_scaled_subject(
            image,
            target,
            target_center_x,
            target_bottom,
        )
        zero_transparent_pixels(normalized).save(metric.path)


def remove_chroma_fringe(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    pixels = np.array(image)
    fringe = (
        (pixels[:, :, 0] >= 120)
        & (pixels[:, :, 2] >= 80)
        & (pixels[:, :, 1] <= 145)
        & ((pixels[:, :, 0].astype(int) - pixels[:, :, 1].astype(int)) >= 45)
        & ((pixels[:, :, 2].astype(int) - pixels[:, :, 1].astype(int)) >= 25)
    )
    pixels[:, :, 3] = np.where(fringe, 0, pixels[:, :, 3])
    image = Image.fromarray(pixels, mode="RGBA")
    return image


def clean_chroma_fringes(root: Path) -> None:
    for action in ACTIONS:
        for path in (root / action).glob("frame_*.png"):
            remove_chroma_fringe(load_rgba(path)).save(path)


def keep_largest_subject(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    alpha = np.array(image.getchannel("A"))
    labels, count = ndimage.label(alpha > 8)
    if count == 0:
        return image

    component_sizes = np.bincount(labels.ravel())
    component_sizes[0] = 0
    keep_threshold = max(5000, int(component_sizes.max() * 0.08))
    keep_labels = component_sizes >= keep_threshold
    cleaned_alpha = np.where(keep_labels[labels], alpha, 0).astype(np.uint8)
    image.putalpha(Image.fromarray(cleaned_alpha, mode="L"))
    return image


def remove_isolated_artifacts(root: Path) -> None:
    for action in ACTIONS:
        for path in (root / action).glob("frame_*.png"):
            keep_largest_subject(load_rgba(path)).save(path)


def compose_stable_loop(root: Path, action: str, source_indices: tuple[int, ...]) -> None:
    action_dir = root / action
    source_frames = [
        load_rgba(action_dir / f"frame_{index:03}.png")
        for index in source_indices
    ]
    for index in range(16):
        source_frames[index % len(source_frames)].save(action_dir / f"frame_{index:03}.png")


def stabilize_loop_actions(root: Path) -> None:
    compose_stable_loop(root, "idle_sit", (12, 13, 14, 15))
    compose_stable_loop(root, "rest_sleep", (12, 13, 14, 15))
    compose_stable_loop(root, "window_linger", (12, 13, 14, 15))


def regenerate_preview(root: Path) -> None:
    indices = (0, 5, 10, 15)
    label_w = 188
    cell_w, cell_h = 184, 168
    pad = 16
    row_h = cell_h + pad
    width = label_w + pad + len(indices) * (cell_w + pad)
    height = pad + len(ACTIONS) * row_h
    canvas = Image.new("RGBA", (width, height), (240, 237, 230, 255))
    for y in range(canvas.height):
        for x in range(label_w, canvas.width):
            if ((x // 12) + (y // 12)) % 2:
                canvas.putpixel((x, y), (226, 223, 216, 255))

    draw = ImageDraw.Draw(canvas)
    for row, action in enumerate(ACTIONS):
        y = pad + row * row_h
        draw.text((pad, y + 68), action, fill=(62, 52, 45, 255))
        for col, index in enumerate(indices):
            frame = load_rgba(root / action / f"frame_{index:03}.png")
            preview = frame.resize((cell_w, cell_h), Image.Resampling.LANCZOS)
            canvas.alpha_composite(preview, (label_w + pad + col * (cell_w + pad), y))
    canvas.save(root / "reference" / "full-pack-desktop-preview.png")


def qa_report(root: Path) -> dict[str, object]:
    report: dict[str, object] = {}
    failures: list[str] = []
    for action in ACTIONS:
        metrics = frame_metrics(root / action)
        if len(metrics) != 16:
            failures.append(f"{action}: expected 16 frames, found {len(metrics)}")
            continue
        centers = [metric.center_x for metric in metrics]
        bottoms = [metric.bottom for metric in metrics]
        widths = [metric.width for metric in metrics]
        heights = [metric.height for metric in metrics]
        ratios = [metric.width / metric.height for metric in metrics]
        modes = {load_rgba(metric.path).mode for metric in metrics}
        sizes = {load_rgba(metric.path).size for metric in metrics}
        center_drift = max(centers) - min(centers)
        bottom_drift = max(bottoms) - min(bottoms)
        width_delta = max(widths) - min(widths)
        height_delta = max(heights) - min(heights)
        ratio_delta = max(ratios) - min(ratios)
        if center_drift > QA["center_max"]:
            failures.append(f"{action}: center drift {center_drift:.1f}px")
        if bottom_drift > QA["bottom_max"]:
            failures.append(f"{action}: bottom drift {bottom_drift}px")
        if action in STRICT_SIZE_ACTIONS and width_delta > QA["locked_width_delta"]:
            failures.append(f"{action}: width delta {width_delta}px")
        if action in STRICT_SIZE_ACTIONS and height_delta > QA["locked_height_delta"]:
            failures.append(f"{action}: height delta {height_delta}px")
        if action in STRICT_SIZE_ACTIONS and ratio_delta > QA["locked_aspect_ratio_max_delta"]:
            failures.append(f"{action}: aspect ratio delta {ratio_delta:.2f}")
        if modes != {"RGBA"} or sizes != {(FRAME_SIZE, FRAME_SIZE)}:
            failures.append(f"{action}: invalid mode/size {modes} {sizes}")
        report[action] = {
            "centerDrift": round(center_drift, 2),
            "bottomDrift": bottom_drift,
            "widthDelta": width_delta,
            "heightDelta": height_delta,
            "widthRange": [min(widths), max(widths)],
            "heightRange": [min(heights), max(heights)],
            "aspectRatioRange": [round(min(ratios), 3), round(max(ratios), 3)],
        }

    report["failures"] = failures
    return report


def stabilize(root: Path) -> None:
    rebuild_frames_from_sources(root)
    stabilize_loop_actions(root)
    clean_chroma_fringes(root)
    remove_isolated_artifacts(root)
    for action in ACTIONS:
        normalize_action(root, action)
    regenerate_preview(root)


def main() -> None:
    parser = argparse.ArgumentParser(description="Stabilize and QA my-cat animation assets.")
    parser.add_argument("--root", default="cat-assets/cats/my-cat", type=Path)
    parser.add_argument("--check", action="store_true", help="Only run QA checks; do not edit images.")
    args = parser.parse_args()

    root = args.root.resolve()
    if not args.check:
        stabilize(root)

    report = qa_report(root)
    print(json.dumps(report, ensure_ascii=False, indent=2))
    if report["failures"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
