using UnityEditor;
using UnityEngine;

namespace Unity.VersionControl.Git.UI
{
    using IO;

    [InitializeOnLoad]
    class Menus
    {
#if GFU_DEBUG_BUILD

        private const string Menu_Window_Git = "Window/Git/Open";
        private const string Menu_Window_Git_Command_Line = "Window/Git/Command Line";
#else
        private const string Menu_Window_Git = "Window/Git";
        private const string Menu_Window_Git_Command_Line = "Window/Git Command Line";
#endif

        [MenuItem(Menu_Window_Git)]
        public static void Window_Git()
        {
            ShowWindow(EntryPoint.ApplicationManager);
        }

        [MenuItem(Menu_Window_Git_Command_Line)]
        public static void Git_CommandLine()
        {
            //EntryPoint.ApplicationManager.ProcessManager.RunCommandLineWindow(SPath.CurrentDirectory);
            //EntryPoint.ApplicationManager.UsageTracker.IncrementApplicationMenuMenuItemCommandLine();
        }

#if GFU_DEBUG_BUILD

        [MenuItem("Git/Select Window")]
        public static void Git_SelectWindow()
        {
            var window = Resources.FindObjectsOfTypeAll(typeof(Window)).FirstOrDefault() as Window;
            Selection.activeObject = window;
        }

        [MenuItem("Git/Restart")]
        public static void Git_Restart()
        {
            EntryPoint.Restart();
        }
#endif

        public static void ShowWindow(IApplicationManager applicationManager)
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
            var window = EditorWindow.GetWindow<Window>(type);
            window.InitializeWindow(applicationManager);
            window.Show();
        }

    }
}
