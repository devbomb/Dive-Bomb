class_name BillboardToCameraDecorator extends Node

@export var y_only: bool = true

func _process(_delta):
	var parent: Node3D = get_parent()
	parent.look_at(get_tree().root.get_camera_3d().global_position)
	
	if (y_only):
		parent.global_rotation.x = 0
		parent.global_rotation.z = 0
