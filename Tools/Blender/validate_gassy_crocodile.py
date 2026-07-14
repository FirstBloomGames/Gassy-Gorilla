import math
import os
import sys

import bpy


EXPECTED_ACTIONS = {
    "Idle_Submerged": (1, 61),
    "Lunge_Snap": (1, 24),
    "Settle_Submerge": (1, 36),
}


def fail(message):
    print("GG_CROCODILE_VALIDATION_ERROR: {}".format(message))
    raise RuntimeError(message)


def evaluated_bounds(scene, mesh_object, frame):
    scene.frame_set(frame)
    bpy.context.view_layer.update()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = mesh_object.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()

    try:
        world_points = [evaluated.matrix_world @ vertex.co for vertex in mesh.vertices]
        minimum = [min(point[index] for point in world_points) for index in range(3)]
        maximum = [max(point[index] for point in world_points) for index in range(3)]
        return tuple(maximum[index] - minimum[index] for index in range(3))
    finally:
        evaluated.to_mesh_clear()


def main():
    args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    blend_path = os.path.abspath(args[0]) if args else bpy.data.filepath
    if not blend_path or not os.path.isfile(blend_path):
        fail("Blend file not found: {}".format(blend_path))

    bpy.ops.wm.open_mainfile(filepath=blend_path)

    scene = bpy.context.scene
    mesh_objects = [obj for obj in scene.objects if obj.type == "MESH" and obj.name == "GG_Crocodile_Mesh"]
    armatures = [obj for obj in scene.objects if obj.type == "ARMATURE" and obj.name == "GG_Crocodile_Rig"]
    if len(mesh_objects) != 1 or len(armatures) != 1:
        fail("Expected one crocodile mesh and one crocodile armature")

    mesh_object = mesh_objects[0]
    armature = armatures[0]
    action_names = {action.name for action in bpy.data.actions}
    if action_names != set(EXPECTED_ACTIONS):
        fail("Action names were {}".format(", ".join(sorted(action_names))))

    if len(armature.data.bones) != 11:
        fail("Expected 11 bones, found {}".format(len(armature.data.bones)))

    triangles = sum(len(polygon.vertices) - 2 for polygon in mesh_object.data.polygons)
    if triangles > 7000:
        fail("Triangle budget exceeded: {}".format(triangles))

    if len(mesh_object.material_slots) != 1:
        fail("Expected one atlas-backed material slot, found {}".format(len(mesh_object.material_slots)))

    if any(abs(value) > 0.0001 for value in armature.location):
        fail("Armature object contains root motion: {}".format(tuple(armature.location)))

    if armature.animation_data is None:
        armature.animation_data_create()

    for name, expected_range in EXPECTED_ACTIONS.items():
        action = bpy.data.actions[name]
        actual_range = tuple(int(round(value)) for value in action.frame_range)
        if actual_range != expected_range:
            fail("{} frame range was {}, expected {}".format(name, actual_range, expected_range))

        armature.animation_data.action = action
        sampled_frames = sorted({expected_range[0], int(sum(expected_range) * 0.5), expected_range[1]})
        dimensions = [evaluated_bounds(scene, mesh_object, frame) for frame in sampled_frames]
        for frame, dimension in zip(sampled_frames, dimensions):
            if any(not math.isfinite(value) or value < 0.25 for value in dimension):
                fail("{} collapses at frame {}: {}".format(name, frame, dimension))

        formatted = ", ".join(
            "{}=({:.2f},{:.2f},{:.2f})".format(frame, *dimension)
            for frame, dimension in zip(sampled_frames, dimensions)
        )
        print("GG_CROCODILE_ACTION_OK: {} {}".format(name, formatted))

    armature.animation_data.action = None
    print("GG_CROCODILE_VALIDATION_OK")
    print("Triangles: {}".format(triangles))
    print("Bones: {}".format(len(armature.data.bones)))
    print("Materials: {}".format(len(mesh_object.material_slots)))


if __name__ == "__main__":
    main()
