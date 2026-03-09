using System;
using Godot;

namespace FastDragon
{
    public partial class WarpTrigger : Area3D
    {
        [Export] public string targetname;
        [Export] public string target;
    }
}