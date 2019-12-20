using System;
namespace Unity.VersionControl.Git
{
    public static class UnityShim
    {
        public static event Action<UnityEditor.Editor> Editor_finishedDefaultHeaderGUI;
        public static void Raise_Editor_finishedDefaultHeaderGUI(UnityEditor.Editor editor)
        {
            if (Editor_finishedDefaultHeaderGUI != null)
                Editor_finishedDefaultHeaderGUI(editor);
        }
    }
}
