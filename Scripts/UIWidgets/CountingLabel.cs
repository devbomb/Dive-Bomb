using Godot;

namespace FastDragon
{
    public partial class CountingLabel : Control
    {
        [Export] public int Value;
        [Export] public double CountingInterval = 1.0 / 30;

        private int _displayedValue;
        private double _timer;

        private Label _label => GetNode<Label>("%Label");

        public override void _Ready()
        {
            _displayedValue = Value;
            _label.Text = _displayedValue.ToString();
        }

        public override void _Process(double delta)
        {
            if (Value != _displayedValue)
            {
                _timer -= CountingInterval;

                if (_timer <= 0)
                {
                    _timer += CountingInterval;

                    if (_displayedValue > Value)
                        _displayedValue--;
                    else
                        _displayedValue++;

                    _label.Text = _displayedValue.ToString();
                }
            }
        }
    }
}