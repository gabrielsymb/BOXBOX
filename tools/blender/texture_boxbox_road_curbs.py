"""Assign photographic concrete UVs to the raised kerbs on the road kit."""
from pathlib import Path

import bpy

ROADS = Path(__file__).resolve().parents[2] / "BOXBOX/boxbox/Assets/World/Roads"

for name in ("road-straight", "road-crossing", "road-crossroad"):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(ROADS / f"{name}.glb"))
    road = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
    mesh = road.data
    uvs = mesh.uv_layers.active.data
    kerb = len(mesh.materials)
    mesh.materials.append(bpy.data.materials.new("MeioFioConcreto"))
    width = 8 if name == "road-crossroad" else 4
    for face in mesh.polygons:
        points = [road.matrix_world @ mesh.vertices[index].co for index in face.vertices]
        if max(point.z for point in points) < 0.019:
            continue
        face.material_index = kerb
        normal = face.normal
        for index in face.loop_indices:
            vertex = road.matrix_world @ mesh.vertices[mesh.loops[index].vertex_index].co
            if abs(normal.z) > 0.5:
                uvs[index].uv = (vertex.x * width / 2, vertex.y * 4)
            elif abs(normal.x) > abs(normal.y):
                uvs[index].uv = (vertex.y * 4, vertex.z * 2)
            else:
                uvs[index].uv = (vertex.x * width / 2, vertex.z * 2)
    bpy.ops.export_scene.gltf(filepath=str(ROADS / "TexturedCurb" / f"{name}.glb"), export_format="GLB")
