@tool
extends EditorPlugin

const MenuItemName := "Generate Trenchbroom Entity Models"
const ExportScriptResPath := "res://addons/GenerateTrenchbroomEntityModels/ExportObj.py"

func _enter_tree():
	add_tool_menu_item(MenuItemName, _generate)

func _exit_tree():
	remove_tool_menu_item(MenuItemName)

func _generate():
	var blend_files_folder: String = ProjectSettings.globalize_path("res://BlenderModels")
	var obj_files_folder: String = ProjectSettings.globalize_path("res://TrenchbroomEntityModels")

	for file_name in DirAccess.get_files_at(blend_files_folder):
		var basename: String = file_name.get_basename()
		var extension: String = file_name.get_extension()

		if (extension != "blend"):
			continue

		var blend_file_path: String = blend_files_folder.trim_suffix("/") + "/" + file_name
		var obj_file_path: String = obj_files_folder.trim_suffix("/") + "/" + basename + ".obj"
		_export_to_obj(blend_file_path, obj_file_path)

func _export_to_obj(blend_file_path: String, output_file_path: String):
	var export_script_path: String = ProjectSettings.globalize_path(ExportScriptResPath)
	var blender_folder: String = EditorInterface.get_editor_settings().get_setting(
		"filesystem/import/blender/blender3_path"
	)
	var blender_path: String = blender_folder.trim_suffix("/") + "/blender.exe"
	var args := [
		blend_file_path,
		"--background",
		"--python",
		export_script_path,
		"--",
		output_file_path
	]

	OS.create_process(blender_path, args, true)
