class_name SpinDecorator extends Node

@export var speed_degrees: Vector3 = Vector3(0, 0, 0)

func _process(delta):
	var parent: Node3D = get_parent()
	parent.rotation_degrees += speed_degrees * delta
