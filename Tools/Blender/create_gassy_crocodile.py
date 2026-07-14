import bpy
import math
import os
import struct
import sys
import zlib
from mathutils import Vector


ASSET_NAME = "GG_Crocodile"
ATLAS_SIZE = 1024
ATLAS_GRID = 4


TILES = {
    "Skin": (0, 3),
    "SkinLight": (1, 3),
    "Belly": (2, 3),
    "Scutes": (3, 3),
    "Mouth": (0, 2),
    "Teeth": (1, 2),
    "Eye": (2, 2),
    "Pupil": (3, 2),
    "Nostril": (0, 1),
    "Claw": (1, 1),
    "Tongue": (2, 1),
    "Brow": (3, 1),
    "SkinShadow": (0, 0),
    "Cheek": (1, 0),
    "WaterAccent": (2, 0),
    "Reserve": (3, 0),
}


BASE_COLORS = {
    "Skin": (48, 116, 54),
    "SkinLight": (78, 151, 65),
    "Belly": (211, 180, 76),
    "Scutes": (24, 67, 34),
    "Mouth": (130, 45, 48),
    "Teeth": (242, 231, 181),
    "Eye": (245, 221, 94),
    "Pupil": (15, 24, 18),
    "Nostril": (21, 38, 24),
    "Claw": (219, 199, 137),
    "Tongue": (204, 86, 101),
    "Brow": (27, 74, 34),
    "SkinShadow": (34, 83, 43),
    "Cheek": (111, 167, 70),
    "WaterAccent": (75, 183, 157),
    "Reserve": (93, 104, 74),
}


ROUGHNESS = {
    "Skin": 0.62,
    "SkinLight": 0.58,
    "Belly": 0.7,
    "Scutes": 0.72,
    "Mouth": 0.5,
    "Teeth": 0.38,
    "Eye": 0.26,
    "Pupil": 0.2,
    "Nostril": 0.4,
    "Claw": 0.46,
    "Tongue": 0.42,
    "Brow": 0.68,
    "SkinShadow": 0.66,
    "Cheek": 0.58,
    "WaterAccent": 0.24,
    "Reserve": 0.6,
}


def command_line_output_dir():
    if "--" in sys.argv:
        args = sys.argv[sys.argv.index("--") + 1:]
        if args:
            return os.path.abspath(args[0])
    return os.path.abspath(os.path.join(os.path.dirname(__file__), "../../Assets/_FirstBloom/Games/GassyGorilla/Models/Blender/Crocodile"))


OUTPUT_DIR = command_line_output_dir()
os.makedirs(OUTPUT_DIR, exist_ok=True)


def png_chunk(chunk_type, payload):
    return struct.pack(">I", len(payload)) + chunk_type + payload + struct.pack(">I", zlib.crc32(chunk_type + payload) & 0xFFFFFFFF)


def clamp_byte(value):
    return max(0, min(255, int(round(value))))


def tile_color(tile_name, local_x, local_y, px, py):
    base = BASE_COLORS[tile_name]
    seed = ((px * 73856093) ^ (py * 19349663) ^ ((px + py) * 83492791)) & 255
    noise = (seed / 255.0) - 0.5
    shade = noise * 8.0

    if tile_name in {"Skin", "SkinLight", "SkinShadow", "Scutes", "Cheek"}:
        cells_x = 8.0
        cells_y = 7.0
        row = int(local_y * cells_y)
        offset = 0.5 if row % 2 else 0.0
        cell_x = (local_x * cells_x + offset) % 1.0
        cell_y = (local_y * cells_y) % 1.0
        dx = (cell_x - 0.5) / 0.5
        dy = (cell_y - 0.54) / 0.46
        radius = math.sqrt(dx * dx + dy * dy)
        rim = max(0.0, min(1.0, (radius - 0.68) * 5.0))
        center = max(0.0, 1.0 - radius) * 6.0
        shade += center - rim * (15.0 if tile_name != "Scutes" else 22.0)
    elif tile_name == "Belly":
        band = math.sin(local_y * math.pi * 12.0)
        shade += band * 8.0 + math.cos(local_x * math.pi * 2.0) * 4.0
    elif tile_name == "Mouth":
        shade += math.sin(local_x * math.pi * 7.0 + local_y * 4.0) * 5.0
    elif tile_name == "Tongue":
        center_line = abs(local_x - 0.5)
        shade += max(0.0, 1.0 - center_line * 10.0) * 14.0
    elif tile_name in {"Teeth", "Claw", "Eye"}:
        shade += (local_y - 0.5) * 16.0
    elif tile_name == "Pupil":
        glint = math.sqrt((local_x - 0.28) ** 2 + (local_y - 0.72) ** 2)
        if glint < 0.09:
            return 224, 245, 208, 255
    elif tile_name == "WaterAccent":
        shade += math.sin((local_x + local_y) * math.pi * 9.0) * 9.0

    return (
        clamp_byte(base[0] + shade),
        clamp_byte(base[1] + shade),
        clamp_byte(base[2] + shade),
        255,
    )


def create_atlas(path):
    tile_size = ATLAS_SIZE // ATLAS_GRID
    tile_lookup = {coords: name for name, coords in TILES.items()}
    raw = bytearray()
    for py in range(ATLAS_SIZE):
        raw.append(0)
        row_from_top = py // tile_size
        tile_row = ATLAS_GRID - 1 - row_from_top
        local_y = 1.0 - ((py % tile_size) + 0.5) / tile_size
        for px in range(ATLAS_SIZE):
            tile_col = px // tile_size
            local_x = ((px % tile_size) + 0.5) / tile_size
            tile_name = tile_lookup[(tile_col, tile_row)]
            raw.extend(tile_color(tile_name, local_x, local_y, px, py))

    header = struct.pack(">IIBBBBB", ATLAS_SIZE, ATLAS_SIZE, 8, 6, 0, 0, 0)
    png = b"\x89PNG\r\n\x1a\n"
    png += png_chunk(b"IHDR", header)
    png += png_chunk(b"IDAT", zlib.compress(bytes(raw), 9))
    png += png_chunk(b"IEND", b"")
    with open(path, "wb") as handle:
        handle.write(png)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.armatures,
        bpy.data.materials,
        bpy.data.cameras,
        bpy.data.lights,
    ):
        for datablock in list(datablocks):
            datablocks.remove(datablock)


def create_materials(atlas_path):
    image = bpy.data.images.load(atlas_path, check_existing=False)
    image.name = ASSET_NAME + "_Atlas"
    image.colorspace_settings.name = "sRGB"
    image.pack()

    materials = {}
    for name in TILES:
        material = bpy.data.materials.new(ASSET_NAME + "_" + name)
        material.use_nodes = True
        material.diffuse_color = tuple(channel / 255.0 for channel in BASE_COLORS[name]) + (1.0,)
        nodes = material.node_tree.nodes
        links = material.node_tree.links
        for node in list(nodes):
            nodes.remove(node)

        output = nodes.new("ShaderNodeOutputMaterial")
        output.location = (360, 0)
        shader = nodes.new("ShaderNodeBsdfPrincipled")
        shader.location = (80, 0)
        shader.inputs["Roughness"].default_value = ROUGHNESS[name]
        shader.inputs["Specular"].default_value = 0.33 if name not in {"Eye", "Pupil", "WaterAccent"} else 0.58
        texture = nodes.new("ShaderNodeTexImage")
        texture.location = (-260, 50)
        texture.image = image
        texture.interpolation = "Linear"
        links.new(texture.outputs["Color"], shader.inputs["Base Color"])
        links.new(shader.outputs["BSDF"], output.inputs["Surface"])
        materials[name] = material
    return materials, image


def activate(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def apply_object_transform(obj):
    activate(obj)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)


def apply_modifier(obj, modifier):
    activate(obj)
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def ensure_uv(obj):
    if not obj.data.uv_layers:
        activate(obj)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.smart_project(island_margin=0.025)
        bpy.ops.object.mode_set(mode="OBJECT")


def remap_uv(obj, tile_name):
    ensure_uv(obj)
    tile_col, tile_row = TILES[tile_name]
    margin = 0.055
    layer = obj.data.uv_layers.active.data
    for loop in layer:
        u = loop.uv.x % 1.0
        v = loop.uv.y % 1.0
        loop.uv.x = (tile_col + margin + u * (1.0 - margin * 2.0)) / ATLAS_GRID
        loop.uv.y = (tile_row + margin + v * (1.0 - margin * 2.0)) / ATLAS_GRID


def finish_part(obj, material_name, bone_name, materials, smooth=True):
    obj.name = ASSET_NAME + "_" + obj.name
    obj.data.name = obj.name + "_Mesh"
    if smooth:
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
    obj.data.materials.append(materials[material_name])
    remap_uv(obj, material_name)
    group = obj.vertex_groups.new(name=bone_name)
    group.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    return obj


def add_sphere(parts, name, location, scale, material_name, bone_name, materials, rotation=(0.0, 0.0, 0.0), segments=16, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    apply_object_transform(obj)
    finish_part(obj, material_name, bone_name, materials, smooth=True)
    parts.append(obj)
    return obj


def add_rounded_box(parts, name, location, scale, material_name, bone_name, materials, rotation=(0.0, 0.0, 0.0), bevel=0.12, segments=2):
    bpy.ops.mesh.primitive_cube_add(size=2.0, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    apply_object_transform(obj)
    modifier = obj.modifiers.new("Soft hand-painted edges", "BEVEL")
    modifier.width = bevel
    modifier.segments = segments
    modifier.affect = "EDGES"
    apply_modifier(obj, modifier)
    finish_part(obj, material_name, bone_name, materials, smooth=True)
    parts.append(obj)
    return obj


def add_cone(parts, name, location, radius, depth, material_name, bone_name, materials, rotation=(0.0, 0.0, 0.0), vertices=8, point_up=True):
    rotation_value = rotation
    if not point_up:
        rotation_value = (rotation[0] + math.pi, rotation[1], rotation[2])
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius, radius2=0.0, depth=depth, location=location, rotation=rotation_value)
    obj = bpy.context.object
    obj.name = name
    finish_part(obj, material_name, bone_name, materials, smooth=False)
    parts.append(obj)
    return obj


def add_cylinder(parts, name, location, radius, depth, material_name, bone_name, materials, rotation=(0.0, 0.0, 0.0), vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    finish_part(obj, material_name, bone_name, materials, smooth=True)
    parts.append(obj)
    return obj


def build_crocodile(materials):
    parts = []

    add_sphere(parts, "Body", (0.0, 0.0, 0.62), (1.58, 0.73, 0.58), "Skin", "Body", materials)
    add_sphere(parts, "Shoulders", (0.82, 0.0, 0.7), (0.92, 0.76, 0.58), "SkinLight", "Body", materials)
    add_sphere(parts, "Belly", (0.32, -0.01, 0.34), (1.23, 0.72, 0.3), "Belly", "Body", materials)
    add_sphere(parts, "Neck", (1.28, 0.0, 0.73), (0.62, 0.67, 0.5), "SkinLight", "Head", materials)

    add_sphere(parts, "Cranium", (1.72, 0.0, 0.82), (0.76, 0.67, 0.51), "Skin", "Head", materials)
    add_rounded_box(parts, "UpperSnout", (2.43, 0.0, 0.7), (0.9, 0.58, 0.27), "SkinLight", "Head", materials, bevel=0.17, segments=3)
    add_rounded_box(parts, "SnoutBridge", (2.1, 0.0, 0.83), (0.73, 0.61, 0.2), "Skin", "Head", materials, bevel=0.13, segments=2)
    add_rounded_box(parts, "UpperPalate", (2.39, 0.0, 0.56), (0.83, 0.48, 0.045), "Mouth", "Head", materials, bevel=0.035, segments=2)
    add_rounded_box(parts, "LowerJaw", (2.39, 0.0, 0.48), (0.91, 0.53, 0.16), "Belly", "Jaw", materials, bevel=0.12, segments=2)
    add_rounded_box(parts, "LowerMouthInterior", (2.39, 0.0, 0.565), (0.83, 0.48, 0.055), "Mouth", "Jaw", materials, bevel=0.04, segments=2)
    add_sphere(parts, "Tongue", (2.48, 0.0, 0.585), (0.65, 0.35, 0.055), "Tongue", "Jaw", materials, segments=14, rings=6)

    for side, y in (("L", 0.48), ("R", -0.48)):
        add_sphere(parts, "EyeBulb_" + side, (1.99, y, 1.13), (0.27, 0.24, 0.28), "Eye", "Head", materials, segments=14, rings=7)
        add_sphere(parts, "Pupil_" + side, (2.15, y * 1.015, 1.14), (0.105, 0.125, 0.13), "Pupil", "Head", materials, segments=12, rings=6)
        add_sphere(parts, "Cheek_" + side, (1.92, y * 1.1, 0.78), (0.38, 0.13, 0.27), "Cheek", "Head", materials, segments=14, rings=7)
        brow_rotation = (0.0, math.radians(-8.0), math.radians(-12.0 if side == "L" else 12.0))
        add_rounded_box(parts, "Brow_" + side, (1.96, y * 1.035, 1.36), (0.37, 0.12, 0.09), "Brow", "Head", materials, rotation=brow_rotation, bevel=0.08, segments=2)

    for side, y in (("L", 0.24), ("R", -0.24)):
        add_sphere(parts, "Nostril_" + side, (3.22, y, 0.85), (0.09, 0.075, 0.055), "Nostril", "Head", materials, segments=10, rings=5)

    upper_tooth_x = [1.83, 2.08, 2.34, 2.6, 2.85, 3.08]
    for side, y in (("L", 0.49), ("R", -0.49)):
        for index, x in enumerate(upper_tooth_x):
            depth = 0.18 if index in {0, 5} else 0.14
            add_cone(parts, "UpperTooth_%s_%02d" % (side, index + 1), (x, y, 0.54), 0.055, depth, "Teeth", "Head", materials, point_up=False, vertices=7)

    lower_tooth_x = [1.95, 2.23, 2.52, 2.8, 3.02]
    for side, y in (("L", 0.46), ("R", -0.46)):
        for index, x in enumerate(lower_tooth_x):
            depth = 0.15 if index in {0, 4} else 0.12
            add_cone(parts, "LowerTooth_%s_%02d" % (side, index + 1), (x, y, 0.64), 0.048, depth, "Teeth", "Jaw", materials, point_up=True, vertices=7)

    tail_specs = [
        ("TailBase", (-1.45, 0.0, 0.6), (1.05, 0.56, 0.45), 0.0, "Tail_01", "Skin"),
        ("TailMid", (-2.42, 0.12, 0.53), (0.93, 0.43, 0.35), math.radians(-7.0), "Tail_02", "Skin"),
        ("TailNarrow", (-3.22, 0.31, 0.46), (0.78, 0.32, 0.27), math.radians(-13.0), "Tail_03", "SkinLight"),
        ("TailTip", (-3.9, 0.55, 0.4), (0.66, 0.22, 0.18), math.radians(-20.0), "Tail_03", "SkinLight"),
    ]
    for name, location, scale, yaw, bone, material in tail_specs:
        add_sphere(parts, name, location, scale, material, bone, materials, rotation=(0.0, 0.0, yaw), segments=14, rings=7)

    scute_positions = [
        (1.05, 0.0, 1.18, 0.19, "Body"),
        (0.58, 0.0, 1.24, 0.21, "Body"),
        (0.1, 0.0, 1.24, 0.22, "Body"),
        (-0.4, 0.0, 1.18, 0.21, "Body"),
        (-0.9, 0.0, 1.08, 0.19, "Tail_01"),
        (-1.42, 0.02, 0.98, 0.17, "Tail_01"),
        (-1.95, 0.09, 0.86, 0.15, "Tail_02"),
        (-2.45, 0.17, 0.77, 0.13, "Tail_02"),
        (-2.92, 0.27, 0.68, 0.11, "Tail_03"),
        (-3.35, 0.4, 0.59, 0.09, "Tail_03"),
    ]
    for index, (x, y, z, size, bone) in enumerate(scute_positions):
        add_cone(parts, "BackScute_%02d" % (index + 1), (x, y, z), size, size * 1.85, "Scutes", bone, materials, vertices=5)

    leg_specs = [
        ("FrontNear", 0.88, -0.62, "Leg_FR", "SkinLight"),
        ("FrontFar", 0.88, 0.62, "Leg_FL", "SkinShadow"),
        ("BackNear", -0.82, -0.64, "Leg_BR", "Skin"),
        ("BackFar", -0.82, 0.64, "Leg_BL", "SkinShadow"),
    ]
    for name, x, y, bone, material in leg_specs:
        near_sign = -1.0 if y < 0 else 1.0
        add_sphere(parts, name + "Thigh", (x, y, 0.46), (0.43, 0.34, 0.31), material, bone, materials, rotation=(0.0, math.radians(12.0), near_sign * math.radians(8.0)), segments=14, rings=7)
        add_sphere(parts, name + "Shin", (x + 0.14, y + near_sign * 0.25, 0.25), (0.42, 0.19, 0.18), material, bone, materials, rotation=(0.0, math.radians(-9.0), near_sign * math.radians(16.0)), segments=12, rings=6)
        foot = add_rounded_box(parts, name + "Foot", (x + 0.34, y + near_sign * 0.34, 0.11), (0.44, 0.26, 0.1), "Belly" if y < 0 else material, bone, materials, rotation=(0.0, 0.0, near_sign * math.radians(7.0)), bevel=0.08, segments=2)
        for toe in range(3):
            toe_y = y + near_sign * 0.34 + (toe - 1) * 0.115
            add_cone(parts, name + "Claw_%d" % (toe + 1), (x + 0.82, toe_y, 0.105), 0.045, 0.18, "Claw", bone, materials, rotation=(0.0, math.radians(90.0), 0.0), vertices=7)

    for side, y in (("L", 0.52), ("R", -0.52)):
        for index, x in enumerate((0.65, 0.15, -0.35)):
            add_sphere(parts, "SideScale_%s_%02d" % (side, index + 1), (x, y, 0.72 - index * 0.04), (0.28, 0.055, 0.2), "SkinLight", "Body", materials, segments=12, rings=6)

    return parts


def create_armature():
    armature_data = bpy.data.armatures.new(ASSET_NAME + "_RigData")
    armature = bpy.data.objects.new(ASSET_NAME + "_Rig", armature_data)
    bpy.context.collection.objects.link(armature)
    armature.show_in_front = True
    armature_data.display_type = "STICK"
    activate(armature)
    bpy.ops.object.mode_set(mode="EDIT")

    bones = {
        "Root": ((0.0, 0.0, 0.0), (0.0, 0.5, 0.0), None),
        "Body": ((0.0, 0.0, 0.55), (0.0, 0.5, 0.55), "Root"),
        "Head": ((1.42, 0.0, 0.75), (1.42, 0.42, 0.75), "Body"),
        "Jaw": ((1.62, 0.0, 0.57), (1.62, 0.38, 0.57), "Head"),
        "Tail_01": ((-0.9, 0.0, 0.58), (-0.9, 0.42, 0.58), "Body"),
        "Tail_02": ((-1.95, 0.08, 0.52), (-1.95, 0.48, 0.52), "Tail_01"),
        "Tail_03": ((-2.92, 0.26, 0.46), (-2.92, 0.62, 0.46), "Tail_02"),
        "Leg_FL": ((0.75, 0.45, 0.48), (0.75, 0.75, 0.48), "Body"),
        "Leg_FR": ((0.75, -0.45, 0.48), (0.75, -0.15, 0.48), "Body"),
        "Leg_BL": ((-0.75, 0.45, 0.45), (-0.75, 0.75, 0.45), "Body"),
        "Leg_BR": ((-0.75, -0.45, 0.45), (-0.75, -0.15, 0.45), "Body"),
    }
    edit_bones = {}
    for name, (head, tail, parent_name) in bones.items():
        bone = armature_data.edit_bones.new(name)
        bone.head = head
        bone.tail = tail
        bone.roll = 0.0
        if parent_name:
            bone.parent = edit_bones[parent_name]
        edit_bones[name] = bone

    bpy.ops.object.mode_set(mode="POSE")
    for pose_bone in armature.pose.bones:
        pose_bone.rotation_mode = "XYZ"
    bpy.ops.object.mode_set(mode="OBJECT")
    return armature


def join_and_rig(parts, armature):
    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    mesh = bpy.context.object
    mesh.name = ASSET_NAME + "_Mesh"
    mesh.data.name = ASSET_NAME + "_MeshData"

    atlas_material = mesh.data.materials[0]
    atlas_material.name = ASSET_NAME + "_AtlasMaterial"
    for polygon in mesh.data.polygons:
        polygon.material_index = 0
    mesh.data.materials.clear()
    mesh.data.materials.append(atlas_material)

    mesh.data.use_auto_smooth = True
    mesh.data.auto_smooth_angle = math.radians(55.0)
    mesh.parent = armature
    modifier = mesh.modifiers.new("Crocodile Armature", "ARMATURE")
    modifier.object = armature
    return mesh


def reset_pose(armature):
    for bone in armature.pose.bones:
        bone.location = (0.0, 0.0, 0.0)
        bone.rotation_euler = (0.0, 0.0, 0.0)
        bone.scale = (1.0, 1.0, 1.0)


def key_pose(armature, action, frame, transforms):
    armature.animation_data.action = action
    bpy.context.scene.frame_set(frame)
    reset_pose(armature)
    for bone_name, values in transforms.items():
        pose_bone = armature.pose.bones[bone_name]
        if "location" in values:
            pose_bone.location = values["location"]
        if "rotation" in values:
            pose_bone.rotation_euler = values["rotation"]
        if "scale" in values:
            pose_bone.scale = values["scale"]
    for bone in armature.pose.bones:
        bone.keyframe_insert(data_path="location", frame=frame, group=bone.name)
        bone.keyframe_insert(data_path="rotation_euler", frame=frame, group=bone.name)
        bone.keyframe_insert(data_path="scale", frame=frame, group=bone.name)


def polish_action(action, interpolation="BEZIER"):
    action.use_fake_user = True
    for curve in action.fcurves:
        for point in curve.keyframe_points:
            point.interpolation = interpolation
            if interpolation == "BEZIER":
                point.handle_left_type = "AUTO_CLAMPED"
                point.handle_right_type = "AUTO_CLAMPED"


def build_animations(armature):
    if armature.animation_data is None:
        armature.animation_data_create()

    idle = bpy.data.actions.new("Idle_Submerged")
    idle_frames = [
        (1, 0.0, 0.0, 0.0, 0.0),
        (16, 0.045, 4.0, 7.0, 1.8),
        (31, 0.0, 0.0, 0.0, 0.0),
        (46, -0.035, -4.0, -7.0, 2.6),
        (61, 0.0, 0.0, 0.0, 0.0),
    ]
    for frame, bob, tail_a, tail_b, jaw_angle in idle_frames:
        key_pose(armature, idle, frame, {
            "Root": {"location": (0.0, 0.0, bob)},
            "Head": {"rotation": (math.radians(-1.5 + bob * 18.0), math.radians(-1.0 + bob * 8.0), 0.0)},
            "Jaw": {"rotation": (0.0, math.radians(jaw_angle), 0.0)},
            "Tail_01": {"rotation": (0.0, 0.0, math.radians(tail_a * 0.55))},
            "Tail_02": {"rotation": (0.0, 0.0, math.radians(tail_a))},
            "Tail_03": {"rotation": (0.0, 0.0, math.radians(tail_b))},
            "Leg_FR": {"rotation": (math.radians(tail_a * 0.18), 0.0, 0.0)},
            "Leg_BL": {"rotation": (math.radians(-tail_a * 0.16), 0.0, 0.0)},
        })
    polish_action(idle)

    lunge = bpy.data.actions.new("Lunge_Snap")
    lunge_poses = [
        (1, (0.0, 0.0, -0.28), (1.0, 1.0, 1.0), 1.5, 0.0, 0.0),
        (4, (-0.18, 0.0, -0.38), (0.96, 0.96, 0.82), 8.0, -7.0, -4.0),
        (8, (0.28, 0.0, 0.2), (1.05, 0.98, 1.08), 34.0, -13.0, 8.0),
        (12, (0.9, 0.0, 0.95), (1.08, 0.96, 1.12), 36.0, -14.0, 13.0),
        (15, (1.18, 0.0, 1.18), (1.02, 1.02, 0.96), 3.0, -7.0, 7.0),
        (18, (0.92, 0.0, 0.72), (0.98, 1.0, 1.02), 0.0, 2.0, -4.0),
        (24, (0.35, 0.0, 0.08), (1.0, 1.0, 1.0), 2.0, 0.0, 0.0),
    ]
    for frame, location, scale, jaw_angle, head_angle, tail_angle in lunge_poses:
        key_pose(armature, lunge, frame, {
            "Root": {"location": location, "scale": scale},
            "Head": {"rotation": (0.0, math.radians(head_angle), 0.0)},
            "Jaw": {"rotation": (0.0, math.radians(jaw_angle), 0.0)},
            "Tail_01": {"rotation": (0.0, 0.0, math.radians(-tail_angle * 0.35))},
            "Tail_02": {"rotation": (0.0, 0.0, math.radians(tail_angle * 0.7))},
            "Tail_03": {"rotation": (0.0, 0.0, math.radians(tail_angle))},
            "Leg_FL": {"rotation": (math.radians(-head_angle * 0.45), 0.0, math.radians(4.0))},
            "Leg_FR": {"rotation": (math.radians(head_angle * 0.35), 0.0, math.radians(-5.0))},
            "Leg_BL": {"rotation": (math.radians(head_angle * 0.25), 0.0, math.radians(3.0))},
            "Leg_BR": {"rotation": (math.radians(-head_angle * 0.35), 0.0, math.radians(-3.0))},
        })
    polish_action(lunge)

    settle = bpy.data.actions.new("Settle_Submerge")
    settle_poses = [
        (1, (0.0, 0.0, 0.12), 5.0, 0.0, 0.0),
        (8, (0.03, 0.0, 0.2), 1.0, -3.0, 5.0),
        (16, (0.0, 0.0, -0.12), 0.0, 2.0, -7.0),
        (25, (-0.08, 0.0, -0.62), 0.0, 5.0, 10.0),
        (36, (-0.18, 0.0, -1.25), 0.0, 8.0, 16.0),
    ]
    for frame, location, jaw_angle, head_angle, tail_angle in settle_poses:
        key_pose(armature, settle, frame, {
            "Root": {"location": location},
            "Head": {"rotation": (0.0, math.radians(head_angle), 0.0)},
            "Jaw": {"rotation": (0.0, math.radians(jaw_angle), 0.0)},
            "Tail_01": {"rotation": (0.0, 0.0, math.radians(tail_angle * 0.35))},
            "Tail_02": {"rotation": (0.0, 0.0, math.radians(-tail_angle * 0.7))},
            "Tail_03": {"rotation": (0.0, 0.0, math.radians(tail_angle))},
        })
    polish_action(settle)

    armature.animation_data.action = idle
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 61
    return idle, lunge, settle


def export_fbx(filepath, armature, mesh, current_action=None, all_actions=False):
    if current_action is not None:
        armature.animation_data.action = current_action
        bpy.context.scene.frame_start = int(current_action.frame_range[0])
        bpy.context.scene.frame_end = int(current_action.frame_range[1])
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    mesh.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True,
        global_scale=1.0,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        use_armature_deform_only=True,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=all_actions,
        bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.15,
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        path_mode="COPY",
        embed_textures=True,
    )


def look_at(obj, point):
    direction = Vector(point) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_preview_scene(materials, armature, idle_action):
    armature.animation_data.action = idle_action
    bpy.context.scene.frame_set(16)

    preview_material = bpy.data.materials.new("Preview_Lagoon")
    preview_material.use_nodes = True
    shader = preview_material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (0.018, 0.12, 0.105, 1.0)
    shader.inputs["Roughness"].default_value = 0.24
    shader.inputs["Specular"].default_value = 0.7
    bpy.ops.mesh.primitive_circle_add(vertices=64, radius=6.8, fill_type="NGON", location=(-0.25, 0.0, -0.03))
    water = bpy.context.object
    water.name = "Preview_Lagoon_Surface"
    water.data.materials.append(preview_material)

    bpy.ops.object.camera_add(location=(5.7, -13.6, 5.7))
    camera = bpy.context.object
    camera.name = "Preview_Camera"
    camera.data.lens = 56.0
    look_at(camera, (-0.35, 0.0, 0.55))
    bpy.context.scene.camera = camera

    lights = [
        ("Key", "AREA", (4.5, -5.0, 8.5), (0.98, 0.82, 0.55), 1050.0, 5.0),
        ("Fill", "AREA", (-4.5, -2.5, 4.0), (0.35, 0.7, 0.55), 720.0, 4.5),
        ("Rim", "AREA", (-2.0, 5.0, 7.0), (0.55, 0.9, 1.0), 980.0, 3.0),
    ]
    for name, light_type, location, color, energy, size in lights:
        light_data = bpy.data.lights.new("Preview_" + name, type=light_type)
        light_data.energy = energy
        light_data.color = color
        light_data.size = size
        light = bpy.data.objects.new("Preview_" + name, light_data)
        bpy.context.collection.objects.link(light)
        light.location = location
        look_at(light, (-0.2, 0.0, 0.6))

    world = bpy.context.scene.world
    world.use_nodes = True
    background = world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.012, 0.035, 0.022, 1.0)
    background.inputs["Strength"].default_value = 0.45

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.eevee.use_gtao = True
    scene.eevee.gtao_distance = 3.0
    scene.eevee.gtao_factor = 1.3
    scene.eevee.use_soft_shadows = True
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 640
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.look = "Medium High Contrast"
    scene.render.filepath = os.path.join(OUTPUT_DIR, ASSET_NAME + "_Preview.png")
    scene.render.film_transparent = False
    bpy.ops.render.render(write_still=True)

    armature.animation_data.action = bpy.data.actions["Lunge_Snap"]
    scene.frame_set(12)
    scene.render.filepath = os.path.join(OUTPUT_DIR, ASSET_NAME + "_LungePreview.png")
    bpy.ops.render.render(write_still=True)
    armature.animation_data.action = idle_action
    scene.frame_set(16)


def write_asset_notes(mesh, actions):
    triangle_count = sum(len(poly.vertices) - 2 for poly in mesh.data.polygons)
    notes_path = os.path.join(OUTPUT_DIR, "README.md")
    with open(notes_path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write("# Gassy Gorilla Crocodile\n\n")
        handle.write("Stylized Blender-authored lagoon crocodile for Gassy Gorilla.\n\n")
        handle.write("- Forward axis: local +X\n")
        handle.write("- Up axis: local +Z in Blender, converted to Unity Y-up by FBX export\n")
        handle.write("- Approximate triangles: %d\n" % triangle_count)
        handle.write("- Texture: one 1024x1024 embedded color atlas\n")
        handle.write("- Root motion: armature object remains stationary\n")
        handle.write("- Materials: %d atlas-backed slots\n\n" % len(mesh.data.materials))
        handle.write("- Runtime FBX: `GG_Crocodile_Rigged.fbx` contains every clip below\n\n")
        handle.write("## Clips\n\n")
        for action in actions:
            start, end = action.frame_range
            handle.write("- `%s`: frames %d-%d at 30 FPS\n" % (action.name, int(start), int(end)))
        handle.write("\n`Idle_Submerged` should loop. `Lunge_Snap` and `Settle_Submerge` should not loop.\n")


def main():
    clear_scene()
    scene = bpy.context.scene
    scene.render.fps = 30
    scene.render.fps_base = 1.0
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0

    atlas_path = os.path.join(OUTPUT_DIR, ASSET_NAME + "_Atlas.png")
    create_atlas(atlas_path)
    materials, atlas_image = create_materials(atlas_path)
    parts = build_crocodile(materials)
    armature = create_armature()
    mesh = join_and_rig(parts, armature)
    actions = build_animations(armature)

    blend_path = os.path.join(OUTPUT_DIR, ASSET_NAME + "_Rigged.blend")
    export_fbx(os.path.join(OUTPUT_DIR, ASSET_NAME + "_Rigged.fbx"), armature, mesh, current_action=actions[0], all_actions=True)

    add_preview_scene(materials, armature, actions[0])
    write_asset_notes(mesh, actions)
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)

    triangle_count = sum(len(poly.vertices) - 2 for poly in mesh.data.polygons)
    print("GG_CROCODILE_BUILD_COMPLETE")
    print("Output: " + OUTPUT_DIR)
    print("Triangles: %d" % triangle_count)
    print("Bones: %d" % len(armature.data.bones))
    print("Materials: %d" % len(mesh.data.materials))
    print("Actions: " + ", ".join(action.name for action in actions))


if __name__ == "__main__":
    main()
