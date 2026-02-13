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
	get_parent().add_child(replacement_node)
	
	while (get_child_count() > 0):
		var child: Node = get_child(0)
		# Avoid this warning: Adding '<node name>' as child to 'DivableWall' will make owner
		# '<map name>' inconsistent. Consider unsetting the owner beforehand.
		var old_owner: Node = child.owner
		child.owner = null
		
		remove_child(child)
		replacement_node.add_child(child)

		child.owner = old_owner
	queue_free()
