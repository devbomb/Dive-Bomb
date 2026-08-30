# Solid objects
Collision layers are named after what objects in that layer they _do_, NOT what
they _are_.

For example:
* Layer 1 is named `BlocksPlayer`.  Anything in that layer is considered solid
    to the player.  The player can stand on top of it, bonk into it, etc.

* Layer 2 is named `BlocksProjectiles`.  Anything in that layer is considered
    solid to non-player entities, such as projectiles, enemies, etc.

* Layer 3 is named `BlocksCamera`.  Anything in that layer causes the camera to
    zoom in if it comes between the camera and the player.

* Layer 32 is named `DetectedAsPlayer`.  Anything in that layer(hint: only the
    player) will trigger any Area3Ds that are looking for the player.


This naming scheme makes it easy to decide which layers an object should go in,
and which layers should be in its mask.  For example:

* Normal walls and floors should be in layers `BlocksPlayer`,
    `BlocksProjectiles`, and `BlocksCamera`.  They should not have anything in
    their mask, because they don't need to detect anything; everything else
    detects _them_.

* Transparent(glass) walls should be in layers `BlocksPlayer` and
    `BlocksProjectiles`.  They should _not_ be in `BlocksCamera`, because the
    camera is able to see through them, so there is no reason for the camera to
    zoom in to avoid them.

* Those thin black slots that moving glass panes go through(IE: from the
    tutorial) should be in layers `BlocksPlayer` and `BlocksCamera`.  They
    should NOT be in `BlocksProjectiles`, because the panes(which are entities)
    need to pass through them.

* The player (and ONLY the player) should have `BlocksPlayer` in their mask.
    That should also be the _only_ layer in their mask.

* Projectiles and mobile enemies should have `BlocksProjectiles` in their mask.

* The player (and _only_ the player) should be in the `DetectedAsPlayer` layer.

* Trigger volumes that are looking for the player should have `DetectedAsPlayer`
    in their mask.

# Beware: collision imported from .glb or .blend files
Collision shapes imported from .glb or .blend files default to having both a
layer and mask of "1"(`BlocksPlayer` under our naming scheme).  This means, if
left unchanged, such colliders will:
* Block the player (duh)
* _Not_ block projectiles or enemies
* _Not_ block the camera(triggering a zoom-in)
* Scan for other objects that block the player(for no real purpose)

This...isn't ideal.  Unfortunately, there's no way to change this default for
all objects.  This default is "good enough" if there won't be any projectiles
or enemies nearby(as is the case with the portals in the hub world), but any
other situation will require you to override the layers using the "editable
children" toggle.