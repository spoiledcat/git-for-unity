using TestUtils;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.VersionControl.Git;

namespace UnitTests
{
    [TestFixture]
    class GitStatusOutputProcessorTests : BaseOutputProcessorTests
    {

        [Test]
        public void ShouldParseDirtyWorkingTreeUntracked()
        {
            var output = new[]
            {
                "## master",
                " M GitHubVS.sln",
                "R  README.md -> README2.md",
                " D deploy.cmd",
                @"A  ""something added.txt""",
                "?? something.txt",
                null
            };

            AssertProcessOutput(output, new GitStatus("master", null, 0, 0,
                new List<GitStatusEntry>
                {
                    new GitStatusEntry("GitHubVS.sln", TestRootPath + @"\GitHubVS.sln", null, GitFileStatus.None, GitFileStatus.Modified),
                    new GitStatusEntry("README2.md", TestRootPath + @"\README2.md", null, GitFileStatus.Renamed, GitFileStatus.None, "README.md"),
                    new GitStatusEntry("deploy.cmd", TestRootPath + @"\deploy.cmd", null, GitFileStatus.None, GitFileStatus.Deleted),
                    new GitStatusEntry("something added.txt", TestRootPath + @"\something added.txt", null, GitFileStatus.Added, GitFileStatus.None),
                    new GitStatusEntry("something.txt", TestRootPath + @"\something.txt", null, GitFileStatus.Untracked, GitFileStatus.Untracked),
                }.OrderBy(entry => entry.Path, GitStatusOutputProcessor.StatusOutputPathComparer.Instance).ToList()
            ));
        }

        [Test]
        public void ShouldParseUnmergedStates()
        {
            var output = new[]
            {
                "## master",
                "DD something1.txt",
                "AU something2.txt",
                "UD something3.txt",
                "UA something4.txt",
                "DU something5.txt",
                "AA something6.txt",
                "UU something7.txt",
                null
            };

            AssertProcessOutput(output, new GitStatus("master", null, 0, 0,
                new List<GitStatusEntry>
                {
                    new GitStatusEntry("something1.txt", TestRootPath + @"\something1.txt", null, GitFileStatus.Deleted, GitFileStatus.Deleted),
                    new GitStatusEntry("something2.txt", TestRootPath + @"\something2.txt", null, GitFileStatus.Added, GitFileStatus.Unmerged),
                    new GitStatusEntry("something3.txt", TestRootPath + @"\something3.txt", null, GitFileStatus.Unmerged, GitFileStatus.Deleted),
                    new GitStatusEntry("something4.txt", TestRootPath + @"\something4.txt", null, GitFileStatus.Unmerged, GitFileStatus.Added),
                    new GitStatusEntry("something5.txt", TestRootPath + @"\something5.txt", null, GitFileStatus.Deleted, GitFileStatus.Unmerged),
                    new GitStatusEntry("something6.txt", TestRootPath + @"\something6.txt", null, GitFileStatus.Added, GitFileStatus.Added),
                    new GitStatusEntry("something7.txt", TestRootPath + @"\something7.txt", null, GitFileStatus.Unmerged, GitFileStatus.Unmerged),
                }.OrderBy(entry => entry.Path, GitStatusOutputProcessor.StatusOutputPathComparer.Instance).ToList()
            ));
        }

        [Test]
        public void ShouldParseDirtyWorkingTreeTrackedAhead1Behind1()
        {
            var output = new[]
            {
                "## master...origin/master [ahead 1, behind 1]",
                " M GitHubVS.sln",
                "R  README.md -> README2.md",
                " D deploy.cmd",
                @"A  ""something added.txt""",
                "?? something.txt",
                null
            };

            AssertProcessOutput(output, new GitStatus("master", "origin/master", 1, 1,
                new List<GitStatusEntry>
                {
                    new GitStatusEntry("GitHubVS.sln", TestRootPath + @"\GitHubVS.sln", null, GitFileStatus.None, GitFileStatus.Modified),
                    new GitStatusEntry("README2.md", TestRootPath + @"\README2.md", null, GitFileStatus.Renamed, GitFileStatus.None, "README.md"),
                    new GitStatusEntry("deploy.cmd", TestRootPath + @"\deploy.cmd", null, GitFileStatus.None, GitFileStatus.Deleted),
                    new GitStatusEntry("something added.txt", TestRootPath + @"\something added.txt", null, GitFileStatus.Added, GitFileStatus.None),
                    new GitStatusEntry("something.txt", TestRootPath + @"\something.txt", null, GitFileStatus.Untracked, GitFileStatus.Untracked),
                }.OrderBy(entry => entry.Path, GitStatusOutputProcessor.StatusOutputPathComparer.Instance).ToList()
            ));
        }

        [Test]
        public void ShouldParseDirtyWorkingTreeTrackedAhead1()
        {
            var output = new[]
            {
                "## master...origin/master [ahead 1]",
                " M GitHubVS.sln",
                "R  README.md -> README2.md",
                " D deploy.cmd",
                @"A  ""something added.txt""",
                "?? something.txt",
                null
            };

            AssertProcessOutput(output, new GitStatus("master", "origin/master", 1, 0,
                new List<GitStatusEntry>
                {
                    new GitStatusEntry("GitHubVS.sln", TestRootPath + @"\GitHubVS.sln", null, GitFileStatus.None, GitFileStatus.Modified),
                    new GitStatusEntry("README2.md", TestRootPath + @"\README2.md", null, GitFileStatus.Renamed, GitFileStatus.None, "README.md"),
                    new GitStatusEntry("deploy.cmd", TestRootPath + @"\deploy.cmd", null, GitFileStatus.None, GitFileStatus.Deleted),
                    new GitStatusEntry("something added.txt", TestRootPath + @"\something added.txt", null, GitFileStatus.Added, GitFileStatus.None),
                    new GitStatusEntry("something.txt", TestRootPath + @"\something.txt", null, GitFileStatus.Untracked, GitFileStatus.Untracked),
                }.OrderBy(entry => entry.Path, GitStatusOutputProcessor.StatusOutputPathComparer.Instance).ToList()
            ));
        }

        [Test]
        public void ShouldParseDirtyWorkingTreeTrackedBehind1()
        {
            var output = new[]
            {
                "## master...origin/master [behind 1]",
                " M GitHubVS.sln",
                "R  README.md -> README2.md",
                " D deploy.cmd",
                @"A  ""something added.txt""",
                "?? something.txt",
                null
            };

            AssertProcessOutput(output, new GitStatus("master", "origin/master", 0, 1,
                new List<GitStatusEntry>
                {
                    new GitStatusEntry("GitHubVS.sln", TestRootPath + @"\GitHubVS.sln", null, GitFileStatus.None, GitFileStatus.Modified),
                    new GitStatusEntry("README2.md", TestRootPath + @"\README2.md", null, GitFileStatus.Renamed, GitFileStatus.None, "README.md"),
                    new GitStatusEntry("deploy.cmd", TestRootPath + @"\deploy.cmd", null, GitFileStatus.None, GitFileStatus.Deleted),
                    new GitStatusEntry("something added.txt", TestRootPath + @"\something added.txt", null, GitFileStatus.Added, GitFileStatus.None),
                    new GitStatusEntry("something.txt", TestRootPath + @"\something.txt", null, GitFileStatus.Untracked, GitFileStatus.Untracked),
                }.OrderBy(entry => entry.Path, GitStatusOutputProcessor.StatusOutputPathComparer.Instance).ToList()
            ));
        }

        [Test]
        public void ShouldParseDirtyWorkingTreeTracked()
        {
            var output = new[]
            {
                "## master...origin/master",
                " M GitHubVS.sln",
                "R  README.md -> README2.md",
                " D deploy.cmd",
                @"A  ""something added.txt""",
                "?? something.txt",
                null
            };

            AssertProcessOutput(output, new GitStatus("master", "origin/master", 0, 0,
                new List<GitStatusEntry>
                {
                    new GitStatusEntry("GitHubVS.sln", TestRootPath + @"\GitHubVS.sln", null, GitFileStatus.None, GitFileStatus.Modified),
                    new GitStatusEntry("README2.md", TestRootPath + @"\README2.md", null, GitFileStatus.Renamed, GitFileStatus.None, "README.md"),
                    new GitStatusEntry("deploy.cmd", TestRootPath + @"\deploy.cmd", null, GitFileStatus.None, GitFileStatus.Deleted),
                    new GitStatusEntry("something added.txt", TestRootPath + @"\something added.txt", null, GitFileStatus.Added, GitFileStatus.None),
                    new GitStatusEntry("something.txt", TestRootPath + @"\something.txt", null, GitFileStatus.Untracked, GitFileStatus.Untracked),
                }.OrderBy(entry => entry.Path, GitStatusOutputProcessor.StatusOutputPathComparer.Instance).ToList()
            ));
        }

        [Test]
        public void ShouldParseCleanWorkingTreeUntracked()
        {
            var output = new[]
            {
                "## something",
                null
            };

            AssertProcessOutput(output, new GitStatus("something", default, default, default, new List<GitStatusEntry>()));
        }

        [Test]
        public void ShouldParseCleanWorkingTreeTrackedAhead1Behind1()
        {
            var output = new[]
            {
                "## master...origin/master [ahead 1, behind 1]",
                null
            };

            AssertProcessOutput(output, new GitStatus("master", "origin/master", 1, 1, new List<GitStatusEntry>()));
        }

        [Test]
        public void ShouldParseCleanWorkingTreeTrackedAhead1()
        {
            var output = new[]
            {
                "## master...origin/master [ahead 1]",
                null
            };

            AssertProcessOutput(output, new GitStatus("master", "origin/master", 1, 0, new List<GitStatusEntry>()));
        }

        [Test]
        public void ShouldParseCleanWorkingTreeTrackedBehind1()
        {
            var output = new[]
            {
                "## master...origin/master [behind 1]",
                null
            };

            AssertProcessOutput(output, new GitStatus("master", "origin/master", 0, 1, new List<GitStatusEntry>()));
        }

        [Test]
        public void ShouldParseCleanWorkingTreeTracked()
        {
            var output = new[]
            {
                "## master...origin/master",
                null
            };

            AssertProcessOutput(output, new GitStatus("master", "origin/master", 0, 0, new List<GitStatusEntry>()));
        }

        [Test]
        public void ShouldSortOutputCorrectly()
        {
            var output = new[]
            {
                "## master",
                "?? Assets/Assets.Test.dll.meta",
                "?? Assets/Assets.Test.dll",
                "?? Plugins/Unity.VersionControl.Git.dll",
                "?? Plugins/Unity.VersionControl.Git.dll.mdb",
                "?? Plugins/Unity.VersionControl.Git.dll.mdb.meta",
                "?? Plugins/Unity.Git2.dll",
                "?? Plugins/Unity.Git2.dll.mdb",
                "?? Plugins/Unity.Git2.dll.mdb.meta",
                "?? Plugins/Unity.Git2.dll.meta",
                "?? Plugins/Unity.VersionControl.Git.dll.meta",
                "?? blah.txt",
                null
            };

            AssertProcessOutput(output, new GitStatus("master", null, 0, 0,
                new List<GitStatusEntry>
                {
                    new GitStatusEntry(@"Assets/Assets.Test.dll", TestRootPath + @"\Assets/Assets.Test.dll", null, GitFileStatus.Untracked, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Assets/Assets.Test.dll.meta", TestRootPath + @"\Assets/Assets.Test.dll.meta", null, GitFileStatus.Untracked, GitFileStatus.Untracked),
                    new GitStatusEntry(@"blah.txt", TestRootPath + @"\blah.txt", null, GitFileStatus.Untracked, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/Unity.Git2.dll", TestRootPath + @"\Plugins/Unity.Git2.dll", null, GitFileStatus.Untracked, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/Unity.Git2.dll.meta", TestRootPath + @"\Plugins/Unity.Git2.dll.meta", null, GitFileStatus.Untracked, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/Unity.Git2.dll.mdb", TestRootPath + @"\Plugins/Unity.Git2.dll.mdb", null, GitFileStatus.Untracked, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/Unity.Git2.dll.mdb.meta", TestRootPath + @"\Plugins/Unity.Git2.dll.mdb.meta", null, GitFileStatus.Untracked, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/Unity.VersionControl.Git.dll", TestRootPath + @"\Plugins/Unity.VersionControl.Git.dll", null, GitFileStatus.Untracked, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/Unity.VersionControl.Git.dll.meta", TestRootPath + @"\Plugins/Unity.VersionControl.Git.dll.meta", null, GitFileStatus.Untracked, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/Unity.VersionControl.Git.dll.mdb", TestRootPath + @"\Plugins/Unity.VersionControl.Git.dll.mdb", null, GitFileStatus.Untracked, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/Unity.VersionControl.Git.dll.mdb.meta", TestRootPath + @"\Plugins/Unity.VersionControl.Git.dll.mdb.meta", null, GitFileStatus.Untracked, GitFileStatus.Untracked),
                }
                ));
        }

        private void AssertProcessOutput(IEnumerable<string> lines, GitStatus expected)
        {
            var gitObjectFactory = SubstituteFactory.CreateGitObjectFactory(TestRootPath);

            GitStatus? result = null;
            var outputProcessor = new GitStatusOutputProcessor(gitObjectFactory);
            outputProcessor.OnEntry += status => result = status;

            foreach (var line in lines)
            {
                outputProcessor.Process(line);
            }

            Assert.IsTrue(result.HasValue);
            result.Value.AssertEqual(expected);
        }
    }
}
