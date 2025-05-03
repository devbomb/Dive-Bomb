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
        private const float ReadingLabelsDuration = 1;
        private const float GemPathBezierControlSpread = 20;
        private const float GemSpawnGrowTime = 0.05f;

        private AudioStreamPlayer _gemSpawnSound => GetNode<AudioStreamPlayer>("%GemSpawnSound");
        private AudioStreamPlayer _gemCountSound => GetNode<AudioStreamPlayer>("%GemCountSound");

        private MeshLabel3D _untalliedGemsLabel => GetNode<MeshLabel3D>("%UntalliedGemsLabel");
        private MeshLabel3D _talliedGemsLabel => GetNode<MeshLabel3D>("%TalliedGemsLabel");
        private AnimationPlayer _labelSlider => GetNode<AnimationPlayer>("%LabelSlider");
        private Dictionary<GemColor, int> _untalliedGems;
        private int _talliedGems;

        private StateMachine _stateMachine = new StateMachine(typeof(RigState));

        public override void _Ready()
        {
            AddChild(_stateMachine);
            _stateMachine.ChangeState<Idle>();

            _untalliedGems = SaveFile.Current.UntalliedGems;
            SaveFile.Current.UntalliedGems = new Dictionary<GemColor, int>();
            _talliedGems = SaveFile.Current.TotalGemCount - TotalUntalliedGems();
        }

        public void Start()
        {
            _stateMachine.ChangeState<SlidingInLabels>();
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

        private void UpdateLabelText()
        {
            _untalliedGemsLabel.Text = $"Treasure found: {TotalUntalliedGems()}";
            _talliedGemsLabel.Text = $"Total treasure: {_talliedGems}";
        }

        private abstract partial class RigState : State
        {
            public virtual bool Skippable => false;
            protected GemCountingRig _self => _stateMachine.GetParent<GemCountingRig>();

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
                _self._untalliedGemsLabel.Visible = false;
                _self._talliedGemsLabel.Visible = false;
            }
        }

        private partial class SlidingInLabels : RigState
        {
            public override bool Skippable => true;

            public override void OnStateEntered()
            {
                _self._untalliedGemsLabel.Visible = true;
                _self._talliedGemsLabel.Visible = true;
                _self.UpdateLabelText();
                _self._labelSlider.Play("SlideIn");

                // We just made the text visible, but it won't naturally jump to
                // its starting position until the next frame when the animator
                // processes.  This will result in the text appearing to "blink"
                // for one frame before the animation actually starts.
                //
                // To avoid this, let's just force it to update right now.
                _self._labelSlider.Seek(0, true);
            }

            public override void _Process(double delta)
            {
                if (!_self._labelSlider.IsPlaying())
                {
                    if (_self.TotalUntalliedGems() > 0)
                    {
                        ChangeState<CountingGems>();
                    }
                    else
                    {
                        ChangeState<LettingPlayerReadLabels>();
                    }
                }
            }
        }

        private partial class CountingGems : RigState
        {
            public override bool Skippable => true;

            private const float MinInterval = 2f / 60;

            private Node3D _gemSpawn => _self.GetNode<Node3D>("%GemSpawn");
            private Node3D _gemDest => _self.GetNode<Node3D>("%GemDest");

            private Random _rng = new Random();

            private double _interval;
            private double _timer;
            private Node3D _gemHolder;

            public override void OnStateEntered()
            {
                GD.Print("Started counting gems");
                _timer = 0;

                _interval = CountingGemsDuration / _self.TotalIndividualUntalliedGems();

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
                _self._talliedGems += value;
                _self.UpdateLabelText();
                _self._gemCountSound.Play();

                if (_self._talliedGems >= SaveFile.Current.TotalGemCount)
                    ChangeState<LettingPlayerReadLabels>();
            }

            public override void _Process(double delta)
            {
                _timer += delta;

                if (_self.TotalUntalliedGems() > 0 && _timer >= _interval)
                {
                    _timer -= _interval;

                    GemColor color = _rng.PickFromWeighted(_self._untalliedGems);
                    SpawnGem(color);
                }
            }

            private void SpawnGem(GemColor value)
            {
                _self._untalliedGems[value]--;
                _self.UpdateLabelText();

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

                _self._gemSpawnSound.Play();
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
                    GemColor.Red => _self.RedGemPrefab,
                    GemColor.Green => _self.GreenGemPrefab,
                    GemColor.Purple => _self.PurpleGemPrefab,
                    GemColor.Yellow => _self.YellowGemPrefab,
                    GemColor.Magenta => _self.MagentaGemPrefab,
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

        private partial class LettingPlayerReadLabels : RigState
        {
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = ReadingLabelsDuration;
                _self._labelSlider.Play("RESET");
                _self._labelSlider.Advance(0);
                _self._untalliedGemsLabel.Visible = true;
                _self._talliedGemsLabel.Visible = true;

                _self._untalliedGems.Clear();
                _self._talliedGems = SaveFile.Current.TotalGemCount;
                _self.UpdateLabelText();
            }

            public override void _Process(double delta)
            {
                _timer -= delta;

                if (_timer <= 0)
                {
                    _self._labelSlider.PlayBackwards("SlideIn");
                    _self.EmitSignal(GemCountingRig.SignalName.Done);
                    ChangeState<DoneState>();
                }
            }
        }

        private partial class DoneState : RigState
        {
        }
    }
}