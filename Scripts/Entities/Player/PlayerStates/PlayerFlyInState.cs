using Godot;

namespace FastDragon
{
    public partial class PlayerFlyInState : PlayerState
    {
        public override void OnStateEntered()
        {
            Vector3 endPos = _player.GlobalPosition;
            Vector3 endRotRad = _player.GlobalRotation;

            _player.Animator.Play("Glide", 0);
            _player.AllowInterpolation = false;

            _player.GlobalPosition += Vector3.Up * _player.FlyInHeight;
            _player.GlobalPosition -= _player.GlobalForward() * _player.FlyInDistance;
            _player.GlobalRotation = Vector3.Zero;
            _player.ResetPhysicsInterpolation();

            _player.Camera.ChangeState<OrbitCameraLockedState>();
            _player.Camera.OrbitDistance = PortalLoadingScreen.CameraDist;
            _player.Camera.OrbitYawRad = PortalLoadingScreen.CameraYawRad;
            _player.Camera.OrbitPitchRad = PortalLoadingScreen.CameraPitchRad;

            double duration = _player.FlyInDuration;
            var tween = CreateTween();
            tween.TweenInterval(0.5);   // HACK: Pause for a bit to hide the "frame hiccup"
            tween.TweenProperty(_player, "global_position", endPos, duration);
            tween.Parallel().TweenProperty(_player, "global_rotation", endRotRad, duration);
            tween.Parallel().TweenAngleRadSinusoidal(_player.Camera, "OrbitYawRad", endRotRad.Y, duration);
            tween.Parallel().TweenAngleRadSinusoidal(_player.Camera, "OrbitPitchRad", 0, duration);
            tween.TweenCallback(Callable.From(Finish));
        }

        private void Finish()
        {
            _player.AllowInterpolation = true;
            _player.Respawn();
        }
    }
}