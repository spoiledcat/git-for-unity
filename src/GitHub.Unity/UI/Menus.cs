using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity.UI
{
    [InitializeOnLoad]
    class Menus : ScriptableObject
    {
        private const string Menu_Window_GitHub = "Window/GitHub/Open";
        private const string Menu_Window_GitHub_Command_Line = "Window/GitHub/Command Line";

        [MenuItem(Menu_Window_GitHub)]
        public static void Window_GitHub()
        {
            ShowWindow(EntryPoint.ApplicationManager);
        }

        [MenuItem(Menu_Window_GitHub_Command_Line)]
        public static void GitHub_CommandLine()
        {
            EntryPoint.ApplicationManager.ProcessManager.RunCommandLineWindow(NPath.CurrentDirectory);
            EntryPoint.ApplicationManager.UsageTracker.IncrementApplicationMenuMenuItemCommandLine();
        }

#if DEVELOPER_BUILD

        [MenuItem("GitHub/Select Window")]
        public static void GitHub_SelectWindow()
        {
            var window = Resources.FindObjectsOfTypeAll(typeof(Window)).FirstOrDefault() as Window;
            Selection.activeObject = window;
        }

        [MenuItem("GitHub/Restart")]
        public static void GitHub_Restart()
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
