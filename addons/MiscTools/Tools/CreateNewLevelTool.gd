@tool
class_name CreateNewLevelTool extends MiscTool

func get_text() -> String: return "Create new level"

func execute() -> void:
	var level_id: String = await MiscTools.prompt_string("Enter a level id(no spaces)")
	if (level_id.is_empty()):
		return
	
	_create_new_level(level_id, "res://Levels")
	
func _create_new_level(level_id: String, parent_folder: String) -> void:
	var level_folder: String = parent_folder.path_join(level_id)
	DirAccess.make_dir_absolute(level_folder)
	
	# Create a maps folder, along with a "hazardous waste containment"
	# folder for all of those pesky autosaves Trenchbroom likes to create.
	var maps_folder: String = level_folder.path_join("Maps")
	DirAccess.make_dir_absolute(maps_folder)
	
	var autosaves_folder: String = level_folder.path_join("Maps/autosave")
	DirAccess.make_dir_absolute(autosaves_folder)
	
	var gitignore_file: FileAccess = FileAccess.open(autosaves_folder.path_join(".gitignore"), FileAccess.WRITE)
	gitignore_file.store_string("*.map")
	gitignore_file.close()
	
	var gdignore_file: FileAccess = FileAccess.open(autosaves_folder.path_join(".gdignore"), FileAccess.WRITE)
	gdignore_file.close()
	
	# Create a placeholder skybox
	var environment_resource = Environment.new()
	ResourceSaver.save(environment_resource, level_folder.path_join(level_id + "Skybox.tres"))
	
	# Create the "official" scene
	var level_root = DiveBombLevel.new()
	level_root.name = level_id
	
	var environment_node = WorldEnvironment.new()
	level_root.add_child(environment_node)
	environment_node.owner = level_root
	environment_node.name = "WorldEnvironment"
	environment_node.environment = environment_resource
	
	var sun = DirectionalLight3D.new()
	level_root.add_child(sun)
	sun.owner = level_root
	sun.name = "DirectionalLight3D"
	sun.position.y = 7
	sun.rotation_degrees.x = -45
	
	# Save the scene
	var scene = PackedScene.new()
	scene.pack(level_root)
	ResourceSaver.save(scene, level_folder.path_join(level_id + ".tscn"))
	
	# Refresh the editor so the new folder can be seen
	EditorInterface.get_resource_filesystem().scan()
	
