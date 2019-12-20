using System.IO;
using System;

namespace Unity.VersionControl.Git
{
    [UnityEditor.InitializeOnLoad]
    public class UnityAPIWrapper : UnityEditor.ScriptableSingleton<UnityAPIWrapper>
    {
        static UnityAPIWrapper()
        {
#if UNITY_2018_2_OR_NEWER
            UnityEditor.Editor.finishedDefaultHeaderGUI += editor => {
                UnityShim.Raise_Editor_finishedDefaultHeaderGUI(editor);
            };
#endif
        }
    }
}
