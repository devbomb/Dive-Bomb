Sometimes, it makes sense to make the background music quieter without fully
muting it---EG: while a character is talking.  This is called "ducking".

One way to achieve this would be to find the `AudioStreamPlayer` node that's
playing the music and adjust its volume property, but that has several problems:

* There is no one single node responsible for playing background music.  It's
    _usually_ a node named `BackgroundMusicPlayer`, but that's not necessarily
    always going to be the case.  Its name could be different, or there could
    even be more than one.

* Even if the correct node could be located, its volume property will likely
    have already been tweaked in the scene editor(since that's easier than
    changing the volume in the mp3 file itself).  You'd need to store the
    pre-ducked volume value and then restore it when ducking is no longer
    needed, which then creates another version of the
    [pausing problem](../Technical/ThePausingProblem.md).

You _could_ try adjusting the `Music` bus's volume slider, but then you run
into the same pausing problem.

# The `MusicDuckingTrigger` bus
Instead, the recommended way to duck the music is to use the `MusicDuckingTrigger`
bus.  Simply play any sound on that bus, and the `Music` bus will automatically
get quieter as long as that sound is above a certain volume threshold.
`res://Common/Audio/SFX/WhiteNoise.mp3` is best for this, because it loops and
its volume is consistent.

This works because the `Music` bus has a `Compressor` effect, which is
side-chained to `MusicDuckingTrigger`.  The reason you don't actually hear the
white noise is because `MusicDuckingTrigger` outputs to the `NullSink` bus,
which has its volume permanently set to zero.