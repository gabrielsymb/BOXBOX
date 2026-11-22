"""Add a metre-scaled bark material slot to the City Pack tree."""
from math import atan2, tau
from pathlib import Path

import bpy

BLOCK = Path(__file__).resolve().parents[2] / "BOXBOX/boxbox/Assets/World/StartingBlock"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(BLOCK / "city-pack-tree.glb"))
tree = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
mesh = tree.data
palette = next(node.image for node in mesh.materials[0].node_tree.nodes if node.type == "TEX_IMAGE")
width, height = palette.size
pixels = list(palette.pixels)
uvs = mesh.uv_layers.active.data
bark_index = len(mesh.materials)
mesh.materials.append(bpy.data.materials.new("CascaDeArvore"))

for face in mesh.polygons:
    u, v = uvs[face.loop_start].uv
    offset = 4 * ((int(u * width) % width) + width * (int(v * height) % height))
    red, green, _ = pixels[offset:offset + 3]
    if red <= green:
        continue
    face.material_index = bark_index
    points = [tree.matrix_world @ mesh.vertices[mesh.loops[index].vertex_index].co
              for index in face.loop_indices]
    angles = [atan2(point.y, point.x) / tau for point in points]
    if max(angles) - min(angles) > 0.5:
        angles = [angle + 1 if angle < 0 else angle for angle in angles]
    for index, point, angle in zip(face.loop_indices, points, angles):
        uvs[index].uv = (angle, point.z * 0.008 / 1.4)

bpy.ops.export_scene.gltf(filepath=str(BLOCK / "arvore-casca.glb"), export_format="GLB")
