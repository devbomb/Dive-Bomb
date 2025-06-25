using Godot;

namespace FastDragon
{
    public partial class ContextuallyHiddenControl : Control
    {
        [Export] public Variant ObservedValue;
        [Export] public double AppearDuration = 0.5;
        [Export] public double StayVisibleDuration = 3;

        private Variant _previousObservedValue;
        private double _timer;

        public override void _Process(double delta)
        {
            if (!ObservedValue.Equals(_previousObservedValue))
            {
                _previousObservedValue = ObservedValue;
                _timer = StayVisibleDuration;
            }

            if (_timer > 0)
            {
                _timer -= delta;
            }

            float targetY = _timer > 0
                ? 1
                : 0;
            var targetScale = new Vector2(1, targetY);

            float speed = (float)(delta / AppearDuration);
            Scale = Scale.MoveToward(targetScale, speed);
        }
    }
}