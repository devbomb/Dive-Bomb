using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class FairyDoor : StaticBody3D
    {
        [Export] public string FairyId;
        [Export] public Node3D Model;
        [Export] public CollisionShape3D CollisionShape;
        [Export] public Area3D SoftlockFailsafeTrigger;

        private FairyJar _fairy;
        private readonly StateMachine _stateMachine = new();

        public FairyDoor()
        {
            AddChild(_stateMachine);
        }

        public override void _Ready()
        {
            Callable.From(() =>
            {
                if (string.IsNullOrEmpty(FairyId))
                    throw new Exception("Fairy door doesn't have a FairyId");

                _fairy = GetTree().Root
                    .EnumerateDescendantsOfType<FairyJar>()
                    .Where(f => !string.IsNullOrEmpty(f.FairyId))
                    .FirstOrDefault();

                if (_fairy == null)
                    throw new Exception($"Could not find a fairy with id {FairyId}");

                SignalBus.Instance.LevelReset += Reset;
                Reset();
            }).CallDeferred();
        }

        private void Reset()
        {
            if (IsFairyFree())
                _stateMachine.ChangeState<Open>();
            else
                _stateMachine.ChangeState<Closed>();
        }

        private bool IsFairyFree()
        {
            return this.GetLevel()
                ?.GetProgress()
                ?.CollectedFairies
                ?.Contains(_fairy.SaveKey) ?? false;
        }

        private class Closed : State<FairyDoor>
        {
            public override void OnStateEntered()
            {
                Self.SoftlockFailsafeTrigger.BodyEntered += OnSoftlockFailsafeTriggerEntered;
            }

            public override void OnStateExited()
            {
                Self.SoftlockFailsafeTrigger.BodyEntered -= OnSoftlockFailsafeTriggerEntered;
            }

            public override void _PhysicsProcess(double delta)
            {
                if (Self.IsFairyFree())
                    ChangeState<Opening>();
            }

            private void OnSoftlockFailsafeTriggerEntered(Node3D body)
            {
                ChangeState<Opening>();
            }
        }

        private class Opening : State<FairyDoor>
        {
            private const double Duration = 0.5;

            private Vector3 _startPos;
            private Vector3 _targetPos;
            private double _timer;

            public override void OnStateEntered()
            {
                var shape = (BoxShape3D)Self.CollisionShape.Shape;
                float height = shape.Size.Y;

                _startPos = Self.GlobalPosition;
                _targetPos = _startPos + (Vector3.Down * height);
                _timer = 0;
            }

            public override void OnStateExited()
            {
                Self.GlobalPosition = _startPos;
                Self.ResetPhysicsInterpolation3D();
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;
                float t = (float)(_timer / Duration);
                t *= t;

                Self.GlobalPosition = _startPos.Lerp(_targetPos, t);

                if (_timer >= Duration)
                    ChangeState<Open>();
            }
        }

        private class Open : State<FairyDoor>
        {
            public override void OnStateEntered()
            {
                Self.Model.Visible = false;
                Self.CollisionShape.Disabled = true;
            }

            public override void OnStateExited()
            {
                Self.Model.Visible = true;
                Self.CollisionShape.Disabled = false;
            }
        }
    }
}