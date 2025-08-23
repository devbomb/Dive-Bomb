extends Area3D

@export var CycleId: String

var _safety_timer: int
var _started: bool

func _ready() -> void:
	$/root/SignalBus.connect("LevelReset", _reset)
	_reset()

func _reset() -> void:
	_safety_timer = 2
	_started = false

func _physics_process(_delta) -> void:
	# HACK: If the level resets while the player is inside the trigger, they will
	# still be consider "inside" the trigger for a few more frames.  To avoid
	# this, wait a few frames after the reset before checking.
	if (_safety_timer > 0):
		_safety_timer -= 1
		return

	if (!_started && !get_overlapping_bodies().is_empty()):
		_started = true
		$"/root/SignalBus".EmitCycleStarted(CycleId)
