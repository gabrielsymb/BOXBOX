"""Create the runtime character from the user-supplied Manus locomotion GLB."""
from pathlib import Path
import sys
import bpy

source, target = [Path(arg) for arg in sys.argv[sys.argv.index("--") + 1:]]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source))
mesh = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH" and obj.data.materials)
bpy.context.view_layer.objects.active = mesh
source_faces = len(mesh.data.polygons)
decimate = mesh.modifiers.new("Gameplay triangle budget", "DECIMATE")
decimate.ratio = min(1.0, 36000 / source_faces)
bpy.ops.object.modifier_move_to_index(modifier=decimate.name, index=0)
bpy.ops.object.modifier_apply(modifier=decimate.name)
print("MESH", source_faces, len(mesh.data.polygons), flush=True)
for obj in list(bpy.context.scene.objects):
    if obj.type == "MESH" and not obj.data.materials:
        bpy.data.objects.remove(obj, do_unlink=True)
for image in bpy.data.images:
    width, height = image.size
    if max(width, height) > 1024:
        scale = 1024 / max(width, height)
        image.scale(max(1, round(width * scale)), max(1, round(height * scale)))
        image.pack()
    if image.size[0] > 0:
        image.name = image.name.split("_")[0]
bpy.ops.export_scene.gltf(filepath=str(target), export_format="GLB",
                          export_animation_mode="NLA_TRACKS")
