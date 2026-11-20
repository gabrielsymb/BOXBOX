import bpy
import math
import os
from mathutils import Vector


ROOT = "/home/yakuzaa/Documentos/ChatGPT/GAME"
OUT_DIR = os.path.join(ROOT, "BOXBOX/asset-library/reports")
BLEND_PATH = os.path.join(OUT_DIR, "boxbox-first-block-curation.blend")
RENDER_PATH = os.path.join(OUT_DIR, "boxbox-first-block-curation.png")

PATHS = {
    "female_a": "BOXBOX/asset-library/review/Kenney/mini-characters-2.0/extracted/Models/GLB format/character-female-a.glb",
    "female_b": "BOXBOX/asset-library/review/Kenney/mini-characters-2.0/extracted/Models/GLB format/character-female-b.glb",
    "male_a": "BOXBOX/asset-library/review/Kenney/mini-characters-2.0/extracted/Models/GLB format/character-male-a.glb",
    "floor_wood": "BOXBOX/asset-library/review/Kenney/mini-shop-2.1/extracted/Models/GLB format/floor-wood.glb",
    "wall": "BOXBOX/asset-library/review/Kenney/mini-shop-2.1/extracted/Models/GLB format/wall.glb",
    "wall_corner": "BOXBOX/asset-library/review/Kenney/mini-shop-2.1/extracted/Models/GLB format/wall-corner.glb",
    "wall_window": "BOXBOX/asset-library/review/Kenney/mini-shop-2.1/extracted/Models/GLB format/wall-window.glb",
    "cash_register": "BOXBOX/asset-library/review/Kenney/mini-shop-2.1/extracted/Models/GLB format/cash-register.glb",
    "shelf_boxes": "BOXBOX/asset-library/review/Kenney/mini-shop-2.1/extracted/Models/GLB format/shelf-boxes.glb",
    "display_fruit": "BOXBOX/asset-library/review/Kenney/mini-shop-2.1/extracted/Models/GLB format/display-fruit.glb",
    "freezer": "BOXBOX/asset-library/review/Kenney/mini-shop-2.1/extracted/Models/GLB format/freezer.glb",
    "road_straight": "BOXBOX/asset-library/review/Kenney/city-kit-roads-2.2/extracted/Models/GLB format/road-straight.glb",
    "road_crossing": "BOXBOX/asset-library/review/Kenney/city-kit-roads-2.2/extracted/Models/GLB format/road-crossing.glb",
    "commercial": "BOXBOX/asset-library/review/Kenney/city-kit-commercial-2.1/extracted/Models/GLB format/building-a.glb",
    "suburban": "BOXBOX/asset-library/review/Kenney/city-kit-suburban-2.0/extracted/Models/GLB format/building-type-a.glb",
    "chair": "BOXBOX/asset-library/review/Kenney/furniture-kit-2.0/extracted/Models/GLTF format/chair.glb",
    "desk": "BOXBOX/asset-library/review/Kenney/furniture-kit-2.0/extracted/Models/GLTF format/desk.glb",
    "box": "BOXBOX/asset-library/review/Kenney/furniture-kit-2.0/extracted/Models/GLTF format/cardboardBoxClosed.glb",
    "apple": "BOXBOX/asset-library/review/Kenney/food-kit-2.0/extracted/Models/GLB format/apple.glb",
    "bread": "BOXBOX/asset-library/review/Kenney/food-kit-2.0/extracted/Models/GLB format/bread.glb",
    "can": "BOXBOX/asset-library/review/Kenney/food-kit-2.0/extracted/Models/GLB format/can.glb",
    "delivery": "BOXBOX/asset-library/review/Kenney/car-kit-3.1/extracted/Models/GLB format/delivery.glb",
    "tree": "BOXBOX/asset-library/review/Kenney/mini-forest-1.0/extracted/Models/GLB format/tree.glb",
    "plant": "BOXBOX/asset-library/review/Kenney/mini-forest-1.0/extracted/Models/GLB format/plant.glb",
}


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.armatures, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def ensure_collection(name):
    collection = bpy.data.collections.get(name)
    if collection is None:
        collection = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(collection)
    return collection


def import_asset(key, name, location=(0, 0, 0), rotation_z=0, scale=1.0, collection="Assets"):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=os.path.join(ROOT, PATHS[key]))
    imported = list(set(bpy.data.objects) - before)
    root = bpy.data.objects.new(name, None)
    target_collection = ensure_collection(collection)
    target_collection.objects.link(root)

    imported_set = set(imported)
    for obj in imported:
        if obj.parent not in imported_set:
            obj.parent = root
        for old_collection in list(obj.users_collection):
            old_collection.objects.unlink(obj)
        target_collection.objects.link(obj)

    root.location = location
    root.rotation_euler[2] = math.radians(rotation_z)
    root.scale = (scale, scale, scale)
    return root


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


os.makedirs(OUT_DIR, exist_ok=True)
reset_scene()

# The storefront is built only from source assets. Camera, lighting and spacing are review apparatus.
for x in range(-3, 4):
    for y in range(0, 5):
        import_asset("floor_wood", f"ShopFloor_{x}_{y}", (x, y, 0), collection="01_BOXBOX_Shop")

for x in range(-3, 4):
    import_asset("wall", f"BackWall_{x}", (x, 4.5, 0.02), collection="01_BOXBOX_Shop")
for y in range(0, 4):
    import_asset("wall_window" if y in (1, 2) else "wall", f"LeftWall_{y}", (-3.5, y + 0.5, 0.02), rotation_z=90, collection="01_BOXBOX_Shop")
for y in range(1, 4):
    import_asset("wall_window" if y == 2 else "wall", f"RightWall_{y}", (3.5, y + 0.5, 0.02), rotation_z=90, collection="01_BOXBOX_Shop")

import_asset("cash_register", "Checkout", (2.15, 2.65, 0.03), rotation_z=-90, scale=0.9, collection="02_Store_Equipment")
import_asset("shelf_boxes", "ShelfLeft", (-1.9, 3.35, 0.03), rotation_z=180, scale=0.9, collection="02_Store_Equipment")
import_asset("shelf_boxes", "ShelfCenter", (0.05, 3.35, 0.03), rotation_z=180, scale=0.9, collection="02_Store_Equipment")
import_asset("display_fruit", "FruitDisplay", (-1.85, 1.25, 0.03), rotation_z=30, scale=0.9, collection="02_Store_Equipment")
import_asset("freezer", "Freezer", (1.15, 3.55, 0.03), rotation_z=180, scale=0.9, collection="02_Store_Equipment")
import_asset("desk", "BackOfficeDesk", (2.35, 3.65, 0.03), rotation_z=180, scale=0.85, collection="02_Store_Equipment")
import_asset("chair", "BackOfficeChair", (2.25, 3.15, 0.03), rotation_z=10, scale=1.1, collection="02_Store_Equipment")
import_asset("box", "DeliveryBoxA", (-2.55, 3.9, 0.03), rotation_z=8, scale=0.9, collection="03_Stock_And_Food")
import_asset("box", "DeliveryBoxB", (-2.15, 3.92, 0.03), rotation_z=-12, scale=0.7, collection="03_Stock_And_Food")
import_asset("bread", "BreadSample", (-1.2, 1.1, 0.55), rotation_z=20, scale=0.7, collection="03_Stock_And_Food")
import_asset("apple", "AppleSample", (-1.65, 1.1, 0.55), scale=0.65, collection="03_Stock_And_Food")
import_asset("can", "CanSample", (-2.05, 1.1, 0.55), scale=0.55, collection="03_Stock_And_Food")

# Provisional protagonist candidate at the threshold; the other two are persistent NPC examples.
import_asset("female_a", "PROVISIONAL_Player_Candidate", (0.4, 1.35, 0.03), rotation_z=155, scale=1.35, collection="04_Characters")
import_asset("male_a", "Persistent_NPC_Customer", (1.55, 1.55, 0.03), rotation_z=-25, scale=1.35, collection="04_Characters")
import_asset("female_b", "Persistent_NPC_Neighbour", (-1.2, -0.35, 0.04), rotation_z=20, scale=1.35, collection="04_Characters")

# A street strip scaled independently because the Roads kit uses one-unit tiles.
for x in (-4, 0, 4):
    key = "road_crossing" if x == 0 else "road_straight"
    import_asset(key, f"Road_{x}", (x, -3.0, 0), rotation_z=90, scale=4.0, collection="05_Street")
import_asset("delivery", "DeliveryVan", (-3.3, -2.7, 0.08), rotation_z=90, scale=0.72, collection="06_Vehicles")

# Background context: not part of the open storefront interior, but useful for kit compatibility.
import_asset("commercial", "CommercialNeighbour", (-6.0, 3.7, 0), rotation_z=-8, scale=3.15, collection="07_Neighbourhood")
import_asset("suburban", "SuburbanNeighbour", (6.2, 3.65, 0), rotation_z=8, scale=3.0, collection="07_Neighbourhood")
for name, loc, rot, scale in (
    ("TreeLeft", (-5.0, 0.2, 0.02), 0, 1.55),
    ("TreeRight", (5.0, 0.25, 0.02), 15, 1.55),
    ("TreeBack", (4.8, 4.9, 0.02), -10, 1.25),
):
    import_asset("tree", name, loc, rotation_z=rot, scale=scale, collection="08_Vegetation")
for index, loc in enumerate(((-3.1, -0.15, 0.02), (3.0, -0.1, 0.02), (4.1, 1.0, 0.02))):
    import_asset("plant", f"Plant_{index}", loc, rotation_z=index * 37, scale=1.2, collection="08_Vegetation")

# Soft isometric review light; no generated meshes or replacement art.
world = bpy.context.scene.world or bpy.data.worlds.new("BOXBOX Review World")
bpy.context.scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.055, 0.075, 0.11, 1.0)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.65

sun_data = bpy.data.lights.new("Sun", type="SUN")
sun_data.energy = 2.2
sun_data.angle = math.radians(18)
sun = bpy.data.objects.new("Sun", sun_data)
ensure_collection("09_Review_Rig").objects.link(sun)
sun.rotation_euler = (math.radians(28), math.radians(-18), math.radians(-32))

area_data = bpy.data.lights.new("Softbox", type="AREA")
area_data.energy = 850
area_data.size = 8
area = bpy.data.objects.new("Softbox", area_data)
ensure_collection("09_Review_Rig").objects.link(area)
area.location = (2.5, -3.0, 10.0)
look_at(area, (0, 1.5, 0))

camera_data = bpy.data.cameras.new("IsometricCamera")
camera = bpy.data.objects.new("IsometricCamera", camera_data)
ensure_collection("09_Review_Rig").objects.link(camera)
camera.location = (12.5, -15.5, 13.0)
camera_data.type = "ORTHO"
camera_data.ortho_scale = 16.5
look_at(camera, (0, 1.35, 0.7))
bpy.context.scene.camera = camera

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.eevee.use_gtao = True
scene.eevee.gtao_distance = 3
scene.eevee.gtao_factor = 1.35
scene.eevee.use_soft_shadows = True
scene.render.resolution_x = 1440
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = RENDER_PATH
scene.render.film_transparent = False
scene.view_settings.view_transform = "Standard"
scene.view_settings.look = "Medium High Contrast"
scene.view_settings.exposure = 0.35
scene.view_settings.gamma = 1.0

# Make the curation file easy to explore by collection and preserve source-file provenance.
scene["boxbox_review_purpose"] = "Asset-only first-block curation; not a final Godot scene"
scene["boxbox_protagonist_status"] = "character-female-a is provisional and awaits user approval"
scene["boxbox_scale_note"] = "Per-kit scale adapters are intentional; source GLBs remain untouched"

bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
bpy.ops.render.render(write_still=True)
print(f"BOXBOX_OUTPUT|blend={BLEND_PATH}")
print(f"BOXBOX_OUTPUT|render={RENDER_PATH}")
