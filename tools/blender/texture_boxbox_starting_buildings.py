"""Create neighborhood GLBs with metre-scaled UVs and named Godot material slots."""
from pathlib import Path

import bpy
from mathutils import Vector

WORLD = Path(__file__).resolve().parents[2] / "BOXBOX/boxbox/Assets/World"
OUTPUT = WORLD / "StartingBlock"

# Scale uses Blender axes (width, depth, height), before the Godot scene rotations.
BUILDINGS = (
    ("Commercial/corner-shop-market", "mercearia-fachada", (7.1, 4.15, 3.85), "RebocoAzul", None, 1.08),
    ("Suburban/building-type-g", "casa-terrea", (4.2, 4.2, 4.2), "RebocoCasa", 0.40, None),
    ("Suburban/building-type-k", "sobrado", (4.6, 4.4, 4.1), "TijoloRemendado", 0.68, None),
    ("Suburban/building-type-q", "casa-dos-fundos", (4.55, 4.45, 4.35), "BlocoAparente", None, 0.63),
)

BEAM_HEIGHTS = {
    "RebocoAzul": (0.17, 0.28, 0.425, 0.825, 1.25),
    "RebocoCasa": (0.025,),
    "TijoloRemendado": (0.025, 0.425, 0.90, 1.0),
    "BlocoAparente": (0.025, 0.425, 0.57, 0.68, 0.825),
}

METRES_PER_TILE = {
    "RebocoAzul": 2.1,
    "RebocoCasa": 2.5,
    "TijoloRemendado": 2.5,
    "BlocoAparente": 2.1,
    "TelhaCeramica": 2.3,
    "Laje": 3.6,
    "TelhaCinza": 2.3,
    "VigaConcreto": 1.45,
    "MetalEsquadria": 0.85,
}


def add_material_slots(obj):
    indices = {}
    for name in METRES_PER_TILE:
        material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        indices[name] = len(obj.data.materials)
        obj.data.materials.append(material)
    return indices


def texture_building(obj, scale, wall, tiled_roof, flat_roof):
    mesh = obj.data
    palette_node = next((node for node in mesh.materials[0].node_tree.nodes
                         if node.type == "TEX_IMAGE"), None)
    if palette_node is None:
        return

    image = palette_node.image
    width, height = image.size
    pixels = list(image.pixels)
    uv = mesh.uv_layers.active.data
    indices = add_material_slots(obj)

    for face in mesh.polygons:
        if face.material_index != 0:
            continue
        u, v = uv[face.loop_start].uv
        pixel = 4 * (int(u * width) % width + width * (int(v * height) % height))
        red, green, blue = pixels[pixel:pixel + 3]
        center = obj.matrix_world @ face.center
        normal = (obj.matrix_world.to_3x3() @ face.normal).normalized()
        material = None
        points = [obj.matrix_world @ mesh.vertices[index].co for index in face.vertices]
        span = max(max(point[axis] for point in points) - min(point[axis] for point in points)
                   for axis in (0, 1))
        trim = 0.22 < red < 0.57 and 0.04 < blue - red < 0.16 and abs(green - red) < 0.045
        if tiled_roof is not None and center.z > tiled_roof and green > red + 0.13 and green > blue + 0.08:
            material = "TelhaCinza" if wall == "TijoloRemendado" else "TelhaCeramica"
        elif flat_roof is not None and center.z > flat_roof and normal.z > 0.97 and face.area > 0.015:
            material = "Laje"
        elif min(red, green, blue) > 0.70 and abs(normal.z) < 0.40:
            material = wall
        elif trim and span > 0.28 and any(abs(center.z - level) < 0.035 for level in BEAM_HEIGHTS[wall]):
            material = "VigaConcreto"
        elif trim and 0.07 < center.z < 0.77 and span < 0.24 and abs(normal.z) < 0.55:
            material = "MetalEsquadria"
        if material is None:
            continue

        face.material_index = indices[material]
        tile = METRES_PER_TILE[material]
        for loop_index in face.loop_indices:
            vertex = mesh.vertices[mesh.loops[loop_index].vertex_index].co
            position = (obj.matrix_world @ vertex) * Vector(scale)
            if material in {"TelhaCeramica", "TelhaCinza", "Laje"} or abs(normal.z) > 0.7:
                uv[loop_index].uv = (position.x / tile, position.y / tile)
            else:
                along = position.y if abs(normal.x) >= abs(normal.y) else position.x
                uv[loop_index].uv = (along / tile, position.z / tile)


for source, name, scale, wall, tiled_roof, flat_roof in BUILDINGS:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(WORLD / "Buildings" / f"{source}.glb"))
    for obj in bpy.context.scene.objects:
        if obj.type == "MESH":
            texture_building(obj, scale, wall, tiled_roof, flat_roof)
    if name == "mercearia-fachada":
        awning = bpy.data.objects["Striped_Awning"]
        awning.location.y = 0.59
    bpy.ops.export_scene.gltf(filepath=str(OUTPUT / f"{name}.glb"), export_format="GLB")
