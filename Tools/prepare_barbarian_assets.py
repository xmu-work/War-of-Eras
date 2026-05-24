from __future__ import annotations

import shutil
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "素材包" / "蛮荒部落素材包"
TARGET = ROOT / "Assets" / "Resources" / "Barbarian"


UNIT_MAP = {
    "Hunter": "猎矛手",
    "Thrower": "掷石奴",
    "Champion": "巨骨勇士",
    "Shaman": "图腾萨满",
}


def background_mask(image: Image.Image, aggressive: bool = False) -> tuple[Image.Image, tuple[int, int, int, int]]:
    rgba = image.convert("RGBA")
    data = np.asarray(rgba, dtype=np.uint8)
    rgb = data[:, :, :3].astype(np.float32)
    h, w = rgb.shape[:2]

    border = max(10, min(w, h) // 12)
    border_pixels = np.concatenate(
        [
            rgb[:border, :, :].reshape(-1, 3),
            rgb[-border:, :, :].reshape(-1, 3),
            rgb[:, :border, :].reshape(-1, 3),
            rgb[:, -border:, :].reshape(-1, 3),
        ],
        axis=0,
    )
    bg = np.median(border_pixels, axis=0)

    distance = np.linalg.norm(rgb - bg, axis=2)
    saturation = rgb.max(axis=2) - rgb.min(axis=2)
    luminance = 0.299 * rgb[:, :, 0] + 0.587 * rgb[:, :, 1] + 0.114 * rgb[:, :, 2]

    if aggressive:
        background_like = (
            ((distance < 104) & (luminance > 96) & (saturation < 108))
            | ((luminance > 166) & (saturation < 88))
        )
    else:
        background_like = (
            ((distance < 58) & (luminance > 142) & (saturation < 68))
            | ((luminance > 196) & (saturation < 48))
        )
    background = flood_background(background_like)
    foreground = ~background

    # Drop the printed frame number that sits near the lower-left corner.
    foreground[int(h * 0.86) :, : int(w * 0.22)] = False

    keep = select_character_components(foreground)
    if not keep.any():
        alpha = np.zeros((h, w), dtype=np.uint8)
        return Image.fromarray(np.dstack([data[:, :, :3], alpha]), "RGBA"), (0, 0, w, h)

    alpha = Image.fromarray((keep.astype(np.uint8) * 255), "L").filter(ImageFilter.GaussianBlur(0.7))
    alpha_data = np.asarray(alpha, dtype=np.uint8)
    output = data.copy()
    output[:, :, 3] = np.minimum(output[:, :, 3], alpha_data)

    ys, xs = np.where(alpha_data > 8)
    pad = 12
    bbox = (
        max(0, int(xs.min()) - pad),
        max(0, int(ys.min()) - pad),
        min(w, int(xs.max()) + pad + 1),
        min(h, int(ys.max()) + pad + 1),
    )
    return Image.fromarray(output, "RGBA"), bbox


def flood_background(background_like: np.ndarray) -> np.ndarray:
    h, w = background_like.shape
    background = np.zeros(background_like.shape, dtype=bool)
    q: deque[tuple[int, int]] = deque()

    for x in range(w):
        if background_like[0, x]:
            background[0, x] = True
            q.append((x, 0))
        if background_like[h - 1, x]:
            background[h - 1, x] = True
            q.append((x, h - 1))

    for y in range(h):
        if background_like[y, 0]:
            background[y, 0] = True
            q.append((0, y))
        if background_like[y, w - 1]:
            background[y, w - 1] = True
            q.append((w - 1, y))

    while q:
        cx, cy = q.popleft()
        for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx = cx + ox
            ny = cy + oy
            if nx < 0 or nx >= w or ny < 0 or ny >= h:
                continue
            if background[ny, nx] or not background_like[ny, nx]:
                continue
            background[ny, nx] = True
            q.append((nx, ny))

    return background


def select_character_components(mask: np.ndarray) -> np.ndarray:
    h, w = mask.shape
    visited = np.zeros(mask.shape, dtype=bool)
    components: list[tuple[int, int, int, int, int]] = []
    neighbors = ((1, 0), (-1, 0), (0, 1), (0, -1))

    for y in range(h):
        for x in range(w):
            if visited[y, x] or not mask[y, x]:
                continue

            visited[y, x] = True
            q: deque[tuple[int, int]] = deque([(x, y)])
            area = 0
            min_x = max_x = x
            min_y = max_y = y

            while q:
                cx, cy = q.popleft()
                area += 1
                min_x = min(min_x, cx)
                max_x = max(max_x, cx)
                min_y = min(min_y, cy)
                max_y = max(max_y, cy)

                for ox, oy in neighbors:
                    nx = cx + ox
                    ny = cy + oy
                    if nx < 0 or nx >= w or ny < 0 or ny >= h:
                        continue
                    if visited[ny, nx] or not mask[ny, nx]:
                        continue
                    visited[ny, nx] = True
                    q.append((nx, ny))

            if area >= 36:
                components.append((area, min_x, min_y, max_x, max_y))

    if not components:
        return mask

    components.sort(reverse=True)
    centered_components = [
        component
        for component in components
        if component[1] > 4 and component[2] > 4 and component[3] < w - 5 and component[4] < h - 5
    ]
    main = centered_components[0] if centered_components else components[0]
    _, main_min_x, main_min_y, main_max_x, main_max_y = main
    expanded = (
        max(0, main_min_x - 48),
        max(0, main_min_y - 48),
        min(w - 1, main_max_x + 48),
        min(h - 1, main_max_y + 48),
    )

    keep = np.zeros(mask.shape, dtype=bool)
    main_area = main[0]
    for area, min_x, min_y, max_x, max_y in components:
        if min_x <= 2 or min_y <= 2 or max_x >= w - 3 or max_y >= h - 3:
            continue
        overlaps_main = not (
            max_x < expanded[0]
            or min_x > expanded[2]
            or max_y < expanded[1]
            or min_y > expanded[3]
        )
        meaningful = area >= max(110, main_area * 0.025)
        if overlaps_main and meaningful:
            keep[min_y : max_y + 1, min_x : max_x + 1] |= mask[min_y : max_y + 1, min_x : max_x + 1]

    return keep


def process_group(sources: list[Path], targets: list[Path], aggressive: bool = False) -> None:
    prepared: list[Image.Image] = []
    boxes: list[tuple[int, int, int, int]] = []

    for source in sources:
        image, bbox = background_mask(Image.open(source), aggressive)
        prepared.append(image)
        boxes.append(bbox)

    left = min(box[0] for box in boxes)
    top = min(box[1] for box in boxes)
    right = max(box[2] for box in boxes)
    bottom = max(box[3] for box in boxes)

    for image, target in zip(prepared, targets):
        target.parent.mkdir(parents=True, exist_ok=True)
        image.crop((left, top, right, bottom)).save(target)


def process_units() -> None:
    for unit_key, chinese_name in UNIT_MAP.items():
        source_files: list[Path] = []
        target_files: list[Path] = []

        for action_root, action_key in (("行进动作_拆分帧", "move"), ("攻击动作_拆分帧", "attack")):
            for frame in range(1, 6):
                source_files.append(
                    SOURCE
                    / action_root
                    / chinese_name
                    / f"{chinese_name}_{action_key}_{frame:02d}.png"
                )
                target_files.append(TARGET / "Units" / unit_key / f"{action_key}_{frame:02d}.png")

        process_group(source_files, target_files)


def process_towers() -> None:
    source_files = [
        SOURCE / "炮塔攻击_拆分帧" / "骨石塔" / f"骨石塔_attack_{frame:02d}.png"
        for frame in range(1, 6)
    ]
    target_files = [
        TARGET / "Towers" / "BoneTower" / f"attack_{frame:02d}.png"
        for frame in range(1, 6)
    ]
    process_group(source_files, target_files, aggressive=True)


def copy_map() -> None:
    target = TARGET / "Maps" / "ForestThreeLanes.png"
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(SOURCE / "地图" / "蛮荒部落_地图样本_v2.png", target)


def main() -> None:
    copy_map()
    process_units()
    process_towers()


if __name__ == "__main__":
    main()
