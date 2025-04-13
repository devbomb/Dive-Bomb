@tool
class_name MaterialSetter
extends Node

@export var material: Material

func _ready():
	# Need to connect this via code because connecting it through the editor
	# produces an error saying that the signal has already been connected to
	# this method.  Sounds like a Godot bug.
	if (!get_parent().is_connected("ready", parent_ready)):
		get_parent().connect("ready", parent_ready)

func parent_ready():
	set_material_recursive(get_parent())

func set_material_recursive(node: Node):
	for child in node.get_children():
		if child is MeshInstance3D:
			child.material_override = material

		set_material_recursive(child)
