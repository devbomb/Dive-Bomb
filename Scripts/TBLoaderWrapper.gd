@tool
extends Node3D

var _tbLoader: TBLoader = TBLoader.new()
var _hackityHackHackDisableRefresh: bool = false

@export var refresh: bool:
    set(_val):
        _refresh()

@export_category("Map")
@export_file var map_resource: String
@export var map_inverse_scale: float = 16

@export_category("Entities")
@export var entity_common: bool = true
@export_dir var entity_path: String = "res://Scenes/Entities"

@export_category("Textures")
@export_dir var texture_path: String = "res://Textures"

func _ready():
    _refresh()

func _refresh():
    if (_hackityHackHackDisableRefresh):
        print("Refresh disabled")
        return
    build_meshes()

func build_meshes():
    # Temporarily remove the TBLoader node from the tree while it's building the
    # map.
    # If the TBLoader is inside the tree while the map is being built, then all
    # entities it spawns will have their _ready() event called before their
    # positions and rotations are set.  This would be a problem for entities
    # that need to know their starting position during _ready() (IE: to save
    # their respawn point).
    if (_tbLoader.get_parent() != null):
        remove_child(_tbLoader)
    
    _tbLoader.map_resource = map_resource
    _tbLoader.map_inverse_scale = map_inverse_scale
    _tbLoader.entity_common = entity_common
    _tbLoader.entity_path = entity_path
    _tbLoader.texture_path = texture_path
    
    _tbLoader.build_meshes()
    add_child(_tbLoader)
