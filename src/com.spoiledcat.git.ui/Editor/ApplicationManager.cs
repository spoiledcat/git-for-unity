using System;
using System.Reflection;
using System.Threading;
using Unity.Editor.Tasks;
using Unity.VersionControl.Git.UI;
using UnityEditor;
using UnityEngine.Events;

namespace Unity.VersionControl.Git
{
    class ApplicationManager : ApplicationManagerBase
    {
        private const string QuitActionFieldName = "editorApplicationQuit";
        private const BindingFlags quitActionBindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

        private FieldInfo quitActionField;

        public ApplicationManager(IMainThreadSynchronizationContext synchronizationContext,
            IGitEnvironment environment)
            : base(synchronizationContext as SynchronizationContext, environment)
        {
            FirstRun = ApplicationCache.Instance.FirstRun;
            InstanceId = ApplicationCache.Instance.InstanceId;

            ListenToUnityExit();
            Initialize();
        }

        public override void InitializeUI()
        {
            Logger.Trace("Restarted {0}", Environment.Repository != null ? Environment.Repository.LocalPath : "null");
            EnvironmentCache.Instance.Flush();

            isBusy = false;
            LfsLocksModificationProcessor.Initialize(Environment, Platform);
            ProjectWindowInterface.Initialize(this);
            HierarchyWindowInterface.Initialize();
            var window = Window.GetWindow();
            if (window != null)
                window.InitializeWindow(this);
            SetProjectToTextSerialization();
        }

        protected void SetProjectToTextSerialization()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
        }

        protected override void InitializationComplete()
        {
            ApplicationCache.Instance.Initialized = true;
        }

        private void ListenToUnityExit()
        {
            EditorApplicationQuit = (UnityAction)Delegate.Combine(EditorApplicationQuit, new UnityAction(Dispose));

            // clean up when entering play mode
#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged += change => {
                if (change == PlayModeStateChange.EnteredPlayMode)
#else
            EditorApplication.playmodeStateChanged += () => {
                if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
#endif
                {
                    Dispose();
                }
            };
        }

        private UnityAction EditorApplicationQuit
        {
            get
            {
                SecureQuitActionField();
                return (UnityAction)quitActionField.GetValue(null);
            }
            set
            {
                SecureQuitActionField();
                quitActionField.SetValue(null, value);
            }
        }

        private void SecureQuitActionField()
        {
            if (quitActionField == null)
            {
                quitActionField = typeof(EditorApplication).GetField(QuitActionFieldName, quitActionBindingFlags);

                if (quitActionField == null)
                {
                    throw new InvalidOperationException("Unable to reflect EditorApplication." + QuitActionFieldName);
                }
            }
        }

        private bool disposed = false;
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    disposed = true;
                }
            }
            base.Dispose(disposing);
        }
    }
}
