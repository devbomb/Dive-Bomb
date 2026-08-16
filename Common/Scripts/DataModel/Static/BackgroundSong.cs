using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class BackgroundSong : Resource
    {
        [Export] public string Title;
        [Export] public AudioStream Music;
        [Export] public float VolumeMultiplierLinear = 1f;

        [Export] public string Artist;
        [Export] public string ArtistLink;
        [Export] public bool ShowPopup = false;
    }
}