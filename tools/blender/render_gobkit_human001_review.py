"""Render a neutral contact sheet for the approved Gobkit human001 review set.

Run with Blender, outside the product runtime:
blender --background --python tools/blender/render_gobkit_human001_review.py
"""

from pathlib import Path

import bpy
from mathutils import Vector


REPO_ROOT = Path(__file__).resolve().parents[2]
ASSET_ROOT = (
    REPO_ROOT
    / "BOXBOX"
    / "asset-library"
    / "review"
    / "Gobkit"
    / "human001"
    / "extracted"
)
OUTPUT_PATH = (
    REPO_ROOT
    / "BOXBOX"
    / "asset-library"
    / "reports"
    / "gobkit-human001-contact-sheet.png"
)

ASSETS = (
    ("Cashier", "Cashier.glb"),
    ("Food Courier", "FoodCourier.glb"),
    ("Grandma Shopper", "GrandmaShopper.glb"),
    ("Office Lady", "OfficeLady.glb"),
    ("Schoolkid", "Schoolkid.glb"),
    ("Stock Clerk", "StockClerk.glb"),
    ("Store Manager", "StoreManager.glb"),
)

POSITIONS = (
    (-5.4, 2.5),
    (-1.8, 2.5),
    (1.8, 2.5),
    (5.4, 2.5),
    (-3.6, -2.3),
    (0.0, -2.3),
    (3.6, -2.3),
)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def import_asset(path):
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.gltf(filepath=str(path))
    return [obj for obj in bpy.context.scene.objects if obj not in before]


def world_bounds(objects):
    corners = []
    for obj in objects:
        if obj.type != "MESH":
            continue
        corners.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    low = Vector((min(v.x for v in corners), min(v.y for v in corners), min(v.z for v in corners)))
    high = Vector((max(v.x for v in corners), max(v.y for v in corners), max(v.z for v in corners)))
    return low, high


def normalize_and_place(objects, x, y):
    roots = [obj for obj in objects if obj.parent is None]
    wrapper = bpy.data.objects.new(f"ReviewWrapper-{x}-{y}", None)
    bpy.context.collection.objects.link(wrapper)
    for root in roots:
        world_matrix = root.matrix_world.copy()
        root.parent = wrapper
        root.matrix_world = world_matrix
    low, high = world_bounds(objects)
    height = max(high.z - low.z, 0.001)
    scale = 2.65 / height
    wrapper.scale = (scale, scale, scale)
    bpy.context.view_layer.update()
    low, high = world_bounds(objects)
    center = (low + high) / 2
    offset = Vector((x, y, (high.z - low.z) / 2)) - center
    wrapper.location += offset
    bpy.context.view_layer.update()


def material(name, color, roughness=0.75):
    value = bpy.data.materials.new(name)
    value.diffuse_color = (*color, 1.0)
    value.use_nodes = True
    shader = next(node for node in value.node_tree.nodes if node.type == "BSDF_PRINCIPLED")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Roughness"].default_value = roughness
    return value


def add_platform(x, y, index):
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=1.35, depth=0.12, location=(x, y, -0.06))
    platform = bpy.context.object
    platform.data.materials.append(
        material(
            f"Platform{index}",
            (0.10, 0.17, 0.24) if index % 2 == 0 else (0.12, 0.21, 0.25),
        )
    )


def add_label(text, x, y):
    bpy.ops.object.text_add(location=(x, y - 1.62, 0.025))
    label = bpy.context.object
    label.data.body = text
    label.data.align_x = "CENTER"
    label.data.align_y = "CENTER"
    label.data.size = 0.36
    label.data.extrude = 0.004
    label.data.materials.append(material(f"Label-{text}", (0.80, 0.88, 0.92), 0.9))


def add_floor():
    bpy.ops.mesh.primitive_plane_add(size=30, location=(0, 0, -0.13))
    floor = bpy.context.object
    floor.data.materials.append(material("Floor", (0.025, 0.045, 0.065), 0.82))


def point_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def configure_scene():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.eevee.use_gtao = True
    scene.eevee.gtao_distance = 3
    scene.eevee.gtao_factor = 1.25
    scene.render.resolution_x = 1800
    scene.render.resolution_y = 1100
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(OUTPUT_PATH)
    scene.render.film_transparent = False
    scene.view_settings.look = "Medium High Contrast"
    scene.world.color = (0.018, 0.028, 0.045)

    bpy.ops.object.light_add(type="AREA", location=(-4.5, -4.0, 11.0))
    key = bpy.context.object
    key.data.energy = 1250
    key.data.size = 7.0
    point_at(key, (0, 0, 1))

    bpy.ops.object.light_add(type="AREA", location=(6.5, 2.0, 7.5))
    fill = bpy.context.object
    fill.data.energy = 800
    fill.data.size = 6.0
    fill.data.color = (0.55, 0.75, 1.0)
    point_at(fill, (0, 0, 1))

    bpy.ops.object.camera_add(location=(10.5, -15.5, 12.5))
    camera = bpy.context.object
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 15.0
    point_at(camera, (0, 0.1, 1.15))
    scene.camera = camera


def main():
    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    clear_scene()
    bpy.context.scene.frame_set(1)
    add_floor()
    for index, ((label, filename), (x, y)) in enumerate(zip(ASSETS, POSITIONS)):
        add_platform(x, y, index)
        objects = import_asset(ASSET_ROOT / filename)
        normalize_and_place(objects, x, y)
        add_label(label, x, y)
    configure_scene()
    bpy.ops.render.render(write_still=True)


main()
