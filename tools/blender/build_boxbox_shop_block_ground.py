"""Export the hand-laid shop block surfaces in Godot world metres."""
from pathlib import Path

import bpy

OUTPUT = Path(__file__).resolve().parents[2] / "BOXBOX/boxbox/Assets/World/StartingBlock"
LEVEL = 0.15


def slab(x0, x1, z0, z1):
    return [(x0, -z0, LEVEL), (x0, -z1, LEVEL), (x1, -z1, LEVEL), (x1, -z0, LEVEL)]


def kerb(x0, z0, x1, z1):
    return [(x0, -z0, LEVEL), (x1, -z1, LEVEL), (x1, -z1, 0), (x0, -z0, 0)]


def export(name, faces, metres_per_tile):
    mesh = bpy.data.meshes.new(name)
    vertices = [point for face in faces for point in face]
    mesh.from_pydata(vertices, [], [range(i, i + 4) for i in range(0, len(vertices), 4)])
    uv = mesh.uv_layers.new()
    for polygon in mesh.polygons:
        upright = abs(polygon.normal.z) < 0.5
        for loop in polygon.loop_indices:
            x, y, z = mesh.vertices[mesh.loops[loop].vertex_index].co
            u = y if upright and abs(polygon.normal.x) > 0.5 else x
            v = z if upright else y
            uv.data[loop].uv = (u / metres_per_tile, v / metres_per_tile)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.wm.obj_export(
        filepath=str(OUTPUT / f"{name}.obj"), export_selected_objects=True,
        forward_axis="NEGATIVE_Z", up_axis="Y", export_materials=False,
    )
    obj.select_set(False)


bpy.ops.wm.read_factory_settings(use_empty=True)
export("Passeio", [
    slab(6, 30, 6, 8), slab(6, 8, 8, 28), slab(28, 30, 8, 28),
    slab(6, 30, 28, 30),
    kerb(6, 6, 30, 6), kerb(30, 6, 30, 30),
    kerb(30, 30, 6, 30), kerb(6, 30, 6, 6),
], 2.8)
export("Patio", [
    slab(8, 18.5, 8, 15), slab(18.5, 20, 8, 28),
    slab(8, 18.5, 15, 17), slab(20, 28, 14, 16),
], 4.5)
export("Quintais", [slab(20, 28, 8, 14)], 5)
export("Canteiros", [
    slab(20, 28, 16, 28),
    slab(8, 18.5, 17, 28),
], 5)
