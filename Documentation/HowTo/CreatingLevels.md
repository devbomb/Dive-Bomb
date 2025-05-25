# Overview
Before you can create levels, you need to make sure Trenchbroom is set up to
work with Dive Bomb.  See [Setting up Trenchbroom](#setting-up-trenchbroom).

A Dive Bomb level consists of:
* A [.map file](#creating-the-map-file) created in Trenchbroom, which contains
    the bulk of the level's contents, including level geometry, gem/enemy 
    placements, the player's spawn point, checkpoints, etc.

* A [.tscn file](#creating-the-tscn-file), which acts as the "official" scene
    for the level.  It contains an 
    [instance](#how-it-works-mapimporter-and-tbloader) of the map file, along 
    with a few Godot-specific nodes that are inconvenient to create in 
    Trenchbroom.

* A [skybox](#creating-the-skybox), which is used both in the level itself and
    as a crucial part of the seamless loading screen effect.

* A [portal](#creating-a-portal) leading to the level from one of the
    home worlds.

# Setting up Trenchbroom
1. Install Trenchbroom.  Dive Bomb requires version 2025.2 or later.
2. Clone this repo
3. Open the project in Godot, and then click 
    `Project -> Tools -> Generate Trenchbroom Entity Models`
    * You will need to repeat this any time a new entity type is added to the
        game.  Otherwise, you won't be able to see its model in Trenchbroom.
4. Install Dive Bomb's Trenchbroom config file.  There are two ways to do this:
    * **Beginner method** (easy to start, but less convenient long-term):
        * Copy the contents of  `<this repo>/addons/tbloader/tb-gameconfig/` to
            `<trenchbroom install folder>/games/FastDragon/`.
        * You will need to repeat the above any time a .fgd file changes; IE: if
            a new enemy is added to the game that you want to use.
    * **Reccommended method**(complicated at first, but easier long-term):
        * Create a symlink from `<trenchbroom install folder>/games/FastDragon`
            to `<this repo>/addons/tbloader/tb-gameconfig`
        * On Windows, you can use 
            [Link Shell Extension](https://schinagl.priv.at/nt/hardlinkshellext/linkshellextension.html) 
            to do this without opening a terminal.
        * On Linux or Mac, run the following command in a terminal:
            `ln -s <this repo>/addons/tbloader/tb-gameconfig <trenchbroom install folder>/games/FastDragon`
5. In Trenchbroom's preferences, set "FastDragon"'s "Game Path" to the root
    of this repo.

# Creating the .map file

In Trenchbroom, create a new .map file(using "FastDragon" as the game) and then
it to `<this-repo>/TrenchbroomMaps/`.

> NOTE: You should only save maps to _this_ specific folder, to ensure 
Trenchbroom's not-so-helpful autosaves are properly gitignored and gdignored.

This is where you'll create the level geometry and place entities.  The most
common entities you'll use are:
* `Transport_/PlayerSpawn`: the player's spawn point.  Every level needs exactly
    one.  There are a few required parameters:
    * `LevelName`: the name of this level.  This is what the level will be
        called in the inventory screen and in Time Trial mode.
    * `HomeWorldMap`: The path to the .tscn file of the level the player should
        return to when the exit is reached(or "exit level" is chosen in the
        pause menu).

* `Transport_/ReturnHomePlatform`: the level exit.  Levels without one of these
    cannot be played in Time Trial mode.

* `Transport_/Checkpoint`: A checkpoint.  Each checkpoint needs to be given a
    unique name so it can be identified in the save file.  If two checkpoints
    have the same name, Bad Things (tm) will happen.

* `Hazard_/DeathBarrier`: A trigger volume that triggers a "falling death" when
    the player touches it.  Place a giant one underneath the level to prevent
    the player from falling for eternity.  You can also place smaller ones
    inside individual bottomless pits.

* `Item_/Fairy`: The McGuffin of this game.  Every level should have about 5 of 
    them for the player to rescue, with one of them being near the exit.

* `Item_/RedGem`, `Item_/GreenGem`, etc: the "coins" of this game.  These should
    be scattered all over the place.  Each gem color has a different value.  The
    total value of all the gems in the level should be a multiple of 100,
    because that looks nice in the inventory screen.
    * Note: Gems count toward the player's completion percentage, so please 
        don't be evil about where you hide them.  Nobody wants to go back and
        hunt for that one lone gem you hit in the most remote corner of the 
        world.

* `Item_/Basket` and `Item_/Vase`: Breakable objects that contain gems.

* `Detail_/WallTorch`: A torch you can place on a wall.  This is the only light
    source you should place from Trenchbroom; any other lighting should be done
    in the Godot editor.
    * Note: Wall torches also act as this game's "yellow paint" equivalent.  If
        players are missing something that you think should be obvious, place a
        wall torch there to subtly draw their attention to it.

There is no need to "compile" or "export" maps you make; Dive Bomb uses a 
[custom plug-in](#how-it-works-mapimporter-and-tbloader) to automatically import 
.map files in a manner similar to .blend files, so godot will treat them just
like any other scene.

# Creating the .tscn file
Trenchbroom is great for creating level geometry and placing entities, but there
are some things that are easier to do in the Godot editor.  For that reason the
"official" scene for each level is a plain old .tscn file.  The .tscn file
contains these key nodes:
* An [instance](#how-it-works-mapimporter-and-tbloader) of the map you created
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

# Creating a portal

# How it works: MapImporter and TBLoader