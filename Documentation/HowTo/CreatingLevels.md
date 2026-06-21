# Overview
Before you can create levels, you need to make sure Trenchbroom is set up to
work with Dive Bomb.  See [Setting up Trenchbroom and func_godot](#setting-up-trenchbroom-and-func_godot).

After you've done that, you can create a new blank level by clicking
`Project -> Tools -> Dive Bomb: Create new level`.  That will run an editor
plugin to create all of the required files for a blank level.

A Dive Bomb level consists of:
* A [folder](#creating-the-level-folder) containing the following files:
    * A [.map file](#creating-the-map-file) created in Trenchbroom, which contains
        the bulk of the level's contents, including level geometry, gem/enemy
        placements, the player's spawn point, checkpoints, etc.

    * A [.tscn file](#creating-the-tscn-file), which acts as the "official" scene
        for the level.  It contains an
        [instance](#how-it-works-funcgodotmapimporter) of the map file, along
        with a few Godot-specific nodes that are inconvenient to create in
        Trenchbroom.

    * A [skybox](#creating-the-skybox), which is used both in the level itself and
        as a crucial part of the seamless loading screen effect.

    * Any other level-specific art assets that are only used here

* A [portal](#creating-a-portal) leading to the level from one of the
    home worlds.

# Setting up Trenchbroom and func_godot
1. Install Trenchbroom.  Dive Bomb requires version 2025.2 or later.
1. Create an empty folder at the path `<trenchbroom install folder>/games/FastDragon`
1. Clone this repo
1. In the [func_godot local config](https://func-godot.github.io/func_godot_docs/FuncGodot%20Manual/FuncGodot%20Manual.html),
    make these changes:
    * Set "Fgd Output Folder" to `<trenchbroom install folder>/games/FastDragon`
    * Set "Trenchbroom Game Config Folder" to `<trenchbroom install folder>/games/FastDragon`
    * Set "Map Editor Game Path" to the root of this repo
    * Click "Export Func Godot Settings"
    > **Don't worry:** the absolute file paths you entered into this resource
        will NOT be saved into the git repo!  The properties on this resource
        are "fake"; they get diverted into a JSON file located elsewhere on your
        computer instead of getting serialized here.
1. In Godot, open the file `res://FuncGodotAssets/TrenchBroomConfig.tres` and
    click "Export file".
    * When you click it, you'll find that some
        files have been generated in `<trenchbroom install folder>/games/FastDragon`.
        These files give Trenchbroom metadata needed for displaying entities
        specific to this game.
    * You will need to repeat this step every time a new entity type is added to
        the game(or when an existing entity type is changed).
1. In Trenchbroom's preferences, set "FastDragon"'s "Game Path" to the root
    of this repo.

# Creating the level folder
Create a folder at `res://Levels/Production/<level-id>`.  This is where the
[.tscn file](#creating-the-tscn-file) will live.  It's also where you should put
art assets that are specific to this level, such as the skybox, custom materials,
or background music.

> TIP: The `Levels/Production` folder is for "real" levels that are actually
meant to be playable.  "Test" levels should go in the `Levels/Debug` folder
instead.

# Creating the .map file
First, ensure the following files exist:
* `res://Levels/<level-id>/Maps/autosave/.gdignore`: must be empty
* `res://Levels/<level-id>/Maps/autosave/.gitignore` must contain "*.map"

Then, in Trenchbroom, create a new .map file(using "FastDragon" as the game) and 
then save it to `res://Levels/<level-id>/Maps/<level-id>.map`.

> NOTE: You should only save maps into a folder with _this_ specific setup, to 
ensure Trenchbroom's not-so-helpful autosaves are properly 
gitignored and gdignored.

This is where you'll create the level geometry and place entities.  The most
common entities you'll use are:
* `transport_player_spawn`: the player's spawn point.  Every level needs exactly
    one.

* `transport_level_exit`: the level exit.  Levels without one of these
    cannot be played in Time Trial mode.

* `transport_checkpoint`: A checkpoint.  Each checkpoint needs to be given a
    unique name so it can be identified in the save file.  If two checkpoints
    have the same name, Bad Things (tm) will happen.

* `hazard_death_barrier`: A trigger volume that triggers a "falling death" when
    the player touches it.  Place a giant one underneath the level to prevent
    the player from falling for eternity.  You can also place smaller ones
    inside individual bottomless pits.

* `item_fairy`: The McGuffin of this game.  Every level should have about 5 of 
    them for the player to rescue, with one of them being near the exit.

* `item_gem_red`, `item_gem_green`, etc: the "coins" of this game.  These should
    be scattered all over the place.  Each gem color has a different value.  The
    total value of all the gems in the level should be a multiple of 100,
    because that looks nice in the inventory screen.
    * Note: Gems count toward the player's completion percentage, so please 
        don't be evil about where you hide them.  Nobody wants to go back and
        hunt for that one lone gem you hid in the most remote corner of the 
        world.

* `item_basket` and `item_vase`: Breakable objects that contain gems.

* `decor_wall_torch`: A torch you can place on a wall.  This is the only light
    source you should place from Trenchbroom; any other lighting should be done
    in the Godot editor.
    * Note: Wall torches also act as this game's "yellow paint" equivalent.  If
        players are missing something that you think should be obvious, place a
        wall torch there to subtly draw their attention to it.

There is no need to "compile" or "export" maps you make; Dive Bomb uses a 
[custom plug-in](#how-it-works-funcgodotmapimporter) to automatically import 
.map files in a manner similar to .blend files, so godot will treat them just
like any other scene.

# Creating the .tscn file
Trenchbroom is great for creating level geometry and placing entities, but there
are some things that are easier to do in the Godot editor.  For that reason the
"official" scene for each level is a plain old .tscn file.  The .tscn file
contains these key nodes:
* An [instance](#how-it-works-funcgodotmapimporter) of the map you created
    in Trenchbroom
* A `DirectionalLight3D` that acts as this level's "sun"
* A `WorldEnvironment` that sets the level's [skybox](#creating-the-skybox)
* Any additional manually-placed lights or particle effects.

> Not sure what should go in the .map and which should go in the .tscn?
For the most part, almost everything should be in the .map.  Only put something
directly in the .tscn if one of these is true:
> * It can't be previewed in Trenchbroom(like lights or particles) and needs to be
    tweaked frequently
> * It can't be easily edited in Trenchbroom(like the path of a moving platform)
> * It doesn't need to be moved if the level geometry moves

# Creating the skybox
Every level needs a skybox, _even indoor levels_.  This is because the seamless
loading screen uses...well, the skybox.  Create an `Environment` asset and save
it to `res://Levels/<level-id>/Skybox.tres`.  You should then use it as the
parameter in the `.tscn` file's `WorldEnvironment` node.

# Creating a portal
In the home world's .map file, place a `Transport_/StandardPortal` entity
with the following parameters:
* `SkyboxEnvironment`: the path to the `.tres` file of the `Environment` asset
    you created in the last step.  Must start with "res://"
* `TargetLevel`: the path to the `.tscn` file for this level.  Must start with
    "res://"
* `Text`: the text that floats in front of the portal.  Should be the same as
    the level's name(as defined in the PlayerSpawn's parameters), but this is
    not enforced.

# How it works: FuncGodotMapImporter

When creating levels for Dive Bomb, there is no need to click a "build" button
to convert your map to a `.tscn`.  Instead, the map files get automatically
imported in the same way `.blend` files are.  This avoids the need to store
duplicate, possibly-conflicting information in the git repo.

This is accomplished by combining
[func_godot](https://func-godot.github.io/func_godot_docs/FuncGodot%20Manual/FuncGodot%20Manual.html) 
with a custom-made [import plugin](https://docs.godotengine.org/en/stable/tutorials/plugins/editor/import_plugins.html) 
called "FuncGodotMapImporter".  FuncGodotMapImporter invokes func_godot to 
convert the map file to a tree of Godot nodes, does some post-processing to 
inject custom materials and work around some bugs, and then saves the result 
in the `.godot` folder.

This means you can drag and drop a `.map` file into a scene, just like you can
with a `.blend` file.

## Tweaks were made to func_godot to make this work
This project uses a modified version of [this release](https://github.com/func-godot/func_godot_plugin/releases/tag/2025.12)
of func_godot.  These were small modifications needed to work around some bugs.
Namely:

* When generating trenchbroom entity models, it now removes any `AnimationPlayer`
    nodes from the scene it's converting to gltf.  This is to work around a
    Godot bug where `GLTFDocument.append_from_scene()` will sometimes freeze
    the editor if you pass a node into it that contains an `AnimationPlayer`.
    > TODO: Submit a GitHub issue to the Godot repo after creating a minimal
        reproduction project.

* When building maps in the Quake format, flip UVs on the y-axis.  This
    fixes a bug in this version of func_godot that causes UVs to be facing the
    wrong way.  See https://github.com/func-godot/func_godot_plugin/issues/161
