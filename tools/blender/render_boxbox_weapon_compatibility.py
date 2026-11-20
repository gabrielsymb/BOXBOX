import bpy
import math
import os
from mathutils import Matrix, Quaternion, Vector


ROOT = "/home/yakuzaa/Documentos/ChatGPT/GAME"
CHAR_DIR = os.path.join(ROOT, "BOXBOX/asset-library/review/Kenney/mini-characters-2.0/extracted/Models/GLB format")
FLOOR = os.path.join(ROOT, "BOXBOX/asset-library/review/Kenney/mini-shop-2.1/extracted/Models/GLB format/floor-wood.glb")
TARGET = os.path.join(ROOT, "BOXBOX/asset-library/review/Kenney/mini-forest-1.0/extracted/Models/GLB format/target.glb")
WEAPON = os.path.join(CHAR_DIR, "weapon-shooter.glb")
FONT = os.path.join(ROOT, "BOXBOX/asset-library/review/GoogleFonts/fredoka/original/Fredoka-wdth-wght.ttf")
OUT_DIR = os.path.join(ROOT, "BOXBOX/asset-library/reports")
BLEND_PATH = os.path.join(OUT_DIR, "boxbox-weapon-compatibility.blend")
RENDER_PATH = os.path.join(OUT_DIR, "boxbox-weapon-compatibility.png")

TESTS = [
    ("DUAS MÃOS", "character-female-a.glb", "holding-both-shoot", "arm-right", -2.6),
    ("MÃO DIREITA", "character-male-a.glb", "holding-right-shoot", "arm-right", 0.0),
    ("MÃO ESQUERDA", "character-female-b.glb", "holding-left-shoot", "arm-left", 2.6),
]


def ensure_collection(name):
    found = bpy.data.collections.get(name)
    if found is None:
        found = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(found)
    return found


def imported_objects(filepath):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=filepath)
    return list(set(bpy.data.objects) - before)


def move_to_collection(objects, name):
    target = ensure_collection(name)
    for obj in objects:
        for current in list(obj.users_collection):
            current.objects.unlink(obj)
        target.objects.link(obj)


def add_label(text, location):
    curve = bpy.data.curves.new(text, type="FONT")
    curve.body = text
    curve.align_x = "CENTER"
    curve.align_y = "CENTER"
    curve.size = 0.24
    curve.extrude = 0.004
    curve.font = bpy.data.fonts.load(FONT)
    material = bpy.data.materials.get("LabelInk")
    if material is None:
        material = bpy.data.materials.new("LabelInk")
        material.diffuse_color = (0.98, 0.99, 1.0, 1.0)
    curve.materials.append(material)
    obj = bpy.data.objects.new(text, curve)
    ensure_collection("Review Labels - not game UI").objects.link(obj)
    obj.location = location
    obj.rotation_euler = (math.radians(78), 0, 0)


def add_floor(x):
    objects = imported_objects(FLOOR)
    move_to_collection(objects, "Imported Review Floor")
    roots = [obj for obj in objects if obj.parent not in set(objects)]
    holder = bpy.data.objects.new(f"Floor_{x}", None)
    ensure_collection("Imported Review Floor").objects.link(holder)
    for root in roots:
        root.parent = holder
    holder.location = (x, 0, 0)
    holder.scale = (2.2, 2.2, 2.2)


def add_character(label, filename, action_prefix, weapon_bone, x):
    objects = imported_objects(os.path.join(CHAR_DIR, filename))
    move_to_collection(objects, "Characters")
    armature = next(obj for obj in objects if obj.type == "ARMATURE")
    suffix = filename.removesuffix(".glb")
    action = next(action for action in bpy.data.actions if action.name == f"{action_prefix}_{suffix}")
    armature.animation_data_create()
    armature.animation_data.action = action

    roots = [obj for obj in objects if obj.parent not in set(objects)]
    holder = bpy.data.objects.new(f"Character_{suffix}", None)
    ensure_collection("Characters").objects.link(holder)
    for root in roots:
        root.parent = holder
    holder.location = (x, 0, 0.04)
    holder.scale = (2.1, 2.1, 2.1)

    bpy.context.scene.frame_set(2)
    bpy.context.view_layer.update()

    weapon_objects = imported_objects(WEAPON)
    move_to_collection(weapon_objects, "Weapons")
    weapon = next(obj for obj in weapon_objects if obj.type == "MESH")
    weapon.name = f"weapon-shooter_{suffix}"
    weapon.parent = armature
    weapon.parent_type = "BONE"
    weapon.parent_bone = weapon_bone
    # This compact rig has no hand bones. The visible grip sits near the arm
    # bone head; a dedicated socket will be authored here for the game export.
    hand_world = armature.matrix_world @ armature.pose.bones[weapon_bone].head
    hand_world.y -= 0.16
    hand_world.z += 0.04
    weapon.matrix_world = Matrix.LocRotScale(
        hand_world,
        Quaternion((1, 0, 0, 0)),
        Vector((2.1, 2.1, 2.1)),
    )

    add_label(label, (x, -1.0, 0.04))
    return holder


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
os.makedirs(OUT_DIR, exist_ok=True)

for label, filename, action, bone, x in TESTS:
    add_floor(x)
    add_character(label, filename, action, bone, x)

target_objects = imported_objects(TARGET)
move_to_collection(target_objects, "Target")
target_root = bpy.data.objects.new("ImportedTarget", None)
ensure_collection("Target").objects.link(target_root)
for obj in [obj for obj in target_objects if obj.parent not in set(target_objects)]:
    obj.parent = target_root
target_root.location = (0, -3.0, 0)
target_root.scale = (1.4, 1.4, 1.4)
target_root.rotation_euler[2] = math.radians(180)

bpy.context.scene.frame_set(2)

world = bpy.context.scene.world or bpy.data.worlds.new("Weapon Review World")
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

camera_data = bpy.data.cameras.new("WeaponTestCamera")
camera_data.type = "ORTHO"
camera_data.ortho_scale = 8.6
camera = bpy.data.objects.new("WeaponTestCamera", camera_data)
ensure_collection("Review Rig").objects.link(camera)
camera.location = (0, -9.5, 6.8)
look_at(camera, (0, -0.3, 0.6))
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
scene["boxbox_review_purpose"] = "Validate existing Kenney shooting actions and weapon attachment before editing protagonist"

bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
bpy.ops.render.render(write_still=True)
print(f"BOXBOX_OUTPUT|blend={BLEND_PATH}")
print(f"BOXBOX_OUTPUT|render={RENDER_PATH}")
