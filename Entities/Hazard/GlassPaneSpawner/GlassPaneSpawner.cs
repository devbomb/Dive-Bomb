using Godot;
using System;

namespace FastDragon
{
    public partial class GlassPaneSpawner : StaticBody3D
    {
        private static readonly PackedScene FxScene =
            ResourceLoader.Load<PackedScene>("res://Entities/Hazard/GlassPaneSpawner/GlassPaneSpawnerFX.tscn");

        [Export] public double SpawnIntervalSeconds;

        private readonly Node3D _objectPool = new();
        private readonly Node3D _activePanes = new();
        private readonly GlassPaneSpawnerFX _fx = FxScene.Instantiate<GlassPaneSpawnerFX>();

        public GlassPaneSpawner()
        {
            AddChild(_objectPool);
            AddChild(_activePanes);
            AddChild(_fx);

            _objectPool.ProcessMode = ProcessModeEnum.Disabled;
            _objectPool.Visible = false;
        }
    }
}
