@tool
class_name CreateNewLevelTool extends MiscTool

func get_text() -> String: return "Create new level"

func execute() -> void:
	var prompt_result: Dictionary = await _prompt_level_id()
	var level_id: String = prompt_result["level_id"]
	var debug: bool = prompt_result["debug"]
	
	if (level_id.is_empty()):
		return
	
	var folder = \
		"res://Levels/Debug" if debug else \
		"res://Levels/Production"
	_create_new_level(level_id, folder, debug)
	
func _create_new_level(level_id: String, parent_folder: String, debug: bool) -> void:
	var level_folder: String = parent_folder.path_join(level_id)
	DirAccess.make_dir_absolute(level_folder)
	
	# Create a maps folder, along with a "hazardous waste containment"
	# folder for all of those pesky autosaves Trenchbroom likes to create.
	var maps_folder: String = level_folder.path_join("Maps")
	DirAccess.make_dir_absolute(maps_folder)
	
	var autosaves_folder: String = level_folder.path_join("Maps/autosave")
	DirAccess.make_dir_absolute(autosaves_folder)
	
	var gitignore_file: FileAccess = FileAccess.open(autosaves_folder.path_join(".gitignore"), FileAccess.WRITE)
	gitignore_file.store_string("*.map")
	gitignore_file.close()
	
	var gdignore_file: FileAccess = FileAccess.open(autosaves_folder.path_join(".gdignore"), FileAccess.WRITE)
	gdignore_file.close()
	
	# Create a manifest for the level
	var manifest = LevelManifest.new()
	manifest.Debug = debug
	manifest.HumanReadableName = level_id
	manifest.SceneFilePath = level_folder.path_join(level_id + ".tscn")
	ResourceSaver.save(manifest, level_folder.path_join(level_id + ".level.tres"))
	
	# Create the "official" scene
	var level_root = DiveBombLevel.new()
	level_root.name = level_id
	level_root.Manifest = manifest
	
	# Use the default skybox
	var environment_node = WorldEnvironment.new()
	level_root.add_child(environment_node)
	environment_node.owner = level_root
	environment_node.name = "WorldEnvironment"
	environment_node.environment = load(manifest.SkyBoxEnvironmentFilePath)
	
	var sun = DirectionalLight3D.new()
	level_root.add_child(sun)
	sun.owner = level_root
	sun.name = "DirectionalLight3D"
	sun.position.y = 7
	sun.rotation_degrees.x = -45
	
	# Save the scene
	var scene = PackedScene.new()
	scene.pack(level_root)
	ResourceSaver.save(scene, manifest.SceneFilePath)
	
	# Refresh the editor so the new folder can be seen
	EditorInterface.get_resource_filesystem().scan()
	
static func _prompt_level_id() -> Dictionary:
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
	
	var debug_checkbox = CheckBox.new()
	#stack_panel.add_spacer(false)
	stack_panel.add_child(debug_checkbox)
	debug_checkbox.text = "Debug"
	
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
	window.title = "Enter a level id(no spaces)"
	window.add_child(stack_panel)
	window.close_requested.connect(func ():
		textbox.text_submitted.emit("")
	)

	EditorInterface.popup_dialog_centered(window, Vector2i(500, 150))
	var result: String = await textbox.text_submitted
	window.queue_free()
	
	return {
		"level_id" = result,
		"debug" = debug_checkbox.button_pressed
	}
