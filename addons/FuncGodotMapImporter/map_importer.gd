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

	if (!source_file.begins_with("res://FuncGodotMaps")):
		return OK
	print("Importing (with func_godot) " + source_file)
	var filePath = "%s.%s" % [save_path, _get_save_extension()]
	
	var mapBuilder = FuncGodotMap.new()
	mapBuilder.block_until_complete = true
	mapBuilder.local_map_file = source_file
	mapBuilder.map_settings = ResourceLoader.load("res://FuncGodotAssets/FuncGodotMapSettings.tres")
	
	print("Calling verify_and_build()")
	mapBuilder.verify_and_build()
	print("verify_and_build() done.")
	
	for node in all_nodes_directly_in_scene(mapBuilder):
		# Set the map root as the owner---otherwise, it won't be saved to the
		# packed scene!
		node.owner = mapBuilder
	
	var scene = PackedScene.new()
	scene.pack(mapBuilder)

	var saveResult = ResourceSaver.save(scene, filePath)
	
	if (saveResult != OK):
		return saveResult
	return OK

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
