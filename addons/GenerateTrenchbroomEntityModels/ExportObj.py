import bpy
import sys

# Doc can be found here: https://docs.blender.org/api/current/bpy.ops.export_scene.html
bpy.ops.export_scene.obj(filepath=sys.argv[-1], axis_forward='X', axis_up='Y')