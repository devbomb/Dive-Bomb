using Godot;

namespace FastDragon
{
    public partial class CountingLabel : Control
    {
        [Export] public int Value;
        [Export] public double CountingInterval = 1.0 / 30;

        private int _displayedValue;
        private double _timer;

        private Label _textSizeLabel => GetNode<Label>("%TextSizeLabel");
        private SpinningCounter3D _spinningCounter => GetNode<SpinningCounter3D>("%SpinningCounter");

        public override void _Ready()
        {
            _displayedValue = Value;
            _textSizeLabel.Text = _displayedValue.ToString();
            _spinningCounter.Value = Value;
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

                    _textSizeLabel.Text = _displayedValue.ToString();
                    _spinningCounter.Value = Value;
                }
            }
        }

        public void ForceSet(int value)
        {
            Value = value;
            _displayedValue = value;
            _textSizeLabel.Text = _displayedValue.ToString();
            _spinningCounter.Value = value;

            _timer = CountingInterval;
        }
    }
}