@tool
extends EditorPlugin

func _enter_tree() -> void:
	add_tool_menu_item("Create new level", execute_create_new_level)
	add_tool_menu_item("Delete all unnecessary player animation tracks", execute_delete_unnecessary_player_animation_tracks)

func execute_create_new_level() -> void:
	var level_id: String = await _prompt_string("Enter a level id(no spaces)")
	if (level_id.is_empty()):
		return
	
	_create_new_level(level_id, "res://Levels")
	
func _create_new_level(level_id: String, parent_folder: String) -> void:
	var level_folder: String = parent_folder.path_join(level_id)
	DirAccess.make_dir_absolute(level_folder)
	
	# Create a placeholder skybox
	var environment = Environment.new()
	ResourceSaver.save(environment, level_folder.path_join(level_id + "Skybox.tres"))
	
	# TODO: Create a maps folder, along with a "hazardous waste containment"
	# folder for all of those pesky autosaves Trenchbroom likes to create.
	
	# Refresh the editor so the new folder can be seen
	get_editor_interface().get_resource_filesystem().scan()
	

func execute_delete_unnecessary_player_animation_tracks():
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

# Returns the string the user typed.  Returns the empty string if the dialog was canceled
func _prompt_string(prompt_text: String) -> String:
	var stack_panel = VBoxContainer.new()
	stack_panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	stack_panel.offset_top = 10
	stack_panel.offset_left = 10
	stack_panel.offset_bottom = -10
	stack_panel.offset_right = -10
	stack_panel.alignment = BoxContainer.ALIGNMENT_CENTER
	
	var textbox = LineEdit.new()
	stack_panel.add_spacer(false)
	stack_panel.add_child(textbox)
	
	var buttons_panel = HBoxContainer.new()
	stack_panel.add_spacer(false)
	stack_panel.add_child(buttons_panel)
	buttons_panel.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	
	var ok_button = Button.new()
	buttons_panel.add_child(ok_button)
	ok_button.text = "OK"
	ok_button.custom_minimum_size.x = 50
	ok_button.pressed.connect(func (): 
		textbox.text_submitted.emit(textbox.text)
	)
	
	var cancel_button = Button.new()
	buttons_panel.add_child(cancel_button)
	cancel_button.text = "Cancel"
	cancel_button.custom_minimum_size.x = 50
	cancel_button.pressed.connect(func (): 
		textbox.text_submitted.emit("")
	)
	
	var window = Window.new()
	window.title = prompt_text
	window.add_child(stack_panel)
	window.close_requested.connect(func ():
		textbox.text_submitted.emit("")
	)

	get_editor_interface().popup_dialog_centered(window, Vector2i(500, 100))
	var result: String = await textbox.text_submitted
	window.queue_free()
	
	return result
