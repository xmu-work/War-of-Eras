#!/usr/bin/env python3
import argparse
import json
import re
import uuid
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
MANIFEST = Path(__file__).with_name("asset_manifest.json")


def unity_guid(meta_path: Path) -> str:
    if meta_path.exists():
        match = re.search(r"^guid: ([0-9a-f]+)$", meta_path.read_text(), re.MULTILINE)
        if match:
            return match.group(1)
    return uuid.uuid4().hex


def write_folder_meta(path: Path) -> None:
    path.mkdir(parents=True, exist_ok=True)
    meta_path = Path(str(path) + ".meta")
    meta_path.write_text(
        f"""fileFormatVersion: 2
guid: {unity_guid(meta_path)}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""
    )


def write_texture_meta(path: Path, width: int, height: int) -> None:
    meta_path = Path(str(path) + ".meta")
    guid = unity_guid(meta_path)
    name = f"{path.stem}_0"
    internal_id = int(uuid.uuid5(uuid.NAMESPACE_URL, guid + ":" + name).int % (2**63 - 1))
    sprite_id = uuid.uuid5(uuid.NAMESPACE_DNS, guid + ":" + name).hex[:32]
    meta_path.write_text(
        f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable:
  - first:
      213: {internal_id}
    second: {name}
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 4096
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
    - serializedVersion: 2
      name: {name}
      rect:
        serializedVersion: 2
        x: 0
        y: 0
        width: {width}
        height: {height}
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      customData:
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {sprite_id}
      internalID: {internal_id}
      vertices: []
      indices:
      edges: []
      weights: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
      {name}: {internal_id}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""
    )


def normalize_map(source: Path, target: Path, size: tuple[int, int]) -> None:
    image = Image.open(source).convert("RGB")
    image.thumbnail(size, Image.Resampling.LANCZOS)
    canvas = Image.new("RGB", size, (34, 38, 34))
    canvas.paste(image, ((size[0] - image.width) // 2, (size[1] - image.height) // 2))
    target.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(target)
    write_folder_meta(target.parent)
    write_texture_meta(target, size[0], size[1])


def normalize_sprite(source: Path, target: Path, size: tuple[int, int]) -> None:
    image = Image.open(source).convert("RGBA")
    image.thumbnail(size, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    canvas.alpha_composite(image, ((size[0] - image.width) // 2, (size[1] - image.height) // 2))
    target.parent.mkdir(parents=True, exist_ok=True)
    write_folder_meta(target.parent)
    canvas.save(target)
    write_texture_meta(target, size[0], size[1])


def split_sheet(source: Path, target_prefix: Path, columns: int, rows: int, size: tuple[int, int] | None) -> None:
    image = Image.open(source).convert("RGBA")
    frame_width = image.width // columns
    frame_height = image.height // rows
    target_prefix.parent.mkdir(parents=True, exist_ok=True)
    write_folder_meta(target_prefix.parent)
    for index in range(columns * rows):
        col = index % columns
        row = index // columns
        frame = image.crop((col * frame_width, row * frame_height, (col + 1) * frame_width, (row + 1) * frame_height))
        if size is not None:
            frame.thumbnail(size, Image.Resampling.LANCZOS)
            canvas = Image.new("RGBA", size, (0, 0, 0, 0))
            canvas.alpha_composite(frame, ((size[0] - frame.width) // 2, (size[1] - frame.height) // 2))
            frame = canvas
        target = target_prefix.parent / f"{target_prefix.name}{index + 1:02d}.png"
        frame.save(target)
        write_texture_meta(target, frame.width, frame.height)


def split_row(
    image: Image.Image,
    row: int,
    columns: int,
    rows: int,
    target_prefix: Path,
    size: tuple[int, int],
) -> None:
    frame_width = image.width // columns
    frame_height = image.height // rows
    target_prefix.parent.mkdir(parents=True, exist_ok=True)
    write_folder_meta(target_prefix.parent)
    for col in range(columns):
        frame = image.crop((col * frame_width, row * frame_height, (col + 1) * frame_width, (row + 1) * frame_height))
        frame.thumbnail(size, Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", size, (0, 0, 0, 0))
        canvas.alpha_composite(frame, ((size[0] - frame.width) // 2, (size[1] - frame.height) // 2))
        target = target_prefix.parent / f"{target_prefix.name}{col + 1:02d}.png"
        canvas.save(target)
        write_texture_meta(target, size[0], size[1])


def split_period_atlas(source: Path, period_key: str, size: tuple[int, int]) -> None:
    manifest = json.loads(MANIFEST.read_text())
    period = next((item for item in manifest["periods"] if item["key"] == period_key), None)
    if period is None:
        raise SystemExit(f"Unknown period: {period_key}")

    image = Image.open(source).convert("RGBA")
    columns = manifest["spriteSheet"]["columns"]
    rows = len(period["units"]) * 2 + 1
    row = 0
    for unit in period["units"]:
        split_row(image, row, columns, rows, ROOT / unit["move"], size)
        row += 1
        split_row(image, row, columns, rows, ROOT / unit["attack"], size)
        row += 1

    split_row(image, row, columns, rows, ROOT / period["tower"]["attack"], size)


def ensure_manifest_dirs() -> None:
    manifest = json.loads(MANIFEST.read_text())
    for period in manifest["periods"]:
        paths = [period["map"], period["base"], period["tower"]["attack"] + "01.png"]
        for unit in period["units"]:
            paths.extend([unit["move"] + "01.png", unit["attack"] + "01.png"])
        for item in paths:
            write_folder_meta((ROOT / item).parent)


def validate_assets() -> None:
    manifest = json.loads(MANIFEST.read_text())
    expected = []
    for period in manifest["periods"]:
        expected.extend([period["map"], period["base"]])
        for unit in period["units"]:
            for prefix in (unit["move"], unit["attack"]):
                expected.extend([prefix + f"{index:02d}.png" for index in range(1, 6)])
        expected.extend([period["tower"]["attack"] + f"{index:02d}.png" for index in range(1, 6)])

    missing = []
    bad_alpha = []
    bad_size = []
    for item in expected:
        path = ROOT / item
        if not path.exists():
            missing.append(item)
            continue

        image = Image.open(path)
        if item.endswith("Maps/ForestThreeLanes.png"):
            if image.size != tuple(manifest["mapSize"]):
                bad_size.append(f"{item}: {image.size}")
            continue

        rgba = image.convert("RGBA")
        alpha = rgba.getchannel("A")
        if alpha.getextrema() == (255, 255):
            bad_alpha.append(item)

    if missing or bad_alpha or bad_size:
        if missing:
            print("Missing assets:")
            print("\n".join(missing))
        if bad_alpha:
            print("Sprites without usable transparency:")
            print("\n".join(bad_alpha))
        if bad_size:
            print("Bad map sizes:")
            print("\n".join(bad_size))
        raise SystemExit(1)

    print(f"Validated {len(expected)} gameplay PNGs.")


def main() -> None:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)
    sub.add_parser("ensure-dirs")

    map_parser = sub.add_parser("map")
    map_parser.add_argument("--source", required=True)
    map_parser.add_argument("--target", required=True)

    sheet_parser = sub.add_parser("sheet")
    sheet_parser.add_argument("--source", required=True)
    sheet_parser.add_argument("--target-prefix", required=True)
    sheet_parser.add_argument("--columns", type=int, default=5)
    sheet_parser.add_argument("--rows", type=int, default=1)
    sheet_parser.add_argument("--width", type=int)
    sheet_parser.add_argument("--height", type=int)

    sprite_parser = sub.add_parser("sprite")
    sprite_parser.add_argument("--source", required=True)
    sprite_parser.add_argument("--target", required=True)
    sprite_parser.add_argument("--width", type=int, required=True)
    sprite_parser.add_argument("--height", type=int, required=True)

    atlas_parser = sub.add_parser("period-atlas")
    atlas_parser.add_argument("--source", required=True)
    atlas_parser.add_argument("--period", required=True)
    atlas_parser.add_argument("--width", type=int, default=256)
    atlas_parser.add_argument("--height", type=int, default=256)

    sub.add_parser("validate")

    args = parser.parse_args()
    manifest = json.loads(MANIFEST.read_text())
    map_size = tuple(manifest["mapSize"])

    if args.command == "ensure-dirs":
        ensure_manifest_dirs()
    elif args.command == "map":
        normalize_map(Path(args.source), ROOT / args.target, map_size)
    elif args.command == "sheet":
        size = (args.width, args.height) if args.width and args.height else None
        split_sheet(Path(args.source), ROOT / args.target_prefix, args.columns, args.rows, size)
    elif args.command == "sprite":
        normalize_sprite(Path(args.source), ROOT / args.target, (args.width, args.height))
    elif args.command == "period-atlas":
        split_period_atlas(Path(args.source), args.period, (args.width, args.height))
    elif args.command == "validate":
        validate_assets()


if __name__ == "__main__":
    main()
