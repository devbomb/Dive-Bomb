using System;
using Godot;

namespace FastDragon
{
    public partial class GemFactory : Node
    {
        [Export] public PackedScene Red;
        [Export] public PackedScene Green;
        [Export] public PackedScene Purple;
        [Export] public PackedScene Yellow;
        [Export] public PackedScene Magenta;

        private static GemFactory _instance;

        public override void _Ready()
        {
            _instance = this;
        }

        public static Gem Create(GemColor color)
        {
            PackedScene prefab = color switch
            {
                GemColor.Red => _instance.Red,
                GemColor.Green => _instance.Green,
                GemColor.Purple => _instance.Purple,
                GemColor.Yellow => _instance.Yellow,
                GemColor.Magenta => _instance.Magenta,
                _ => throw new ArgumentException()
            };

            return prefab.Instantiate<Gem>();
        }
    }
}