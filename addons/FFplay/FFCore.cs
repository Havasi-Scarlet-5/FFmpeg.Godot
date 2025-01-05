

using System.IO;
using System.Runtime.InteropServices;
using Godot;

namespace FFmpeg.Godot
{
    partial class FFCore : Node
    {
        public override void _Ready()
        {
            // Call this one time only for loading library files using autoload
            Initialize();
        }

        public static void Initialize()
        {
            foreach (string file in Directory.EnumerateFiles($"addons/FFplay/libs"))
                NativeLibrary.Load(file);
        }
    }
}