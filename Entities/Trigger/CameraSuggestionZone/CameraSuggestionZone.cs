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

        private class SuggestingAngle(CameraSuggestionZone Owner) : PlayerCamera.CustomState
        {
            protected override float TransitionDuration => 0.5f;
            private SphereCoords _initialOrbitCoords;

            public override void OnStateEntered()
            {
                _initialOrbitCoords = Self.OrbitCoordinates;
                base.OnStateEntered();
            }

            public override void OnStateExited()
            {
                // Prevent the camera from suddenly snapping when going to the
                // Following state.
                // TODO: Refactor so this isn't necessary
                Camera.DetectAnglesAndDistance();
                base.OnStateExited();
            }

            protected override Transform3D GetCustomPosition()
            {
                var suggestedCoords = new SphereCoords(
                    Mathf.DegToRad(Owner.SuggestedYawDeg),
                    Mathf.DegToRad(Owner.SuggestedPitchDeg),
                    Owner.SuggestedDistance
                );

                return Camera.PositionFromOrbitCoords(suggestedCoords);
            }

            protected override Transform3D Transition(Transform3D startPos, Transform3D currentPos, float t)
            {
                t = MathUtils.LerpSinusoidal(0, 1, t);

                var currentOrbitCoords = Camera.OrbitCoordsFromPosition(currentPos.Origin);
                var tweenedOrbitCoords = _initialOrbitCoords.Lerp(currentOrbitCoords, t);
                return Camera.PositionFromOrbitCoords(tweenedOrbitCoords);
            }

            public override void OnOrbitRequested(float deltaYawRad, float deltaPitchRad)
            {
                Camera.StartFollowing();
            }
        }

    }
}