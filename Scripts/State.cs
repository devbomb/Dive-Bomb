using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public abstract partial class State : Node
    {
        public virtual void OnStateEntered() {}
        public virtual void OnStateExited() {}
    }
}