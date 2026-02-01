using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public static class OpenableWallNodeExtensions
    {
        public static OpenableWall GetOpenableWall(this Node node, string openableWallId)
        {
            return node.GetTree()
                .Root
                .EnumerateDescendantsOfType<OpenableWall>()
                .FirstOrDefault(m => m.OpenableWallId == openableWallId);
        }
    }

    public partial class OpenableWall : AnimatableBody3D, IPowerable
    {
        [Export] public string OpenableWallId;
        [Export] public string ClosedPosMarkerId;
        [Export] public string OpenPosMarkerId;

        [Export] public double OpenDuration = 0.5;
        [Export] public double CloseDuration = 0.5;

        private bool _initialized;
        private Vector3 _closedPos;
        private Vector3 _openPos;

        private readonly StateMachine _stateMachine = new();

        public OpenableWall()
        {
            AddChild(_stateMachine);
        }

        public override void _Ready()
        {
            this.SetPhysicsInterpolated(true);

            Callable.From(() =>
            {
                var closedMarker = this.GetNamedMarker3D(ClosedPosMarkerId);
                var openMarker = this.GetNamedMarker3D(OpenPosMarkerId);

                Vector3 diff = openMarker.GlobalPosition - closedMarker.GlobalPosition;
                _closedPos = GlobalPosition;
                _openPos = _closedPos + diff;

                _initialized = true;
            }).CallDeferred();
        }

        string IPowerable.Id => OpenableWallId;

        void IPowerable.SetPowered(bool powered)
        {
            if (powered)
                StartOpening();
            else
                StartClosing();
        }

        void IPowerable.ForceSetPowered(bool powered)
        {
            if (powered)
                InstantOpen();
            else
                InstantClose();
        }


        public void StartOpening()
        {
            _stateMachine.ChangeState<Opening>();
        }

        public void StartClosing()
        {
            _stateMachine.ChangeState<Closing>();
        }

        public void InstantOpen() => DeferIfUninitialized(() =>
        {
            _stateMachine.ChangeState<Open>();
        });

        public void InstantClose() => DeferIfUninitialized(() =>
        {
            _stateMachine.ChangeState<Closed>();
        });

        private void DeferIfUninitialized(System.Action action)
        {
            if (!_initialized)
            {
                GD.PushWarning($"OpenableWall {OpenableWallId} isn't initialized yet.  Deferring call.");
                Callable.From(action).CallDeferred();
            }
            else
            {
                action();
            }
        }

        private class Closed : State<OpenableWall>
        {
            public override void OnStateEntered()
            {
                Self.GlobalPosition = Self._closedPos;
                Self.ResetPhysicsInterpolation3D();
            }
        }

        private class Closing : State<OpenableWall>
        {
            public override void _PhysicsProcess(double delta)
            {
                float speed = Self._closedPos.DistanceTo(Self._openPos) / ((float)Self.CloseDuration);
                Self.GlobalPosition = Self.GlobalPosition.MoveToward(Self._closedPos, speed * (float)delta);

                if (Self.GlobalPosition.IsEqualApprox(Self._closedPos))
                    ChangeState<Closed>();
            }
        }

        private class Open : State<OpenableWall>
        {
            public override void OnStateEntered()
            {
                Self.GlobalPosition = Self._openPos;
                Self.ResetPhysicsInterpolation3D();
            }
        }

        private class Opening : State<OpenableWall>
        {
            public override void _PhysicsProcess(double delta)
            {
                float speed = Self._closedPos.DistanceTo(Self._openPos) / ((float)Self.OpenDuration);
                Self.GlobalPosition = Self.GlobalPosition.MoveToward(Self._openPos, speed * (float)delta);

                if (Self.GlobalPosition.IsEqualApprox(Self._openPos))
                    ChangeState<Open>();
            }
        }
    }
}