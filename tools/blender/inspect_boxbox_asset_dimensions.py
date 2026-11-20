import bpy
import os
from mathutils import Vector


ROOT = "/home/yakuzaa/Documentos/ChatGPT/GAME"
ASSETS = [
    "BOXBOX/asset-library/review/Kenney/mini-characters-2.0/extracted/Models/GLB format/character-female-a.glb",
    "BOXBOX/asset-library/review/Kenney/mini-shop-2.1/extracted/Models/GLB format/floor-wood.glb",
    "BOXBOX/asset-library/review/Kenney/mini-shop-2.1/extracted/Models/GLB format/wall.glb",
    "BOXBOX/asset-library/review/Kenney/mini-shop-2.1/extracted/Models/GLB format/cash-register.glb",
    "BOXBOX/asset-library/review/Kenney/city-kit-roads-2.2/extracted/Models/GLB format/road-straight.glb",
    "BOXBOX/asset-library/review/Kenney/city-kit-commercial-2.1/extracted/Models/GLB format/building-a.glb",
    "BOXBOX/asset-library/review/Kenney/city-kit-suburban-2.0/extracted/Models/GLB format/building-type-a.glb",
    "BOXBOX/asset-library/review/Kenney/furniture-kit-2.0/extracted/Models/GLTF format/chair.glb",
    "BOXBOX/asset-library/review/Kenney/food-kit-2.0/extracted/Models/GLB format/apple.glb",
    "BOXBOX/asset-library/review/Kenney/car-kit-3.1/extracted/Models/GLB format/delivery.glb",
    "BOXBOX/asset-library/review/Kenney/mini-forest-1.0/extracted/Models/GLB format/tree.glb",
]


def bounds(objects):
    corners = []
    for obj in objects:
        if obj.type not in {"MESH", "CURVE"}:
            continue
        corners.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    minimum = Vector((min(p.x for p in corners), min(p.y for p in corners), min(p.z for p in corners)))
    maximum = Vector((max(p.x for p in corners), max(p.y for p in corners), max(p.z for p in corners)))
    return minimum, maximum, maximum - minimum


bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)

for relative_path in ASSETS:
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=os.path.join(ROOT, relative_path))
    imported = list(set(bpy.data.objects) - before)
    minimum, maximum, size = bounds(imported)
    print(f"BOXBOX_DIM|{os.path.basename(relative_path)}|min={tuple(round(v, 3) for v in minimum)}|max={tuple(round(v, 3) for v in maximum)}|size={tuple(round(v, 3) for v in size)}")
    bpy.ops.object.select_all(action="DESELECT")
    for obj in imported:
        obj.select_set(True)
    bpy.ops.object.delete(use_global=False)
