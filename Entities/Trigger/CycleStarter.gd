extends Area3D

@export var CycleId: String

var _started: bool

func _ready() -> void:
	SignalBus.connect("LevelReset", _reset)
	_reset()

func _reset() -> void:
	_started = false

func _physics_process(_delta) -> void:
	if (!_started && !Area3DSafetyTimer.GetOverlappingBodies(self, 2).is_empty()):
		_started = true
		$"/root/SignalBus".EmitCycleStarted(CycleId)
