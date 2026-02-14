using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public static class TargetNameExtensions
    {
        public static TNode FindNodeByTargetName<TNode>(this Node _, string targetname) where TNode : class
        {
            // HACK: Build a shortcut dictionary and stash it away in the scene's
            // metadata, so we don't need to walk the tree every time.
            //
            // We're keeping it in the scene's metadata instead of some singleton
            // to avoid leaving behind a stale reference when switching to a
            // different level.
            var scene = _.GetTree().CurrentScene;

            const string metadataKey = "TargetNameToNodeDict";
            if (!scene.HasMeta(metadataKey))
            {
                var dict = _.GetTree()
                    .CurrentScene
                    .EnumerateDescendants()
                    .Where(n => !string.IsNullOrEmpty(n.Get("targetname").AsString()))
                    .ToDictionary(n => n.Get("targetname").AsString());

                scene.SetMeta(metadataKey, new Godot.Collections.Dictionary<string, Node>(dict));
            }

            var nodesByTargetName = scene.GetMeta(metadataKey).AsGodotDictionary<string, Node>();
            if (!nodesByTargetName.ContainsKey(targetname))
            {
                throw new Exception($"Could not find a Node with a targetname of {targetname}");
            }

            Node node = nodesByTargetName[targetname];
            if (node is not TNode)
            {
                throw new Exception($"Expected Node with targetname of {targetname} to be a {typeof(TNode)}, but it is actually a {node.GetType()}");
            }

            return node as TNode;
        }
    }
}