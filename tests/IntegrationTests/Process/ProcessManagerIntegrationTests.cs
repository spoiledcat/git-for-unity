using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.VersionControl.Git;
using System.Threading.Tasks;
using BaseTests;
using Unity.VersionControl.Git.IO;
using Unity.VersionControl.Git.Tasks;
using Unity.Editor.Tasks;

namespace IntegrationTests
{
    [TestFixture]
    class ProcessManagerIntegrationTests : BaseTest
    {
        [Test]
        [Category("DoNotRunOnAppVeyor")]
        public async Task BranchListTest()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanUnsynchronized))
            {
                var gitBranches = await new GitListLocalBranchesTask(test.Platform)
                                        .Configure(test.ProcessManager)
                                        .StartAwait();

                gitBranches.MatchesUnsorted(new[] {
                    new GitBranch("master", "origin/master"),
                    new GitBranch("feature/document", "origin/feature/document")
                });
            }
        }

        [Test]
        public async Task LogEntriesTest()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanUnsynchronized))
            {
                var logEntries = await new GitLogTask(test.Platform, new GitObjectFactory(test.Environment), 2)
                                       .Configure(test.ProcessManager)
                    .StartAwait();

                var firstCommitTime = new DateTimeOffset(2017, 1, 27, 17, 19, 32, TimeSpan.FromHours(-5));
                var secondCommitTime = new DateTimeOffset(2017, 1, 17, 11, 46, 16, TimeSpan.FromHours(-8));

                var expected = new[]
                {
                    new GitLogEntry("018997938335742f8be694240a7c2b352ec0835f",
                        "Author Person", "author@example.com", "Author Person",
                        "author@example.com",
                        "Moving project files where they should be kept",
                        "",
                        firstCommitTime,
                        firstCommitTime, new List<GitStatusEntry>
                        {
                            new GitStatusEntry("Assets/TestDocument.txt".ToSPath(),
                                TestData.TestRepoMasterCleanUnsynchronized + "/Assets/TestDocument.txt".ToSPath(), "Assets/TestDocument.txt".ToSPath(),
                                GitFileStatus.Renamed, GitFileStatus.None, "TestDocument.txt")
                        }),

                    new GitLogEntry("03939ffb3eb8486dba0259b43db00842bbe6eca1",
                        "Author Person", "author@example.com", "Author Person",
                        "author@example.com",
                        "Initial Commit",
                        "",
                        secondCommitTime,
                        secondCommitTime, new List<GitStatusEntry>
                        {
                            new GitStatusEntry("TestDocument.txt".ToSPath(),
                                TestData.TestRepoMasterCleanUnsynchronized + "/TestDocument.txt".ToSPath(), "TestDocument.txt".ToSPath(),
                                GitFileStatus.Added, GitFileStatus.None),
                        }),
                };

                logEntries.ForEach(x => test.Logger.Info(x.GetHashCode().ToString()));
                expected.All(x => { test.Logger.Info(x.GetHashCode().ToString()); return true; });

                logEntries.Matches(expected);
            }
        }

        [Test]
        public async Task RussianLogEntriesTest()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanUnsynchronizedRussianLanguage))
            {
                var logEntries = await new GitLogTask(test.Platform, new GitObjectFactory(test.Environment), 1)
                                       .Configure(test.ProcessManager)
                                       .StartAwait();

                var commitTime = new DateTimeOffset(2017, 4, 20, 11, 47, 18, TimeSpan.FromHours(-4));

                var expected = new GitLogEntry("06d6451d351626894a30e9134f551db12c74254b",
                    "Author Person", "author@example.com", "Author Person",
                    "author@example.com",
                    "Я люблю github",
                    "",
                    commitTime,
                    commitTime, new List<GitStatusEntry>
                    {
                        new GitStatusEntry(@"Assets\A new file.txt".ToSPath(),
                            TestData.TestRepoMasterCleanUnsynchronizedRussianLanguage + "/Assets/A new file.txt".ToSPath(), "Assets/A new file.txt".ToSPath(),
                            GitFileStatus.Added, GitFileStatus.None),
                    });

                logEntries.Matches(new[] { expected });

                Assert.AreEqual(expected.Summary, logEntries[0].Summary);
            }
        }

        [Test]
        public async Task RemoteListTest()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanUnsynchronized))
            {
                var gitRemotes = await new GitRemoteListTask(test.Platform)
                                       .Configure(test.ProcessManager)
                                   .StartAwait();

                gitRemotes.Matches(new[] {
                    new GitRemote("origin", "github.com", "https://github.com/EvilStanleyGoldman/IOTestsRepo.git", GitRemoteFunction.Both)
                    });
            }
        }

        [Test]
        public async Task StatusTest()
        {
            using (var test = StartTest(TestData.TestRepoMasterDirtyUnsynchronized))
            {
                var gitStatus = await new GitStatusTask(test.Platform, new GitObjectFactory(test.Environment))
                                      .Configure(test.ProcessManager)
                    .StartAwait();


                gitStatus.Matches(new GitStatus("master", "origin/master", 0, 1,
                    new List<GitStatusEntry>
                    {
                        new GitStatusEntry("Assets/Added Document.txt".ToSPath(),
                            "Assets/Added Document.txt".ToSPath().MakeAbsolute(),
                            "Assets/Added Document.txt".ToSPath(),
                            GitFileStatus.Added, GitFileStatus.None),

                        new GitStatusEntry("Assets/Renamed TestDocument.txt".ToSPath(),
                            "Assets/Renamed TestDocument.txt".ToSPath().MakeAbsolute(),
                            "Assets/Renamed TestDocument.txt".ToSPath(),
                            GitFileStatus.Renamed,  GitFileStatus.None, "Assets/TestDocument.txt".ToSPath()),

                        new GitStatusEntry("Assets/Untracked Document.txt".ToSPath(),
                            "Assets/Untracked Document.txt".ToSPath().MakeAbsolute(),
                            "Assets/Untracked Document.txt".ToSPath(),
                            GitFileStatus.Untracked, GitFileStatus.Untracked),
                    }
                ));
            }
        }

        //[Test]
        //public async Task CredentialHelperGetTest()
        //{
        //    InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);

        //    await ProcessManager
        //        .GetGitCreds(TestRepoMasterCleanSynchronized)
        //        .StartAsAsync();
        //}
    }
}
