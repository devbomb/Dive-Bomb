using Godot;

namespace FastDragon
{
    public partial class SingleDigitCounter3D : Node3D
    {
        [Export] public float RotSpeedDeg = 360 * 2;
        [Export] public int Digit = 0;
        private int _displayedDigit = 0;

        private MeshInstance3D _mesh => GetNode<MeshInstance3D>("%Mesh");

        public override void _Ready()
        {
            UpdateMeshText(_displayedDigit);
        }

        public override void _Process(double deltaD)
        {
            float delta = (float)deltaD;

            if (_displayedDigit != Digit)
            {
                var targetRotDeg = new Vector3(0, 90, 0);
                _mesh.RotationDegrees = _mesh.RotationDegrees.MoveToward(targetRotDeg, RotSpeedDeg * delta);

                if (_mesh.RotationDegrees.IsEqualApprox(targetRotDeg))
                {
                    _mesh.RotationDegrees = new Vector3(0, -90, 0);
                    _displayedDigit = Digit;
                    UpdateMeshText(_displayedDigit);
                }
            }
            else
            {
                _mesh.RotationDegrees = _mesh.RotationDegrees.MoveToward(Vector3.Zero, RotSpeedDeg * delta);
            }

            // On top of the spin counting, make it always sway a little too
            Rotation = new Vector3(
                0,
                Mathf.Sin(3 * Time.GetTicksMsec() / 1000f) * Mathf.DegToRad(22.5f),
                0
            );
        }

        private void UpdateMeshText(int digit)
        {
            ((TextMesh)_mesh.Mesh).Text = digit.ToString();
        }
    }
}