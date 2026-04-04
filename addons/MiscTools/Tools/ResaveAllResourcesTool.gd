@tool
class_name ResaveAllResourcesTool extends MiscTool

func get_text() -> String: return "Resave all scenes and resources"
func execute() -> void:
	
	print("resaving resources")
	var resource_paths: Array[String] = MiscTools.all_files_of_type(".tres")
	
	for file_path in resource_paths:
		print("resaving " + file_path)
		var resource: Resource = ResourceLoader.load(file_path)
		ResourceSaver.save(resource, file_path)
		print("saved")
	print("Done resaving resources")
	
	print("Resaving scenes")
	var scene_paths: Array[String] = MiscTools.all_files_of_type(".tscn")
	var initially_open_scenes: PackedStringArray = EditorInterface.get_open_scenes()
	
	for file_path in scene_paths:
		EditorInterface.open_scene_from_path(file_path)
		EditorInterface.save_scene()
		
		if !initially_open_scenes.has(file_path):
			EditorInterface.close_scene()
	print("Done resaving scenes")
