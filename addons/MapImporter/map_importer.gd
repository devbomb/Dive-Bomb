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
	return "tscn"

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
				},
				{
					"name": "texture_material_map",
					"default_value": MaterialMap.new()
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
	r_gen_files: Array[String]) -> Error:
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
		
		# Map textures to their materials based
		if node is MeshInstance3D && !is_root_of_another_scene(node):
			replace_materials(node, options.texture_material_map.texture_to_material)

	# Save the map as a packed scene
	var filePath = "%s.%s" % [save_path, _get_save_extension()]
	var scene = PackedScene.new()
	scene.pack(mapRoot)
	
	var saveResult = ResourceSaver.save(scene, filePath)
	if (saveResult != OK):
		return saveResult
	
	# HACK: ResourceSaver.save() has a bug where signal connections from
	# instantiated child scenes are unnecessarily saved again in the parent
	# scene, resulting in a "Signal 'Foo' is already connected" error.
	# See https://github.com/godotengine/godot/issues/48064
	#
	# The workaround: edit the generated .tscn file and delete the "connection"
	# lines.
	_strip_duplicate_signal_connections(filePath)
	return OK

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

func replace_materials(meshInstance: MeshInstance3D, textureToMaterial: Dictionary[Texture2D, Material]):
	for surfaceIndex in meshInstance.mesh.get_surface_count():
		var surfaceMaterial: Material = meshInstance.mesh.surface_get_material(surfaceIndex)
		if !(surfaceMaterial is StandardMaterial3D):
			continue

		var texture: Texture2D = surfaceMaterial.albedo_texture
		if texture == null:
			continue
		
		if !textureToMaterial.has(texture):
			continue

		var replacementMaterial: Material = textureToMaterial.get(texture)
		meshInstance.set_surface_override_material(surfaceIndex, replacementMaterial)

func _strip_duplicate_signal_connections(filePath: String) -> Error:
	var originalText: String = FileAccess.get_file_as_string(filePath)
	
	var regex = RegEx.new()
	regex.compile("\\[connection signal=.+\\]\\n")
	var strippedText: String = regex.sub(originalText, "", true)
	
	var file: FileAccess = FileAccess.open(filePath, FileAccess.WRITE)
	file.store_string(strippedText)
	file.close()
	# TODO: catch and return errors?
	return OK
