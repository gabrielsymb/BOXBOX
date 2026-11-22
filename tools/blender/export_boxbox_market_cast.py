"""Curate the licensed Polyfork pedestrians for the BOXBOX street catalog."""
from pathlib import Path

import bpy

PROJECT = Path(__file__).resolve().parents[2] / "BOXBOX"
SOURCE = PROJECT / "asset-library/review/NEW_ASSETS/NEW_ASSETS"
OUTPUT = PROJECT / "boxbox/Assets/Characters/NewCast/Pedestrians"

for source, name, omitted in [
    ("commuter-381e7f", "boxbox-commuter", ("attach-lefthand", "attach-righthand")),
    ("shopkeeper-be5547", "boxbox-shopkeeper", ()),
    ("surveyor-82e5c7", "boxbox-surveyor", ("attach-righthand",)),
]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(SOURCE / source / f"{source}.glb"))
    for obj in list(bpy.context.scene.objects):
        if obj.name.startswith(omitted) or (obj.type == "MESH" and not obj.data.materials):
            bpy.data.objects.remove(obj, do_unlink=True)
    bpy.ops.export_scene.gltf(filepath=str(OUTPUT / f"{name}.glb"), export_format="GLB",
                              export_animation_mode="NLA_TRACKS")
