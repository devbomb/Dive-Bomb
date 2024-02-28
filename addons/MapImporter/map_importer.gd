@tool
extends EditorImportPlugin

enum Presets { DEFAULT }

func _get_importer_name():
    return "map_importer"

func _get_visible_name():
    return "Map Importer"

func _get_priority():
    return 2

func _get_recognized_extensions():
    return ["map"]

func _get_save_extension():
    return "scn"

func _get_resource_type():
    return "PackedScene"

func _get_preset_count():
    return Presets.size()

func _get_preset_name(preset_index):
    match preset_index:
        Presets.DEFAULT:
            return "Default"
        _:
            return "Unknown"

func _get_import_options(path, preset_index):
    match preset_index:
        Presets.DEFAULT:
            return [
                {
                    "name": "map_inverse_scale",
                    "default_value": 16
                },
                {
                    "name": "entity_path",
                    "default_value": "res://Scenes/Entities"
                },
                {
                    "name": "texture_path",
                    "default_value": "res://Textures"
                }
            ]
        _:
            return []

func _get_import_order():
    return 100

func _get_option_visibility(path, option_name, options):
    return true

func _import(source_file, save_path, options, r_platform_variants, r_gen_files):
    print("Importing " + source_file)
    var tbLoader: TBLoader = TBLoader.new()
    
    tbLoader.map_resource = source_file
    tbLoader.entity_common = true
    tbLoader.map_inverse_scale = options.map_inverse_scale
    tbLoader.entity_path = options.entity_path
    tbLoader.texture_path = options.texture_path
    
    tbLoader.build_meshes()

    var mapRoot = Node3D.new()
    mapRoot.name = get_root_node_name(source_file)
    
    move_children(tbLoader, mapRoot)
    fix_duplicate_node_names(mapRoot)
    set_owner_of_descendants(mapRoot, mapRoot)
    
    var scene = PackedScene.new()
    scene.pack(mapRoot)
    return ResourceSaver.save(scene, "%s.%s" % [save_path, _get_save_extension()])

func set_owner_of_descendants(node: Node, newOwner: Node):
    for child in node.get_children():
        if child.owner != null:
            continue
        child.owner = newOwner
        set_owner_of_descendants(child, newOwner)

func fix_duplicate_node_names(node: Node):
    var nameCounts: Dictionary = {}
    for child in node.get_children():
        var originalName = child.name

        if nameCounts.has(originalName):
            child.name = originalName + str(nameCounts[originalName])
            nameCounts[originalName] += 1
        else:
            nameCounts[originalName] = 1

        fix_duplicate_node_names(child)

func move_children(src: Node, dst: Node):
    for child in src.get_children():
        src.remove_child(child)
        dst.add_child(child)

func get_root_node_name(source_file: String):
    var parts = source_file.trim_prefix("res://").split("/")
    return parts[parts.size() - 1].trim_suffix(".map")
