using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class Player : CharacterBody3D
    {
        [Signal] public delegate void RespawningEventHandler();
        [Signal] public delegate void FlyInFinishedEventHandler();

        [Export] public float FlyInHeight = 10;
        [Export] public float FlyInDistance = 10;
        [Export] public float FlyInDuration = 4;

        public PlayerState CurrentState => (PlayerState)_stateMachine.CurrentState;

        public PlayerCamera Camera => GetNode<PlayerCamera>("%Camera");
        public PlayerCameraFocusPoint CameraFocus => GetNode<PlayerCameraFocusPoint>("%CameraFocus");

        public Node3D Model => GetNode<Node3D>("%Model");
        public AnimationPlayer Animator => GetNode<AnimationPlayer>("%Animator");

        public Area3D KickHitbox => GetNode<Area3D>("%KickHitbox");
        public Area3D DiveExtraHitbox => GetNode<Area3D>("%DiveExtraHitbox");
        public Area3D RollExtraHitbox => GetNode<Area3D>("%RollExtraHitbox");

        public LedgeDetector LedgeDetector => GetNode<LedgeDetector>("%LedgeDetector");
        public Node3D LedgeGrabPoint => GetNode<Node3D>("%LedgeGrabPoint");
        public Node3D MinLedgeGrabHeight => GetNode<Node3D>("%MinLedgeGrabHeight");

        public Node3D FairyKissPoint => GetNode<Node3D>("%FairyKissPoint");
        public Node3D FairyKissCamRightPoint => GetNode<Node3D>("%FairyKissCamRightPoint");
        public Node3D FairyKissCamLeftPoint => GetNode<Node3D>("%FairyKissCamLeftPoint");

        public readonly PlayerSafeGround SafeGround;

        /// <summary>
        /// Used to let the player press "jump" slightly before landing and
        /// still have it count.
        /// </summary>
        public float EarlyJumpBufferTimer { get; private set; }

        public float FSpeed
        {
            get => Velocity.Flattened().Length();
            set
            {
                Vector3 vel = this.GlobalForward() * value;
                vel.Y = Velocity.Y;
                Velocity = vel;
            }
        }

        public float VSpeed
        {
            get => Velocity.Y;
            set
            {
                Vector3 vel = Velocity;
                vel.Y = value;
                Velocity = vel;
            }
        }

        public float YawRad
        {
            get => GlobalRotation.Y;
            set
            {
                var rot = GlobalRotation;
                rot.Y = value;
                GlobalRotation = rot;
            }
        }

        public float ModelPitchRad
        {
            get => Model.Rotation.X;
            set
            {
                var rot = Model.Rotation;
                rot.X = value;
                Model.Rotation = rot;
            }
        }

        /// <summary>
        /// An accessor for <see cref="LastSafeGround"/> that GDScript can see
        /// </summary>
        public Transform3D LastSafeGroundPos => SafeGround.LastSafeGround.PlayerPos;

        private readonly StateMachine _stateMachine = new StateMachine();
        private Transform3D _spawnPos;

        private float _damageCooldownTimer;

        public Player()
        {
            SafeGround = new(this);
        }

        public override void _Ready()
        {
            AddChild(_stateMachine);
            _stateMachine.StateChanging += OnStateChanging;

            base._Ready();

            SignalBus.Instance.LevelReset += Respawn;
            _spawnPos = GlobalTransform;

            Respawn();
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                EarlyJumpBufferTimer = Player.Default.EarlyJumpBufferTime;
            }
        }

        public void Respawn()
        {
            EmitSignal(SignalName.Respawning);

            var checkpoint = GetTree().Root
                .EnumerateDescendantsOfType<Checkpoint>()
                .FirstOrDefault(c => c.IsCurrent || c.DebugSpawnHere);

            GlobalTransform = checkpoint == null
                ? _spawnPos
                : checkpoint.GlobalTransform;

            Velocity = Vector3.Zero;
            this.ResetPhysicsInterpolation3D();

            CameraFocus.Reset();
            Camera.Reset();
            SafeGround.Reset();

            Animator.Play("RESET", 0);
            Animator.Advance(0);
            ChangeState<PlayerWalkState>();

            EarlyJumpBufferTimer = 0;
            _damageCooldownTimer = 0;
        }

        public void SetVisibleInPortals(bool visible)
        {
            var visuals = this.EnumerateDescendantsOfType<VisualInstance3D>();
            foreach (var v in visuals)
            {
                v.SetLayerMaskValue(RenderLayer.VisibleInPortals, visible);
            }
        }

        /// <summary>
        /// If the player is currently vulnerable, deals 1 damage.
        ///
        /// If the player is not currently vulnerable, then this method does
        /// nothing.
        ///
        /// Returns true if the player was successfully damaged, or false otherwise
        /// </summary>
        /// <param name="invulnerablePeriod">
        ///     How many seconds of invulnerability the player receives after
        ///     getting hit by this attack.  If the player is in a damange
        ///     animation state (such as <see cref="PlayerDamageFlipState"/>),
        ///     the timer won't start until AFTER that state is completed and
        ///     the player is actionable again.
        /// </param>
        public bool TryDamage(float invulnerablePeriod = 0)
        {
            if (_damageCooldownTimer > 0)
                return false;

            if (CurrentState.Invincible)
                return false;

            _damageCooldownTimer = invulnerablePeriod;
            SaveFileManager.Current.PlayerHealth--;
            return true;
        }

        /// <summary>
        /// If the player is currently vulnerable, deals 1 damage and changes
        /// to the given state.  The given state should be some sort of damage
        /// animation state.
        ///
        /// If the player is not currently vulnerable, then this method does
        /// nothing.
        ///
        /// Returns true if the player was successfully damaged, or false otherwise
        /// </summary>
        /// <param name="invulnerablePeriod">
        ///     How many seconds of invulnerability the player receives after
        ///     getting hit by this attack.  The timer starts AFTER after the
        ///     damage animation is completed and the player is back in an
        ///     actionable state.
        /// </param>
        /// <typeparam name="TState"></typeparam>
        public bool TryDamage<TState>(float invulnerablePeriod = 0) where TState : PlayerState, new()
        {
            bool result = TryDamage(invulnerablePeriod);

            if (result)
                ChangeState<TState>();

            return result;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            Camera.DisableInput = CurrentState.DisableCameraInput;

            if (EarlyJumpBufferTimer > 0)
                EarlyJumpBufferTimer -= delta;

            if (_damageCooldownTimer > 0 && !CurrentState.PauseDamageCooldownTimer)
                _damageCooldownTimer -= delta;
        }

        public void ChangeState<TState>() where TState : PlayerState, new()
        {
            _stateMachine.ChangeState<TState>();
        }

        private void OnStateChanging(IState currentState, IState incomingState)
        {
            GD.Print($"Changing state to {incomingState.GetType().Name}");
        }
    }
}
