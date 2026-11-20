import bpy
import os
from mathutils import Vector


ROOT = "/home/yakuzaa/Documentos/ChatGPT/GAME"
CHARACTER = os.path.join(ROOT, "BOXBOX/asset-library/review/Kenney/mini-characters-2.0/extracted/Models/GLB format/character-female-a.glb")
WEAPON = os.path.join(ROOT, "BOXBOX/asset-library/review/Kenney/mini-characters-2.0/extracted/Models/GLB format/weapon-shooter.glb")


def clear():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def bounds(objects):
    points = []
    for obj in objects:
        if obj.type == "MESH":
            points.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    if not points:
        return None
    low = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    high = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return low, high, high - low


clear()
bpy.ops.import_scene.gltf(filepath=CHARACTER)
for obj in bpy.context.scene.objects:
    if obj.type == "ARMATURE":
        print(f"BOXBOX_ARMATURE|{obj.name}")
        for bone in obj.data.bones:
            print(f"BOXBOX_BONE|{bone.name}|parent={bone.parent.name if bone.parent else '-'}|head={tuple(round(v, 4) for v in bone.head_local)}|tail={tuple(round(v, 4) for v in bone.tail_local)}")

for action in bpy.data.actions:
    print(f"BOXBOX_ACTION|{action.name}|frames={tuple(round(v, 2) for v in action.frame_range)}|fcurves={len(action.fcurves)}")

before = set(bpy.data.objects)
bpy.ops.import_scene.gltf(filepath=WEAPON)
weapon_objects = list(set(bpy.data.objects) - before)
weapon_bounds = bounds(weapon_objects)
if weapon_bounds:
    low, high, size = weapon_bounds
    print(f"BOXBOX_WEAPON|low={tuple(round(v, 4) for v in low)}|high={tuple(round(v, 4) for v in high)}|size={tuple(round(v, 4) for v in size)}")
for obj in weapon_objects:
    print(f"BOXBOX_WEAPON_OBJECT|{obj.name}|type={obj.type}|location={tuple(round(v, 4) for v in obj.location)}")
