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

        [Export] public Node3D Model;
        [Export] public AnimationPlayer Animator;

        [ExportGroup("Sound Players")]
        [Export] public AudioStreamPlayer RollSoundPlayer;
        [Export] public AudioStreamPlayer BonkSoundPlayer;
        [Export] public AudioStreamPlayer FlipDamageSound;
        [Export] public AudioStreamPlayer DrownStartSplashSound;

        [ExportGroup("FX")]
        [Export] public GpuParticles3D RollDust;
        [Export] public MeshInstance3D RollThuum;
        [Export] public MeshInstance3D DiveThuum;

        [ExportGroup("Camera")]
        [Export] public PlayerCamera Camera;
        [Export] public PlayerCameraFocusPoint CameraFocus;

        [ExportGroup("Hitboxes")]
        [Export] public CollisionShape3D BodyCollisionShape;
        [Export] public Area3D KickHitbox;
        [Export] public Area3D DiveExtraHitbox;
        [Export] public Area3D RollExtraHitbox;

        [ExportGroup("Ledge grabbing")]
        [Export] public LedgeDetector LedgeDetector;
        [Export] public Node3D LedgeGrabPoint;
        [Export] public Node3D MinLedgeGrabHeight;

        [ExportGroup("Fairy kiss")]
        [Export] public Node3D FairyKissPoint;
        [Export] public Node3D FairyKissCamRightPoint;
        [Export] public Node3D FairyKissCamLeftPoint;

        public int Health = MaxHealth;

        public readonly PlayerSafeGround SafeGround;

        /// <summary>
        /// The velocity of the last platform the player stood on.
        /// Useful for enforcing a mid-air speed cap that's relative to the
        /// platform the player just jumped from.
        /// </summary>
        public Vector3 LastPlatformVelocity { get; set; }
        private bool _wasOnFloorLastPhysicsFrame;

        /// <summary>
        /// The player's velocity relative to the last platform they stood on
        /// (regardless of if they're in the air or on the ground).
        /// </summary>
        public Vector3 LocalVelocity
        {
            // Godot treats the Velocity property differently depending on if
            // you're in the air or on the ground.
            //
            // In the air, Velocity is your "absolute" velocity, relative to
            // the raw coordinate system.
            //
            // On the ground, Velocity is how fast you're moving relative to
            // the ground you're currently standing on, hence why we only
            // subtract LastPlatformVelocity in the air.
            get => IsOnFloor()
                ? Velocity
                : Velocity - LastPlatformVelocity;

            set
            {
                if (IsOnFloor())
                    Velocity = value;
                else
                    Velocity = value + LastPlatformVelocity;
            }
        }

        public Vector3 TotalVelocity
        {
            get => IsOnFloor()
                ? Velocity + GetPlatformVelocity()
                : Velocity;

            set
            {
                if (IsOnFloor())
                    Velocity = value - GetPlatformVelocity();
                else
                    Velocity = value;
            }
        }

        /// <summary>
        /// Used to let the player press "jump" slightly before landing and
        /// still have it count.
        /// </summary>
        public float EarlyJumpBufferTimer { get; private set; }

        public float FSpeed
        {
            get => LocalVelocity.Flattened().Length();
            set
            {
                Vector3 vel = this.GlobalForward() * value;
                vel.Y = LocalVelocity.Y;
                LocalVelocity = vel;
            }
        }

        public float VSpeed
        {
            get => LocalVelocity.Y;
            set
            {
                Vector3 vel = LocalVelocity;
                vel.Y = value;
                LocalVelocity = vel;
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
            _stateMachine.AfterPhysicsProcess += AfterStatePhysicsProcess;

            base._Ready();

            SignalBus.Instance.LevelReset += Reset;
            _spawnPos = GlobalTransform;

            if (this.PlaySceneFromHereWasUsed())
            {
                LoadDebugSpawnPoint();
            }

            Reset();
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                EarlyJumpBufferTimer = Player.Default.EarlyJumpBufferTime;
            }
        }

        public void Reset()
        {
            EmitSignal(SignalName.Respawning);

            Health = MaxHealth;

            GlobalTransform = GetRespawnPoint();
            this.ResetPhysicsInterpolation3D();

            Velocity = Vector3.Zero;
            LastPlatformVelocity = Vector3.Zero;    // The respawn point will
                                                    // never be on a moving
                                                    // platform, so this is OK.
            _wasOnFloorLastPhysicsFrame = true;

            CameraFocus.Reset();
            Camera.Reset();
            SafeGround.Reset();

            Animator.Play("RESET", 0);
            Animator.Advance(0);
            ChangeState<PlayerStandState>();

            EarlyJumpBufferTimer = 0;
            _damageCooldownTimer = 0;
        }

        private void LoadDebugSpawnPoint()
        {
            Transform3D cameraPos = PlaySceneFromHereExtensions.CameraPos;
            Vector3 playerPos = cameraPos.TranslatedLocal(Vector3.Forward * 6).Origin;
            playerPos -= CameraFocus.GlobalPosition;

            _spawnPos = Transform3D.Identity
                .Rotated(Vector3.Up, cameraPos.Basis.GetEuler().Y)
                .Translated(playerPos);
        }

        private Transform3D GetRespawnPoint()
        {
            if (this.IsTimeTrialMode())
                return _spawnPos;

            var checkpoint = GetTree().Root
                .EnumerateDescendantsOfType<Checkpoint>()
                .FirstOrDefault(c => c.IsCurrent);

            return checkpoint == null
                ? _spawnPos
                : checkpoint.GlobalTransform;
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
            Health--;
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

        /// <summary>
        /// Increments the death counter and reloads the last checkpoint,
        /// without playing a death animation.
        /// </summary>
        public void Die()
        {
            LevelTransitionManager.Instance.ReloadCheckpoint();

            if (!this.IsTimeTrialMode())
            {
                SaveFileManager.Current.TotalDeaths++;
                SaveFileManager.Current.CurrentLevelVisit.Deaths++;
                SaveFileManager.Instance.RequestAutosave();
                GD.Print($"Deaths: {SaveFileManager.Current.TotalDeaths}");
            }
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

        /// <summary>
        /// Occurs after the current state's _PhysicsProcess().
        /// Put things here if they need to occur after MoveAndSlide()
        /// </summary>
        private void AfterStatePhysicsProcess(double delta)
        {
            UpdatePlatformVelocity();
        }

        public void ChangeState<TState>() where TState : PlayerState, new()
        {
            _stateMachine.ChangeState<TState>();
        }

        private void OnStateChanging(IState currentState, IState incomingState)
        {
            GD.Print($"Changing state to {incomingState.GetType().Name}");
        }

        private void UpdatePlatformVelocity()
        {
            bool onFloor = IsOnFloor();
            bool wasOnFloor = _wasOnFloorLastPhysicsFrame;
            _wasOnFloorLastPhysicsFrame = onFloor;

            if (onFloor)
                LastPlatformVelocity = GetPlatformVelocity();

            // Adjust velocity to be relative to the platform velocity, so the
            // player doesn't "skip forward" when landing on a moving platform
            // at the same horizontal speed as it.
            if (onFloor && !wasOnFloor)
            {
                Velocity -= GetPlatformVelocity();

                // HACK: IsOnFloor() only works reliably if Velocity.Y is at
                // least a little bit negative.  It being exactly zero results
                // in it flickering between returning true and false.
                //
                // Furthermore, a velocity of exactly zero results in the
                // following error:
                // "instance_set_transform: Condition "!v.is_finite()" is true."
                //
                // Adding a little bit of downward velocity when landing fixes
                // both problems.
                Velocity += Vector3.Down;
            }
        }
    }
}
