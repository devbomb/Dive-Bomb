@tool
class_name MiscTools extends EditorPlugin

func _enter_tree() -> void:
	add_tool_menu_item("Dive Bomb: Create new level", CreateNewLevelTool.execute)
	add_tool_menu_item("Delete all unnecessary player animation tracks", DeleteUnnecessaryPlayerAnimationsTool.execute)
	
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
