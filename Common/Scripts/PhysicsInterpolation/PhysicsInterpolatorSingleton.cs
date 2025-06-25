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

        private bool AllowInterpolation => UserSettings.Instance.UsePhysicsInterpolation;
        private double _physicsDelta = 1;
        private double _timer;

        private record struct TruePos(Transform3D Current, Transform3D Prev);
        private Dictionary<Node3D, TruePos> _truePos = new Dictionary<Node3D, TruePos>();
        private Dictionary<Node3D, TruePos> _truePosSwap = new Dictionary<Node3D, TruePos>();

        public PhysicsInterpolatorSingleton()
        {
            Instance = this;
        }

        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.Always;

            RenderingServer.FramePostDraw += OnFramePostDraw;

            ProcessPriority = int.MaxValue - 1; // -1 to allow for AnchoredLine3D to go later than it
            ProcessPhysicsPriority = int.MaxValue;
        }

        public override void _PhysicsProcess(double delta)
        {
            _physicsDelta = delta;
            _timer -= delta;

            // Save the current and previous position of every node, both so
            // we can interpolate between them before rendering, AND so we can
            // restore it back to its true position after rendering is done.
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

        public override void _Process(double delta)
        {
            _timer += delta;

            // I _wanted_ to to use the FramePreDraw signal instead of _Process,
            // but that signal apparently happpens too late for any position
            // changes to affect the rendered frame.  I guess all of the
            // triangles have already been queued up by then, or something.
            //
            // Thankfully, _Process with a high ProcessPriority is a close-enough
            // substitute.
            OnFramePreDraw();
        }

        private void OnFramePreDraw()
        {
            if (!AllowInterpolation)
                return;

            // Temporarily move all interpolated objects to their interpolated
            // position.  We will move them back to their real position after
            // rendering is done.
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
                node.ForceUpdateTransform();
            }
        }

        private void OnFramePostDraw()
        {
            if (!AllowInterpolation)
                return;

            // Now that the frame has been drawn, move all objects back to their
            // true positions.
            foreach (var node in AllInterpolatableNodes())
            {
                node.Transform = GetTruePosStruct(node).Current;
                node.ForceUpdateTransform();
            }
        }

        private IEnumerable<Node3D> AllInterpolatableNodes()
        {
            return GetTree().GetNodesInGroup(GroupName)
                .Cast<Node3D>();
        }

        private TruePos GetTruePosStruct(Node3D node)
        {
            if (!_truePos.TryGetValue(node, out var truePos))
            {
                return new TruePos(node.Transform, node.Transform);
            }

            return truePos;
        }

        public void ResetPhysicsInterpolation3D(Node3D node)
        {
            _truePos.Remove(node);
        }
    }
}