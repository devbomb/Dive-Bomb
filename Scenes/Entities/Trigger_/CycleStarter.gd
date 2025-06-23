extends Area3D

@export var CycleId: String

func _ready():
	connect("body_entered", _on_body_entered)

func _on_body_entered(body: Node3D) -> void:
	$"/root/SignalBus".EmitCycleStarted(CycleId)
