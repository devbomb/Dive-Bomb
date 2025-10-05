@tool
extends EditorPlugin

var import_plugin

func _enter_tree():
	import_plugin = preload("map_importer.gd").new()
	add_import_plugin(import_plugin)
	add_tool_menu_item("Reimport all FuncGodot maps", _reimport_all_map_files)
	add_tool_menu_item("Delete all unnecessary player animation tracks", _delete_unnecessary_player_animation_tracks)

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

func _delete_unnecessary_player_animation_tracks():
	var library: AnimationLibrary = ResourceLoader.load("res://Entities/Player/KennifiedPlayerAnimations.tres")
	var reset_animation: Animation = library.get_animation("RESET")
	var bonk_animation: Animation = ResourceLoader.load("res://Entities/Player/Animations/Bonk.tres")
	
	print("Deleting unnecessary tracks from " + bonk_animation.resource_path)
	
	# Iterating the tracks in reverse so the remaining track indices don't shift
	# when we delete them.
	var track_idxs = range(bonk_animation.get_track_count())
	track_idxs.reverse()
	for track_idx in track_idxs:
		if (bonk_animation.track_get_key_count(track_idx) > 1):
			continue
			
		var track_path = bonk_animation.track_get_path(track_idx)
		var track_type = bonk_animation.track_get_type(track_idx)
		var track_value: Variant = bonk_animation.track_get_key_value(track_idx, 0)
		
		var reset_track_idx: int = reset_animation.find_track(track_path, track_type)
		var reset_track_value: Variant = reset_animation.track_get_key_value(reset_track_idx, 0)
		
		if (track_value == reset_track_value):
			print("Deleting track " + str(track_path) + "(" + str(track_type) + ")")
			bonk_animation.remove_track(track_idx)
	
	ResourceSaver.save(bonk_animation, bonk_animation.resource_path)
