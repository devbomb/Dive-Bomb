extends Decal

@export_file var TexturePath: String = ""
@export var SizeX: float = 1
@export var SizeY: float = 1
@export var SizeZ: float = 1

func _ready():
	size = Vector3(SizeX, SizeY, SizeZ)
	texture_albedo = load(TexturePath)
