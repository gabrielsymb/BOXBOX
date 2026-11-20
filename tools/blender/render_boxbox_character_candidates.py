import bpy
import math
import os
from mathutils import Vector


ROOT = "/home/yakuzaa/Documentos/ChatGPT/GAME"
CHAR_DIR = os.path.join(ROOT, "BOXBOX/asset-library/review/Kenney/mini-characters-2.0/extracted/Models/GLB format")
FLOOR = os.path.join(ROOT, "BOXBOX/asset-library/review/Kenney/mini-shop-2.1/extracted/Models/GLB format/floor-wood.glb")
FONT = os.path.join(ROOT, "BOXBOX/asset-library/review/GoogleFonts/fredoka/original/Fredoka-wdth-wght.ttf")
OUT_DIR = os.path.join(ROOT, "BOXBOX/asset-library/reports")
BLEND_PATH = os.path.join(OUT_DIR, "boxbox-character-candidates.blend")
RENDER_PATH = os.path.join(OUT_DIR, "boxbox-character-candidates.png")

CANDIDATES = [
    ("Feminina A", "character-female-a.glb"),
    ("Feminina B", "character-female-b.glb"),
    ("Feminina C", "character-female-c.glb"),
    ("Feminina D", "character-female-d.glb"),
    ("Feminina E", "character-female-e.glb"),
    ("Feminina F", "character-female-f.glb"),
    ("Trabalhador", "character-worker.glb"),
    ("Masculino A", "character-male-a.glb"),
    ("Masculino B", "character-male-b.glb"),
    ("Masculino C", "character-male-c.glb"),
    ("Masculino D", "character-male-d.glb"),
    ("Masculino E", "character-male-e.glb"),
    ("Masculino F", "character-male-f.glb"),
    ("Masculino G", "character-male-g.glb"),
]


def collection(name):
    found = bpy.data.collections.get(name)
    if found is None:
        found = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(found)
    return found


def import_group(filepath, name, location, scale=1.0):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=filepath)
    imported = list(set(bpy.data.objects) - before)
    root = bpy.data.objects.new(name, None)
    collection("Candidates").objects.link(root)
    imported_set = set(imported)
    for obj in imported:
        if obj.parent not in imported_set:
            obj.parent = root
        for current in list(obj.users_collection):
            current.objects.unlink(obj)
        collection("Candidates").objects.link(obj)
    root.location = location
    root.scale = (scale, scale, scale)
    # Kenney's GLBs face the review camera on their native forward axis.
    root.rotation_euler[2] = 0
    return root


def add_label(text, location):
    curve = bpy.data.curves.new(text, type="FONT")
    curve.body = text
    curve.align_x = "CENTER"
    curve.align_y = "CENTER"
    curve.size = 0.22
    curve.extrude = 0.004
    curve.font = bpy.data.fonts.load(FONT)
    material = bpy.data.materials.get("LabelInk")
    if material is None:
        material = bpy.data.materials.new("LabelInk")
        material.diffuse_color = (0.96, 0.98, 1.0, 1.0)
    curve.materials.append(material)
    obj = bpy.data.objects.new(text, curve)
    collection("Review Labels - not game UI").objects.link(obj)
    obj.location = location
    obj.rotation_euler = (math.radians(78), 0, 0)


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
os.makedirs(OUT_DIR, exist_ok=True)

for index, (label, filename) in enumerate(CANDIDATES):
    row = index // 7
    col = index % 7
    x = (col - 3) * 1.65
    y = 1.7 - row * 3.4
    import_group(os.path.join(CHAR_DIR, filename), filename.removesuffix(".glb"), (x, y, 0.04), scale=1.5)
    add_label(label, (x, y - 0.98, 0.04))
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=FLOOR)
    imported = list(set(bpy.data.objects) - before)
    root = bpy.data.objects.new(f"Floor_{index}", None)
    collection("Imported Review Floor").objects.link(root)
    imported_set = set(imported)
    for obj in imported:
        if obj.parent not in imported_set:
            obj.parent = root
        for current in list(obj.users_collection):
            current.objects.unlink(obj)
        collection("Imported Review Floor").objects.link(obj)
    root.location = (x, y, 0)
    root.scale = (1.5, 1.5, 1.5)

world = bpy.context.scene.world or bpy.data.worlds.new("Candidate Review World")
bpy.context.scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.035, 0.052, 0.08, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.6

sun_data = bpy.data.lights.new("Sun", "SUN")
sun_data.energy = 2.5
sun_data.angle = math.radians(22)
sun = bpy.data.objects.new("Sun", sun_data)
collection("Review Rig").objects.link(sun)
sun.rotation_euler = (math.radians(35), math.radians(-20), math.radians(-30))

area_data = bpy.data.lights.new("Softbox", "AREA")
area_data.energy = 1000
area_data.size = 9
area = bpy.data.objects.new("Softbox", area_data)
collection("Review Rig").objects.link(area)
area.location = (0, -5, 9)
look_at(area, (0, 0, 0.5))

camera_data = bpy.data.cameras.new("CandidateCamera")
camera_data.type = "ORTHO"
camera_data.ortho_scale = 12.2
camera = bpy.data.objects.new("CandidateCamera", camera_data)
collection("Review Rig").objects.link(camera)
camera.location = (0, -11.5, 8.4)
look_at(camera, (0, 0.1, 0.5))
bpy.context.scene.camera = camera

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.eevee.use_gtao = True
scene.eevee.gtao_factor = 1.4
scene.eevee.gtao_distance = 2.5
scene.eevee.use_soft_shadows = True
scene.render.resolution_x = 1680
scene.render.resolution_y = 820
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = RENDER_PATH
scene.view_settings.view_transform = "Standard"
scene.view_settings.look = "Medium High Contrast"
scene.view_settings.exposure = 0.35
scene["boxbox_review_purpose"] = "Civil protagonist candidate sheet; labels are review-only and use approved Fredoka"

bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
bpy.ops.render.render(write_still=True)
print(f"BOXBOX_OUTPUT|blend={BLEND_PATH}")
print(f"BOXBOX_OUTPUT|render={RENDER_PATH}")
