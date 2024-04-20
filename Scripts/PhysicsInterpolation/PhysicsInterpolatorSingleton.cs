using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class PhysicsInterpolatorSingleton : Node
    {
        public const string GroupName = "PhysicsInterpolated";
        public static PhysicsInterpolatorSingleton Instance {get; private set;}

        public bool AllowInterpolation = true;
        private double _physicsDelta = 1;
        private double _timer;

        private record struct TruePos(Transform3D Current, Transform3D Prev);
        private Dictionary<Node3D, TruePos> _truePos = new Dictionary<Node3D, TruePos>();
        private Dictionary<Node3D, TruePos> _truePosSwap = new Dictionary<Node3D, TruePos>();

        public PhysicsInterpolatorSingleton()
        {
            Instance = this;
        }

        private TruePos GetTruePosStruct(Node3D node)
        {
            if (!_truePos.TryGetValue(node, out var truePos))
            {
                return new TruePos(node.Transform, node.Transform);
            }

            return truePos;
        }

        private void SetTruePosStruct(Node3D node, TruePos value)
        {
            _truePos[node] = value;
        }

        public void ResetPhysicsInterpolation(Node3D node)
        {
            _truePos.Remove(node);
        }

        public override void _Ready()
        {
            var beginSpy = new PhysicsProcessSpy(int.MinValue);
            beginSpy.PhysicsProcessed += OnPhysicsFrameStarted;
            AddChild(beginSpy);

            var endSpy = new PhysicsProcessSpy(int.MaxValue);
            endSpy.PhysicsProcessed += OnPhysicsFrameEnded;
            AddChild(endSpy);

            ProcessPriority = int.MinValue;
        }

        public override void _Process(double delta)
        {
            if (!AllowInterpolation)
                return;

            _timer += delta;

            double t = _timer / _physicsDelta;
            if (t > 1)
                t = 1;

            foreach (var node in AllInterpolatableNodes())
            {
                var truePos = GetTruePosStruct(node);

                if (!truePos.Current.Basis.GetRotationQuaternion().IsNormalized())
                {
                    continue;
                }

                if (!truePos.Prev.Basis.GetRotationQuaternion().IsNormalized())
                {
                    continue;
                }

                node.Transform = truePos.Prev.InterpolateWith(truePos.Current, (float)t);
            }
        }

        private void OnPhysicsFrameStarted(double delta)
        {
            if (!AllowInterpolation)
                return;

            _physicsDelta = delta;
            _timer -= delta;

            // Move all the nodes back to their true positions before any other
            // _PhysicsProcess() code has a chance to run.  This effectively
            // undoes the smoothing we did in _Process(), to ensure
            // _PhysicsProcess() stays deterministic.
            foreach (var node in AllInterpolatableNodes())
            {
                node.Transform = GetTruePosStruct(node).Current;
                node.ForceUpdateTransform();
            }
        }

        private void OnPhysicsFrameEnded(double delta)
        {
            if (!AllowInterpolation)
                return;

            // Save the current and previous position of every node, both so
            // we can smooth it out during _Process(), AND so we can undo the
            // smoothing at the start of the next physics frame.
            foreach (var node in AllInterpolatableNodes())
            {
                var truePos = GetTruePosStruct(node);
                truePos.Prev = truePos.Current;
                truePos.Current = node.Transform;

                _truePosSwap[node] = truePos;
            }

            // Swap the two dictionaries, and then clear the one we just swapped
            // out.  This effectively cleans up any deleted nodes from the
            // dictionary, without needing to allocate on the heap or loop
            // through the dictionary an extra time.
            var holder = _truePos;
            _truePos = _truePosSwap;
            _truePosSwap = holder;
            _truePosSwap.Clear();
        }

        private IEnumerable<Node3D> AllInterpolatableNodes()
        {
            return GetTree().GetNodesInGroup(GroupName)
                .Cast<Node3D>();
        }

        private partial class PhysicsProcessSpy : Node
        {
            public event Action<double> PhysicsProcessed;

            public PhysicsProcessSpy(int priority)
            {
                ProcessPhysicsPriority = priority;
            }

            public override void _PhysicsProcess(double delta)
            {
                PhysicsProcessed?.Invoke(delta);
            }
        }
    }
}