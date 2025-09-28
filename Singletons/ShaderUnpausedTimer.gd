@tool
extends Node

var _unpaused_time: float = 0

func _process(delta: float) -> void:
	_unpaused_time += delta
	RenderingServer.global_shader_parameter_set("UNPAUSED_TIME", _unpaused_time)
