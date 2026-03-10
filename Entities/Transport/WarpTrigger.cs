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

        private Node3D _entranceOrigin;
        private Node3D _exitOrigin;

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
            SetUpEntranceAndExit();
        }

        private void SetUpEntranceAndExit()
        {
            _entranceOrigin = new Node3D();
            AddChild(_entranceOrigin);
            _entranceOrigin.GlobalPosition = GetEntrancePoint();
            _entranceOrigin.GlobalRotation = GetEntranceNormal().ForwardToEulerAnglesRad();

            _exitOrigin = new Node3D();
            AddChild(_exitOrigin);
            _exitOrigin.GlobalPosition = GetEntrancePoint();
            _exitOrigin.GlobalRotation = (-GetEntranceNormal()).ForwardToEulerAnglesRad();
        }

        private void OnBodyEntered(Node3D body)
        {
            if (body is not Player player)
                return;

            Transform3D playerPosRelativeToSrc = _entranceOrigin.GlobalTransform.Inverse() * player.GlobalTransform;
            Transform3D cameraPosRelativeToPlayer = player.GlobalTransform.Inverse() * player.Camera.GlobalTransform;

            // Teleport the player
            var destWarp = this.FindNodeByTargetName<WarpTrigger>(target);
            player.GlobalTransform = destWarp._exitOrigin.GlobalTransform * playerPosRelativeToSrc;
            player.GlobalPosition += destWarp.GetEntranceNormal();
            player.ResetPhysicsInterpolation3D();

            // Teleport the camera
            player.Camera.StartManhandling(player.GlobalTransform * cameraPosRelativeToPlayer);
            player.Camera.StartFollowing(0.1f);
            player.Camera.ResetPhysicsInterpolation3D();

            // TODO: Rotate the player's _velocity_ too
        }

        private Vector3 GetEntranceNormal()
        {
            var entranceFace = GetFaceMetadata()
                .FirstOrDefault(f => f.TextureName == EntranceTextureName);

            if (entranceFace == null)
                throw new Exception($"Warp trigger has no faces with the {EntranceTextureName} texture");

            return entranceFace.Normal;
        }

        private Vector3 GetEntrancePoint()
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

            return ToGlobal(average);
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