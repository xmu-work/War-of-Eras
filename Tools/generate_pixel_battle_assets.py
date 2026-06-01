from __future__ import annotations

import json
import math
import shutil
import uuid
from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageOps


WIDTH = 2400
HEIGHT = 1350
PPU = 55
LEGACY_MAP_RESOURCE_NAME = "PixelFrontlineThreeLanes"

ROOT = Path(__file__).resolve().parents[1]
SOURCE_DIR = ROOT / "Assets" / "Art" / "Generated" / "PixelRedesign"
MAP_SOURCE = SOURCE_DIR / "source_pixel_map.png"
ATLAS_SOURCE = SOURCE_DIR / "source_pixel_units_facilities_atlas.png"
MAP_RESOURCE_DIR = ROOT / "Assets" / "Resources" / "Battle" / "Maps"
FACILITY_RESOURCE_DIR = ROOT / "Assets" / "Resources" / "Battle" / "Facilities"
DOCS_DIR = ROOT / "docs"

AGE_SPECS = [
    {
        "key": "Barbarian",
        "display": "蛮荒部落",
        "map_name": "PixelFrontline_Barbarian",
        "row": (20, 190),
        "accent": (238, 188, 101),
        "map_tint": (86, 69, 41),
        "theme": "barbarian",
        "units": [
            {"name": "Hunter", "source": "light", "variant": "baseline", "attack_style": 0, "max_size": 214},
            {"name": "Thrower", "source": "ranged", "variant": "slinger", "attack_style": 1, "max_size": 210},
            {"name": "BoneArcher", "source": "ranged", "variant": "marksman", "attack_style": 1, "max_size": 212},
            {"name": "TuskRider", "source": "heavy", "variant": "vehicle", "attack_style": 2, "max_size": 218},
            {"name": "Champion", "source": "light", "variant": "elite", "attack_style": 0, "max_size": 226},
        ],
        "towers": [
            {"name": "BoneTower", "variant": "watch", "attack_style": 0},
            {"name": "SlingNest", "variant": "volley", "attack_style": 1},
            {"name": "MammothTotem", "variant": "heavy", "attack_style": 2},
        ],
    },
    {
        "key": "Machine",
        "display": "机械工坊",
        "map_name": "PixelFrontline_Machine",
        "row": (195, 360),
        "accent": (92, 172, 235),
        "map_tint": (89, 78, 62),
        "theme": "machine",
        "units": [
            {"name": "GearSoldier", "source": "light", "variant": "baseline", "attack_style": 0, "max_size": 214},
            {"name": "SteamCrossbow", "source": "ranged", "variant": "marksman", "attack_style": 1, "max_size": 212},
            {"name": "BoilerGrenadier", "source": "ranged", "variant": "caster", "attack_style": 2, "max_size": 214},
            {"name": "SiegeRoller", "source": "heavy", "variant": "vehicle", "attack_style": 2, "max_size": 218},
            {"name": "ClockworkGuard", "source": "light", "variant": "elite", "attack_style": 0, "max_size": 228},
        ],
        "towers": [
            {"name": "GearTower", "variant": "watch", "attack_style": 0},
            {"name": "SteamCannonTower", "variant": "volley", "attack_style": 2},
            {"name": "RivetMortar", "variant": "heavy", "attack_style": 2},
        ],
    },
    {
        "key": "Electric",
        "display": "电力时代",
        "map_name": "PixelFrontline_Electric",
        "row": (360, 530),
        "accent": (94, 224, 255),
        "map_tint": (37, 86, 102),
        "theme": "electric",
        "units": [
            {"name": "VoltGuard", "source": "light", "variant": "baseline", "attack_style": 0, "max_size": 214},
            {"name": "ArcRunner", "source": "light", "variant": "fast", "attack_style": 0, "max_size": 205},
            {"name": "CoilShooter", "source": "ranged", "variant": "marksman", "attack_style": 1, "max_size": 212},
            {"name": "CrawlerTank", "source": "heavy", "variant": "vehicle", "attack_style": 2, "max_size": 218},
            {"name": "ThunderMech", "source": "ranged", "variant": "elite", "attack_style": 2, "max_size": 226},
        ],
        "towers": [
            {"name": "TeslaTower", "variant": "watch", "attack_style": 1},
            {"name": "ArcPylon", "variant": "volley", "attack_style": 1},
            {"name": "RailgunTower", "variant": "heavy", "attack_style": 2},
        ],
    },
    {
        "key": "Nuclear",
        "display": "核能纪元",
        "map_name": "PixelFrontline_Nuclear",
        "row": (530, 710),
        "accent": (163, 245, 80),
        "map_tint": (55, 97, 42),
        "theme": "nuclear",
        "units": [
            {"name": "RadTrooper", "source": "light", "variant": "baseline", "attack_style": 0, "max_size": 214},
            {"name": "IsotopeScout", "source": "light", "variant": "fast", "attack_style": 0, "max_size": 205},
            {"name": "FissionLancer", "source": "ranged", "variant": "marksman", "attack_style": 1, "max_size": 212},
            {"name": "ReactorWalker", "source": "ranged", "variant": "caster", "attack_style": 2, "max_size": 224},
            {"name": "NuclearTank", "source": "heavy", "variant": "vehicle", "attack_style": 2, "max_size": 220},
        ],
        "towers": [
            {"name": "ParticleGunTower", "variant": "watch", "attack_style": 1},
            {"name": "ReactorMortar", "variant": "volley", "attack_style": 2},
            {"name": "FalloutObelisk", "variant": "heavy", "attack_style": 2},
        ],
    },
    {
        "key": "Starsea",
        "display": "星海文明",
        "map_name": "PixelFrontline_Starsea",
        "row": (705, 885),
        "accent": (188, 101, 255),
        "map_tint": (56, 40, 98),
        "theme": "starsea",
        "units": [
            {"name": "LaserTrooper", "source": "light", "variant": "baseline", "attack_style": 0, "max_size": 214},
            {"name": "PhotonBlade", "source": "light", "variant": "fast", "attack_style": 0, "max_size": 205},
            {"name": "SkimmerMech", "source": "ranged", "variant": "marksman", "attack_style": 1, "max_size": 212},
            {"name": "GravityDrone", "source": "ranged", "variant": "caster", "attack_style": 2, "max_size": 218},
            {"name": "AntimatterColossus", "source": "heavy", "variant": "vehicle", "attack_style": 2, "max_size": 222},
        ],
        "towers": [
            {"name": "TitaniumRayTower", "variant": "watch", "attack_style": 1},
            {"name": "PlasmaSpire", "variant": "volley", "attack_style": 1},
            {"name": "SingularityBeacon", "variant": "heavy", "attack_style": 2},
        ],
    },
]

ATLAS_COLUMNS = {
    "light": (145, 350),
    "ranged": (320, 535),
    "heavy": (500, 770),
    "base": (765, 1045),
    "tower": (1065, 1295),
}

BASES = {
    "PlayerBasePoint": (190, 675),
    "EnemyBasePoint": (2210, 675),
}

LANES = [
    [
        (190, 675),
        (305, 560),
        (430, 440),
        (690, 390),
        (1015, 390),
        (1230, 390),
        (1485, 410),
        (1780, 395),
        (2070, 465),
        (2210, 675),
    ],
    [
        (190, 675),
        (430, 675),
        (700, 675),
        (975, 675),
        (1200, 675),
        (1425, 675),
        (1700, 675),
        (1970, 675),
        (2210, 675),
    ],
    [
        (190, 675),
        (305, 790),
        (455, 930),
        (725, 965),
        (1045, 950),
        (1230, 960),
        (1510, 940),
        (1810, 960),
        (2070, 875),
        (2210, 675),
    ],
]

CONNECTOR_NODES = {
    "LeftUpperCut": (735, 530),
    "LeftLowerCut": (735, 815),
    "CenterUpperCut": (1200, 525),
    "CenterLowerCut": (1200, 825),
    "RightUpperCut": (1660, 535),
    "RightLowerCut": (1660, 815),
}

TOWER_SLOTS = {
    "PlayerTowerSlot_0": (445, 430),
    "PlayerTowerSlot_1": (405, 675),
    "PlayerTowerSlot_2": (445, 920),
    "EnemyTowerSlot_0": (1955, 430),
    "EnemyTowerSlot_1": (1995, 675),
    "EnemyTowerSlot_2": (1955, 920),
}

RESOURCE_WELLS = {
    "PlayerWellSlot_0": (830, 545),
    "PlayerWellSlot_1": (830, 805),
    "EnemyWellSlot_0": (1570, 545),
    "EnemyWellSlot_1": (1570, 805),
}

NEUTRAL_POINTS = {
    "CentralCrossroads": (1200, 675),
    "NorthBridge": (1200, 390),
    "SouthBridge": (1230, 960),
}


def map_point(pixel_x: float, pixel_y: float) -> tuple[float, float]:
    return ((pixel_x - WIDTH * 0.5) / PPU, (HEIGHT * 0.5 - pixel_y) / PPU)


def ensure_folder(path: Path) -> None:
    path.mkdir(parents=True, exist_ok=True)
    write_folder_meta(path)


def remove_chroma(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    width, height = rgba.size
    key = (0, 255, 0)
    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            distance = math.sqrt((r - key[0]) ** 2 + (g - key[1]) ** 2 + (b - key[2]) ** 2)
            if distance < 115 and g > 165:
                pixels[x, y] = (r, g, b, 0)
    return rgba


def keep_main_alpha_component(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A")
    width, height = rgba.size
    seen: set[tuple[int, int]] = set()
    best: list[tuple[int, int]] = []
    pixels = alpha.load()

    for start_y in range(height):
        for start_x in range(width):
            if pixels[start_x, start_y] == 0 or (start_x, start_y) in seen:
                continue

            component: list[tuple[int, int]] = []
            queue: deque[tuple[int, int]] = deque([(start_x, start_y)])
            seen.add((start_x, start_y))
            while queue:
                x, y = queue.popleft()
                component.append((x, y))
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= nx < width and 0 <= ny < height and (nx, ny) not in seen and pixels[nx, ny] > 0:
                        seen.add((nx, ny))
                        queue.append((nx, ny))

            if len(component) > len(best):
                best = component

    if not best:
        return rgba

    mask = Image.new("L", (width, height), 0)
    mask_pixels = mask.load()
    for x, y in best:
        mask_pixels[x, y] = pixels[x, y]
    cleaned = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    cleaned.alpha_composite(rgba)
    cleaned.putalpha(mask)
    return cleaned


def alpha_bbox(image: Image.Image) -> tuple[int, int, int, int]:
    bbox = image.getchannel("A").getbbox()
    if bbox is None:
        return (0, 0, image.width, image.height)
    return bbox


def crop_sprite(atlas: Image.Image, x_range: tuple[int, int], y_range: tuple[int, int], padding: int = 8) -> Image.Image:
    crop = atlas.crop((x_range[0], y_range[0], x_range[1], y_range[1]))
    crop = remove_chroma(crop)
    crop = keep_main_alpha_component(crop)
    x0, y0, x1, y1 = alpha_bbox(crop)
    x0 = max(0, x0 - padding)
    y0 = max(0, y0 - padding)
    x1 = min(crop.width, x1 + padding)
    y1 = min(crop.height, y1 + padding)
    return crop.crop((x0, y0, x1, y1))


def fit_on_canvas(sprite: Image.Image, canvas_size: int, max_size: int, y_bias: int = 0) -> Image.Image:
    sprite = sprite.convert("RGBA")
    scale = min(max_size / sprite.width, max_size / sprite.height)
    new_size = (max(1, round(sprite.width * scale)), max(1, round(sprite.height * scale)))
    resized = sprite.resize(new_size, Image.Resampling.NEAREST)
    canvas = Image.new("RGBA", (canvas_size, canvas_size), (0, 0, 0, 0))
    x = (canvas_size - resized.width) // 2
    y = (canvas_size - resized.height) // 2 + y_bias
    canvas.alpha_composite(resized, (x, y))
    return canvas


def clamp_channel(value: int) -> int:
    return max(0, min(255, value))


def adjust_color(color: tuple[int, int, int], amount: int) -> tuple[int, int, int]:
    return tuple(clamp_channel(channel + amount) for channel in color)


def rgba(color: tuple[int, int, int], alpha: int = 255) -> tuple[int, int, int, int]:
    return (*color, alpha)


def draw_pixel_box(
    draw: ImageDraw.ImageDraw,
    box: tuple[int, int, int, int],
    fill: tuple[int, int, int],
    outline: tuple[int, int, int] = (32, 24, 18),
    width: int = 2,
) -> None:
    draw.rectangle(box, fill=rgba(outline), outline=rgba(outline))
    inner = (box[0] + width, box[1] + width, box[2] - width, box[3] - width)
    if inner[0] < inner[2] and inner[1] < inner[3]:
        draw.rectangle(inner, fill=rgba(fill))


def draw_pixel_ellipse(
    draw: ImageDraw.ImageDraw,
    box: tuple[int, int, int, int],
    fill: tuple[int, int, int],
    outline: tuple[int, int, int] = (32, 24, 18),
    width: int = 2,
) -> None:
    draw.ellipse(box, fill=rgba(outline))
    inner = (box[0] + width, box[1] + width, box[2] - width, box[3] - width)
    if inner[0] < inner[2] and inner[1] < inner[3]:
        draw.ellipse(inner, fill=rgba(fill))


def prepare_unit_sprite(
    source: Image.Image,
    unit: dict,
    age_key: str,
    accent: tuple[int, int, int],
) -> Image.Image:
    base = fit_on_canvas(source, 256, unit.get("max_size", 214), unit.get("y_bias", 10))
    return apply_unit_variant(base, unit["variant"], age_key, accent)


def apply_unit_variant(
    sprite: Image.Image,
    variant: str,
    age_key: str,
    accent: tuple[int, int, int],
) -> Image.Image:
    image = sprite.copy().convert("RGBA")
    draw = ImageDraw.Draw(image, "RGBA")
    dark = (31, 24, 19)
    bright = adjust_color(accent, 34)
    muted = adjust_color(accent, -44)

    if variant == "baseline":
        draw.line((79, 146, 104, 155), fill=rgba(dark, 210), width=4)
        draw.rectangle((92, 49, 112, 54), fill=rgba(bright, 220))
    elif variant == "fast":
        draw.polygon([(92, 66), (123, 74), (110, 92), (84, 84)], fill=rgba(dark, 230))
        draw.polygon([(96, 68), (119, 75), (108, 87), (90, 82)], fill=rgba(bright, 235))
        draw.line((82, 126, 54, 142), fill=rgba(bright, 190), width=5)
        draw.line((84, 138, 52, 156), fill=rgba(muted, 170), width=4)
        draw.rectangle((116, 52, 129, 58), fill=rgba(bright, 230))
    elif variant == "slinger":
        draw.arc((138, 78, 190, 132), 235, 60, fill=rgba(dark, 230), width=5)
        draw.arc((143, 83, 184, 126), 235, 60, fill=rgba(bright, 210), width=3)
        draw_pixel_ellipse(draw, (183, 94, 202, 113), muted)
        draw.line((122, 101, 181, 104), fill=rgba(dark, 220), width=3)
    elif variant == "marksman":
        draw.line((122, 101, 202, 76), fill=rgba(dark, 245), width=7)
        draw.line((128, 99, 199, 78), fill=rgba(bright, 230), width=3)
        draw.line((186, 72, 208, 66), fill=rgba((245, 238, 197), 230), width=3)
        draw.rectangle((73, 74, 86, 116), fill=rgba(dark, 210))
        draw.rectangle((76, 78, 83, 111), fill=rgba(muted, 220))
    elif variant == "caster":
        draw.line((104, 70, 158, 47), fill=rgba(dark, 235), width=5)
        draw.line((108, 68, 157, 49), fill=rgba(bright, 230), width=2)
        draw_pixel_ellipse(draw, (151, 34, 181, 64), bright)
        draw_pixel_box(draw, (76, 118, 101, 148), muted)
        draw.line((88, 117, 112, 95), fill=rgba(bright, 200), width=3)
        if age_key in {"Nuclear", "Starsea"}:
            draw.ellipse((159, 42, 173, 56), fill=rgba((246, 255, 226), 210))
    elif variant == "vehicle":
        for cx in (80, 124, 168):
            draw_pixel_ellipse(draw, (cx - 18, 164, cx + 18, 200), muted)
            draw_pixel_ellipse(draw, (cx - 7, 175, cx + 7, 189), bright, width=1)
        draw_pixel_box(draw, (74, 137, 180, 168), adjust_color(accent, -28))
        draw.line((146, 104, 204, 84), fill=rgba(dark, 245), width=9)
        draw.line((150, 103, 202, 86), fill=rgba(bright, 230), width=4)
        draw.rectangle((194, 79, 211, 89), fill=rgba((248, 226, 145), 235))
    elif variant == "elite":
        draw.line((154, 46, 150, 149), fill=rgba(dark, 245), width=6)
        draw.polygon([(154, 47), (200, 62), (154, 81)], fill=rgba(dark, 235))
        draw.polygon([(158, 53), (192, 63), (158, 76)], fill=rgba(bright, 230))
        draw_pixel_box(draw, (74, 81, 103, 122), muted)
        draw_pixel_box(draw, (118, 79, 148, 121), muted)
        draw.rectangle((104, 45, 132, 53), fill=rgba(bright, 235))
        draw.rectangle((111, 35, 125, 46), fill=rgba(dark, 230))
        draw.rectangle((114, 37, 122, 46), fill=rgba(bright, 235))

    if age_key == "Machine":
        draw.arc((74, 50, 112, 88), 0, 320, fill=rgba((196, 145, 61), 210), width=4)
        draw.rectangle((85, 48, 101, 53), fill=rgba((62, 46, 30), 230))
    elif age_key == "Electric":
        draw.line((92, 54, 123, 42, 153, 54), fill=rgba(accent, 210), width=3)
        draw.rectangle((120, 38, 128, 46), fill=rgba((236, 255, 255), 230))
    elif age_key == "Nuclear":
        draw.rectangle((91, 54, 129, 60), fill=rgba((39, 42, 20), 240))
        draw.rectangle((99, 54, 107, 60), fill=rgba((240, 224, 55), 240))
        draw.rectangle((116, 54, 124, 60), fill=rgba((240, 224, 55), 240))
    elif age_key == "Starsea":
        draw.polygon([(107, 38), (120, 50), (107, 62), (94, 50)], fill=rgba(dark, 230))
        draw.polygon([(107, 42), (116, 50), (107, 58), (98, 50)], fill=rgba(bright, 235))

    return image


def translated_frame(sprite: Image.Image, offset: tuple[int, int], canvas_size: int = 256) -> Image.Image:
    frame = Image.new("RGBA", (canvas_size, canvas_size), (0, 0, 0, 0))
    frame.alpha_composite(sprite, offset)
    return frame


def draw_attack_effect(frame: Image.Image, archetype: int, frame_index: int, accent: tuple[int, int, int]) -> None:
    draw = ImageDraw.Draw(frame, "RGBA")
    alpha = [80, 130, 210, 150, 70][frame_index]
    if archetype == 0:
        draw.line((154, 93, 203, 55), fill=(*accent, alpha), width=5)
        draw.line((158, 101, 205, 78), fill=(255, 245, 198, max(0, alpha - 40)), width=3)
    elif archetype == 1:
        for i in range(3):
            x = 160 + frame_index * 8 + i * 18
            draw.rectangle((x, 103 + i % 2, x + 12, 108 + i % 2), fill=(*accent, alpha))
    else:
        cx = 188 + frame_index * 3
        cy = 116
        radius = 10 + frame_index * 4
        draw.ellipse((cx - radius, cy - radius, cx + radius, cy + radius), outline=(*accent, alpha), width=4)
        draw.rectangle((cx - 5, cy - 5, cx + 5, cy + 5), fill=(255, 240, 180, max(0, alpha - 30)))


def save_unit_frames(base: Image.Image, target_dir: Path, archetype: int, accent: tuple[int, int, int]) -> None:
    ensure_folder(target_dir)
    move_offsets = [(-5, 0), (-2, -5), (0, -2), (2, -5), (5, 0)]
    attack_offsets = [(0, 0), (6, -2), (11, 0), (4, 1), (0, 0)]
    for i, offset in enumerate(move_offsets, 1):
        frame = translated_frame(base, offset)
        save_png(frame, target_dir / f"move_{i:02d}.png")
    for i, offset in enumerate(attack_offsets, 1):
        frame = translated_frame(base, offset)
        draw_attack_effect(frame, archetype, i - 1, accent)
        save_png(frame, target_dir / f"attack_{i:02d}.png")


def save_base(source: Image.Image, target_dir: Path) -> None:
    ensure_folder(target_dir)
    base = fit_on_canvas(source, 512, 420, 24)
    save_png(base, target_dir / "Base.png")


def apply_tower_variant(base: Image.Image, variant: str, accent: tuple[int, int, int]) -> Image.Image:
    image = base.copy().convert("RGBA")
    draw = ImageDraw.Draw(image, "RGBA")
    dark = (30, 24, 18)
    bright = adjust_color(accent, 36)
    muted = adjust_color(accent, -42)

    if variant == "watch":
        draw.line((148, 44, 166, 20), fill=rgba(dark, 230), width=4)
        draw.polygon([(166, 20), (209, 32), (166, 48)], fill=rgba(dark, 235))
        draw.polygon([(170, 25), (199, 32), (170, 43)], fill=rgba(bright, 220))
    elif variant == "volley":
        draw.line((88, 82, 48, 61), fill=rgba(dark, 240), width=8)
        draw.line((91, 86, 48, 91), fill=rgba(dark, 240), width=8)
        draw.line((166, 82, 207, 61), fill=rgba(dark, 240), width=8)
        draw.line((164, 86, 208, 92), fill=rgba(dark, 240), width=8)
        draw.line((91, 82, 52, 63), fill=rgba(bright, 230), width=3)
        draw.line((164, 82, 204, 63), fill=rgba(bright, 230), width=3)
        draw_pixel_box(draw, (102, 38, 154, 62), muted)
    elif variant == "heavy":
        draw_pixel_ellipse(draw, (86, 18, 170, 88), bright)
        draw_pixel_box(draw, (72, 132, 184, 180), muted)
        draw.line((128, 53, 212, 30), fill=rgba(dark, 245), width=11)
        draw.line((133, 52, 209, 32), fill=rgba(bright, 235), width=5)
        draw.rectangle((205, 25, 223, 37), fill=rgba((255, 235, 168), 235))

    return image


def save_tower_frames(source: Image.Image, target_dir: Path, accent: tuple[int, int, int], variant: str, attack_style: int) -> None:
    ensure_folder(target_dir)
    base = apply_tower_variant(fit_on_canvas(source, 256, 205, 8), variant, accent)
    for i in range(5):
        frame = translated_frame(base, (0, 0))
        draw = ImageDraw.Draw(frame, "RGBA")
        pulse = 70 + i * 36 if i < 3 else 120 - (i - 2) * 25
        if attack_style == 0:
            draw.line((128, 56, 190 + i * 5, 42 - i * 2), fill=(*accent, 175), width=4)
        elif attack_style == 1:
            draw.ellipse((93 - i * 2, 44 - i * 3, 163 + i * 2, 100 + i * 3), outline=(*accent, pulse), width=3)
            if i in (2, 3):
                draw.line((128, 48, 190, 28), fill=(*accent, 190), width=4)
        else:
            radius = 18 + i * 7
            draw.ellipse((128 - radius, 55 - radius, 128 + radius, 55 + radius), outline=(*accent, pulse), width=4)
            if i in (2, 3):
                draw.rectangle((181, 27, 213, 40), fill=(*accent, 190))
        save_png(frame, target_dir / f"attack_{i + 1:02d}.png")


def make_facility_site_sprite() -> Image.Image:
    image = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image, "RGBA")
    draw.polygon([(64, 22), (106, 64), (64, 106), (22, 64)], fill=(54, 50, 42, 190), outline=(240, 204, 95, 255))
    draw.polygon([(64, 32), (96, 64), (64, 96), (32, 64)], fill=(91, 86, 72, 225), outline=(51, 45, 35, 255))
    draw.line((44, 64, 84, 64), fill=(255, 230, 132, 230), width=4)
    draw.line((64, 44, 64, 84), fill=(255, 230, 132, 230), width=4)
    return image


def save_facility_sprites(resource_source: Image.Image) -> None:
    ensure_folder(FACILITY_RESOURCE_DIR)
    well = fit_on_canvas(resource_source, 256, 185, 6)
    save_png(make_facility_site_sprite(), FACILITY_RESOURCE_DIR / "ResourceWellSite.png")
    save_png(well, FACILITY_RESOURCE_DIR / "ResourceWellBuilt.png")


def save_contact_sheet() -> None:
    image = Image.new("RGBA", (1660, 960), (36, 40, 36, 255))
    draw = ImageDraw.Draw(image, "RGBA")
    for row, spec in enumerate(AGE_SPECS):
        y = 35 + row * 180
        draw.text((24, y + 48), f"{spec['display']} / {spec['key']}", fill=(240, 230, 195, 255))
        paths = [
            *(ROOT / "Assets" / "Resources" / spec["key"] / "Units" / unit["name"] / "move_01.png" for unit in spec["units"]),
            ROOT / "Assets" / "Resources" / spec["key"] / "Base" / "Base.png",
            *(ROOT / "Assets" / "Resources" / spec["key"] / "Towers" / tower["name"] / "attack_03.png" for tower in spec["towers"]),
        ]
        labels = [*(unit["name"] for unit in spec["units"]), "Base", *(tower["name"] for tower in spec["towers"])]
        for col, (path, label) in enumerate(zip(paths, labels)):
            sprite = Image.open(path).convert("RGBA")
            sprite.thumbnail((128, 128), Image.Resampling.NEAREST)
            x = 180 + col * 155
            image.alpha_composite(sprite, (x + (128 - sprite.width) // 2, y + (128 - sprite.height) // 2))
            draw.text((x, y + 130), label, fill=(210, 210, 200, 255))
    save_png(image.convert("RGB"), SOURCE_DIR / "PixelAssetContactSheet.png")


def save_png(image: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path)
    write_texture_meta(path)


def resize_map() -> Image.Image:
    source = Image.open(MAP_SOURCE).convert("RGB")
    return source.resize((WIDTH, HEIGHT), Image.Resampling.NEAREST)


def tint_map(base: Image.Image, tint: tuple[int, int, int], alpha: int) -> Image.Image:
    image = ImageEnhance.Color(base.convert("RGB")).enhance(0.92)
    image = ImageEnhance.Contrast(image).enhance(1.05)
    overlay = Image.new("RGBA", image.size, (*tint, alpha))
    return Image.alpha_composite(image.convert("RGBA"), overlay)


def draw_theme_decals(image: Image.Image, spec: dict) -> Image.Image:
    themed = image.convert("RGBA")
    overlay = Image.new("RGBA", themed.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay, "RGBA")
    accent = spec["accent"]
    theme = spec["theme"]

    for lane in LANES:
        draw_polyline(draw, lane, (*accent, 42), 10)

    if theme == "barbarian":
        for x, y in [(285, 235), (515, 1085), (860, 275), (1510, 1090), (2060, 245)]:
            draw.polygon([(x, y - 36), (x + 35, y + 8), (x - 34, y + 8)], fill=(70, 54, 33, 220), outline=(226, 184, 90, 230))
            draw.rectangle((x - 24, y + 8, x + 24, y + 40), fill=(102, 72, 38, 210), outline=(42, 30, 20, 230))
        for x, y in [(620, 565), (1130, 1010), (1775, 565)]:
            draw.line((x, y - 42, x, y + 54), fill=(44, 28, 16, 230), width=10)
            draw.ellipse((x - 25, y - 64, x + 25, y - 20), outline=(235, 221, 180, 230), width=8)
    elif theme == "machine":
        for y in (265, 1085):
            draw.line((210, y, 2190, y), fill=(80, 63, 45, 160), width=22)
            for x in range(250, 2200, 95):
                draw.rectangle((x, y - 20, x + 42, y + 20), fill=(126, 93, 52, 150))
        for x, y in [(520, 520), (920, 860), (1460, 510), (1840, 840)]:
            draw.rectangle((x - 18, y - 60, x + 18, y + 34), fill=(62, 61, 55, 210), outline=(165, 131, 68, 210))
            draw.rectangle((x - 35, y + 20, x + 35, y + 44), fill=(76, 61, 42, 220))
            draw.ellipse((x - 46, y - 100, x + 46, y - 50), fill=(48, 48, 45, 75))
    elif theme == "electric":
        for a, b in [(RESOURCE_WELLS["PlayerWellSlot_0"], RESOURCE_WELLS["EnemyWellSlot_0"]), (RESOURCE_WELLS["PlayerWellSlot_1"], RESOURCE_WELLS["EnemyWellSlot_1"])]:
            draw.line((a[0], a[1], b[0], b[1]), fill=(*accent, 125), width=8)
            draw.line((a[0], a[1], b[0], b[1]), fill=(242, 255, 255, 90), width=3)
        for x, y in TOWER_SLOTS.values():
            draw.ellipse((x - 34, y - 34, x + 34, y + 34), outline=(*accent, 190), width=7)
            draw.line((x - 42, y, x + 42, y), fill=(*accent, 130), width=4)
            draw.line((x, y - 42, x, y + 42), fill=(*accent, 130), width=4)
    elif theme == "nuclear":
        for x, y in RESOURCE_WELLS.values():
            draw.ellipse((x - 56, y - 36, x + 56, y + 36), fill=(107, 199, 61, 92), outline=(*accent, 185), width=5)
            draw.pieslice((x - 26, y - 26, x + 26, y + 26), 30, 150, fill=(34, 38, 22, 180))
            draw.pieslice((x - 26, y - 26, x + 26, y + 26), 150, 270, fill=(240, 224, 55, 180))
            draw.pieslice((x - 26, y - 26, x + 26, y + 26), 270, 30, fill=(34, 38, 22, 180))
        for x in range(360, 2050, 180):
            draw.rectangle((x, 310, x + 80, 328), fill=(34, 34, 24, 170))
            draw.polygon([(x, 310), (x + 28, 310), (x + 10, 328)], fill=(235, 214, 48, 180))
    elif theme == "starsea":
        for x, y in [(345, 250), (640, 1080), (995, 285), (1390, 1080), (1760, 290), (2110, 1060)]:
            draw.polygon([(x, y - 45), (x + 38, y), (x, y + 45), (x - 38, y)], fill=(42, 36, 96, 185), outline=(*accent, 220))
            draw.polygon([(x, y - 22), (x + 18, y), (x, y + 22), (x - 18, y)], fill=(*accent, 185))
        for a, b, c in [
            (LANES[0][4], CONNECTOR_NODES["CenterUpperCut"], LANES[1][4]),
            (LANES[1][4], CONNECTOR_NODES["CenterLowerCut"], LANES[2][5]),
        ]:
            draw_polyline(draw, [a, b, c], (*accent, 135), 18)
            draw_polyline(draw, [a, b, c], (235, 224, 255, 95), 7)

    return Image.alpha_composite(themed, overlay).convert("RGB")


def create_era_map(base: Image.Image, spec: dict) -> Image.Image:
    alpha = 42 if spec["theme"] in {"barbarian", "machine"} else 54
    return draw_theme_decals(tint_map(base, spec["map_tint"], alpha), spec)


def draw_polyline(draw: ImageDraw.ImageDraw, points: list[tuple[int, int]], fill: tuple[int, int, int, int], width: int) -> None:
    draw.line(points, fill=fill, width=width, joint="curve")
    radius = width // 2
    for x, y in points:
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=fill)


def create_annotated_map(base: Image.Image) -> Image.Image:
    image = base.convert("RGBA")
    overlay = Image.new("RGBA", image.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay, "RGBA")
    for lane in LANES:
        draw_polyline(draw, lane, (255, 233, 150, 145), 20)
    for a, b, c in [
        (LANES[0][3], CONNECTOR_NODES["LeftUpperCut"], LANES[1][2]),
        (LANES[1][2], CONNECTOR_NODES["LeftLowerCut"], LANES[2][3]),
        (LANES[0][5], CONNECTOR_NODES["CenterUpperCut"], LANES[1][4]),
        (LANES[1][4], CONNECTOR_NODES["CenterLowerCut"], LANES[2][5]),
        (LANES[0][7], CONNECTOR_NODES["RightUpperCut"], LANES[1][6]),
        (LANES[1][6], CONNECTOR_NODES["RightLowerCut"], LANES[2][7]),
    ]:
        draw_polyline(draw, [a, b, c], (114, 214, 255, 135), 16)
    for name, point in BASES.items():
        draw.ellipse((point[0] - 30, point[1] - 30, point[0] + 30, point[1] + 30), outline=(255, 255, 255, 230), width=6)
    for point in TOWER_SLOTS.values():
        draw.rectangle((point[0] - 20, point[1] - 20, point[0] + 20, point[1] + 20), outline=(255, 210, 87, 230), width=5)
    for point in RESOURCE_WELLS.values():
        draw.ellipse((point[0] - 22, point[1] - 22, point[0] + 22, point[1] + 22), outline=(96, 255, 207, 225), width=5)
    return Image.alpha_composite(image, overlay).convert("RGB")


def write_layout_json() -> None:
    DOCS_DIR.mkdir(parents=True, exist_ok=True)
    data = {
        "map": {"width": WIDTH, "height": HEIGHT, "pixelsPerUnit": PPU, "resourcePath": f"Battle/Maps/{AGE_SPECS[0]['map_name']}"},
        "eraMaps": {
            spec["key"]: {
                "display": spec["display"],
                "resourcePath": f"Battle/Maps/{spec['map_name']}",
                "theme": spec["theme"],
            }
            for spec in AGE_SPECS
        },
        "lanes": [
            [{"pixel": [x, y], "world": [round(map_point(x, y)[0], 6), round(map_point(x, y)[1], 6)]} for x, y in lane]
            for lane in LANES
        ],
        "connectorNodes": {
            key: {"pixel": [x, y], "world": [round(map_point(x, y)[0], 6), round(map_point(x, y)[1], 6)]}
            for key, (x, y) in CONNECTOR_NODES.items()
        },
        "bases": {
            key: {"pixel": [x, y], "world": [round(map_point(x, y)[0], 6), round(map_point(x, y)[1], 6)]}
            for key, (x, y) in BASES.items()
        },
        "towerSlots": {
            key: {"pixel": [x, y], "world": [round(map_point(x, y)[0], 6), round(map_point(x, y)[1], 6)]}
            for key, (x, y) in TOWER_SLOTS.items()
        },
        "resourceWells": {
            key: {"pixel": [x, y], "world": [round(map_point(x, y)[0], 6), round(map_point(x, y)[1], 6)]}
            for key, (x, y) in RESOURCE_WELLS.items()
        },
        "neutralPoints": {
            key: {"pixel": [x, y], "world": [round(map_point(x, y)[0], 6), round(map_point(x, y)[1], 6)]}
            for key, (x, y) in NEUTRAL_POINTS.items()
        },
        "scaleRules": {
            "unitVisualScale": 0.72,
            "baseVisualScale": 0.28,
            "towerVisualScale": 0.22,
            "resourceWellVisualScale": 0.22,
        },
    }
    (DOCS_DIR / "pixel_frontline_layout.json").write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")


def write_design_note() -> None:
    unit_lines = []
    for spec in AGE_SPECS:
        unit_names = " / ".join(unit["name"] for unit in spec["units"])
        tower_names = " / ".join(tower["name"] for tower in spec["towers"])
        unit_lines.append(f"- {spec['display']}：5 个兵种 `{unit_names}`；3 个炮塔 `{tower_names}`；地图 `{spec['map_name']}`。")

    text = f"""# 像素风战场与时代美术重做说明

本轮继续沿用现有像素风，但把角色形态从单一的“三件套”扩展为更接近横版进化战争游戏的完整时代阵容。设计参考的是“原始近战/远程/重装一路进化到机械、电力、核能、星海”的形态节奏，不复刻具体商业素材。

## 地图结构

- 默认兼容地图：`Assets/Resources/Battle/Maps/{LEGACY_MAP_RESOURCE_NAME}.png`
- 每个时代都有同布局、不同主题的战场图：`Assets/Resources/Battle/Maps/PixelFrontline_*.png`
- 标注校准图：`Assets/Art/Generated/PixelRedesign/{LEGACY_MAP_RESOURCE_NAME}_annotated.png`
- 点位数据：`docs/pixel_frontline_layout.json`
- 结构规则：左右两个基地，上中下三条主路都连接双方基地，中段有 6 个支路节点供单位转线。

## 角色与设施

{chr(10).join(unit_lines)}

## 设计原则

- 兵种按“轻装、快速、远程、机械/载具、精英重装”形成五档选择，方便战斗节奏逐步升级。
- 炮塔按“基础速射、范围/连发、重炮/高伤”形成三档选择，避免单一塔贯穿全局。
- 地图不改变路线和点位，只通过地表色调、装饰物、能量线、污染标记、星海晶体等时代元素改变观感。
- 所有 PNG 使用 point filter、无 mipmap、Sprite 单图导入，保持清晰像素边缘。

## 比例

- `unitVisualScale`: 0.72
- `baseVisualScale`: 0.28
- `towerVisualScale`: 0.22
- `resourceWellVisualScale`: 0.22
"""
    (DOCS_DIR / "pixel_art_redesign.md").write_text(text, encoding="utf-8")
    return
    text = """# 像素风战场与美术重做说明

本轮已删除旧战斗美术资源，并用 image 生成的像素风地图与角色设施 atlas 重新裁切生成。

## 地图结构

- 游戏加载地图：`Assets/Resources/Battle/Maps/PixelFrontlineThreeLanes.png`
- 标注校准图：`Assets/Art/Generated/PixelRedesign/PixelFrontlineThreeLanes_annotated.png`
- 点位数据：`docs/pixel_frontline_layout.json`
- 结构规则：只有左、右两个基地；上、中、下三条主线路都从左基地连到右基地；中段用 6 个支路节点穿插三路，形成可转线结构。

## 角色与设施

- 五个时代的 `Base / Towers / Units` 目录已重新生成。
- 单位保持三类轮廓：轻型步兵、远程兵、重装/攻城单位。
- 设施包含每个时代的基地、炮塔，以及统一的资源井建造位和建成资源井。
- 资源井外部资源位于 `Assets/Resources/Battle/Facilities/`。

## 比例

- `unitVisualScale`: 0.72
- `baseVisualScale`: 0.28
- `towerVisualScale`: 0.22
- `resourceWellVisualScale`: 0.22
"""
    (DOCS_DIR / "pixel_art_redesign.md").write_text(text, encoding="utf-8")


def write_folder_meta(path: Path) -> None:
    meta_path = Path(str(path) + ".meta")
    if meta_path.exists():
        return
    meta_path.write_text(
        "fileFormatVersion: 2\n"
        f"guid: {uuid.uuid4().hex}\n"
        "folderAsset: yes\n"
        "DefaultImporter:\n"
        "  externalObjects: {}\n"
        "  userData:\n"
        "  assetBundleName:\n"
        "  assetBundleVariant:\n",
        encoding="utf-8",
    )


def write_texture_meta(path: Path) -> None:
    meta_path = Path(str(path) + ".meta")
    if meta_path.exists():
        return
    meta_path.write_text(
        "fileFormatVersion: 2\n"
        f"guid: {uuid.uuid4().hex}\n"
        "TextureImporter:\n"
        "  internalIDToNameTable: []\n"
        "  externalObjects: {}\n"
        "  serializedVersion: 13\n"
        "  mipmaps:\n"
        "    mipMapMode: 0\n"
        "    enableMipMap: 0\n"
        "    sRGBTexture: 1\n"
        "    linearTexture: 0\n"
        "    fadeOut: 0\n"
        "    borderMipMap: 0\n"
        "    mipMapsPreserveCoverage: 0\n"
        "    alphaTestReferenceValue: 0.5\n"
        "    mipMapFadeDistanceStart: 1\n"
        "    mipMapFadeDistanceEnd: 3\n"
        "  bumpmap:\n"
        "    convertToNormalMap: 0\n"
        "    externalNormalMap: 0\n"
        "    heightScale: 0.25\n"
        "    normalMapFilter: 0\n"
        "    flipGreenChannel: 0\n"
        "  isReadable: 0\n"
        "  streamingMipmaps: 0\n"
        "  streamingMipmapsPriority: 0\n"
        "  vTOnly: 0\n"
        "  ignoreMipmapLimit: 0\n"
        "  grayScaleToAlpha: 0\n"
        "  generateCubemap: 6\n"
        "  cubemapConvolution: 0\n"
        "  seamlessCubemap: 0\n"
        "  textureFormat: 1\n"
        "  maxTextureSize: 4096\n"
        "  textureSettings:\n"
        "    serializedVersion: 2\n"
        "    filterMode: 0\n"
        "    aniso: 1\n"
        "    mipBias: 0\n"
        "    wrapU: 1\n"
        "    wrapV: 1\n"
        "    wrapW: 1\n"
        "  nPOTScale: 0\n"
        "  lightmap: 0\n"
        "  compressionQuality: 50\n"
        "  spriteMode: 1\n"
        "  spriteExtrude: 1\n"
        "  spriteMeshType: 1\n"
        "  alignment: 0\n"
        "  spritePivot: {x: 0.5, y: 0.5}\n"
        "  spriteBorder: {x: 0, y: 0, z: 0, w: 0}\n"
        "  spriteGenerateFallbackPhysicsShape: 1\n"
        "  alphaUsage: 1\n"
        "  alphaIsTransparency: 1\n"
        "  spriteTessellationDetail: -1\n"
        "  textureType: 0\n"
        "  textureShape: 1\n"
        "  singleChannelComponent: 0\n"
        "  flipbookRows: 1\n"
        "  flipbookColumns: 1\n"
        "  maxTextureSizeSet: 0\n"
        "  compressionQualitySet: 0\n"
        "  textureFormatSet: 0\n"
        "  ignorePngGamma: 0\n"
        "  applyGammaDecoding: 0\n"
        "  swizzle: 50462976\n"
        "  cookieLightType: 0\n"
        "  platformSettings:\n"
        "  - serializedVersion: 4\n"
        "    buildTarget: DefaultTexturePlatform\n"
        "    maxTextureSize: 4096\n"
        "    resizeAlgorithm: 0\n"
        "    textureFormat: -1\n"
        "    textureCompression: 0\n"
        "    compressionQuality: 50\n"
        "    crunchedCompression: 0\n"
        "    allowsAlphaSplitting: 0\n"
        "    overridden: 0\n"
        "    ignorePlatformSupport: 0\n"
        "    androidETC2FallbackOverride: 0\n"
        "    forceMaximumCompressionQuality_BC6H_BC7: 0\n"
        "  spriteSheet:\n"
        "    serializedVersion: 2\n"
        "    sprites: []\n"
        "    outline: []\n"
        "    customData:\n"
        "    physicsShape: []\n"
        "    bones: []\n"
        "    spriteID:\n"
        "    internalID: 0\n"
        "    vertices: []\n"
        "    indices:\n"
        "    edges: []\n"
        "    weights: []\n"
        "    secondaryTextures: []\n"
        "    spriteCustomMetadata:\n"
        "      entries: []\n"
        "    nameFileIdTable: {}\n"
        "  mipmapLimitGroupName:\n"
        "  pSDRemoveMatte: 0\n"
        "  userData:\n"
        "  assetBundleName:\n"
        "  assetBundleVariant:\n",
        encoding="utf-8",
    )


def remove_path_with_meta(path: Path) -> None:
    if path.is_dir():
        shutil.rmtree(path)
    elif path.exists():
        path.unlink()

    meta_path = Path(str(path) + ".meta")
    if meta_path.is_dir():
        shutil.rmtree(meta_path)
    elif meta_path.exists():
        meta_path.unlink()


def clean_obsolete_generated_assets() -> None:
    allowed_map_names = {spec["map_name"] for spec in AGE_SPECS}
    if MAP_RESOURCE_DIR.exists():
        for path in MAP_RESOURCE_DIR.iterdir():
            if path.suffix.lower() == ".png" and path.stem.startswith("PixelFrontline") and path.stem not in allowed_map_names:
                remove_path_with_meta(path)

    for spec in AGE_SPECS:
        era_root = ROOT / "Assets" / "Resources" / spec["key"]
        stale_map_dir = era_root / "Maps"
        if stale_map_dir.exists():
            remove_path_with_meta(stale_map_dir)

        allowed_units = {unit["name"] for unit in spec["units"]}
        unit_root = era_root / "Units"
        if unit_root.exists():
            for path in unit_root.iterdir():
                if path.is_dir() and path.name not in allowed_units:
                    remove_path_with_meta(path)

        allowed_towers = {tower["name"] for tower in spec["towers"]}
        tower_root = era_root / "Towers"
        if tower_root.exists():
            for path in tower_root.iterdir():
                if path.is_dir() and path.name not in allowed_towers:
                    remove_path_with_meta(path)


def main() -> None:
    for folder in [SOURCE_DIR, MAP_RESOURCE_DIR, FACILITY_RESOURCE_DIR]:
        ensure_folder(folder)
    DOCS_DIR.mkdir(parents=True, exist_ok=True)
    clean_obsolete_generated_assets()

    map_image = resize_map()
    save_png(map_image, SOURCE_DIR / f"{LEGACY_MAP_RESOURCE_NAME}_clean.png")
    save_png(create_annotated_map(map_image), SOURCE_DIR / f"{LEGACY_MAP_RESOURCE_NAME}_annotated.png")
    for spec in AGE_SPECS:
        era_map = create_era_map(map_image, spec)
        save_png(era_map, MAP_RESOURCE_DIR / f"{spec['map_name']}.png")
        save_png(era_map, SOURCE_DIR / f"{spec['map_name']}.png")

    atlas = Image.open(ATLAS_SOURCE).convert("RGBA")
    for spec in AGE_SPECS:
        era_root = ROOT / "Assets" / "Resources" / spec["key"]
        ensure_folder(era_root)
        for sub in ["Base", "Towers", "Units"]:
            ensure_folder(era_root / sub)

        row = spec["row"]
        light = crop_sprite(atlas, ATLAS_COLUMNS["light"], row)
        ranged = crop_sprite(atlas, ATLAS_COLUMNS["ranged"], row)
        heavy = crop_sprite(atlas, ATLAS_COLUMNS["heavy"], row)
        unit_sources = {
            "light": light,
            "ranged": ranged,
            "heavy": heavy,
        }
        for unit in spec["units"]:
            unit_sprite = prepare_unit_sprite(unit_sources[unit["source"]], unit, spec["key"], spec["accent"])
            save_unit_frames(unit_sprite, era_root / "Units" / unit["name"], unit["attack_style"], spec["accent"])

        save_base(crop_sprite(atlas, ATLAS_COLUMNS["base"], row), era_root / "Base")
        tower_source = crop_sprite(atlas, ATLAS_COLUMNS["tower"], row)
        for tower in spec["towers"]:
            save_tower_frames(
                tower_source,
                era_root / "Towers" / tower["name"],
                spec["accent"],
                tower["variant"],
                tower["attack_style"],
            )

    resource_source = crop_sprite(atlas, (575, 860), (855, 1015), padding=10)
    save_facility_sprites(resource_source)
    save_contact_sheet()
    write_layout_json()
    write_design_note()
    print(f"Wrote {len(AGE_SPECS)} era maps under {MAP_RESOURCE_DIR}")
    print("Wrote five unit roles, three tower choices, bases, facilities, and era maps for all eras.")


if __name__ == "__main__":
    main()
