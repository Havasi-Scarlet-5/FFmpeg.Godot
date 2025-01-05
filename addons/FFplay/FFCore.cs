

using System.IO;
using System.Runtime.InteropServices;

namespace FFmpeg.Godot
{
    static class FFCore
    {
        public static void Initialize()
        {
            // Call this one time only for loading library files
            foreach (string file in Directory.EnumerateFiles($"addons/FFplay/libs"))
                NativeLibrary.Load(file);
        }
    }
}