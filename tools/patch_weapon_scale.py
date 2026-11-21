#!/usr/bin/env python3
"""Patch the embedded weapon node in the BOXBOX protagonist GLB.

The character source has a compact seven-bone rig, so the weapon remains a child
of WeaponSocket. This patch changes only the weapon node transform and preserves
all mesh, skin, animation and material chunks byte-for-byte.
"""
from __future__ import annotations

import json
import shutil
import struct
import sys
from pathlib import Path

JSON_CHUNK = 0x4E4F534A
BIN_CHUNK = 0x004E4942
HEADER = struct.Struct("<4sII")
CHUNK_HEADER = struct.Struct("<II")


def read_glb(path: Path) -> tuple[dict, bytes, bytes, bytes]:
    raw = path.read_bytes()
    magic, version, total = HEADER.unpack_from(raw, 0)
    if magic != b"glTF" or version != 2 or total != len(raw):
        raise ValueError(f"Unsupported or malformed GLB: {path}")
    pos = HEADER.size
    json_chunk = None
    bin_chunk = None
    other_chunks: list[bytes] = []
    while pos < len(raw):
        length, kind = CHUNK_HEADER.unpack_from(raw, pos)
        start = pos + CHUNK_HEADER.size
        data = raw[start : start + length]
        pos = start + length
        if kind == JSON_CHUNK:
            json_chunk = data.rstrip(b" \t\r\n")
        elif kind == BIN_CHUNK:
            bin_chunk = data
        else:
            other_chunks.append(CHUNK_HEADER.pack(length, kind) + data)
    if json_chunk is None or bin_chunk is None:
        raise ValueError("GLB must contain JSON and BIN chunks")
    return json.loads(json_chunk.decode("utf-8")), bin_chunk, raw, b"".join(other_chunks)


def chunk(kind: int, payload: bytes) -> bytes:
    padded = payload + b" " * ((4 - len(payload) % 4) % 4) if kind == JSON_CHUNK else payload
    return CHUNK_HEADER.pack(len(padded), kind) + padded


def main() -> int:
    if len(sys.argv) != 3:
        print("usage: patch_weapon_scale.py INPUT.glb OUTPUT.glb", file=sys.stderr)
        return 2
    source, target = map(Path, sys.argv[1:])
    gltf, bin_chunk, _, other = read_glb(source)
    weapon = next((n for n in gltf.get("nodes", []) if n.get("name") == "BOXBOX_WeaponShooter"), None)
    socket = next((n for n in gltf.get("nodes", []) if n.get("name") == "WeaponSocket"), None)
    if weapon is None or socket is None:
        raise SystemExit("Expected BOXBOX_WeaponShooter and WeaponSocket nodes")

    # The source mesh is about 0.30 m long while the normalized actor is 0.78 m.
    # 1.65x makes the rifle read as a deliberate prop without changing the rig.
    weapon["scale"] = [1.65, 1.65, 1.65]
    weapon.setdefault("extras", {})["boxbox_weapon_scale_pass"] = "1.65x; socket preserved"
    socket.setdefault("extras", {})["boxbox_attachment_contract"] = "weapon grip follows arm-right socket"

    json_payload = json.dumps(gltf, separators=(",", ":"), ensure_ascii=False).encode("utf-8")
    result = HEADER.pack(b"glTF", 2, 0) + chunk(JSON_CHUNK, json_payload) + chunk(BIN_CHUNK, bin_chunk) + other
    result = result[:8] + struct.pack("<I", len(result)) + result[12:]
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(result)
    print(f"patched={target}")
    print(f"weapon_scale={weapon['scale']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
