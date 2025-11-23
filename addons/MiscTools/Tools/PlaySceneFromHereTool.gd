@tool
class_name PlaySceneFromHereTool extends MiscTool

func get_text() -> String: return "Play scene from here"

func execute() -> void:
	var camera = EditorInterface.get_editor_viewport_3d().get_camera_3d()
	print("scene: " + EditorInterface.get_edited_scene_root().scene_file_path)
	print("camera pos: " + str(camera.global_position))
	print("camera rot: " + str(camera.global_rotation))
	
	ProjectSettings.set_setting("temp/play_from_here/scene", EditorInterface.get_edited_scene_root().scene_file_path)
	ProjectSettings.set_setting("temp/play_from_here/pos", camera.global_position)
	ProjectSettings.set_setting("temp/play_from_here/yawRad", camera.global_rotation.y)
	ProjectSettings.save()
	
	EditorInterface.play_current_scene()
	await EditorInterface.get_edited_scene_root().get_tree().create_timer(1).timeout

	ProjectSettings.clear("temp/play_from_here/scene")
	ProjectSettings.clear("temp/play_from_here/pos")
	ProjectSettings.clear("temp/play_from_here/yawRad")
	ProjectSettings.save()
