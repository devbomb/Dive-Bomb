@tool
extends EditorPlugin

var import_plugin

func _enter_tree():
	import_plugin = preload("map_importer.gd").new()
	add_import_plugin(import_plugin)
	add_tool_menu_item("Reimport all FuncGodot maps", _reimport_all_map_files)

func _exit_tree():
	remove_import_plugin(import_plugin)
	import_plugin = null

func _get_priority():
	return 0

func _reimport_all_map_files():
	var file_system: EditorFileSystem = get_editor_interface().get_resource_filesystem()
	var files: Array[String] = []
	_find_map_files(file_system.get_filesystem_path("res://"), files)
	file_system.reimport_files(files)

func _find_map_files(directory: EditorFileSystemDirectory, files: Array[String]) -> void:
	for i in range(0, directory.get_file_count()):
		var path: String = directory.get_file_path(i)
		if path.ends_with(".map"):
			files.append(path)
	
	for i in range(0, directory.get_subdir_count()):
		_find_map_files(directory.get_subdir(i), files)
