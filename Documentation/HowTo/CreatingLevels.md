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

# Creating the .map file

# Creating the .tscn file

# Creating the skybox

# Creating a portal

# How it works: MapImporter and TBLoader