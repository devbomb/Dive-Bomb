@tool
class_name MaterialSetter
extends Node

@export var material: Material

func _ready():
	if (!get_parent().is_connected("ready", parent_ready)):
		get_parent().connect("ready", parent_ready)

func parent_ready():
	set_material_recursive(get_parent())

func set_material_recursive(node: Node):
	for child in node.get_children():
		if child is MeshInstance3D:
			child.material_override = material

		set_material_recursive(child)
