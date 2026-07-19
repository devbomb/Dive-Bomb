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
			return []
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
	mapBuilder.local_map_file = source_file
	mapBuilder.map_settings = preload("res://FuncGodotAssets/FuncGodotMapSettings.tres")
	
	print("Calling build()")
	mapBuilder.build()
	print("build() done.")

	var mapRoot = Node3D.new()
	mapRoot.name = get_root_node_name(source_file)

	move_children_and_replace_owners(mapBuilder, mapRoot)
	fix_duplicate_node_names(mapRoot)
	
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

# Moves all the nodes from one scene to another, including updating their owners.
func move_children_and_replace_owners(src_scene: Node, dst_scene: Node):
	for child in src_scene.get_children():
		# Avoid this warning: Adding '<node name>' as child to '<dst_scene name>' will make owner
		# '' inconsistent. Consider unsetting the owner beforehand.
		var old_owner = child.owner
		child.owner = null 

		src_scene.remove_child(child)
		dst_scene.add_child(child)

		child.owner = dst_scene if (old_owner == src_scene) else old_owner
	
	# The for-loop updated the owners of the immediate children, but all of
	# the grandchildren (and so-on) still have src_scene as their owner.
	for descendant in all_descendant_nodes(dst_scene):
		if descendant.owner == src_scene:
			descendant.owner = dst_scene
	

func get_root_node_name(source_file: String):
	var parts = source_file.trim_prefix("res://").split("/")
	return parts[parts.size() - 1].trim_suffix(".map")

func is_root_of_another_scene(node: Node) -> bool:
	return node.scene_file_path != ""

func all_descendant_nodes(node: Node) -> Array[Node]:
	var array: Array[Node] = []
	for child in node.get_children():
		_all_descendant_nodes(child, array)
	return array

func _all_descendant_nodes(node: Node, array: Array[Node]):
	array.append(node)
	for child in node.get_children():
		_all_descendant_nodes(child, array)

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
