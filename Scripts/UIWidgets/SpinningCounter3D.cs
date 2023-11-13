using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class SpinningCounter3D : Node3D
    {
        [Export] public PackedScene SingleDigitPrefab;
        [Export] public float DigitSeparation = 0.125f;
        [Export] public int Value;

        public override void _Process(double delta)
        {
            int[] digits = EnumerateDigitsIn(Value).ToArray();

            int numChildren = GetChildCount();
            for (int i = 0; i < numChildren; i++)
            {
                var child = GetChild<SingleDigitCounter3D>(numChildren - i - 1);

                child.Digit = i < digits.Length
                    ? digits[i]
                    : 0;
            }
        }

        private IEnumerable<int> EnumerateDigitsIn(int value)
        {
            value = Math.Abs(value);

            do
            {
                yield return value % 10;
                value /= 10;
            }
            while (value > 0);
        }
    }
}