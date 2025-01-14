using System.IO;
using System.Runtime.InteropServices;
using Godot;

namespace FFmpeg.Godot
{
    partial class FFCore : Node
    {
        public override void _EnterTree()
        {
            // Call this one time only for loading library files using autoload
            Initialize();
        }

        public static void Initialize()
        {
            // We don't need this on exported build
            if (OS.HasFeature("editor"))
                foreach (string file in Directory.EnumerateFiles($"addons/FFplay/libs"))
                    NativeLibrary.Load(file);
        }
    }
}