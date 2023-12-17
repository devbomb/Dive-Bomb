using Godot;

namespace FastDragon
{
    [Tool]
    [GlobalClass]
    public partial class MeshLabel3D : Node3D
    {
        [Export] public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }
        private string _text = "Hello";

        [Export] public Material Material
        {
            get => _material;
            set => SetProperty(ref _material, value);
        }
        private Material _material;

        [Export] public Font Font
        {
            get => _font;
            set => SetProperty(ref _font, value);
        }
        private Font _font;

        [Export] public int FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }
        private int _fontSize = 12;

        private readonly MeshInstance3D _meshInstance = new MeshInstance3D();
        private readonly TextMesh _textMesh = new TextMesh();

        public override void _Ready()
        {
            _meshInstance.Mesh = _textMesh;
            AddChild(_meshInstance);

            Refresh();
        }

        private void Refresh()
        {
            _textMesh.Material = Material;
            _textMesh.Font = Font;
            _textMesh.FontSize = FontSize;
            _textMesh.Text = Text;
        }

        private void SetProperty<T>(ref T storage, T value)
        {
            storage = value;
            Refresh();
        }
    }
}