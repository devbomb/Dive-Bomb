using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class DivableWallFX : Node3D
    {
        private AudioStreamPlayer _shatterSound => GetNode<AudioStreamPlayer>("%ShatterSound");
        private GpuParticles3D _shatterPartciles => GetNode<GpuParticles3D>("%ExplosionParticles");

        public void Play()
        {
            _shatterSound.Play();

            _shatterPartciles.Restart();
            _shatterPartciles.Emitting = true;

            // Walls can be large.  If we always spawn the particles at the
            // origin point, it could be far away from where the player actually
            // hit, which would look weird.  Therefore, let's spawn the
            // particles at the player's position.
            var player = GetTree().FindNode<Player>();
            _shatterPartciles.GlobalPosition = player.GlobalPosition;
            _shatterPartciles.GlobalPosition += Vector3.Up;
        }

        public void Stop()
        {
            _shatterPartciles.Emitting = false;
        }
    }
}