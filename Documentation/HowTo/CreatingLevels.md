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

# Creating the .tscn file

# Creating the skybox

# Creating a portal

# How it works: MapImporter and TBLoader