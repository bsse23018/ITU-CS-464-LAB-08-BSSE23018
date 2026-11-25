#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lab07.EditorTools
{
    /// <summary>
    /// Editor helpers for Lab 07.
    ///   Lab07 > Create Menu Scene          : creates Assets/Lab07/Scenes/Menu.unity (one GameObject with the App component) and sets build settings.
    ///   Lab07 > Capture Screenshots        : plays the app and saves one colour PNG per screen into Capture/shots (see README).
    ///   Lab07 > Record Demo Frames         : plays the full navigation flow and dumps video frames into Capture/video_frames.
    /// A tiny command pump also lets scripts trigger these from outside the editor (Capture/pump.txt).
    /// </summary>
    [InitializeOnLoad]
    public static class CapturePump
    {
        const string ScenePath = "Assets/Lab07/Scenes/Menu.unity";
        static double next;
        static string CapDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../Capture"));

        static CapturePump() { EditorApplication.update += Poll; }

        static void Log(string m) { Directory.CreateDirectory(CapDir); File.AppendAllText(Path.Combine(CapDir, "pump.log"), System.DateTime.Now.ToString("HH:mm:ss") + " " + m + "\n"); Debug.Log("[Lab07] " + m); }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < next) return; next = EditorApplication.timeSinceStartup + .5;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var f = Path.Combine(CapDir, "pump.txt"); if (!File.Exists(f)) return;
            string cmd = File.ReadAllText(f).Trim(); File.Delete(f);
            Log("command: " + cmd);
            if (cmd == "makescene") MakeScene();
            else if (cmd == "shots") Run("shots");
            else if (cmd == "video" || cmd == "restart-video") Run(cmd);
            else if (cmd == "stop") EditorApplication.isPlaying = false;
            else if (cmd == "ping") Log("pong playing=" + EditorApplication.isPlaying + " paused=" + EditorApplication.isPaused + " focused=" + UnityEditorInternal.InternalEditorUtility.isApplicationActive);
            else if (cmd == "checkandroid")
            {
                bool supported = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
                Log("android supported=" + supported);
            }
            else if (cmd == "buildapk")
            {
                BuildApk();
            }
            else if (cmd == "refresh") { AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); Log("refresh"); }
        }

        [MenuItem("Lab07/Create Menu Scene")]
        public static void MakeScene()
        {
            Directory.CreateDirectory("Assets/Lab07/Scenes");
            var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("App"); go.AddComponent<App>();
            EditorSceneManager.SaveScene(sc, ScenePath);
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene("Assets/Lab07/Scenes/Flight.unity", true)
            };
            PlayerSettings.productName = "Nova Rush";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            AssetDatabase.SaveAssets(); Log("scene created");
        }

        [MenuItem("Lab07/Capture Screenshots")] static void Shots() { Run("shots"); }
        [MenuItem("Lab08/Record Demo Frames")] static void Video() { Run("video"); }

        [MenuItem("Lab07/Build Android APK")]
        public static void BuildApk()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Log("ERROR: Android build support is not installed in this Unity editor.");
                return;
            }
            if (!File.Exists(ScenePath)) MakeScene();
            Directory.CreateDirectory("Builds");
            string outPath = Path.GetFullPath("Builds/TayyabRush.apk");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.itu.tayyabrush");
            PlayerSettings.productName = "Tayyab Rush";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = System.Array.ConvertAll(System.Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled), scene => scene.path),
                locationPathName = outPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opt);
            Log("build finished: summary=" + report.summary.result + " path=" + outPath);
        }

        static void Run(string mode)
        {
            if (!File.Exists(ScenePath)) MakeScene();
            Directory.CreateDirectory(CapDir); File.WriteAllText(Path.Combine(CapDir, "tour.flag"), mode);
            var done = Path.Combine(CapDir, "done.txt"); if (File.Exists(done)) File.Delete(done);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode(); Log("entering play mode: " + mode);
        }
    }
}
#endif
