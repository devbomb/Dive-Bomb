@tool
extends EditorImportPlugin

enum Presets { DEFAULT }

func _get_importer_name() -> String:
	return "map_importer"

func _get_visible_name() -> String:
	return "Map Importer"

func _get_priority() -> float:
	return 2

func _get_recognized_extensions() -> PackedStringArray:
	return ["map"]

func _get_save_extension() -> String:
	return "scn"

func _get_resource_type() -> String:
	return "PackedScene"

func _get_preset_count() -> int:
	return Presets.size()

func _get_preset_name(preset_index) -> String:
	match preset_index:
		Presets.DEFAULT:
			return "Default"
		_:
			return "Unknown"

func _get_import_options(path, preset_index) -> Array[Dictionary]:
	match preset_index:
		Presets.DEFAULT:
			return [
				{
					"name": "map_inverse_scale",
					"default_value": 16
				},
				{
					"name": "entity_path",
					"default_value": "res://Scenes/Entities"
				},
				{
					"name": "texture_path",
					"default_value": "res://Textures"
				},
				{
					"name": "visual_layer_mask",
					"default_value": 1 | 8,
					"property_hint": PROPERTY_HINT_LAYERS_3D_RENDER
				},
				{
					"name": "collision_layer_mask",
					"default_value": 1 | 2,
					"property_hint": PROPERTY_HINT_LAYERS_3D_PHYSICS
				}
			]
		_:
			return []

func _get_import_order() -> int:
	return 101 # Make it process _after_ .tscn files, to ensure the entities
			   # have been imported before we try to instantiate them.

func _get_option_visibility(path, option_name, options):
	return true

func _import(
	source_file: String,
	save_path: String,
	options: Dictionary,
	r_platform_variants: Array[String],
	r_gen_files: Array[String]):
	print("Importing " + source_file)
	var tbLoader: TBLoader = TBLoader.new()

	tbLoader.map_resource = source_file
	tbLoader.entity_common = true
	tbLoader.map_inverse_scale = options.map_inverse_scale
	tbLoader.entity_path = options.entity_path
	tbLoader.texture_path = options.texture_path
	tbLoader.option_visual_layer_mask = options.visual_layer_mask

	tbLoader.build_meshes()

	var mapRoot = Node3D.new()
	mapRoot.name = get_root_node_name(source_file)

	move_children(tbLoader, mapRoot)
	fix_duplicate_node_names(mapRoot)

	for node in all_nodes_directly_in_scene(mapRoot):
		# Set the map root as the owner---otherwise, it won't be saved to the
		# packed scene!
		node.owner = mapRoot

		if node is StaticBody3D && !is_root_of_another_scene(node):
			node.collision_layer = options.collision_layer_mask

	# Save the map as a packed scene
	var scene = PackedScene.new()
	scene.pack(mapRoot)
	return ResourceSaver.save(scene, "%s.%s" % [save_path, _get_save_extension()])

func fix_duplicate_node_names(node: Node):
	var nameCounts: Dictionary = {}
	for child in node.get_children():
		var originalName = child.name

		if nameCounts.has(originalName):
			child.name = originalName + str(nameCounts[originalName])
			nameCounts[originalName] += 1
		else:
			nameCounts[originalName] = 1

		fix_duplicate_node_names(child)

func move_children(src: Node, dst: Node):
	for child in src.get_children():
		src.remove_child(child)
		dst.add_child(child)

func get_root_node_name(source_file: String):
	var parts = source_file.trim_prefix("res://").split("/")
	return parts[parts.size() - 1].trim_suffix(".map")

func is_root_of_another_scene(node: Node) -> bool:
	return node.scene_file_path != ""

# Returns all nodes that are directly inside the given scene
# (IE: were not brought in by a child scene)
func all_nodes_directly_in_scene(sceneRoot: Node) -> Array[Node]:
	var array: Array[Node] = []
	for child in sceneRoot.get_children():
		_all_nodes_directly_in_scene(child, sceneRoot, array)
	return array

func _all_nodes_directly_in_scene(node: Node, sceneRoot: Node, array: Array[Node]):
	var is_from_another_scene: bool = node.owner != null && node.owner != sceneRoot
	if is_from_another_scene:
		return

	array.append(node)

	for child in node.get_children():
		_all_nodes_directly_in_scene(child, sceneRoot, array)
