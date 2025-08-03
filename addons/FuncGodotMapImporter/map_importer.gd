@tool
extends EditorImportPlugin

enum Presets { DEFAULT }

func _get_importer_name() -> String:
	return "func_godot_map_importer"

func _get_visible_name() -> String:
	return "Func Godot Map Importer"

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
	print("Importing (with func_godot) " + source_file)
	
	var mapBuilder = FuncGodotMap.new()
	mapBuilder.block_until_complete = true
	mapBuilder.local_map_file = source_file
	mapBuilder.map_settings = preload("res://FuncGodotAssets/FuncGodotMapSettings.tres")
	
	print("Calling verify_and_build()")
	mapBuilder.verify_and_build()
	print("verify_and_build() done.")

	var mapRoot = Node3D.new()
	mapRoot.name = get_root_node_name(source_file)

	move_children(mapBuilder, mapRoot)
	fix_duplicate_node_names(mapRoot)

	for node in all_nodes_directly_in_scene(mapRoot):
		# Set the map root as the owner---otherwise, it won't be saved to the
		# packed scene!
		node.owner = mapRoot
		
		# Map textures to their materials based on the provided map
		if node is MeshInstance3D && !is_root_of_another_scene(node):
			replace_materials(node, options.texture_material_map)

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

func replace_materials(meshInstance: MeshInstance3D, textureToMaterial: MaterialMap):
	for surfaceIndex in meshInstance.mesh.get_surface_count():
		var surfaceMaterial: Material = meshInstance.mesh.surface_get_material(surfaceIndex)
		if !(surfaceMaterial is StandardMaterial3D):
			continue

		var texture: Texture2D = surfaceMaterial.albedo_texture
		if texture == null:
			continue
		
		if !textureToMaterial.has_material_for(texture):
			continue

		var replacementMaterial: Material = textureToMaterial.get_material_for(texture)
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
