using Godot;

namespace FastDragon
{
    public partial class BlobShadow : Node3D
    {
        [Export] public CharacterBody3D Body;

        public override void _PhysicsProcess(double delta)
        {
            var collision = Body.MoveAndCollide(
                Vector3.Down * 10,
                testOnly: true,
                recoveryAsCollision: true
            );

            if (collision == null)
            {
                Visible = false;
                return;
            }

            Visible = true;

            var position = Body.GlobalPosition;
            position.Y = collision.GetPosition().Y;
            GlobalPosition = position + collision.GetNormal() * 0.001f;
            GlobalRotation = collision.GetNormal().ForwardToEulerAnglesRad();
        }
    }
}