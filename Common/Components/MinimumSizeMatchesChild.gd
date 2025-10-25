@tool
class_name MinimumSizeMatchesChild extends Control

var _last_min_size: Vector2 = Vector2.ZERO

func _ready() -> void:
	_last_min_size = _get_minimum_size()
	update_minimum_size()

func _process(_delta: float) -> void:
	var min_size = _get_minimum_size()
	if (min_size != _last_min_size):
		_last_min_size = min_size
		update_minimum_size()

func _get_minimum_size() -> Vector2:
	var min_size: Vector2 = Vector2.ZERO
	
	for child in get_children():
		if (child is Control):
			var child_min: Vector2 = child.size
			if (child_min.x > min_size.x):
				min_size.x = child_min.x
			if (child_min.y > min_size.y):
				min_size.y = child_min.y
				
	return min_size
