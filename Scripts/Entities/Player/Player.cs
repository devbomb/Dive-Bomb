using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class Player : CharacterBody3D
    {
        [Signal] public delegate void RespawningEventHandler();

        /// <summary>
        /// The current level's human-friendly name.
        /// Will be stored in the Atlas cache.
        /// </summary>
        /// <returns></returns>
        [Export] public string LevelName;

        /// <summary>
        /// The map we should return to when "exit level" is selected in the
        /// pause menu, or when a vortex is used.
        ///
        /// Set to null to indicate that this map is a home world.
        /// </summary>
        /// <returns></returns>
        [Export(PropertyHint.File)] public string HomeWorldMap;

        [Export] public float FlyInHeight = 10;
        [Export] public float FlyInDistance = 10;
        [Export] public float FlyInDuration = 4;

        public PlayerState CurrentState => (PlayerState)_stateMachine.CurrentState;

        public PlayerCamera Camera => GetNode<PlayerCamera>("%Camera");
        public Node3D CameraFocus => GetNode<Node3D>("%CameraFocus");
        public Node3D CameraFocusRestPos => GetNode<Node3D>("%CameraFocusRestPos");

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

        /// <summary>
        /// Used to let the player press "jump" slightly before landing and
        /// still have it count.
        /// </summary>
        public float EarlyJumpBufferTimer { get; private set; }

        /// <summary>
        /// If the player initiates a roll from a grounded state while this
        /// timer is active, then that roll will have a much lower max speed.
        /// The auto-roll at the end of a dive is not affected by this.
        ///
        /// The purpose of this timer is to ensure that the optimal way of
        /// moving is to jump -> dive -> roll -> jump -> dive -> roll.
        /// Without this cooldown, the fastest way to move would just be
        /// roll -> roll -> roll -> roll -> roll, and that's no fun.
        /// </summary>
        public float GroundRollCooldownTimer;

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

        private readonly StateMachine _stateMachine = new StateMachine(typeof(PlayerState));
        private Vector3 _spawnPoint;
        private Vector3 _spawnRotation;

        public override void _Ready()
        {
            AddChild(_stateMachine);
            _stateMachine.StateChanging += OnStateChanging;

            base._Ready();

            SignalBus.Instance.LevelReset += Respawn;
            _spawnPoint = GlobalPosition;
            _spawnRotation = GlobalRotation;

            Respawn();

            // Count all the gems/fairies in the level, including the collected
            // ones, and then cache it.
            //
            // Defer doing so until the next frame, because we don't know if
            // all of the gem containers have spawned in yet.
            Callable.From(UpdateAtlasCache).CallDeferred();
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

            if (SaveFile.Current.CurrentCheckpoint == null)
            {
                GlobalPosition = _spawnPoint;
                GlobalRotation = _spawnRotation;
            }
            else
            {
                var checkpoint = GetTree().Root
                    .EnumerateDescendantsOfType<Checkpoint>()
                    .First(c => c.IsCurrent);

                GlobalPosition = checkpoint.GlobalPosition;
                GlobalRotation = checkpoint.GlobalRotation;
            }

            Velocity = Vector3.Zero;
            this.ResetPhysicsInterpolation();

            CameraFocus.GlobalTransform = CameraFocusRestPos.GlobalTransform;
            CameraFocus.ResetPhysicsInterpolation();
            Camera.ForceRecenter();

            Animator.Play("RESET", 0);
            Animator.Advance(0);
            ChangeState<PlayerWalkState>();

            EarlyJumpBufferTimer = 0;
            GroundRollCooldownTimer = 0;
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
        /// If the player is currently vulnerable, deals 1 damage and changes
        /// to the given state.  The given state should be some sort of damage
        /// animation state.
        ///
        /// If the player is not currently vulnerable, then this method does
        /// nothing.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        public void TryDamage<TState>() where TState : PlayerState, new()
        {
            if (!CurrentState.Invincible)
            {
                SaveFile.Current.PlayerHealth--;
                ChangeState<TState>();
            }
        }

        private void UpdateAtlasCache()
        {
            AtlasCache.Instance.UpdateCache(SaveFile.Current.CurrentMap, GetTree().CurrentScene);
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            Camera.DisableInput = CurrentState.DisableCameraInput;

            if (EarlyJumpBufferTimer > 0) EarlyJumpBufferTimer -= delta;
            if (GroundRollCooldownTimer > 0) GroundRollCooldownTimer -= delta;

            if (CurrentState.UseMario64CameraFocus)
            {
                var groundPos = FindGroundPosition();
                float yFocusGround = groundPos.Y + CameraFocusRestPos.Position.Y;
                float targetYFocus = Mathf.Max(
                    yFocusGround,
                    GlobalPosition.Y
                );

                var focusPos = CameraFocusRestPos.GlobalPosition;
                focusPos.Y = AccelMath.SmoothStepToward(
                    CameraFocus.GlobalPosition.Y,
                    targetYFocus,
                    Player.Default.Gravity,
                    delta,
                    ref _cameraFocusYSpeed
                );
                CameraFocus.GlobalPosition = focusPos;
            }
            else
            {
                CameraFocus.GlobalPosition = CameraFocusRestPos.GlobalPosition;
            }

            // HACK: Keep the camera focus rotated with the player, so recentering
            // works correctly.
            //
            // The camera uses the global rotation of whatever it's following to
            // determine where to go when it recenters.  Its follow target
            // happens to be the camera focus.  The camera focus is top-level
            // (I forget why), so it doesn't rotate when the player rotates.
            // Thus, it always has a global rotation of (0, 0, 0) unless we
            // intervene, so the camera always faces north when it recenters.
            // To avoid this, just manually change the focus's rotation.
            CameraFocus.GlobalRotation = GlobalRotation;
        }

        private float _cameraFocusYSpeed = 0;

        private Vector3 FindGroundPosition()
        {
            const float maxHeight = 10;
            var collision = MoveAndCollide(
                Vector3.Down * maxHeight,
                testOnly: true,
                recoveryAsCollision: true
            );

            return collision == null
                ? (GlobalPosition + (Vector3.Down * maxHeight))
                : collision.GetPosition();
        }

        public void ChangeState<TState>() where TState : PlayerState, new()
        {
            _stateMachine.ChangeState<TState>();
        }

        private void OnStateChanging(State currentState, State incomingState)
        {
            GD.Print($"Changing state to {incomingState.GetType().Name}");
        }
    }
}
