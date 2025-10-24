@tool
class_name MinimumSizeMatchesChild extends Control

var _size_provider: Control:
	get: return _size_provider
	set(value):
		if (is_instance_valid(_size_provider)):
			_size_provider.resized.disconnect(update_minimum_size)
		
		_size_provider = value
		if (is_instance_valid(_size_provider)):
			_size_provider.resized.connect(update_minimum_size)
		
		update_minimum_size()

func _ready() -> void:
	child_order_changed.connect(_on_child_order_changed)
	_size_provider = _find_size_provider()

func _on_child_order_changed() -> void:
	_size_provider = _find_size_provider()

func _find_size_provider() -> Control:
	for child in get_children():
		if (child is Control):
			return child
	return null

func _get_minimum_size() -> Vector2:
	for child in get_children():
		if (child is Control):
			return child.size
	return Vector2.ZERO
