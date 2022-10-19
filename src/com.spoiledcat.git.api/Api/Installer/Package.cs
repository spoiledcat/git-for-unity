using Unity.VersionControl.Git;
using System;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Extensions;
using Unity.Editor.Tasks.Helpers;
using Unity.Editor.Tasks.Logging;

namespace Unity.VersionControl.Git
{
    using IO;

    public class Package
    {
        private string version;
        public string Md5 { get; set; }
        public string Url { get; set; }
        [NotSerialized] private UriString uri;
        [NotSerialized] public UriString Uri
        {
            get
            {
                if (uri == null)
                    uri = Url.ToString();
                return uri;
            }
        }
        public string ReleaseNotes { get; set; }
        public string ReleaseNotesUrl { get; set; }
        public string Message { get; set; }
        [NotSerialized] public TheVersion Version { get { return TheVersion.Parse(version); } set { version = value.ToString(); } }

        public static Package Load(ITaskManager taskManager, IGitEnvironment environment, UriString packageFeed)
        {
            Package package = null;

            var filename = packageFeed.Filename.ToSPath();
            if (!filename.IsInitialized || filename.IsRoot)
                return package;

            var key = filename.FileNameWithoutExtension + "_updatelastCheckTime";
            var now = DateTimeOffset.Now;
            var feed = environment.UserCachePath.Combine(packageFeed.Filename);

            if (!feed.FileExists() || now.Date > environment.UserSettings.Get<DateTimeOffset>(key).Date)
            {
                feed = new DownloadTask(taskManager, packageFeed, environment.UserCachePath)
                    .Catch(ex =>
                    {
                        Logger.Warning(@"Error downloading package feed:{0} ""{1}"" Message:""{2}""", packageFeed, ex.GetType().ToString(), ex.GetExceptionMessageShort());
                        return true;
                    })
                    .RunSynchronously().ToSPath();

                if (feed.IsInitialized)
                    environment.UserSettings.Set<DateTimeOffset>(key, now);
            }

            if (!feed.IsInitialized)
            {
                // try from assembly resources
                feed = AssemblyResources.ToFile(ResourceType.Platform, packageFeed.Filename, environment.UserCachePath, environment);
            }

            if (feed.IsInitialized)
            {
                try
                {
                    package = feed.ReadAllText().FromJson<Package>(true, false);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
            return package;
        }

        private static ILogging Logger { get; } = LogHelper.GetLogger<Package>();
    }
}
