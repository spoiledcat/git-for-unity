using Unity.VersionControl.Git;
using System;
using System.Linq;
using Unity.Editor.Tasks.Logging;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.VersionControl.Git
{
    using IO;

    [AttributeUsage(AttributeTargets.Class)]
    sealed class LocationAttribute : Attribute
    {
        public enum Location { PreferencesFolder, ProjectFolder, LibraryFolder, UserFolder }

        private string relativePath;
        private Location location;

        private string filePath;
        public string FilePath {
            get {
                if (filePath != null) return filePath;

                if (relativePath[0] == '/')
                    relativePath = relativePath.Substring(1);

                if (location == Location.PreferencesFolder)
                    filePath = InternalEditorUtility.unityPreferencesFolder + "/" + relativePath;
                else if (location == Location.UserFolder)
                    filePath = EntryPoint.ApplicationManager.Environment.UserCachePath.Combine(relativePath).ToString(SlashMode.Forward);
                else if (location == Location.LibraryFolder)
                    filePath = EntryPoint.ApplicationManager.Environment.UnityProjectPath.ToSPath().Combine("Library", "gfu", relativePath);

                return filePath;
            }
        }

        public LocationAttribute(string relativePath, Location location)
        {
            Guard.ArgumentNotNullOrWhiteSpace(relativePath, "relativePath");
            this.relativePath = relativePath;
            this.location = location;
        }
    }


    class ScriptObjectSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                    CreateAndLoad();
                return instance;
            }
        }

        protected ScriptObjectSingleton()
        {
            if (instance != null)
            {
                LogHelper.GetLogger<T>().Error("Singleton already exists!");
            }
            else
            {
                instance = this as T;
                System.Diagnostics.Debug.Assert(instance != null);
            }
        }

        private static void CreateAndLoad()
        {
            System.Diagnostics.Debug.Assert(instance == null);

            string filePath = GetFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
                InternalEditorUtility.LoadSerializedFileAndForget(filePath);
            }

            if (instance == null)
            {
                var inst = CreateInstance<T>() as ScriptObjectSingleton<T>;
                inst.hideFlags = HideFlags.HideAndDontSave;
            }

            System.Diagnostics.Debug.Assert(instance != null);
        }

        protected virtual void Save(bool saveAsText)
        {
            if (instance == null)
            {
                LogHelper.GetLogger<T>().Error("Cannot save singleton, no instance!");
                return;
            }

            SPath? locationFilePath = GetFilePath();
            if (locationFilePath != null)
            {
                locationFilePath.Value.Parent.EnsureDirectoryExists();
                InternalEditorUtility.SaveToSerializedFileAndForget(new[] { instance }, locationFilePath, saveAsText);
            }
        }

        private static SPath? GetFilePath()
        {
            return typeof(T).GetCustomAttributes(true)
                .OfType<LocationAttribute>()
                .FirstOrDefault()?.FilePath.ToSPath();
        }
    }
}
