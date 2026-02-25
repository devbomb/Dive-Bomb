using System;
using Godot;

namespace FastDragon
{
    public partial class GlassPaneSpawnerFX : Node3D
    {
        [ExportCategory("Internal")]
        [Export] public AnimationPlayer Animator;
        [Export] public MeshInstance3D SolidMesh;
        [Export] public MeshInstance3D LiquidMesh;
        [Export] public MeshInstance3D FadeCurtainMesh;

        public void Initialize(Mesh mesh, float animDuration)
        {
            SolidMesh.Mesh = mesh;
            LiquidMesh.Mesh = mesh;
            FadeCurtainMesh.Mesh = mesh;

            var aabb = mesh.GetAabb();
            LiquidMesh.SetInstanceShaderParameter("container_height", aabb.Size.Y);
            LiquidMesh.SetInstanceShaderParameter("radius", Mathf.Max(aabb.Size.X, aabb.Size.Z));

            var animation = Animator.GetAnimation("Fill");
            Animator.Play("Fill", customSpeed: animation.Length / animDuration);
        }
    }
}