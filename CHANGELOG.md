# [Unreleased]
* **Added:** A change log.  Wow!

* **Changed:** Under-the-hood changes for how textures and materials are applied
    to level geometry when importing level files.  We now use FuncGodot's
    built-in functionality for replacing textures with materials, instead of our
    own custom solution.  You shouldn't notice any difference.

* **Changed:** It is now possible to perform a "bound jump"(this game's term for
    what Mario 64 calls a "double jump") from a standstill.  You previously
    needed to have a tiny amount of horizontal speed for it to trigger, which
    made some tricks unnecessarily difficult to set up.

* **Fixed:** Fixed the player incorrectly gaining small amounts of horizontal
    speed when jumping and landing, even if you weren't touching the stick.

* **Fixed:** Fixed a texture in the tutorial level that was missing its normal
    map.  It should now look shinier and less flat.

* **Fixed:** Fixed the wrong background music playing if you die or reload a
    checkpoint during the tutorial level's escape sequence.

# [0.0.1] - Open Source release
This was the first verison of the game that was made public.
As such, there is no "previous" version to compare to.