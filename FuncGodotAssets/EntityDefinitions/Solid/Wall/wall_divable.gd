@tool
extends StaticBody3D

var scene: PackedScene = preload("res://Entities/Wall/DivableWall/DivableWall.tscn")

func _ready():
	replace.call_deferred()

func replace():
	# Move all the geometry generated for this node into a replacement coming
	# from the scene.
	var replacement_node: Node3D = scene.instantiate()
	replacement_node.transform = transform
	
	while (get_child_count() > 0):
		var child: Node = get_child(0)
		remove_child(child)
		replacement_node.add_child(child)
		
	get_parent().add_child(replacement_node)
	queue_free()
