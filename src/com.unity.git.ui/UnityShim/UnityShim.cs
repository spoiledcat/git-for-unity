using System;
using UnityEditor;
namespace Unity.VersionControl.Git
{
    public static class UnityShim
    {
        public static event Action<Editor> Editor_finishedDefaultHeaderGUI;
        public static void Raise_Editor_finishedDefaultHeaderGUI(Editor editor)
        {
            if (Editor_finishedDefaultHeaderGUI != null)
                Editor_finishedDefaultHeaderGUI(editor);
        }
    }
}