class_name MeshReferencingCollisionShape3D extends CollisionShape3D

@export_node_path("MeshInstance3D") var mesh_path: NodePath

func _ready():
	var mesh_instance: MeshInstance3D = get_node(mesh_path)
	var mesh: ArrayMesh = mesh_instance.mesh
	shape = mesh.create_convex_shape()
