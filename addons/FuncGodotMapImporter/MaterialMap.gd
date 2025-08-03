@tool
class_name MaterialMap extends Resource

@export var inherited_material_maps: Array[MaterialMap] = []
@export var texture_to_material: Dictionary[Texture2D, Material] = {}

func has_material_for(texture: Texture2D) -> bool:
	return get_material_for(texture) != null

## Returns the material that this texture should be mapped to, or null if no
## such material could be found.
func get_material_for(texture: Texture2D) -> Material:
	
	if texture_to_material.has(texture):
		return texture_to_material[texture]
	
	# Check all of the other material maps in reverse order, such that later
	# maps override eariler ones
	var other_maps_count = inherited_material_maps.size()
	for i in range(other_maps_count):
		var other_map: MaterialMap = inherited_material_maps[other_maps_count - i - 1]
		var material: Material = other_map.get_material_for(texture)
		if material != null:
			return material
	
	return null
