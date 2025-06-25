@tool
class_name TextureSaver extends Node

@export var texture: Texture2D
@export var fileName: String
@export_tool_button("Save") var saveButton = save

func save():
	texture.get_image().save_png(fileName)
