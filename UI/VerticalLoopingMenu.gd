extends VBoxContainer

var _first: Control
var _last: Control

func _process(delta: float):
	# Doing this every frame instead of once at startup
	# in case an option's visibility changes
	update_first_and_last()

func update_first_and_last():
	if (_first != null):
		_first.focus_neighbor_top = ""
	if (_last != null):
		_last.focus_neighbor_bottom = ""
		
	_first = first_visible_child()
	_last = last_visible_child()
	
	_first.focus_neighbor_top = _last.get_path()
	_last.focus_neighbor_bottom = _first.get_path()

func first_visible_child() -> Control:
	for child in get_children():
		if (child.visible):
			return child
	return null

func last_visible_child() -> Control:
	var last: Control = null
	for child in get_children():
		if (child.visible):
			last = child
	return last
