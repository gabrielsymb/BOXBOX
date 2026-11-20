import bpy
import bmesh
import os
from collections import Counter
from mathutils import Matrix, Quaternion, Vector


ROOT = "/home/yakuzaa/Documentos/ChatGPT/GAME"
SOURCE_DIR = os.path.join(ROOT, "BOXBOX/asset-library/review/Kenney/mini-characters-2.0/extracted/Models/GLB format")
CHARACTER = os.path.join(SOURCE_DIR, "character-male-b.glb")
WEAPON = os.path.join(SOURCE_DIR, "weapon-shooter.glb")
OUT_DIR = os.path.join(ROOT, "BOXBOX/boxbox/Assets/Characters")
BLEND_PATH = os.path.join(OUT_DIR, "boxbox-protagonist.blend")
GLB_PATH = os.path.join(OUT_DIR, "boxbox-protagonist.glb")


def dominant_group(mesh, polygon):
    totals = Counter()
    for vertex_index in polygon.vertices:
        for membership in mesh.data.vertices[vertex_index].groups:
            totals[membership.group] += membership.weight
    return mesh.vertex_groups[totals.most_common(1)[0][0]].name if totals else None


def make_tank_top(body):
    skin = bpy.data.materials.new("BOXBOX_LightSkin")
    skin.use_nodes = True
    shader = next(node for node in skin.node_tree.nodes if node.type == "BSDF_PRINCIPLED")
    shader.inputs["Base Color"].default_value = (0.78, 0.52, 0.36, 1.0)
    shader.inputs["Roughness"].default_value = 0.72
    body.data.materials.append(skin)
    skin_slot = len(body.data.materials) - 1
    for polygon in body.data.polygons:
        if dominant_group(body, polygon) in {"arm-left", "arm-right"}:
            polygon.material_index = skin_slot


def remove_facial_hair(head):
    mesh = bmesh.new()
    mesh.from_mesh(head.data)
    facial_hair = [
        face
        for face in mesh.faces
        if min(vertex.co.y for vertex in face.verts) < -0.17
        and face.calc_center_median().z < 0.46
    ]
    bmesh.ops.delete(mesh, geom=facial_hair, context="FACES")
    mesh.to_mesh(head.data)
    mesh.free()
    head.data.update()


bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
os.makedirs(OUT_DIR, exist_ok=True)

bpy.ops.import_scene.gltf(filepath=CHARACTER)
armature = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
body = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH" and obj.name.startswith("body-mesh"))
head = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH" and obj.name.startswith("head-mesh"))
armature.name = "BOXBOX_ProtagonistRig"
body.name = "BOXBOX_ProtagonistBody"
head.name = "BOXBOX_ProtagonistHead"
make_tank_top(body)
remove_facial_hair(head)

# Author a reusable weapon socket while the arm is in its shooting pose.
shoot_action = bpy.data.actions["holding-right-shoot_character-male-b"]
armature.animation_data.action = shoot_action
bpy.context.scene.frame_set(2)
bpy.context.view_layer.update()
hand_world = armature.matrix_world @ armature.pose.bones["arm-right"].head
hand_world.y -= 0.076
hand_world.z += 0.019

socket = bpy.data.objects.new("WeaponSocket", None)
bpy.context.scene.collection.objects.link(socket)
socket.parent = armature
socket.parent_type = "BONE"
socket.parent_bone = "arm-right"
socket.matrix_world = Matrix.LocRotScale(hand_world, Quaternion((1, 0, 0, 0)), Vector((1, 1, 1)))

before = set(bpy.data.objects)
bpy.ops.import_scene.gltf(filepath=WEAPON)
weapon = next(obj for obj in set(bpy.data.objects) - before if obj.type == "MESH")
weapon.name = "BOXBOX_WeaponShooter"
weapon.parent = socket
weapon.matrix_parent_inverse = socket.matrix_world.inverted()
weapon.matrix_world = socket.matrix_world.copy()

armature.animation_data.action = bpy.data.actions["idle_character-male-b"]
bpy.context.scene.frame_set(0)
bpy.context.view_layer.update()

for obj in bpy.context.scene.objects:
    obj.select_set(True)

bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
bpy.ops.export_scene.gltf(
    filepath=GLB_PATH,
    export_format="GLB",
    use_selection=True,
    export_animations=True,
    export_nla_strips=True,
    export_yup=True,
    export_apply=False,
)
print(f"BOXBOX_OUTPUT|blend={BLEND_PATH}")
print(f"BOXBOX_OUTPUT|glb={GLB_PATH}")
