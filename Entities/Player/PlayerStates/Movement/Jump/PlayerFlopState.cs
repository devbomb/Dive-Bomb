using Godot;

namespace FastDragon
{
    public partial class PlayerFlopState : PlayerJumpState
    {
        public override string AnimationName => "Flop";
        public override float FallGravity => Player.Default.Gravity;
        public override float FullJumpRiseGravity => Player.Default.Gravity;
        public override float ShortHopGravity => Player.Default.Gravity;

        private double _coyoteTimer;

        public override void OnStateEntered()
        {
            Self.Animator.Play(AnimationName, 0);
            // Deliberately NOT setting the VSpeed here, because we want to
            // conserve it from the previous state.  Simply setting InitVSpeed
            // to 0 wouldn't achieve that.
        }

        public override void OnStateEntered(IState prevState)
        {
            if (prevState is PlayerWalkState)
                _coyoteTimer = Player.Default.CoyoteTime;
            else
                _coyoteTimer = 0;
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev) && _coyoteTimer > 0)
            {
                GD.Print("Coyote jump!");
                Self.ChangeState<PlayerWalkJumpState>();
                return;
            }

           base._Input(ev);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_coyoteTimer > 0)
                _coyoteTimer -= delta;

            base._PhysicsProcess(delta);
        }
    }
}