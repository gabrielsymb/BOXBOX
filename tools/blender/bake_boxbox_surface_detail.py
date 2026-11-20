"""Bake restrained CC0 surface detail into curated GLBs without losing their palette."""

from pathlib import Path
import sys

import bpy


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.materials, bpy.data.images, bpy.data.meshes):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def ensure_detail_uv(obj):
    mesh = obj.data
    original_name = mesh.uv_layers.active.name if mesh.uv_layers.active else "UVMap"
    if not mesh.uv_layers:
        mesh.uv_layers.new(name=original_name)
    detail = mesh.uv_layers.get("BOXBOX_DetailUV") or mesh.uv_layers.new(name="BOXBOX_DetailUV")
    mesh.uv_layers.active = detail
    detail.active_render = True

    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=1.15192, island_margin=0.03)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.select_set(False)
    return original_name


def bake_object(obj, detail_image, output_image_path):
    original_uv = ensure_detail_uv(obj)
    material = obj.data.materials[0] if obj.data.materials else bpy.data.materials.new("colormap")
    if not obj.data.materials:
        obj.data.materials.append(material)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links

    base_texture = next((node for node in nodes if node.type == "TEX_IMAGE" and node.image), None)
    if base_texture is None:
        raise RuntimeError(f"No palette texture found on {obj.name}")

    base_uv = nodes.new("ShaderNodeUVMap")
    base_uv.uv_map = original_uv
    links.new(base_uv.outputs["UV"], base_texture.inputs["Vector"])

    detail_uv = nodes.new("ShaderNodeUVMap")
    detail_uv.uv_map = "BOXBOX_DetailUV"
    detail_texture = nodes.new("ShaderNodeTexImage")
    detail_texture.image = detail_image
    detail_texture.extension = "REPEAT"
    links.new(detail_uv.outputs["UV"], detail_texture.inputs["Vector"])

    mix = nodes.new("ShaderNodeMixRGB")
    mix.blend_type = "OVERLAY"
    mix.inputs[0].default_value = 0.24
    links.new(base_texture.outputs["Color"], mix.inputs[1])
    links.new(detail_texture.outputs["Color"], mix.inputs[2])

    emission = nodes.new("ShaderNodeEmission")
    links.new(mix.outputs["Color"], emission.inputs["Color"])
    output = next(node for node in nodes if node.type == "OUTPUT_MATERIAL")
    for link in list(output.inputs["Surface"].links):
        links.remove(link)
    links.new(emission.outputs["Emission"], output.inputs["Surface"])

    baked = bpy.data.images.new(f"{obj.name}_surface", width=512, height=512, alpha=False)
    baked.filepath_raw = str(output_image_path)
    baked.file_format = "PNG"
    bake_target = nodes.new("ShaderNodeTexImage")
    bake_target.image = baked
    nodes.active = bake_target
    bake_target.select = True

    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.context.scene.render.engine = "CYCLES"
    bpy.context.scene.cycles.samples = 1
    bpy.context.scene.render.bake.margin = 8
    bpy.ops.object.bake(type="EMIT")
    baked.save()
    obj.select_set(False)

    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    baked_texture = nodes.new("ShaderNodeTexImage")
    baked_texture.image = baked
    uv_node = nodes.new("ShaderNodeUVMap")
    uv_node.uv_map = "BOXBOX_DetailUV"
    links.new(uv_node.outputs["UV"], baked_texture.inputs["Vector"])
    links.new(baked_texture.outputs["Color"], shader.inputs["Base Color"])
    shader.inputs["Roughness"].default_value = 0.82
    links.new(shader.outputs["BSDF"], output.inputs["Surface"])


def process(glb_path, detail_path):
    reset_scene()
    bpy.ops.import_scene.gltf(filepath=str(glb_path))
    detail_image = bpy.data.images.load(str(detail_path), check_existing=True)
    output_dir = glb_path.parent / "Textures" / "DetailBaked"
    output_dir.mkdir(parents=True, exist_ok=True)

    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    for index, obj in enumerate(mesh_objects):
        output_image = output_dir / f"{glb_path.stem}-{index}.png"
        bake_object(obj, detail_image, output_image)

    bpy.ops.export_scene.gltf(
        filepath=str(glb_path),
        export_format="GLB",
        export_apply=True,
        export_materials="EXPORT",
    )
    print(f"BOXBOX_DETAIL_BAKED|{glb_path}|meshes={len(mesh_objects)}")


def main():
    args = sys.argv[sys.argv.index("--") + 1 :]
    if len(args) < 2 or len(args) % 2:
        raise SystemExit("Use pairs: <glb> <detail-image> [...]")
    for index in range(0, len(args), 2):
        process(Path(args[index]).resolve(), Path(args[index + 1]).resolve())


if __name__ == "__main__":
    main()
