"""Blender batch utility: repair imported GLB road normals without changing layout/materials."""

import argparse
import os
import sys

import bmesh
import bpy


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def repair_mesh(mesh):
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.dissolve_degenerate(bm, dist=0.000001, edges=list(bm.edges))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(mesh)
    bm.free()
    for polygon in mesh.polygons:
        polygon.use_smooth = False
    mesh.validate(clean_customdata=True)
    mesh.update(calc_edges=True)


def process(source, destination):
    clear_scene()
    bpy.ops.import_scene.gltf(filepath=source)
    for obj in bpy.context.scene.objects:
        if obj.type == "MESH":
            repair_mesh(obj.data)
    bpy.ops.export_scene.gltf(
        filepath=destination,
        export_format="GLB",
        export_normals=True,
        export_tangents=False,
        export_materials="EXPORT",
        export_animations=False,
        export_yup=True,
    )


script_args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
parser = argparse.ArgumentParser()
parser.add_argument("--source-dir", required=True)
parser.add_argument("--output-dir", required=True)
args = parser.parse_args(script_args)
os.makedirs(args.output_dir, exist_ok=True)

for filename in sorted(os.listdir(args.source_dir)):
    if not filename.endswith(".glb"):
        continue
    process(
        os.path.join(args.source_dir, filename),
        os.path.join(args.output_dir, filename),
    )
    print(f"REPAIRED {filename}")
