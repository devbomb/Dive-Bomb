using Godot;
using System.Collections.Generic;
using System.Linq;

namespace FastDragon
{
    public partial class PlayerDiveState : PlayerState
    {
        private const float ThuumFadeTime = 0.5f;

        public override bool DisableCameraInput => _redirectTimer <= 0;

        private float _redirectTimer;
        private float _targetCameraYawRad;
        private float _startY;

        private List<IBreakable> _brokenObjects = new();
        private List<IBreakable> _unbrokenObjects = new();
        private List<IBreakable> _detectedObjects = new();

        public override void OnStateEntered()
        {
            Self.Animator.Play("Dive");
            Self.DiveThuum.Visible = true;
            Self.DiveThuum.Transparency = 1;

            Self.VSpeed = Player.Dive.InitialVSpeed;
            Self.FSpeed = Player.Dive.FSpeed;

            _redirectTimer = Player.Dive.RedirectTimeWindow;
            _targetCameraYawRad = Self.GlobalRotation.Y;

            _startY = Self.GlobalPosition.Y;
        }

        public override void OnStateExited()
        {
            ResetModelPitch();
            Self.DiveThuum.Visible = false;
        }

        public override void _Process(double deltaD)
        {
            float delta = (float)deltaD;

            Self.DiveThuum.Transparency = Mathf.MoveToward(
                Self.DiveThuum.Transparency,
                0,
                delta / ThuumFadeTime
            );

            AngleModelPitchWithVelocity();

            if (_redirectTimer <= 0 && !Self.Camera.IsSuggestingAngle)
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
            _detectedObjects.Clear();

            MoveAndSlideBreakingObjects();

            foreach (var b in _brokenObjects)
            {
                b.OnRolledInto();
                Break(b);
            }

            foreach (var b in _unbrokenObjects)
                b.OnBreakRejected();

            // Apply an extra-wide hitbox to catch objects that the player
            // barely grazes past without touching.
            _detectedObjects.AddRange(_brokenObjects);
            _detectedObjects.AddRange(_unbrokenObjects);

            ApplyHitboxToBreakableObjects(
                Self.DiveExtraHitbox,
                _detectedObjects,
                b => b.VulnerableToRoll,
                b => b.OnRolledInto()
            );

            // It's possible for the objects hit by the hitbox to change the
            // current state(IE: because you bonked, or you freed a fairy).
            // We don't want to overwrite that state change if it happened right
            // as we hit the ground.
            if (!IsCurrent)
                return;

            if (Self.IsOnFloor())
            {
                Self.ChangeState<PlayerRollState>();
                return;
            }
        }

        private void MoveAndSlideBreakingObjects()
        {
            Vector3 prevPos = Self.GlobalPosition;
            Vector3 prevVel = Self.Velocity;
            Self.MoveAndSlideEx(OnCollision);

            int numCollisions = Self.GetSlideCollisionCount();
            if (_brokenObjects.Any(b => b.CausesBonk))
            {
                Self.ChangeState<PlayerBonkState>();
                return;
            }

            if (DeceleratedEnoughToBonk(prevVel, Self.Velocity))
            {
                // HACK: If a ledge is detected, move the player up to it instead
                // of bonking.  This is to reduce the amount of "WTF?  I bonked
                // on air?!" moments caused by the spherical collider not matching
                // up with the player model.
                var ledge = Self.LedgeDetector.DetectLedge();
                if (ledge.HasValue && !ledge.Value.IsClimbingPathBlocked)
                {
                    const float forgivableHeight = 0.6f;
                    float ledgeHeight = ledge.Value.LedgePoint.Y - Self.GlobalPosition.Y;
                    if (ledgeHeight < forgivableHeight && ledgeHeight >= 0)
                    {
                        var pos = Self.GlobalPosition;
                        pos.Y = ledge.Value.LedgePoint.Y;
                        Self.GlobalPosition = pos;
                        Self.Velocity = prevVel;
                        GD.Print($"Ledge detected; bonk forgiven (height: {ledgeHeight})");
                        return;
                    }
                    else
                    {
                        GD.Print($"Ledge detected, but bonk not forgiven (height: {ledgeHeight})");
                    }
                }

                Self.ChangeState<PlayerBonkState>();
                return;
            }
        }

        private MoveAndSlideExResponse OnCollision(KinematicCollision3D collision)
        {
            var hitObject = collision.GetCollider();

            if (hitObject is not IBreakable b)
                return MoveAndSlideExResponse.Slide;

            if (!b.VulnerableToRoll)
            {
                _unbrokenObjects.Add(b);
                return MoveAndSlideExResponse.Slide;
            }

            _brokenObjects.Add(b);

            if (b.CausesBonk)
            {
                return MoveAndSlideExResponse.Stop;
            }

            return MoveAndSlideExResponse.Ignore;
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

        private void RedirectFSpeedTowardYaw()
        {
            Vector3 vel = Self.GlobalForward() * Self.FSpeed;
            vel.Y = Self.VSpeed;
            Self.LocalVelocity = vel;
        }

        private void AngleModelPitchWithVelocity()
        {
            var rot = Self.LocalVelocity.Normalized().ForwardToEulerAnglesRad();
            Self.ModelPitchRad = rot.X;
        }
    }
}