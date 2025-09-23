using System;
using Godot;

namespace FastDragon
{
    public class PlayerSafeGround
    {
        /// <summary>
        /// The location the player will teleport to if they fall in water
        /// </summary>
        public SafeGroundPos LastSafeGround;
        public struct SafeGroundPos
        {
            public Transform3D PlayerPos;
            public float CameraYawRad;
            public float CameraPitchRad;
        }

        private readonly Player _player;

        public PlayerSafeGround(Player player)
        {
            _player = player;
        }

        public void SetLastSafeGroundHere()
        {
            LastSafeGround = new SafeGroundPos
            {
                PlayerPos = _player.GlobalTransform,
                CameraYawRad = _player.Camera.OrbitYawRad,
                CameraPitchRad = _player.Camera.OrbitPitchRad
            };
        }

        public void ReturnToLastSafeGround()
        {
            _player.GlobalTransform = LastSafeGround.PlayerPos;
            _player.ResetPhysicsInterpolation3D();
            _player.Velocity = Vector3.Zero;
            _player.ChangeState<PlayerStandState>();

            if (!_player.Camera.IsBeingManhandled)
            {
                _player.CameraFocus.Reset();

                _player.Camera.OrbitYawRad = LastSafeGround.CameraYawRad;
                _player.Camera.OrbitPitchRad = LastSafeGround.CameraPitchRad;
                _player.Camera.StartFollowing();
                _player.Camera.ResetPhysicsInterpolation3D();
            }
        }

        /// <summary>
        /// Sets the player's last safe pos to here, if they're currently
        /// standing on solid ground that isn't flagged as "unsafe".
        ///
        /// Call this after MoveAndSlide() if the current state is one where
        /// you're comfortable updating it.
        /// </summary>
        public void UpdateLastSafeGroundPos()
        {
            var collision = new KinematicCollision3D();
            bool onGround = _player.TestMove(
                _player.GlobalTransform,
                -_player.UpDirection * 0.1f,
                collision
            );

            if (!onGround)
                return;

            if (collision.GetCollider() is not StaticBody3D ground)
                return;

            if (ground is AnimatableBody3D)
                return;

            if (ground.ConstantLinearVelocity != Vector3.Zero)
                return;

            if (ground.ConstantAngularVelocity != Vector3.Zero)
                return;

            // Don't consider the ground "safe" if the normal vector is too
            // sloped.  This prevents the player from being respawned at the
            // very very edge of a platform(which would just lead to them
            // falling again)
            if (collision.GetAngle() > Mathf.DegToRad(45))
                return;

            bool isUnsafe = ((Node)collision.GetCollider()).IsInGroup("UnsafeGround");
            if (isUnsafe)
                return;

            SetLastSafeGroundHere();
        }
    }
}