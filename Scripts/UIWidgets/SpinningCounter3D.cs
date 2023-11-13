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

        private List<int> _digits = new List<int>();

        public override void _Process(double delta)
        {
            UpdateDigitsList();
            EnsureEnoughChildren();

            int numChildren = GetChildCount();
            for (int i = 0; i < GetChildCount(); i++)
            {
                var child = GetChild<SingleDigitCounter3D>(numChildren - i - 1);

                child.Digit = i < _digits.Count
                    ? _digits[i]
                    : 0;
            }
        }

        private void UpdateDigitsList()
        {
            _digits.Clear();
            int value = Math.Abs(Value);

            do
            {
                _digits.Add(value % 10);
                value /= 10;
            }
            while (value > 0);
        }

        private void EnsureEnoughChildren()
        {
            while (GetChildCount() < _digits.Count)
            {
                var child = SingleDigitPrefab.Instantiate<SingleDigitCounter3D>();
                AddChild(child);
            }

            while (GetChildCount() > _digits.Count)
            {
                RemoveChild(GetChild(0));
            }

            for (int i = 0; i < _digits.Count; i++)
            {
                var child = GetChild<Node3D>(i);
                child.Position = Vector3.Right * i * DigitSeparation;
            }
        }
    }
}