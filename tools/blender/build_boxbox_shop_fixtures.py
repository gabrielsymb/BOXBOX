"""Export finished fixtures and the two interior floors from the source kit."""
from pathlib import Path

import bpy

INTERIOR = Path(__file__).resolve().parents[2] / "BOXBOX/boxbox/Assets/Grocery/Interior"
OUTPUT = INTERIOR / "Fixtures"


def import_model(name):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(INTERIOR / f"{name}.glb"))


def export(name):
    bpy.ops.export_scene.gltf(filepath=str(OUTPUT / f"{name}.glb"), export_format="GLB")


for name in ("shelf-boxes", "shelf-bags", "freezers-standing", "freezer"):
    import_model(name)
    fixture = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH" and obj.name == name)
    mesh = fixture.data
    material = bpy.data.materials.new("FixtureSteel")
    slot = len(mesh.materials)
    palette = next(node.image for node in mesh.materials[0].node_tree.nodes if node.type == "TEX_IMAGE")
    width, height = palette.size
    pixels = list(palette.pixels)
    mesh.materials.append(material)
    uv = mesh.uv_layers.active.data
    for face in mesh.polygons:
        u, v = uv[face.loop_start].uv
        pixel = 4 * (int(u * width) % width + width * (int(v * height) % height))
        green, blue = pixels[pixel + 1:pixel + 3]
        if face.material_index != 0 or (name.startswith("freezer") and green > 0.71 and blue > 0.86):
            continue
        face.material_index = slot
        axis = max(range(3), key=lambda index: abs(face.normal[index]))
        other = [index for index in range(3) if index != axis]
        for loop in face.loop_indices:
            point = mesh.vertices[mesh.loops[loop].vertex_index].co
            uv[loop].uv = (point[other[0]] * 1.6, point[other[1]] * 1.6)
    export(f"{name}-finished")

for name, output, width, depth in [
    ("floor-wood", "retail-floor", 9, 7),
    ("floor", "warehouse-floor", 13, 10),
]:
    import_model(name)
    tile = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
    bpy.ops.object.select_all(action="DESELECT")
    tile.select_set(True)
    for x in range(width):
        for y in range(depth):
            if x == 0 and y == 0:
                continue
            copy = tile.copy()
            bpy.context.collection.objects.link(copy)
            copy.location = (x - (width - 1) / 2, y - (depth - 1) / 2, 0)
            copy.select_set(True)
    tile.location = (-(width - 1) / 2, -(depth - 1) / 2, 0)
    bpy.context.view_layer.objects.active = tile
    bpy.ops.object.join()
    export(output)
