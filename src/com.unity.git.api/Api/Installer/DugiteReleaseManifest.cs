#pragma warning disable 169,649

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Helpers;

namespace Unity.VersionControl.Git
{
    using Json;
    using IO;

    public class DugiteReleaseManifest
    {
        private long id;
        private UriString url;
        private UriString assets_url;
        private string tag_name;
        private string name;
        private DateTimeOffset published_at;
        private List<Asset> assets;

        public struct Asset
        {
            private long id;
            private string name;
            private string content_type;
            private long size;
            private DateTimeOffset updated_at;
            private UriString browser_download_url;


            [NotSerialized] public string Name => name;
            [NotSerialized] public string ContentType => content_type;
            [NotSerialized] public DateTimeOffset Timestamp => updated_at;
            [NotSerialized] public UriString Url => browser_download_url;
            public string Hash { get; set; }
        }

        [NotSerialized] public TheVersion Version => TheVersion.Parse(tag_name.Substring(1));

        [NotSerialized] public DateTimeOffset Timestamp => published_at;

        [NotSerialized] public Asset DugitePackage { get; private set; }

        [NotSerialized] public IEnumerable<Asset> Assets => assets;

        private (Asset zipFile, Asset shaFile) GetAsset(IEnvironment environment)
        {
            var arch = environment.Is32Bit ? "x86" : "x64";
            var os = environment.IsWindows ? "windows" : environment.IsMac ? "macOS" : "ubuntu";
            var name = os;
            if (environment.IsWindows)
                name += $"-{arch}";
            name += ".tar.gz";
            return (assets.FirstOrDefault(x => x.Name.EndsWith(name)),
                assets.FirstOrDefault(x => x.Name.EndsWith(name + ".sha256")));
        }

        public static DugiteReleaseManifest Load(ITaskManager taskManager, SPath path, IGitEnvironment environment)
        {
            var manifest = path.ReadAllText().FromJson<DugiteReleaseManifest>(true, false);
            var (zipAsset, shaAsset) = manifest.GetAsset(environment);
            var shaAssetPath = environment.UserCachePath.Combine("downloads", shaAsset.Name);
                if (!shaAssetPath.FileExists())
            {
                var downloader = new Downloader(taskManager);
                downloader.QueueDownload(shaAsset.Url, shaAssetPath.Parent, shaAssetPath.FileName);
                downloader.RunSynchronously();
            }
            zipAsset.Hash = shaAssetPath.ReadAllText();
            manifest.DugitePackage = zipAsset;
            return manifest;
        }

        public static DugiteReleaseManifest Load(ITaskManager taskManager, SPath localCacheFile, UriString packageFeed, IGitEnvironment environment,
            bool alwaysDownload = false)
        {
            DugiteReleaseManifest package = null;
            var filename = localCacheFile.FileName;
            var cacheDir = localCacheFile.Parent;
            var key = localCacheFile.FileNameWithoutExtension + "_updatelastCheckTime";
            var now = DateTimeOffset.Now;

            if (!localCacheFile.FileExists() ||
                (alwaysDownload || now.Date > environment.UserSettings.Get<DateTimeOffset>(key).Date))
            {
                var result = new DownloadTask(taskManager, packageFeed,
                    localCacheFile.Parent, filename)
                .Catch(ex => {
                    LogHelper.Warning(@"Error downloading package feed:{0} ""{1}"" Message:""{2}""", packageFeed,
                        ex.GetType().ToString(), ex.GetExceptionMessageShort());
                    return true;
                }).RunSynchronously();
                localCacheFile = result.ToSPath();
                if (localCacheFile.IsInitialized && !alwaysDownload)
                    environment.UserSettings.Set<DateTimeOffset>(key, now);
            }

            if (!localCacheFile.IsInitialized)
            {
                // try from assembly resources
                localCacheFile = AssemblyResources.ToFile(ResourceType.Platform, filename, cacheDir, environment);
            }

            if (localCacheFile.IsInitialized)
            {
                try
                {
                    package = Load(taskManager, localCacheFile, environment);
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex);
                }
            }
            return package;

        }
    }
}
