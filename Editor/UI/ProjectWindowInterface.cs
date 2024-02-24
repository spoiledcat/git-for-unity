using System;
using System.Collections.Generic;
using System.Linq;
using SpoiledCat.Git;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Logging;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.VersionControl.Git.UI
{
    using IO;

    class ProjectWindowInterface
    {
        private const string AssetsMenuRequestLock = "Assets/Request Lock";
        private const string AssetsMenuReleaseLock = "Assets/Release Lock";
        private const string AssetsMenuReleaseLockForced = "Assets/Release Lock (forced)";

        private static List<GitStatusEntry> entries = new List<GitStatusEntry>();
        private static List<GitLock> locks = new List<GitLock>();
        private static Dictionary<string, int> guids = new Dictionary<string, int>();
        private static List<string> guidsLocks = new List<string>();
        private static string currentUsername;

        private static IApplicationManager manager;
        private static bool isBusy = false;
        private static ILogging logger;
        private static ILogging Logger { get { return logger = logger ?? LogHelper.GetLogger<ProjectWindowInterface>(); } }
        private static CacheUpdateEvent lastRepositoryStatusChangedEvent;
        private static CacheUpdateEvent lastLocksChangedEvent;
        private static CacheUpdateEvent lastCurrentRemoteChangedEvent;
        private static IRepository Repository { get { return manager != null ? manager.Environment.Repository : null; } }
        private static IPlatform Platform { get { return manager != null ? manager.Platform : null; } }
        private static bool IsInitialized { get { return Repository != null; } }

        public static void Initialize(IApplicationManager theManager)
        {
            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;

            manager = theManager;

            //Platform.Keychain.ConnectionsChanged += UpdateCurrentUsername;
            UpdateCurrentUsername();

            if (IsInitialized)
            {
                Repository.StatusEntriesChanged += RepositoryOnStatusEntriesChanged;
                Repository.LocksChanged += RepositoryOnLocksChanged;
                Repository.CurrentRemoteChanged += RepositoryOnCurrentRemoteChanged;
                ValidateCachedData();
            }
        }

        public static Texture2D GetStatusIconForAssetGUID(string guid)
        {
            if (!guids.TryGetValue(guid, out int index))
                return null;

            var indexLock = guidsLocks.IndexOf(guid);

            if (index < 0 && indexLock < 0)
            {
                return null;
            }

            GitStatusEntry? gitStatusEntry = null;
            GitFileStatus status = GitFileStatus.None;

            if (index >= 0)
            {
                gitStatusEntry = entries[index];
                status = gitStatusEntry.Value.Status;
            }

            var isLocked = indexLock >= 0;
            var texture = Styles.GetFileStatusIcon(status, isLocked);

            if (texture == null)
            {
                var path = gitStatusEntry.HasValue ? gitStatusEntry.Value.Path : string.Empty;
                Logger.Warning("Unable to retrieve texture for Guid:{0} EntryPath:{1} Status: {2} IsLocked:{3}", guid, path, status.ToString(), isLocked);
            }

            return texture;
        }

        private static bool EnsureInitialized()
        {
            if (locks == null)
                locks = new List<GitLock>();
            if (entries == null)
                entries = new List<GitStatusEntry>();
            if (guids == null)
                guids = new Dictionary<string, int>();
            if (guidsLocks == null)
                guidsLocks = new List<string>();
            return IsInitialized;
        }

        private static void ValidateCachedData()
        {
            Repository.CheckAndRaiseEventsIfCacheNewer(CacheType.GitStatus, lastRepositoryStatusChangedEvent);
            Repository.CheckAndRaiseEventsIfCacheNewer(CacheType.GitLocks, lastLocksChangedEvent);
        }

        private static void RepositoryOnStatusEntriesChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastRepositoryStatusChangedEvent.Equals(cacheUpdateEvent))
            {
                lastRepositoryStatusChangedEvent = cacheUpdateEvent;
                entries.Clear();
                entries.AddRange(Repository.CurrentChanges);
                OnStatusUpdate();
            }
        }

        private static void RepositoryOnLocksChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastLocksChangedEvent.Equals(cacheUpdateEvent))
            {
                lastLocksChangedEvent = cacheUpdateEvent;
                locks = Repository.CurrentLocks;
                OnLocksUpdate();
            }
        }

        private static void RepositoryOnCurrentRemoteChanged(CacheUpdateEvent cacheUpdateEvent)
        {
            if (!lastCurrentRemoteChangedEvent.Equals(cacheUpdateEvent))
            {
                lastCurrentRemoteChangedEvent = cacheUpdateEvent;
            }
        }

        private static void UpdateCurrentUsername()
        {
            var username = String.Empty;
            if (Repository != null)
            {
                //Connection connection;
                //if (!string.IsNullOrEmpty(Repository.CloneUrl))
                //{
                //    var host = Repository.CloneUrl
                //                         .ToRepositoryUri()
                //                         .GetComponents(UriComponents.Host, UriFormat.SafeUnescaped);

                //    connection = Platform.Keychain.Connections.FirstOrDefault(x => x.Host == host);
                //}
                //else
                //{
                //    connection = Platform.Keychain.Connections.FirstOrDefault(HostAddress.IsGitHubDotCom);
                //}

                //if (connection != null)
                //{
                //    username = connection.Username;
                //}
            }

            currentUsername = username;
        }

        [MenuItem(AssetsMenuRequestLock, true, 10000)]
        private static bool ContextMenu_CanLock()
        {
            if (!EnsureInitialized())
                return false;
            if (!Repository.CurrentRemote.HasValue)
                return false;
            if (isBusy)
                return false;
            return Selection.objects.Any(IsObjectUnlocked);
        }

        [MenuItem(AssetsMenuReleaseLock, true, 10001)]
        private static bool ContextMenu_CanUnlock()
        {
            if (!EnsureInitialized())
                return false;
            if (!Repository.CurrentRemote.HasValue)
                return false;
            if (isBusy)
                return false;
            return Selection.objects.Any(f => IsObjectLocked(f , true));
        }

        [MenuItem(AssetsMenuReleaseLockForced, true, 10002)]
        private static bool ContextMenu_CanUnlockForce()
        {
            if (!EnsureInitialized())
                return false;
            if (!Repository.CurrentRemote.HasValue)
                return false;
            if (isBusy)
                return false;
            return Selection.objects.Any(IsObjectLocked);
        }

        [MenuItem(AssetsMenuRequestLock, false, 10000)]
        private static void ContextMenu_Lock()
        {
            RunLockUnlock(EntryPoint.ApplicationManager.TaskManager, IsObjectUnlocked, CreateLockObjectTask, Localization.RequestLockActionTitle, "Failed to lock: no permissions");
        }

        [MenuItem(AssetsMenuReleaseLock, false, 10001)]
        private static void ContextMenu_Unlock()
        {
            RunLockUnlock(EntryPoint.ApplicationManager.TaskManager, IsObjectLocked, x => CreateUnlockObjectTask(x, false), Localization.ReleaseLockActionTitle, "Failed to unlock: no permissions");
        }

        [MenuItem(AssetsMenuReleaseLockForced, false, 10002)]
        private static void ContextMenu_UnlockForce()
        {
            RunLockUnlock(EntryPoint.ApplicationManager.TaskManager, IsObjectLocked, x => CreateUnlockObjectTask(x, true), Localization.ReleaseLockActionTitle, "Failed to unlock: no permissions");
        }

        private static void RunLockUnlock(ITaskManager taskManager, Func<Object, bool> selector, Func<Object, ITask> creator, string title, string errorMessage)
        {
            isBusy = true;
            var taskQueue = new TaskQueue(taskManager);
            foreach (var lockedObject in Selection.objects.Where(selector))
            {
                taskQueue.Queue(creator(lockedObject));
            }
            taskQueue.FinallyInUI((success, exception) =>
            {
                if (!success)
                {
                    var error = exception.Message;
                    if (error.Contains("exit status 255"))
                        error = errorMessage;
                    EditorUtility.DisplayDialog(title, error, Localization.Ok);
                }
                isBusy = false;
            });
            taskQueue.Start();
        }

        private static bool IsObjectUnlocked(Object selected)
        {
            if (selected == null)
                return false;

            SPath assetPath = AssetDatabase.GetAssetPath(selected.GetInstanceID()).ToSPath();
            SPath repositoryPath = assetPath.RelativeToRepository(manager.Environment);

            var alreadyLocked = locks.Any(x => repositoryPath == x.Path);
            if (alreadyLocked)
                return false;

            GitFileStatus status = GitFileStatus.None;
            if (entries != null)
            {
                status = entries.FirstOrDefault(x => repositoryPath == x.Path.ToSPath()).Status;
            }
            return status != GitFileStatus.Untracked && status != GitFileStatus.Ignored;
        }

        private static bool IsObjectLocked(Object selected)
        {
            return IsObjectLocked(selected, false);
        }

        private static bool IsObjectLocked(Object selected, bool isLockedByCurrentUser)
        {
            if (selected == null)
                return false;

            SPath assetPath = AssetDatabase.GetAssetPath(selected.GetInstanceID()).ToSPath();
            SPath repositoryPath = assetPath.RelativeToRepository(manager.Environment);

            return locks.Any(x => repositoryPath == x.Path && (!isLockedByCurrentUser || x.Owner.Name == currentUsername));
        }

        private static ITask CreateUnlockObjectTask(Object selected, bool force)
        {
            SPath assetPath = AssetDatabase.GetAssetPath(selected.GetInstanceID()).ToSPath();
            SPath repositoryPath = assetPath.RelativeToRepository(manager.Environment);

            var task = Repository.ReleaseLock(repositoryPath, force);
            //task.OnEnd += (_, s, __) => { if (s) manager.TaskManager.Run(manager.UsageTracker.IncrementUnityProjectViewContextLfsUnlock, null); };
            return task;
        }

        private static ITask CreateLockObjectTask(Object selected)
        {
            SPath assetPath = AssetDatabase.GetAssetPath(selected.GetInstanceID()).ToSPath();
            SPath repositoryPath = assetPath.RelativeToRepository(manager.Environment);

            var task = Repository.RequestLock(repositoryPath);
            //task.OnEnd += (_, s, ___) => { if (s) manager.TaskManager.Run(manager.UsageTracker.IncrementUnityProjectViewContextLfsLock, null); };
            return task;
        }

        private static void OnLocksUpdate()
        {
            guidsLocks.Clear();
            foreach (var lck in locks)
            {
                SPath repositoryPath = lck.Path;
                SPath assetPath = repositoryPath.RelativeToProject(manager.Environment);

                var g = AssetDatabase.AssetPathToGUID(assetPath);
                guidsLocks.Add(g);
            }

            // https://github.com/github-for-unity/Unity/pull/959#discussion_r236694800
            // We need to repaint not only the project window, but also the inspector.
            // so that we can show the "this thing is locked by X" and that the IsOpenForEdit call happens
            // and the inspector is disabled. There's no way to refresh the editor directly
            // (well, there is, but it's an internal api), so this just causes Unity to repaint everything.
            // Nail, meet bazooka, unfortunately, but that's the only way to do it with public APIs ¯_(ツ)_/¯

            //EditorApplication.RepaintProjectWindow();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private static void OnStatusUpdate()
        {
            for (var index = 0; index < entries.Count; ++index)
            {
                var guid = AssetDatabase.AssetPathToGUID(entries[index].ProjectPath);
                guids[guid] = index;
            }

            AssetDatabase.Refresh();
        }

        private static void OnProjectWindowItemGUI(string guid, Rect itemRect)
        {
            if (!EnsureInitialized())
                return;

            if (!ApplicationConfiguration.ProjectIconsEnabled)
                return;

            if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(guid))
                return;

            Texture2D texture = GetStatusIconForAssetGUID(guid);
            if (texture == null)
                return;

            Rect rect;

            // End of row placement
            if (itemRect.width > itemRect.height)
            {
                rect = new Rect(itemRect.xMax - texture.width, itemRect.y, texture.width,
                    Mathf.Min(texture.height, EditorGUIUtility.singleLineHeight));
            }
            // Corner placement
            // TODO: Magic numbers that need reviewing. Make sure this works properly with long filenames and wordwrap.
            else
            {
                var scale = itemRect.height / 90f;
                var size = new Vector2(texture.width * scale, texture.height * scale);
                size = size / EditorGUIUtility.pixelsPerPoint;
                var offset = new Vector2(itemRect.width * Mathf.Min(.4f * scale, .2f), itemRect.height * Mathf.Min(.2f * scale, .2f));
                rect = new Rect(itemRect.center.x - size.x * .5f + offset.x, itemRect.center.y - size.y * .5f + offset.y, size.x, size.y);
            }

            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
        }
    }
}
