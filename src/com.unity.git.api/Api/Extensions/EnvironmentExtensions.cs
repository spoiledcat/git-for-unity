using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    using IO;

    public static class EnvironmentExtensions
    {
        public static SPath GetRepositoryPath(this IGitEnvironment environment, SPath path)
        {
            Guard.ArgumentNotNull(path, nameof(path));

            SPath projectPath = environment.UnityProjectPath.ToSPath();
            SPath repositoryPath = environment.RepositoryPath;
            if (projectPath == repositoryPath)
            {
                return path;
            }

            if (repositoryPath.IsChildOf(projectPath))
            {
                throw new InvalidOperationException($"RepositoryPath:\"{repositoryPath}\" should not be child of ProjectPath:\"{projectPath}\"");
            }

            return projectPath.RelativeTo(repositoryPath).Combine(path);
        }

        public static SPath GetAssetPath(this IGitEnvironment environment, SPath path)
        {
            Guard.ArgumentNotNull(path, nameof(path));

            SPath projectPath = environment.UnityProjectPath.ToSPath();
            SPath repositoryPath = environment.RepositoryPath;
            if (projectPath == repositoryPath)
            {
                return path;
            }

            if (repositoryPath.IsChildOf(projectPath))
            {
                throw new InvalidOperationException($"RepositoryPath:\"{repositoryPath}\" should not be child of ProjectPath:\"{projectPath}\"");
            }

            return repositoryPath.Combine(path).MakeAbsolute().RelativeTo(projectPath);
        }

        public static IEnumerable<SPath> ToSPathList(this string envPath, IEnvironment environment)
        {
            return envPath
                    .Split(Path.PathSeparator)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select(x => environment.ExpandEnvironmentVariables(x.Trim('"', '\'')))
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select(x => x.ToSPath());
        }
    }
}
