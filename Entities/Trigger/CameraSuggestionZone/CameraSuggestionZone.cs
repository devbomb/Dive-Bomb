using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class CameraSuggestionZone : Area3D
    {
        [Export] public float SuggestedYawDeg = 0;
        [Export] public float SuggestedPitchDeg = 0;
        [Export] public float SuggestedDistance = 6;

        [Export] public string target;

        private readonly SuggestingAngle _cameraState;
        private class SuggestingAngle(CameraSuggestionZone Owner) : PlayerCamera.CustomState
        {
            private const float Duration = 0.5f;

            private float _timer;
            private SphereCoords _initialOrbitCoords;

            public override void OnStateEntered()
            {
                _timer = 0;
                _initialOrbitCoords = Self.OrbitCoordinates;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                // Move the camera to the suggested angle
                _timer += (float)deltaD;

                float t = _timer / Duration;
                t = Mathf.Min(1, t);
                t = MathUtils.LerpSinusoidal(0, 1, t);

                var suggestedCoords = new SphereCoords(
                    Mathf.DegToRad(Owner.SuggestedYawDeg),
                    Mathf.DegToRad(Owner.SuggestedPitchDeg),
                    Owner.SuggestedDistance
                );

                Self.OrbitCoordinates = _initialOrbitCoords.Lerp(suggestedCoords, t);
                Self.ApplyAnglesAndDistance();
            }

            public override void OnOrbitRequested(float deltaYawRad, float deltaPitchRad)
            {
                Self.StartFollowing();
            }
        }

        public CameraSuggestionZone()
        {
            _cameraState = new(this);
        }

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
            BodyExited += OnBodyExited;

            Callable.From(() =>
            {
                if (string.IsNullOrEmpty(target))
                    return;

                var marker = this.FindNodeByTargetName<NamedMarker3D>(target);
                SuggestedYawDeg = marker.GlobalRotationDegrees.Y;
                SuggestedPitchDeg = marker.GlobalRotationDegrees.X;
            }).CallDeferred();
        }

        public void OnBodyEntered(Node3D body)
        {
            if (body is Player player && !player.Camera.IsUsingMouselook)
            {
                player.Camera.ChangeState(_cameraState);
            }
        }

        public void OnBodyExited(Node3D body)
        {
            if (body is Player player)
            {
                player.Camera.StartFollowing();
            }
        }
    }
}