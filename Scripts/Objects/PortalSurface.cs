using System;
using Godot;

namespace FastDragon
{
    public partial class PortalSurface : Node3D
    {
        [Export] public Godot.Environment Skybox {get; private set;}

        private Camera3D _portalCamera => GetNode<Camera3D>("%PortalCamera");
        private Camera3D _mainCamera => GetTree().Root.GetCamera3D();

        public override void _Process(double delta)
        {
            _portalCamera.GlobalPosition = _mainCamera.GlobalPosition;
            _portalCamera.GlobalRotation = _mainCamera.GlobalRotation;
            _portalCamera.Environment = Skybox;
        }
    }
}