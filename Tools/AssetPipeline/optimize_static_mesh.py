"""Create a lightweight FBX derivative while preserving the source asset.

Usage (Blender 2.93+):
  blender --background --python optimize_static_mesh.py -- input.fbx output.fbx 20000
"""

import os
import sys

import bpy


LOG_PREFIX = "[GG_MESH_OPTIMIZER]"


def parse_arguments():
    if "--" not in sys.argv:
        raise ValueError("Expected input FBX, output FBX, and target triangle count after --.")

    arguments = sys.argv[sys.argv.index("--") + 1 :]
    if len(arguments) != 3:
        raise ValueError("Usage: -- input.fbx output.fbx target_triangles")

    input_path = os.path.abspath(arguments[0])
    output_path = os.path.abspath(arguments[1])
    target_triangles = int(arguments[2])
    if not os.path.isfile(input_path):
        raise FileNotFoundError(input_path)
    if target_triangles < 100:
        raise ValueError("Target triangle count must be at least 100.")

    return input_path, output_path, target_triangles


def triangle_count(mesh_object):
    mesh_object.data.calc_loop_triangles()
    return len(mesh_object.data.loop_triangles)


def optimize(input_path, output_path, target_triangles):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=input_path, use_anim=False)

    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not mesh_objects:
        raise RuntimeError("The source FBX contains no mesh objects.")

    source_triangles = sum(triangle_count(obj) for obj in mesh_objects)
    ratio = min(1.0, float(target_triangles) / float(source_triangles))
    print(
        f"{LOG_PREFIX} source={source_triangles:,} target={target_triangles:,} "
        f"ratio={ratio:.6f} meshes={len(mesh_objects)}"
    )

    if ratio < 0.9999:
        for mesh_object in mesh_objects:
            bpy.context.view_layer.objects.active = mesh_object
            mesh_object.select_set(True)
            modifier = mesh_object.modifiers.new(name="FirstBloom_WebGL_Decimate", type="DECIMATE")
            modifier.decimate_type = "COLLAPSE"
            modifier.ratio = ratio
            modifier.use_collapse_triangulate = True
            bpy.ops.object.modifier_apply(modifier=modifier.name)
            mesh_object.select_set(False)

    for obj in bpy.context.scene.objects:
        obj.select_set(obj.type == "MESH")

    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=output_path,
        use_selection=True,
        object_types={"MESH"},
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        use_tspace=True,
        use_custom_props=False,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        axis_forward="-Z",
        axis_up="Y",
    )

    result_triangles = sum(triangle_count(obj) for obj in mesh_objects)
    output_megabytes = os.path.getsize(output_path) / (1024.0 * 1024.0)
    print(
        f"{LOG_PREFIX} result={result_triangles:,} fileMB={output_megabytes:.2f} "
        f"output={output_path}"
    )


if __name__ == "__main__":
    optimize(*parse_arguments())
