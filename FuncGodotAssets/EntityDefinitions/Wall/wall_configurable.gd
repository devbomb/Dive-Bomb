@tool
extends StaticBody3D

static var _layer_name_to_index: Dictionary[String, int] = _get_layer_name_indices()

func _func_godot_apply_properties(entity_properties: Dictionary):
	_apply_collision_layer("BlocksPlayer", entity_properties)
	_apply_collision_layer("BlocksProjectiles", entity_properties)
	_apply_collision_layer("BlocksCamera", entity_properties)

func _apply_collision_layer(layer_name: String, entity_properties: Dictionary):
	var layer_index: int = _layer_name_to_index[layer_name]
	set_collision_layer_value(layer_index, entity_properties[layer_name])

static func _get_layer_name_indices() -> Dictionary[String, int]:
	var dict: Dictionary[String, int] = {}
	
	for i in range(0, 32):
		var setting_name: String = "layer_names/3d_physics/layer_" + str(i + 1)
		var layer_name: String = ProjectSettings.get_setting(setting_name)
		if layer_name != null && !layer_name.is_empty():
			dict[layer_name] = i + 1
			
	return dict
