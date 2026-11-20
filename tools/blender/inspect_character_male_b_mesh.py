import bpy
import os
from collections import Counter, defaultdict
from mathutils import Vector


ROOT = "/home/yakuzaa/Documentos/ChatGPT/GAME"
PATH = os.path.join(ROOT, "BOXBOX/asset-library/review/Kenney/mini-characters-2.0/extracted/Models/GLB format/character-male-b.glb")

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=PATH)

for obj in bpy.context.scene.objects:
    if obj.type != "MESH":
        continue
    print(f"BOXBOX_MESH|{obj.name}|verts={len(obj.data.vertices)}|polys={len(obj.data.polygons)}|materials={len(obj.data.materials)}")
    for index, material in enumerate(obj.data.materials):
        print(f"BOXBOX_MATERIAL|{obj.name}|{index}|{material.name}")
        if material.use_nodes:
            for node in material.node_tree.nodes:
                if node.type == "TEX_IMAGE" and node.image:
                    print(f"BOXBOX_IMAGE|{obj.name}|{node.image.name}|size={tuple(node.image.size)}|path={node.image.filepath}")
    for attr in obj.data.attributes:
        print(f"BOXBOX_ATTRIBUTE|{obj.name}|{attr.name}|domain={attr.domain}|type={attr.data_type}")
    groups = [(group.index, group.name) for group in obj.vertex_groups]
    print(f"BOXBOX_GROUPS|{obj.name}|{groups}")

    image = next(
        node.image
        for material in obj.data.materials
        if material.use_nodes
        for node in material.node_tree.nodes
        if node.type == "TEX_IMAGE" and node.image
    )
    width, height = image.size
    pixels = list(image.pixels)
    uv_layer = obj.data.uv_layers.active.data
    group_names = {group.index: group.name for group in obj.vertex_groups}
    colors_by_group = defaultdict(Counter)
    for polygon in obj.data.polygons:
        weight_totals = Counter()
        for vertex_index in polygon.vertices:
            for membership in obj.data.vertices[vertex_index].groups:
                weight_totals[membership.group] += membership.weight
        dominant_group = group_names[weight_totals.most_common(1)[0][0]] if weight_totals else "none"
        uv = sum((uv_layer[loop].uv for loop in polygon.loop_indices), Vector((0, 0))) / len(polygon.loop_indices)
        px = min(width - 1, max(0, int(uv.x * width)))
        py = min(height - 1, max(0, int(uv.y * height)))
        offset = 4 * (py * width + px)
        rgba = tuple(round(pixels[offset + channel], 3) for channel in range(4))
        colors_by_group[dominant_group][rgba] += 1
    for group_name, counts in colors_by_group.items():
        print(f"BOXBOX_COLORS|{obj.name}|{group_name}|{counts.most_common()}")

    if obj.name.startswith("head-mesh"):
        adjacency = {vertex.index: set() for vertex in obj.data.vertices}
        for edge in obj.data.edges:
            a, b = edge.vertices
            adjacency[a].add(b)
            adjacency[b].add(a)
        unseen = set(adjacency)
        component_index = 0
        while unseen:
            seed = unseen.pop()
            stack = [seed]
            component = {seed}
            while stack:
                current = stack.pop()
                for neighbour in adjacency[current]:
                    if neighbour in unseen:
                        unseen.remove(neighbour)
                        component.add(neighbour)
                        stack.append(neighbour)
            coordinates = [obj.data.vertices[index].co for index in component]
            low = tuple(round(min(point[axis] for point in coordinates), 4) for axis in range(3))
            high = tuple(round(max(point[axis] for point in coordinates), 4) for axis in range(3))
            polygons = [poly for poly in obj.data.polygons if set(poly.vertices).issubset(component)]
            print(f"BOXBOX_COMPONENT|{component_index}|verts={len(component)}|polys={len(polygons)}|low={low}|high={high}")
            component_index += 1
