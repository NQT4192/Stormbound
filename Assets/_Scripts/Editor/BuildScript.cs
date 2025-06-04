using System;
using UnityEditor;

namespace _Scripts.Editor
{
    public class BuildScript
    {
        [MenuItem("Build/Build All")]
        public static void BuildAll()
        {
            BuildWindowsServer();
            BuildLinuxServer();
            BuildWindowsClient();
        }

        [MenuItem("Build/Build Server (Windows)")]
        public static void BuildWindowsServer()
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = "Builds/Windows/Server/Server.exe",
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                options = BuildOptions.CompressWithLz4HC
            };

            Console.WriteLine("Building Server (Windows)...");
            BuildPipeline.BuildPlayer(buildPlayerOptions);
            Console.WriteLine("Built Server (Windows).");
        }

        [MenuItem("Build/Build Server (Linux)")]
        public static void BuildLinuxServer()
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = "Builds/Linux/Server/Server.x86_64",
                target = BuildTarget.StandaloneLinux64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                options = BuildOptions.CompressWithLz4HC
            };

            Console.WriteLine("Building Server (Linux)...");
            BuildPipeline.BuildPlayer(buildPlayerOptions);
            Console.WriteLine("Built Server (Linux).");
        }

        [MenuItem("Build/Build Client (Windows)")]
        public static void BuildWindowsClient()
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = "Builds/Windows/Client/Client.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CompressWithLz4HC
            };

            Console.WriteLine("Building Client (Windows)...");
            BuildPipeline.BuildPlayer(buildPlayerOptions);
            Console.WriteLine("Built Client (Windows).");
        }
    }
}
