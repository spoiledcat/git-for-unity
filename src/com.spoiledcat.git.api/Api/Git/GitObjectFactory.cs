using System;
using Unity.VersionControl.Git;

namespace Unity.VersionControl.Git
{
    using IO;

    public class GitObjectFactory : IGitObjectFactory
    {
        private readonly IGitEnvironment environment;

        public GitObjectFactory(IGitEnvironment environment)
        {
            this.environment = environment;
        }

        public GitStatusEntry CreateGitStatusEntry(string path, GitFileStatus indexStatus, GitFileStatus workTreeStatus = GitFileStatus.None, string originalPath = null)
        {
            var absolutePath = path.ToSPath().MakeAbsolute();
            var relativePath = absolutePath.RelativeTo(environment.RepositoryPath);
            var projectPath = absolutePath.RelativeTo(environment.UnityProjectPath.ToSPath());

            return new GitStatusEntry(relativePath, absolutePath, projectPath, indexStatus, workTreeStatus, originalPath?.ToSPath());
        }
    }
}
