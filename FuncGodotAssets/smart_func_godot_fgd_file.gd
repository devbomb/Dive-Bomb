@tool
@icon("res://addons/func_godot/icons/icon_godot_ranger.svg")
class_name SmartFuncGodotFGDFile extends FuncGodotFGDFile

## If an entity definition resource's file name starts with this, it will
## automatically be included in this fgd file.
@export var entity_definiton_prefix: String = "entity_"

## Overridden from the base class.
## Instead of reading from the entity definitions property, it searches the file
## system for entity definition resources and returns those.
func get_fgd_classes() -> Array:
	var res : Array = super()
	for path in _get_matching_file_paths("res://"):
		var resource: Resource = ResourceLoader.load(path)
		if resource is FuncGodotFGDEntityClass:
			res.append(resource)
	return res

func _get_matching_file_paths(path: String) -> Array[String]:
	var file_paths: Array[String] = []
	var dir = DirAccess.open(path)
	dir.list_dir_begin()
	var file_name = dir.get_next()
	while file_name != "":
		var file_path = path + "/" + file_name
		if dir.current_is_dir():
			file_paths += _get_matching_file_paths(file_path)
		elif file_name.begins_with(entity_definiton_prefix) && file_name.ends_with(".tres"):
			file_paths.append(file_path)
		file_name = dir.get_next()
	return file_paths
