using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class WarpTrigger : Area3D
    {
        private const string EntranceTextureName = "Textures/WarpEntrance";

        [Export] public string targetname;
        [Export] public string target;

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
        }

        private void OnBodyEntered(Node3D body)
        {
            if (body is not Player player)
                return;

            var destWarp = this.FindNodeByTargetName<WarpTrigger>(target);
            player.GlobalPosition = destWarp.GetEntrancePoint() + destWarp.GetEntranceNormal();
            player.ResetPhysicsInterpolation3D();
            player.Velocity = Vector3.Zero;

            // TODO: Rotate the player
            // TODO: Rotate the player's _velocity_ too
            // TODO: Maintain the player's relative position to the trigger
            // TODO: Teleport and rotate the camera, too.
        }

        public Vector3 GetEntranceNormal()
        {
            var entranceFace = GetFaceMetadata()
                .FirstOrDefault(f => f.TextureName == EntranceTextureName);

            if (entranceFace == null)
                throw new Exception($"Warp trigger has no faces with the {EntranceTextureName} texture");

            return entranceFace.Normal;
        }

        public Vector3 GetEntrancePoint()
        {
            // Even if the entrance face _looks_ like a quad, it's actually
            // secretly two triangles.  Thankfully, the average of those two
            // triangles' center points will equal the center point of the
            // quad they form.
            var entranceFaces = GetFaceMetadata()
                .Where(f => f.TextureName == EntranceTextureName)
                .ToArray();

            if (!entranceFaces.Any())
                throw new Exception($"Warp trigger has no faces with the {EntranceTextureName} texture");

            var sum = Vector3.Zero;
            foreach (var face in entranceFaces)
                sum += face.Position;

            var average = sum / entranceFaces.Length;

            return GlobalPosition + average;
        }

        private IEnumerable<FaceMetadata> GetFaceMetadata()
        {
            var metadata = GetMeta("func_godot_mesh_data").AsGodotDictionary();
            var textureNames = metadata["texture_names"].AsStringArray();
            var textures = metadata["textures"].AsInt32Array();
            var normals = metadata["normals"].AsVector3Array();
            var positions = metadata["positions"].AsVector3Array();

            for (int i = 0; i < textures.Length; i++)
            {
                yield return new FaceMetadata
                {
                    Normal = normals[i],
                    Position = positions[i],
                    TextureName = textureNames[textures[i]],
                };
            }
        }

        private class FaceMetadata
        {
            public Vector3 Normal;
            public Vector3 Position;
            public string TextureName;
        }
    }
}