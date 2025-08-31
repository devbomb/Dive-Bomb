using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Godot;

namespace FastDragon
{
    public partial class SaveFileManager : RefCounted
    {
        public static SaveFile Current = new SaveFile();

        public static void Reset()
        {
            Current = new SaveFile();
        }
    }
}