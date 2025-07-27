@tool
class_name MaterialMap extends Resource

@export var texture_to_material: Dictionary[Texture2D, Material] = {}

func has_material_for(texture: Texture2D) -> bool:
	return texture_to_material.has(texture)

func get_material_for(texture: Texture2D) -> Material:
	return texture_to_material[texture]
