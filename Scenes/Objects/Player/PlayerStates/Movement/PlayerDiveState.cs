using Godot;
using System.Collections.Generic;

namespace FastDragon
{
    public partial class PlayerDiveState : PlayerState
    {
        public override bool DisableCameraInput => _redirectTimer <= 0;

        private float _redirectTimer;
        private float _targetCameraYawRad;
        private float _startY;

        private List<IBreakable> _brokenObjects = new List<IBreakable>();
        private List<IBreakable> _unbrokenObjects = new List<IBreakable>();

        private MeshInstance3D _thuum => Self.GetNode<MeshInstance3D>("%DiveThuum");

        public override void OnStateEntered()
        {
            Self.Animator.Play("Dive");
            _thuum.Visible = true;

            Self.VSpeed = Player.Dive.InitialVSpeed;
            Self.FSpeed = Player.Dive.FSpeed;

            _redirectTimer = Player.Dive.RedirectTimeWindow;
            _targetCameraYawRad = Self.GlobalRotation.Y;

            _startY = Self.GlobalPosition.Y;
        }

        public override void OnStateExited()
        {
            ResetModelPitch();
            _thuum.Visible = false;
        }

        public override void _Process(double deltaD)
        {
            float delta = (float)deltaD;

            AngleModelPitchWithVelocity(delta);

            if (_redirectTimer <= 0)
            {
                var camera = Self.Camera;

                camera.OrbitDistance = MathUtils.DecayToward(
                    camera.OrbitDistance,
                    Player.Dive.CameraDistance,
                    Player.Dive.CameraDecayRate,
                    delta
                );

                camera.OrbitYawRad = AngleMath.DecayToward(
                    camera.OrbitYawRad,
                    _targetCameraYawRad,
                    Player.Dive.CameraDecayRate,
                    delta
                );

                // Allow the camera to look down at the player if they've
                // fallen below the height that they started the dive at.
                var transform = camera.GlobalTransform;
                transform = transform.LookingAt(Self.GlobalPosition);
                float pitchRad = Self.GlobalPosition.Y < _startY - 2
                    ? Mathf.Min(transform.Basis.GetEuler().X, Player.Dive.CameraPitchRad)
                    : Player.Dive.CameraPitchRad;

                camera.OrbitPitchRad = AngleMath.DecayToward(
                    camera.OrbitPitchRad,
                    pitchRad,
                    Player.Dive.CameraDecayRate / 4,
                    delta
                );
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            _redirectTimer -= delta;
            if (_redirectTimer > 0)
            {
                float speed = Self.FSpeed;
                RotateInstantlyTowardLeftStick();
                Self.FSpeed = speed;
            }

            if (_redirectTimer > -0.1f)
                _targetCameraYawRad = Self.GlobalRotation.Y;

            RotateTowardLeftStick(Player.Dive.TurnSpeedRad, delta);
            RedirectFSpeedTowardYaw();

            ApplyGravity(delta, Player.Dive.Gravity);

            _brokenObjects.Clear();
            _unbrokenObjects.Clear();

            bool bonked = MoveAndSlideBreakingObjects<IBreakable>(
                isVulnerable: b => b.VulnerableToRoll,
                causesBonkWhenBroken: b => b.CausesBonk,
                brokenObjects: _brokenObjects,
                unbrokenObjects: _unbrokenObjects,
                delta
            );

            foreach (var b in _brokenObjects)
            {
                b.OnRolledInto();
                Break(b);
            }

            foreach (var b in _unbrokenObjects)
                b.OnBreakRejected();

            if (bonked)
                return;

            // Apply an extra-wide hitbox to catch objects that the player
            // barely grazes past without touching.
            // TODO: Don't apply the extra hitbox to objects that have already
            // been hit by the main hitbox
            ApplyHitboxToBreakableObjects(
                Self.DiveExtraHitbox,
                b => b.VulnerableToRoll,
                b => b.OnRolledInto()
            );

            if (Self.IsOnFloor())
            {
                Self.ChangeState<PlayerRollState>();
                return;
            }
        }

        private void Break(IBreakable b)
        {
            b.OnBroken();
            Self.Camera.Shake(
                b.CameraShakeMagnitude,
                b.CameraShakeFrequency,
                b.CameraShakeDuration
            );
        }
    }
}