using Godot;
using System.Linq;

namespace FastDragon
{
    public partial class Gem : CharacterBody3D
    {
        public const float HomingDuration = 0.5f;

        [Export] public GemColor Value;

        public enum State
        {
            Hidden,
            Revealed,
            Collected,
            Homing
        }
        public State CurrentState = State.Revealed;

        private AnimationPlayer _animator => GetNode<AnimationPlayer>("%AnimationPlayer");

        private Vector3 _initialPos;
        private State _initialState;

        private Vector3 _homingStartPos;
        private float _homingTimer;

        public override void _Ready()
        {
            _initialPos = Position;
            _initialState = CurrentState;
            Reset();

            SignalBus.Instance.LevelReset += Reset;

            _animator.Seek(GD.Randf() * _animator.CurrentAnimationLength);
        }

        public void Reset()
        {
            Position = _initialPos;
            CurrentState = SaveFile.Current.CollectedGems.Contains(GetPath())
                ? State.Collected
                : _initialState;

            Velocity = Vector3.Zero;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            switch (CurrentState)
            {
                case State.Collected:
                case State.Hidden:
                {
                    Visible = false;
                    break;
                }

                case State.Revealed:
                {
                    Visible = true;
                    Velocity += Vector3.Down * 9.8f * delta;

                    var collision = MoveAndCollide(Velocity * delta);
                    if (collision != null)
                        Velocity = Vector3.Zero;

                    break;
                }

                case State.Homing:
                {
                    Visible = true;

                    _homingTimer += delta;
                    float t = _homingTimer / HomingDuration;

                    Vector3 start = _homingStartPos;
                    Vector3 end = GetPlayer().GlobalPosition + (Vector3.Up * 0.25f);
                    Vector3 control = GetTree().Root.GetCamera3D().GlobalPosition + (Vector3.Up * 3);

                    GlobalPosition = BezierCurve(start, end, control, t);

                    Vector3 forward = (GlobalPosition - control).Normalized();
                    Vector3 targetRot = forward.ForwardToEulerAnglesRad();

                    float decayRate = 5f;
                    GlobalRotation = new Vector3(
                        AngleMath.DecayToward(GlobalRotation.X, targetRot.X, decayRate, delta),
                        AngleMath.DecayToward(GlobalRotation.Y, targetRot.Y, decayRate, delta),
                        AngleMath.DecayToward(GlobalRotation.Z, targetRot.Z, decayRate, delta)
                    );

                    if (_homingTimer >= HomingDuration)
                    {
                        Collect();
                    }

                    break;
                }
            }
        }

        public void OnCollectionAreaBodyEntered(Node3D body)
        {
            if (body is Player && CurrentState == State.Revealed)
                StartHomingIn();
        }

        public void Reveal()
        {
            CurrentState = State.Revealed;
            Velocity = Vector3.Up * 10;

            GD.Print($"Revealed gem {GetPath()}");
        }

        public void StartHomingIn()
        {
            CurrentState = State.Homing;
            _homingStartPos = GlobalPosition;
            _homingTimer = 0;
        }

        public void Collect()
        {
            SaveFile.Current.TotalGemCount += (int)Value;
            SaveFile.Current.CollectedGems.Add(GetPath());
            CurrentState = State.Collected;

            GD.Print($"{SaveFile.Current.TotalGemCount}: Collected gem {GetPath()}");
        }

        private Vector3 BezierCurve(
            Vector3 start,
            Vector3 end,
            Vector3 control,
            float t
        )
        {
            var a = start.Lerp(control, t);
            var b = start.Lerp(end, t);
            return a.Lerp(b, t);
        }

        private Node3D GetPlayer()
        {
            var player = GetTree().Root
                .EnumerateDescendants()
                .First(n => n is Player);

            return (Node3D)player;
        }
    }
}