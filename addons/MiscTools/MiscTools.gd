@tool
class_name MiscTools extends EditorPlugin

func _enter_tree() -> void:
	_add_tool(CreateNewLevelTool.new())
	
	_add_tool(PlaySceneFromHereTool.new())
	_add_3d_editor_button(PlaySceneFromHereTool.new())
	
	_add_tool(AlignMapsTool.new(get_undo_redo()))
	_add_3d_editor_button(AlignMapsTool.new(get_undo_redo()))
	
	_add_tool(DeleteUnnecessaryPlayerAnimationsTool.new())
	_add_tool(ResaveAllResourcesTool.new())

func _add_tool(tool: MiscTool) -> void:
	add_tool_menu_item("Dive Bomb:  " + tool.get_text(), tool.execute)

func _add_3d_editor_button(tool: MiscTool) -> void:
	var button = Button.new()
	button.pressed.connect(tool.execute)
	button.text = tool.get_text()
	add_control_to_container(EditorPlugin.CONTAINER_SPATIAL_EDITOR_MENU, button)

# Returns the string the user typed.  Returns the empty string if the dialog was canceled
static func prompt_string(prompt_text: String) -> String:
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

	EditorInterface.popup_dialog_centered(window, Vector2i(500, 100))
	var result: String = await textbox.text_submitted
	window.queue_free()
	
	return result

# Returns a list of all file paths in "res://" (including addons) that end with the given file
# extension.
static func all_files_of_type(file_extension_including_dot: String) -> Array[String]:
	var file_system: EditorFileSystem = EditorInterface.get_resource_filesystem()
	var files: Array[String] = []
	
	_find_all_files_of_type_recursive(
		file_system.get_filesystem_path("res://"),
		file_extension_including_dot,
		files
	)
	
	return files

static func _find_all_files_of_type_recursive(
	directory: EditorFileSystemDirectory,
	file_extension_including_dot: String,
	out_files: Array[String]
) -> void:
	for i in range(0, directory.get_file_count()):
		var path: String = directory.get_file_path(i)
		if path.ends_with(file_extension_including_dot):
			out_files.append(path)
	
	for i in range(0, directory.get_subdir_count()):
		_find_all_files_of_type_recursive(
			directory.get_subdir(i),
			file_extension_including_dot,
			out_files
		)
