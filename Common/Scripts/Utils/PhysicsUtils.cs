#nullable enable
using System;
using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public static class PhysicsUtils
    {
        private static readonly Lazy<IReadOnlyDictionary<string, uint>> _layerNameToFlag = new(() =>
        {
            var dict = new Dictionary<string, uint>();
            uint mask = 1;
            for (int i = 0; i < 32; i++)
            {
                string settingName = $"layer_names/3d_physics/layer_{i + 1}";
                string layerName = ProjectSettings.GetSetting(settingName).AsString();
                dict[layerName] = mask;

                mask *= 2;
            }

            return dict;
        });
        public static uint CollisionFlagsFromNames(params string[] layerNames)
        {
            uint flags = 0;

            foreach (var layerName in layerNames)
            {
                flags |= _layerNameToFlag.Value[layerName];
            }

            return flags;
        }

        /// <summary>
        /// A convenient way to do a raycast.
        /// It allocates, though, so use a <see cref="RayCast3D"/> node
        /// instead if you need to check it every frame.
        /// </summary>
        public static bool RaycastInefficient(
            Vector3 from,
            Vector3 to,
            uint collisionMask
        ) => RaycastInefficient(from, to, collisionMask, out _);

        /// <summary>
        /// A convenient way to do a raycast.
        /// It allocates, though, so use a <see cref="RayCast3D"/> node
        /// instead if you need to check it every frame.
        /// </summary>
        public static bool RaycastInefficient(
            Vector3 from,
            Vector3 to,
            uint collisionMask,
            out RaycastResult result
        )
        {
            var tree = (SceneTree)Godot.Engine.GetMainLoop();
            var spaceState = tree.Root.FindWorld3D().DirectSpaceState;

            var query = new PhysicsRayQueryParameters3D
            {
                From = from,
                To = to,
                CollisionMask = collisionMask,
            };

            var dictResult = spaceState.IntersectRay(query);
            if (dictResult.Count <= 0)
            {
                result = default;
                return false;
            }

            result = new RaycastResult
            {
                Collider = dictResult["collider"].AsGodotObject(),
                Position = dictResult["position"].AsVector3(),
                Normal = dictResult["normal"].AsVector3(),
            };
            return true;
        }

        public struct RaycastResult
        {
            public GodotObject Collider;
            public Vector3 Position;
            public Vector3 Normal;
        }
    }
}