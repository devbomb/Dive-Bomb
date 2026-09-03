# Unreleased
* **Breaking:** Changed the way levels are identified in save files.  This means
    all save files from previous versions of the game are now incompatible and
    cannot be loaded.
    * **Personal note:** The game is still in alpha, so you can expect further
        compatibility-breaking updates in the future.  This isn't much of a loss
        right now, since the game is so short that losing your save isn't a big
        deal.  When the game starts getting longer (and starts reaching more
        people), I'll start looking into ways to automatically update/migrate
        save files from old versions.  For now, though, don't get too attached
        to your 20 minutes of progress ;D

* **Changed:** Save files now have a "format version" number embedded in them,
    so the game can tell if a save file is compatible with the current version
    of the game.

* **Fixed:** If an incompatible or corrupted save file exists, it will no longer
    completely destroy the save management menu.  It will instead label that
    save as either "incompatible" or "toast", and give you the opportunity to
    delete it.

* **Changed:** Completely refactored the collision layers for various objects.
    This should make it easier for me to decide which collision layer any
    particular object should go in.
    * Note: This _shouldn't_ have had any noticable effect on the game from a
        player's perspective, but there's a nonzero chance that I've messed
        something up in the process.  If something seems broken, you can report
        any bugs you find on the itch.io page, the discord server, or the github
        issues page.

* **Fixed:** Fixed the player being unable to use the mouse to override
    "suggested" camera angles.
    [(issue)](https://github.com/devbomb/Dive-Bomb/issues/6)

# [0.0.3]
* **Fixed:** Fixed music not playing in time trial mode
    [(issue)](https://github.com/devbomb/Dive-Bomb/issues/1)

* **Fixed:** Fixed the wrong music playing when loading a save file during
    the tutorial's escape sequence
    [(issue)](https://github.com/devbomb/Dive-Bomb/issues/2)

* **Fixed:** Fixed the door being incorrectly locked behind you when loading a
    save file during the tutorial's escape sequence
    [(issue)](https://github.com/devbomb/Dive-Bomb/issues/3)

* **Fixed:** Fixed the player not moving with conveyor belts while they recover
    from a bonk
    [(issue)](https://github.com/devbomb/Dive-Bomb/issues/4)

* **Fixed:** Fixed the Atlas showing levels you haven't visited yet, if you open
    it from a hub world
    [(issue)](https://github.com/devbomb/Dive-Bomb/issues/5)


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