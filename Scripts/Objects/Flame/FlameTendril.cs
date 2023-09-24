using Godot;

namespace FastDragon
{
    [Tool]
    public partial class FlameTendril : Node3D
    {
        [Export] public float Length
        {
            get => _length;
            set => SetProperty(ref _length, value);
        }

        [Export] public float Radius
        {
            get => _radius;
            set => SetProperty(ref _radius, value);
        }

        private float _length = 1.5f;
        private float _radius = 0.25f;

        private Node3D _sphere => GetNode<Node3D>("%Sphere");

        private void UpdateSize()
        {
            _sphere.Scale = Vector3.One * _radius;
            _sphere.Position = Vector3.Forward * _length;
        }

        private void SetProperty(ref float storage, float value)
        {
            storage = value;
            UpdateSize();
        }
    }
}