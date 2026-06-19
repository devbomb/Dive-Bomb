@tool
class_name AlignMapsTool extends MiscTool

func get_text() -> String: return "Align map edges"

func execute() -> void:
	var scene_root: Node = EditorInterface.get_edited_scene_root()
	assemble(scene_root)

func assemble(scene_root: Node) -> void:
	# This is actually a Dictionary[Node, Dictionary[String, MapEdge]], but
	# GDScript doesn't support annotating nested generic types yet at the time 
	# of writing.
	#
	# If you're reading this in the future and GDScript _does_ support it now,
	# then congratulations, I'm jealous!  I bet you guys have traits, too.
	var edges_by_map: Dictionary[Node, Dictionary] = _build_edges_by_map(scene_root)
	
	if edges_by_map.is_empty():
		print("No maps with edges found")
		return
	
	var fixed_edges: Dictionary[String, MapEdge] = {}
	var unvisited_maps: Dictionary[Node3D, bool] = {}
	for map in edges_by_map.keys():
		unvisited_maps[map] = false
	
	var first_map: Node3D = edges_by_map.keys()[0]
	_visit(first_map, unvisited_maps, fixed_edges, edges_by_map)
	
func _visit(
	map: Node3D,
	unvisited_maps: Dictionary[Node3D, bool],
	fixed_edges: Dictionary[String, MapEdge],
	edges_by_map: Dictionary[Node, Dictionary] # Dictionary[Node, Dictionary[String, MapEdge]]
) -> void:
	print("Placing " + map.name)
	unvisited_maps.erase(map)
	
	var edges_in_this_map: Dictionary[String, MapEdge] = edges_by_map[map]
	
	# Move this map into its place.
	# If there's more than one edge that needs to match up, take the average of
	# all of them.
	var matching_edges: Array[MapEdge]
	matching_edges.assign(edges_in_this_map \
		.keys() \
		.filter(func(id: String): return fixed_edges.has(id)) \
		.map(func(id: String): return fixed_edges[id])
	)
	
	var transforms: Array[Transform3D]
	transforms.assign(matching_edges \
		.map(func(edge: MapEdge): return _global_transform_if_linked_to_edge(map, edge, edges_by_map))
	)
	
	map.global_transform = _average(transforms)
	
	# Fix all the remaining edges of this map in place
	var edges_to_fix: Array[MapEdge]
	edges_to_fix.assign(edges_in_this_map \
		.keys() \
		.map(func(id: String): return edges_in_this_map[id]) \
		.filter(func(edge: MapEdge): return !fixed_edges.has(edge.id))
	)
	
	for edge in edges_to_fix:
		fixed_edges[edge.id] = edge
	
	# Visit all unvisited neighbors
	var unvisited_neighbors: Array[Node3D] = unvisited_maps \
		.keys() \
		.filter(func(m: Node3D): return _are_neighbors(map, m, edges_by_map))
	
	for neighbor in unvisited_neighbors:
		# One of our children may have already visited this neighbor, so check again.
		if !unvisited_maps.has(neighbor):
			continue
		
		_visit(neighbor, unvisited_maps, fixed_edges, edges_by_map)

func _build_edges_by_map(scene_root: Node) -> Dictionary[Node, Dictionary]:  # Dictionary[Node, Dictionary[String, MapEdge]]
	var edges_by_map: Dictionary[Node, Dictionary] = {} # Dictionary[Node, Dictionary[String, MapEdge]]
	
	for edge in scene_root.get_tree().get_nodes_in_group("MapEdge"):
		var map_edge: MapEdge = edge
		var map: Node3D = map_edge.owner
	
		if edges_by_map.has(map):
			edges_by_map[map][map_edge.id] = map_edge
		else:
			var edges_by_id: Dictionary[String, MapEdge] = {}
			edges_by_id[edge.id] = edge
			edges_by_map[map] = edges_by_id
	
	return edges_by_map

func _are_neighbors(
	map_a: Node3D,
	map_b: Node3D,
	edges_by_map: Dictionary[Node, Dictionary] # Dictionary[Node, Dictionary[String, MapEdge]]
) -> bool:
	var map_a_edges: Dictionary[String, MapEdge] = edges_by_map[map_a]
	var map_b_edges: Dictionary[String, MapEdge] = edges_by_map[map_b]
	
	for key in map_a_edges.keys():
		if map_b_edges.has(key):
			return true
	
	return false

func _global_transform_if_linked_to_edge(
	map: Node3D,
	edge: MapEdge,
	edges_by_map: Dictionary[Node, Dictionary] # Dictionary[Node, Dictionary[String, MapEdge]]
) -> Transform3D:
	var local_edge: MapEdge = (edges_by_map[map] as Dictionary[String, MapEdge])[edge.id]
	var map_in_temp_origin_space: Transform3D = _global_to_local(map.global_transform, local_edge.global_transform)
	
	var temp_origin_global: Transform3D = edge \
		.global_transform \
		.rotated_local(Vector3.UP, deg_to_rad(180))
	
	return _local_to_global(map_in_temp_origin_space, temp_origin_global)

func _global_to_local(child_global: Transform3D, parent_global: Transform3D) -> Transform3D:
	return child_global * parent_global.inverse()

func _local_to_global(child_local: Transform3D, parent_global: Transform3D) -> Transform3D:
	return parent_global * child_local

func _average(transforms: Array[Transform3D]) -> Transform3D:
	# Algorithm taken from
	# https://stackoverflow.com/questions/21241965/average-transformation-matrix-for-a-list-of-transformations
	var count: int = 1
	var average: Transform3D = Transform3D.IDENTITY
	
	for tf in transforms:
		average = average.interpolate_with(tf, 1.0 / count)
		count += 1
	
	return average
