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
	
	var player_model_scene: PackedScene = ResourceLoader.load("res://Entities/Player/Parts/PlayerModel/KennifiedPlayerModel.blend")
	var player_model: Node3D = player_model_scene.instantiate()
	var skeleton: Skeleton3D = player_model.get_node("Armature/Skeleton3D")
	
	for animation_name in library.get_animation_list():
		if (animation_name == "RESET"):
			continue
			
		var animation = library.get_animation(animation_name)
		if (animation.is_built_in()):
			continue
			
		_delete_unnecessary_tracks_from(animation, reset_animation, skeleton)
		ResourceSaver.save(animation, animation.resource_path)

func _delete_unnecessary_tracks_from(animation: Animation, reset_animation: Animation, skeleton: Skeleton3D) -> void:
	print("Deleting unnecessary tracks from " + animation.resource_path)
	
	# Iterating the tracks in reverse so the remaining track indices don't shift
	# when we delete them.
	var track_idxs = range(animation.get_track_count())
	track_idxs.reverse()
	for track_idx in track_idxs:
			
		var track_path = animation.track_get_path(track_idx)
		var track_type = animation.track_get_type(track_idx)
		var track_value: Variant = animation.track_get_key_value(track_idx, 0)
		
		var disallowed_types: Array[Animation.TrackType] = [
			Animation.TrackType.TYPE_AUDIO,
			Animation.TrackType.TYPE_METHOD,
			Animation.TrackType.TYPE_ANIMATION
		]
		if (disallowed_types.has(track_type)):
			continue
		
		# Don't delete if it doesn't have a RESET value
		var reset_track_idx: int = reset_animation.find_track(track_path, track_type)
		if (reset_track_idx == -1):
			print("!!! " + str(track_path) + "(" + str(track_type) + ") doesn't have a RESET value")
			continue
		
		# Don't delete if its value isn't the same as its RESET value
		if (animation.track_get_key_count(track_idx) > 1):
			continue
			
		var reset_track_value: Variant = reset_animation.track_get_key_value(reset_track_idx, 0)
		if (track_value != reset_track_value):
			continue
		
		# Don't delete if it's one of the tracks I've specifically made un-deletable
		const bone_prefix = "KennifiedPlayerModel/Armature/Skeleton3D:"
		if (str(track_path).begins_with(bone_prefix)):
			var bone_name = str(track_path).trim_prefix(bone_prefix)
			var undeletable_bones: Array[String] = [
				"Hand_L",
				"Hand_L.002",
				"Hand_L.001",
				"Hand_L.003",
				"Thumb_L",
				"Thumb_L.001",
				"Hand_R",
				"Hand_R.002",
				"Hand_R.001",
				"Hand_R.003",
				"Thumb_R",
				"Thumb_R.001",
			]
			if (undeletable_bones.has(bone_name)):
				continue
		
		# OK, fine, you can delete it
		print("Deleting track " + str(track_path) + "(" + str(track_type) + ")")
		animation.remove_track(track_idx)
