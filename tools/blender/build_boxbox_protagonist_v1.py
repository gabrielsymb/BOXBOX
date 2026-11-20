import bpy
import bmesh
import math
import os
from collections import Counter
from mathutils import Matrix, Quaternion, Vector


ROOT = "/home/yakuzaa/Documentos/ChatGPT/GAME"
CHAR_DIR = os.path.join(ROOT, "BOXBOX/asset-library/review/Kenney/mini-characters-2.0/extracted/Models/GLB format")
CHARACTER = os.path.join(CHAR_DIR, "character-male-b.glb")
WEAPON = os.path.join(CHAR_DIR, "weapon-shooter.glb")
FLOOR = os.path.join(ROOT, "BOXBOX/asset-library/review/Kenney/mini-shop-2.1/extracted/Models/GLB format/floor-wood.glb")
FONT = os.path.join(ROOT, "BOXBOX/asset-library/review/GoogleFonts/fredoka/original/Fredoka-wdth-wght.ttf")
OUT_DIR = os.path.join(ROOT, "BOXBOX/asset-library/reports")
BLEND_PATH = os.path.join(OUT_DIR, "boxbox-protagonist-v1.blend")
RENDER_PATH = os.path.join(OUT_DIR, "boxbox-protagonist-v1.png")

POSES = [
    ("NEUTRO", "idle", -2.55, False, "arm-right", 7),
    ("DUAS MÃOS", "holding-both-shoot", 0.0, True, "arm-right", 2),
    ("UMA MÃO", "holding-right-shoot", 2.55, True, "arm-right", 2),
]


def ensure_collection(name):
    result = bpy.data.collections.get(name)
    if result is None:
        result = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(result)
    return result


def import_objects(filepath):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=filepath)
    return list(set(bpy.data.objects) - before)


def move_to_collection(objects, name):
    destination = ensure_collection(name)
    for obj in objects:
        for current in list(obj.users_collection):
            current.objects.unlink(obj)
        destination.objects.link(obj)


def dominant_group(mesh, polygon):
    totals = Counter()
    for vertex_index in polygon.vertices:
        for membership in mesh.data.vertices[vertex_index].groups:
            totals[membership.group] += membership.weight
    if not totals:
        return None
    return mesh.vertex_groups[totals.most_common(1)[0][0]].name


def convert_sleeves_to_bare_arms(body):
    # A material slot authored in Blender turns the existing arm geometry into
    # bare skin. The mesh, weights, proportions and source texture stay intact.
    material = bpy.data.materials.new(f"BOXBOX_LightSkin_{body.name}")
    material.use_nodes = True
    shader = next(node for node in material.node_tree.nodes if node.type == "BSDF_PRINCIPLED")
    shader.inputs["Base Color"].default_value = (0.78, 0.52, 0.36, 1.0)
    shader.inputs["Roughness"].default_value = 0.72
    body.data.materials.append(material)
    skin_slot = len(body.data.materials) - 1
    for polygon in body.data.polygons:
        if dominant_group(body, polygon) in {"arm-left", "arm-right"}:
            polygon.material_index = skin_slot


def remove_beard_and_moustache(head):
    # Male B's beard is disconnected geometry in front of the lower face. Keep
    # the face, eyes, mouth and blond top hair while removing only that layer.
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


def add_floor(x):
    objects = import_objects(FLOOR)
    move_to_collection(objects, "Imported Review Floor")
    object_set = set(objects)
    holder = bpy.data.objects.new(f"Floor_{x}", None)
    ensure_collection("Imported Review Floor").objects.link(holder)
    for obj in objects:
        if obj.parent not in object_set:
            obj.parent = holder
    holder.location = (x, 0, 0)
    holder.scale = (2.15, 2.15, 2.15)


def add_label(text, x):
    curve = bpy.data.curves.new(text, "FONT")
    curve.body = text
    curve.align_x = "CENTER"
    curve.align_y = "CENTER"
    curve.size = 0.25
    curve.extrude = 0.004
    curve.font = bpy.data.fonts.load(FONT)
    ink = bpy.data.materials.get("LabelInk")
    if ink is None:
        ink = bpy.data.materials.new("LabelInk")
        ink.diffuse_color = (0.98, 0.99, 1, 1)
    curve.materials.append(ink)
    label = bpy.data.objects.new(text, curve)
    ensure_collection("Review Labels - not game UI").objects.link(label)
    label.location = (x, -0.95, 0.04)
    label.rotation_euler = (math.radians(78), 0, 0)


def add_protagonist(label, action_prefix, x, armed, weapon_bone, frame):
    objects = import_objects(CHARACTER)
    move_to_collection(objects, "BOXBOX Protagonist V1")
    object_set = set(objects)
    armature = next(obj for obj in objects if obj.type == "ARMATURE")
    body = next(obj for obj in objects if obj.type == "MESH" and obj.name.startswith("body-mesh"))
    head = next(obj for obj in objects if obj.type == "MESH" and obj.name.startswith("head-mesh"))
    convert_sleeves_to_bare_arms(body)
    remove_beard_and_moustache(head)

    action_name = f"{action_prefix}_character-male-b"
    action = next(item for item in bpy.data.actions if item.name == action_name)
    armature.animation_data_create()
    armature.animation_data.action = action

    holder = bpy.data.objects.new(f"Protagonist_{label}", None)
    ensure_collection("BOXBOX Protagonist V1").objects.link(holder)
    for obj in objects:
        if obj.parent not in object_set:
            obj.parent = holder
    holder.location = (x, 0, 0.04)
    holder.scale = (2.1, 2.1, 2.1)

    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()

    if armed:
        weapon_objects = import_objects(WEAPON)
        move_to_collection(weapon_objects, "Weapon Socket Test")
        weapon = next(obj for obj in weapon_objects if obj.type == "MESH")
        weapon.name = f"weapon-shooter_{label}"
        weapon.parent = armature
        weapon.parent_type = "BONE"
        weapon.parent_bone = weapon_bone
        hand_world = armature.matrix_world @ armature.pose.bones[weapon_bone].head
        hand_world.y -= 0.16
        hand_world.z += 0.04
        weapon.matrix_world = Matrix.LocRotScale(
            hand_world,
            Quaternion((1, 0, 0, 0)),
            Vector((2.1, 2.1, 2.1)),
        )

    add_label(label, x)


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
os.makedirs(OUT_DIR, exist_ok=True)

for label, action, x, armed, bone, frame in POSES:
    add_floor(x)
    add_protagonist(label, action, x, armed, bone, frame)

# Use a common frame that keeps the short shooting actions posed.
bpy.context.scene.frame_set(2)

world = bpy.context.scene.world or bpy.data.worlds.new("Protagonist Review World")
bpy.context.scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.035, 0.052, 0.08, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.65

sun_data = bpy.data.lights.new("Sun", "SUN")
sun_data.energy = 2.4
sun_data.angle = math.radians(20)
sun = bpy.data.objects.new("Sun", sun_data)
ensure_collection("Review Rig").objects.link(sun)
sun.rotation_euler = (math.radians(32), math.radians(-18), math.radians(-28))

area_data = bpy.data.lights.new("Softbox", "AREA")
area_data.energy = 850
area_data.size = 8
area = bpy.data.objects.new("Softbox", area_data)
ensure_collection("Review Rig").objects.link(area)
area.location = (0, -4, 8)
look_at(area, (0, 0, 0.5))

camera_data = bpy.data.cameras.new("ProtagonistCamera")
camera_data.type = "ORTHO"
camera_data.ortho_scale = 8.4
camera = bpy.data.objects.new("ProtagonistCamera", camera_data)
ensure_collection("Review Rig").objects.link(camera)
camera.location = (0, -9.5, 6.8)
look_at(camera, (0, -0.15, 0.65))
bpy.context.scene.camera = camera

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.eevee.use_gtao = True
scene.eevee.gtao_factor = 1.4
scene.eevee.gtao_distance = 2.5
scene.eevee.use_soft_shadows = True
scene.render.resolution_x = 1500
scene.render.resolution_y = 820
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = RENDER_PATH
scene.view_settings.view_transform = "Standard"
scene.view_settings.look = "Medium High Contrast"
scene.view_settings.exposure = 0.35
scene["boxbox_review_purpose"] = "Protagonist V1: light skin, blond hair, tank top, clean-shaven"
scene["boxbox_source"] = "Kenney Mini Characters 2.0 character-male-b, CC0"

bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
bpy.ops.render.render(write_still=True)
print(f"BOXBOX_OUTPUT|blend={BLEND_PATH}")
print(f"BOXBOX_OUTPUT|render={RENDER_PATH}")
