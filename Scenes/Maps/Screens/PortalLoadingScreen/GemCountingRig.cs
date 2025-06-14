using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class GemCountingRig : Node3D
    {
        [Signal] public delegate void DoneEventHandler();

        [Export] public PackedScene RedGemPrefab;
        [Export] public PackedScene GreenGemPrefab;
        [Export] public PackedScene PurpleGemPrefab;
        [Export] public PackedScene YellowGemPrefab;
        [Export] public PackedScene MagentaGemPrefab;

        private const float CountingGemsDuration = 1;
        private const float MaxDeductingCostsDuration = 1;
        private const float ReadingLabelsDuration = 1;
        private const float GemPathBezierControlSpread = 20;
        private const float GemSpawnGrowTime = 0.05f;

        private AudioStreamPlayer _gemSpawnSound => GetNode<AudioStreamPlayer>("%GemSpawnSound");
        private AudioStreamPlayer _gemCountSound => GetNode<AudioStreamPlayer>("%GemCountSound");
        private AudioStreamPlayer _gemSpentSound => GetNode<AudioStreamPlayer>("%GemSpentSound");

        private MeshLabel3D _untalliedGemsLabel => GetNode<MeshLabel3D>("%UntalliedGemsLabel");
        private MeshLabel3D _talliedGemsLabel => GetNode<MeshLabel3D>("%TalliedGemsLabel");
        private MeshLabel3D _spentGemsLabel => GetNode<MeshLabel3D>("%SpentGemsLabel");
        private AnimationPlayer _labelSlider => GetNode<AnimationPlayer>("%LabelSlider");

        private Dictionary<GemColor, int> _untalliedGems;
        private int _untalliedSpentGems;
        private int _talliedGems;

        private StateMachine _stateMachine = new StateMachine(typeof(RigState));

        public override void _Ready()
        {
            AddChild(_stateMachine);
            _stateMachine.ChangeState<Idle>();

            _untalliedGems = SaveFile.Current.UntalliedGemsCollected;
            SaveFile.Current.UntalliedGemsCollected = new Dictionary<GemColor, int>();

            _untalliedSpentGems = SaveFile.Current.UntalliedGemsSpent;
            SaveFile.Current.UntalliedGemsSpent = 0;

            _talliedGems = SaveFile.Current.TotalGemCount;
            _talliedGems -= TotalUntalliedGems();
            _talliedGems += _untalliedSpentGems;
        }

        public void Start()
        {
            if (TotalIndividualUntalliedGems() > 0)
                _stateMachine.ChangeState<SlidingInGemsFound>();
            else
                _stateMachine.ChangeState<SlidingInNoGemsFound>();
        }

        public void StartAfterSkipped()
        {
            _stateMachine.ChangeState<LettingPlayerReadLabels>();
        }

        private int TotalUntalliedGems()
        {
            return _untalliedGems
                .Select(kvp => (int)kvp.Key * kvp.Value)
                .Sum();
        }

        private int TotalIndividualUntalliedGems()
        {
            return _untalliedGems
                .Select(kvp => kvp.Value)
                .Sum();
        }

        private void SetLabelsVisible(bool visible)
        {
            _untalliedGemsLabel.Visible = visible;
            _spentGemsLabel.Visible = visible;
            _talliedGemsLabel.Visible = visible;
        }

        private void UpdateLabelText()
        {
            _untalliedGemsLabel.Text = $"Treasure found: {TotalUntalliedGems()}";
            _spentGemsLabel.Text = $"Tresure spent: -{_untalliedSpentGems}";
            _talliedGemsLabel.Text = $"Total treasure: {_talliedGems}";
        }

        private abstract partial class RigState : State
        {
            public virtual bool Skippable => false;
            protected GemCountingRig Self => _stateMachine.GetParent<GemCountingRig>();

            public override void _Input(InputEvent ev)
            {
                if (InputService.SkipJustPressed(ev) && Skippable)
                {
                    GD.Print("Skipping gem count animation");
                    ChangeState<LettingPlayerReadLabels>();
                }
            }
        }

        private partial class Idle : RigState
        {
            public override void OnStateEntered()
            {
                Self.SetLabelsVisible(false);
            }
        }

        private partial class SlidingInGemsFound : RigState
        {
            public override bool Skippable => true;

            public override void OnStateEntered()
            {
                Self.SetLabelsVisible(true);
                Self.UpdateLabelText();
                Self._labelSlider.Play("SlideInGemsFound");

                // We just made the text visible, but it won't naturally jump to
                // its starting position until the next frame when the animator
                // processes.  This will result in the text appearing to "blink"
                // for one frame before the animation actually starts.
                //
                // To avoid this, let's just force it to update right now.
                Self._labelSlider.Seek(0, true);
            }

            public override void _Process(double delta)
            {
                if (!Self._labelSlider.IsPlaying())
                {
                    ChangeState<CountingGems>();
                }
            }
        }

        private partial class SlidingInNoGemsFound : RigState
        {
            public override bool Skippable => true;

            public override void OnStateEntered()
            {
                Self.SetLabelsVisible(true);
                Self.UpdateLabelText();
                Self._labelSlider.Play("SlideInNoGemsFound");

                // We just made the text visible, but it won't naturally jump to
                // its starting position until the next frame when the animator
                // processes.  This will result in the text appearing to "blink"
                // for one frame before the animation actually starts.
                //
                // To avoid this, let's just force it to update right now.
                Self._labelSlider.Seek(0, true);
            }

            public override void _Process(double delta)
            {
                if (!Self._labelSlider.IsPlaying())
                {
                    if (Self._untalliedSpentGems > 0)
                        ChangeState<SlidingInGemsSpent>();
                    else
                        ChangeState<LettingPlayerReadLabels>();
                }
            }
        }

        private partial class CountingGems : RigState
        {
            public override bool Skippable => true;

            private const float MinInterval = 2f / 60;

            private Node3D _gemSpawn => Self.GetNode<Node3D>("%GemSpawn");
            private Node3D _gemDest => Self.GetNode<Node3D>("%GemDest");

            private Random _rng = new Random();

            private double _interval;
            private double _timer;
            private Node3D _gemHolder;

            public override void OnStateEntered()
            {
                GD.Print("Started counting gems");
                _timer = 0;

                _interval = CountingGemsDuration / Self.TotalIndividualUntalliedGems();

                if (_interval < MinInterval)
                    _interval = MinInterval;

                _interval = MinInterval;

                _gemHolder = new Node3D();
                _gemSpawn.GetParent().AddChild(_gemHolder);
            }

            public override void OnStateExited()
            {
                _gemHolder.QueueFree();
                _gemHolder = null;
            }

            private void OnGemCounted(int value)
            {
                Self._talliedGems += value;
                Self.UpdateLabelText();
                Self._gemCountSound.Play();

                if (Self._talliedGems >= SaveFile.Current.TotalGemCount + Self._untalliedSpentGems)
                    ChangeState<MovingTotalToTop>();
            }

            public override void _Process(double delta)
            {
                _timer += delta;

                if (Self.TotalUntalliedGems() > 0 && _timer >= _interval)
                {
                    _timer -= _interval;

                    GemColor color = _rng.PickFromWeighted(Self._untalliedGems);
                    SpawnGem(color);
                }
            }

            private void SpawnGem(GemColor value)
            {
                Self._untalliedGems[value]--;
                Self.UpdateLabelText();

                var gem = new CountableGem(
                    value,
                    _gemSpawn.Position,
                    _gemDest.Position,
                    RandomBezierControlPoint(GemPathBezierControlSpread)
                );
                _gemHolder.AddChild(gem);

                var model = ModelPrefabForGemColor(value).Instantiate<Node3D>();
                gem.AddChild(model);
                gem.Counted += OnGemCounted;

                Self._gemSpawnSound.Play();
            }

            private Vector3 RandomBezierControlPoint(float spread)
            {
                Vector3 center = _gemDest.Position;
                float offset = (GD.Randf() - 0.5f) * spread;

                return center + (Vector3.Right * offset);
            }

            private PackedScene ModelPrefabForGemColor(GemColor color)
            {
                return color switch
                {
                    GemColor.Red => Self.RedGemPrefab,
                    GemColor.Green => Self.GreenGemPrefab,
                    GemColor.Purple => Self.PurpleGemPrefab,
                    GemColor.Yellow => Self.YellowGemPrefab,
                    GemColor.Magenta => Self.MagentaGemPrefab,
                    _ => throw new Exception("Invalid gem color chosen")
                };
            }

            private partial class CountableGem : Node3D
            {
                [Signal] public delegate void CountedEventHandler(int value);

                private readonly GemColor _value;
                private readonly Vector3 _start;
                private readonly Vector3 _end;
                private readonly Vector3 _control;

                private float _timer = 0;

                public CountableGem(
                    GemColor value,
                    Vector3 start,
                    Vector3 end,
                    Vector3 control
                )
                {
                    _value = value;
                    _start = start;
                    _end = end;
                    _control = control;

                    Position = _start;
                    Scale = Vector3.Zero;
                }

                public override void _Process(double deltaD)
                {
                    float delta = (float)deltaD;

                    _timer += delta;

                    float positionT = Mathf.Min(1, _timer / CountingGemsDuration);
                    float scaleT = Mathf.Min(1, _timer / GemSpawnGrowTime);

                    Position = _start.LerpBezier(_end, _control, positionT);
                    Scale = Vector3.Zero.Lerp(Vector3.One, scaleT);

                    if (_timer >= CountingGemsDuration)
                    {
                        EmitSignal(SignalName.Counted, (int)_value);
                        QueueFree();
                    }
                }
            }
        }

        private partial class MovingTotalToTop : RigState
        {
            public override bool Skippable => true;

            public override void OnStateEntered()
            {
                Self._labelSlider.Play("MoveTotalToTop");
            }

            public override void _Process(double delta)
            {
                if (!Self._labelSlider.IsPlaying())
                {
                    if (Self._untalliedSpentGems > 0)
                        ChangeState<SlidingInGemsSpent>();
                    else
                        ChangeState<LettingPlayerReadLabels>();
                }
            }
        }

        private partial class SlidingInGemsSpent : RigState
        {
            public override bool Skippable => true;

            public override void OnStateEntered()
            {
                Self._labelSlider.Play("SlideInGemsSpent");
            }

            public override void _Process(double delta)
            {
                if (!Self._labelSlider.IsPlaying())
                    ChangeState<DeductingCosts>();
            }
        }

        private partial class DeductingCosts : RigState
        {
            public override bool Skippable => true;

            private const double Interval = 2.0 / 60;
            private double _timer;
            private double _totalTimer;

            public override void OnStateEntered()
            {
                _timer = Interval;
                _totalTimer = MaxDeductingCostsDuration;
            }

            public override void _Process(double delta)
            {
                if (Self._untalliedSpentGems <= 0)
                {
                    ChangeState<SlidingOutGemsSpent>();
                    return;
                }

                _timer -= delta;
                if (_timer <= 0)
                {
                    _timer += Interval;

                    Self._untalliedSpentGems--;
                    Self._talliedGems--;
                    Self.UpdateLabelText();

                    Self._gemSpentSound.Play();
                }

                // Look, the player gets the picture.  We don't need to show
                // the _entire_ countdown if it's too long.
                _totalTimer -= delta;
                if (_totalTimer <= 0)
                {
                    ChangeState<SlidingOutGemsSpent>();
                }
            }

            public override void OnStateExited()
            {
                Self._talliedGems = SaveFile.Current.TotalGemCount;
                Self._untalliedSpentGems = 0;
                Self.UpdateLabelText();
            }
        }

        private partial class SlidingOutGemsSpent : RigState
        {
            public override bool Skippable => true;

            public override void OnStateEntered()
            {
                Self._labelSlider.PlayBackwards("SlideInGemsSpent");
            }

            public override void _Process(double delta)
            {
                if (!Self._labelSlider.IsPlaying())
                    ChangeState<LettingPlayerReadLabels>();
            }
        }

        private partial class LettingPlayerReadLabels : RigState
        {
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = ReadingLabelsDuration;
                Self._labelSlider.Play("RESET");
                Self._labelSlider.Advance(0);
                Self.SetLabelsVisible(true);

                Self._untalliedGems.Clear();
                Self._talliedGems = SaveFile.Current.TotalGemCount;
                Self.UpdateLabelText();
            }

            public override void _Process(double delta)
            {
                _timer -= delta;

                if (_timer <= 0)
                {
                    Self._labelSlider.Play("SlideOut");
                    Self.EmitSignal(GemCountingRig.SignalName.Done);
                    ChangeState<DoneState>();
                }
            }
        }

        private partial class DoneState : RigState
        {
        }
    }
}