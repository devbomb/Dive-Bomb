# [0.0.2]
* **Added:** A change log.  Wow!

* **Added:** Bonking now leaves behind an imprint of Special Agent D's face at
    the exact point of the collision.  Now you'll know exactly how close you
    were to making it up that ledge.

* **Changed:** Under-the-hood changes for how textures and materials are applied
    to level geometry when importing level files.  We now use FuncGodot's
    built-in functionality for replacing textures with materials, instead of our
    own custom solution.  You shouldn't notice any difference.

* **Changed:** It is now possible to perform a "bound jump"(this game's term for
    what Mario 64 calls a "double jump") from a standstill.  You previously
    needed to have a tiny amount of horizontal speed for it to trigger, which
    made some tricks unnecessarily difficult to set up.

* **Fixed:** Fixed the player bonking against breakable objects(such as vases)
    in very rare circumstances.  Hopefully.

* **Fixed:** Rolling off of a conveyor belt and onto stationary ground no longer
    causes you to bonk against the air.

* **Fixed:** Fixed the player incorrectly gaining small amounts of horizontal
    speed when jumping and landing, even if you weren't touching the stick.

* **Fixed:** Fixed the player being able to gain infinite height by wall jumping
    off of a wall after jumping off of a conveyor belt moving towards that wall.
    * This was happening because the player still kept the momentum from the
    conveyor belt even after jumping off of the wall, meaning your jump didn't
    take you as far away from the wall as it was supposed to.  This meant you
    could get back to the wall to jump again much quicker, allowing you to gain
    more height than you lost.

    * **Personal note**: I debated a lot about whether I should fix this or not.
        This is a game about speedrunning, and speedrunners _love_ this kind of
        bug!  What kind of signal would I be sending by fixing it?

        Ultimately, though, I decided that it was too easy to pull off for how
        potentially powerful it can be.  I'll try to think of ways I can
        reintroduce a nerfed version of this trick as an intended feature, but
        in the meantime, it had to go.

* **Fixed:** Fixed a texture in the tutorial level that was missing its normal
    map.  It should now look shinier and less flat.

* **Fixed:** Fixed the wrong background music playing if you die or reload a
    checkpoint during the tutorial level's escape sequence.

# [0.0.1] - Open Source release
This was the first verison of the game that was made public.
As such, there is no "previous" version to compare to.