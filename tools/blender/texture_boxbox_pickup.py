"""Prepare vehicle material slots and UVs while preserving original wheel pivots."""
from math import atan2, tau
from pathlib import Path

import bpy

VEHICLES = Path(__file__).resolve().parents[2] / "BOXBOX/boxbox/Assets/World/PlayerVehicles"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(VEHICLES / "player-pickup.glb"))

for obj in bpy.context.scene.objects:
    if obj.type != "MESH":
        continue
    mesh = obj.data
    wheel = obj.name != "body"
    colors = mesh.color_attributes[0]
    uv = mesh.uv_layers.active or mesh.uv_layers.new(name="PickupUV")
    slots = {}
    for face in mesh.polygons:
        color_index = face.loop_start if colors.domain == "CORNER" else face.vertices[0]
        red, green, blue = colors.data[color_index].color[:3]
        center = obj.matrix_world @ face.center
        if wheel:
            name = "PickupRubber" if max(red, green, blue) < 0.1 else "PickupSteel"
        elif red > 0.75 and green > 0.75 and abs(center.x) > 0.35 and center.y < -2.25:
            name = "Headlight_Mat"
        elif red > 0.5 and green < 0.10 and center.y > 2.05:
            name = "Taillight_Mat"
        elif center.z > 1.19 and 0.11 < red < 0.19 and green > 0.12 and blue > green:
            name = "PickupGlass"
        elif (red > 0.65 and 0.43 < green < 0.70) or (center.z > 1.18 and red > 1.7 * green and red < 0.23):
            name = "PickupPaint"
        elif (0.50 < red < 0.60 and blue > red) or (0.22 < red < 0.26 and 0.09 < green < 0.13) or (
            max(red, green, blue) < 0.075 and abs(center.x) > 0.96
            and abs(center.y) < 2.25 and 0.86 < center.z < 1.12
        ):
            name = "PickupSteel"
        elif center.y > 0.60 and 0.53 < red < 0.60 and 0.44 < green < 0.53:
            name = "PickupBed"
        else:
            continue

        if name not in slots:
            material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
            slots[name] = len(mesh.materials)
            mesh.materials.append(material)
        face.material_index = slots[name]
        points = [mesh.vertices[mesh.loops[index].vertex_index].co for index in face.loop_indices]
        normal = face.normal
        if wheel and name == "PickupRubber" and abs(normal.x) < 0.55:
            angles = [atan2(point.z, point.y) / tau for point in points]
            if max(angles) - min(angles) > 0.5:
                angles = [value + 1 if value < 0 else value for value in angles]
            mapped = [(point.x * 2.6, angle * 4) for point, angle in zip(points, angles)]
        else:
            axis = max(range(3), key=lambda idx: abs(normal[idx]))
            axes = [idx for idx in range(3) if idx != axis]
            scale = 1.8 if wheel else 0.85
            mapped = [(point[axes[0]] * scale, point[axes[1]] * scale) for point in points]
        for loop, coords in zip(face.loop_indices, mapped):
            uv.data[loop].uv = coords

bpy.ops.export_scene.gltf(filepath=str(VEHICLES / "player-pickup-textured.glb"), export_format="GLB")
