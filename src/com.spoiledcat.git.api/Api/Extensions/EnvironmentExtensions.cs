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
        public static SPath RelativeToRepository(this SPath path, IGitEnvironment environment)
        {
            path.ThrowIfNotInitialized();
            Guard.ArgumentNotNull(environment, nameof(environment));

            var projectPath = environment.UnityProjectPath.ToSPath();
            var repositoryPath = environment.RepositoryPath;

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

        public static SPath RelativeToProject(this SPath path, IGitEnvironment environment)
        {
            path.ThrowIfNotInitialized();
            Guard.ArgumentNotNull(environment, nameof(environment));

            var projectPath = environment.UnityProjectPath.ToSPath();
            var repositoryPath = environment.RepositoryPath;
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
