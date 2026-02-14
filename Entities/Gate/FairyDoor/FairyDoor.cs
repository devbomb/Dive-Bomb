using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class FairyDoor : StaticBody3D
    {
        [Export] public string FairyId;

        [ExportGroup("Internal")]
        [Export] public Node3D DoorModel;
        [Export] public Node3D LockModel;
        [Export] public AnimationPlayer LockAnimator;
        [Export] public CollisionShape3D CollisionShape;
        [Export] public Area3D SoftlockFailsafeTrigger;

        private FairyJar _jar;
        private FairyGuide _guide;
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

                _jar = GetTree().Root
                    .EnumerateDescendantsOfType<FairyJar>()
                    .Where(f => f.targetname == FairyId)
                    .FirstOrDefault();

                if (_jar == null)
                    throw new Exception($"Could not find a fairy jar with id {FairyId}");

                _guide = GetTree().Root
                    .EnumerateDescendantsOfType<FairyGuide>()
                    .Where(f => f.FairyId == FairyId)
                    .FirstOrDefault();

                SignalBus.Instance.LevelReset += Reset;
                Reset();
            }).CallDeferred();
        }

        private void Reset()
        {
            if (IsStoryFlagSet())
                _stateMachine.ChangeState<Open>();
            else
                _stateMachine.ChangeState<Closed>();
        }

        private bool IsFairyFree()
        {
            return this.GetLevel()
                ?.GetProgress()
                ?.CollectedFairies
                ?.Contains(_jar.SaveKey) ?? false;
        }

        private string StoryFlagId() => $"{FairyId}_DoorOpened";

        private bool IsStoryFlagSet()
        {
            return this.GetLevel()
                    ?.GetProgress()
                    ?.StoryFlags
                    ?.Contains(StoryFlagId()) ?? false;
        }

        private void SetStoryFlag()
        {
            this.GetLevel()
                ?.GetProgress()
                ?.StoryFlags
                .Add(StoryFlagId());

            SaveFileManager.Instance.RequestAutosave();
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
                {
                    // If this door's fairy has a guide associated with it, then
                    // don't open the door until the fairy guide reaches it, to
                    // ensure the player sees the fairy opening the door
                    if (Self._guide?.IsAtEnd() ?? true)
                        ChangeState<Opening>();
                }
            }

            private void OnSoftlockFailsafeTriggerEntered(Node3D body)
            {
                ChangeState<Opening>();
            }
        }

        private class Opening : State<FairyDoor>
        {
            private const double Duration = 0.5;

            private Transform3D _lockStartPos;

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

                _lockStartPos = Self.LockModel.GlobalTransform;
                Self.LockModel.TopLevel = true;
                Self.LockModel.GlobalTransform = _lockStartPos;
                Self.LockAnimator.Play("Unlock");

                Self.SetStoryFlag();
            }

            public override void OnStateExited()
            {
                Self.GlobalPosition = _startPos;
                Self.ResetPhysicsInterpolation3D();

                Self.LockAnimator.Play("RESET");
                Self.LockAnimator.Advance(0);
                Self.LockModel.TopLevel = false;
                Self.LockModel.GlobalTransform = _lockStartPos;
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
                Self.Visible = false;
                Self.CollisionShape.Disabled = true;
            }

            public override void OnStateExited()
            {
                Self.Visible = true;
                Self.CollisionShape.Disabled = false;
            }
        }
    }
}