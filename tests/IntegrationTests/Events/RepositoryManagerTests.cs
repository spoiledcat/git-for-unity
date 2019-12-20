using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseTests;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;using TestUtils;
using TestUtils.Events;
using Unity.Editor.Tasks;
using Unity.VersionControl.Git;

namespace IntegrationTests
{
    [TestFixture, Category("DoNotRunOnAppVeyor")]
    class RepositoryManagerTests : BaseTest
    {
        [Test]
        public async Task ShouldDetectFileChanges()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);

                await events.WaitForNotBusy();
                listener.ClearReceivedCalls();
                events.Reset();

                var foobarTxt = test.Environment.RepositoryPath.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();

                var received = await ProcessEvents(events);

                // we expect these events
                AssertReceivedEvent(nameof(events.GitStatusUpdated), received);
                AssertReceivedEvent(nameof(events.LocalBranchesUpdated), received);
                AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), received);
                AssertReceivedEvent(nameof(events.CurrentBranchUpdated), received);
            }
        }

        [Test]
        public async Task ShouldAddAndCommitFiles()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                var foobarTxt = test.Environment.RepositoryPath.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();

                listener.ClearReceivedCalls();
                events.Reset();

                var filesToCommit = new List<string> { "foobar.txt" };
                var commitMessage = "IntegrationTest Commit";
                var commitBody = string.Empty;

                await test.RepositoryManager.CommitFiles(filesToCommit, commitMessage, commitBody).StartAsAsync();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();

                var received = await ProcessEvents(events);

                AssertReceivedEvent(nameof(events.LocalBranchesUpdated), received);
                AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), received);
                AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), received);
                AssertReceivedEvent(nameof(events.GitLogUpdated), received);
                AssertReceivedEvent(nameof(events.GitStatusUpdated), received);
            }
        }

        [Test]
        public async Task ShouldAddAndCommitAllFiles()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                var foobarTxt = test.Environment.RepositoryPath.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();

                listener.ClearReceivedCalls();
                events.Reset();

                await test.RepositoryManager.CommitAllFiles("IntegrationTest Commit", string.Empty).StartAsAsync();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                var received = await ProcessEvents(events);

                // we expect these events
                AssertReceivedEvent(nameof(events.GitStatusUpdated), received);
                AssertReceivedEvent(nameof(events.LocalBranchesUpdated), received);
                AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), received);
                AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), received);
                AssertReceivedEvent(nameof(events.GitLogUpdated), received);
            }
        }

        [Test]
        public async Task ShouldDetectBranchChange()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                await test.RepositoryManager.SwitchBranch("feature/document").StartAsAsync();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                var received = await ProcessEvents(events);

                // we expect these events
                AssertReceivedEvent(nameof(events.GitStatusUpdated), received);
                AssertReceivedEvent(nameof(events.GitLogUpdated), received);
                AssertReceivedEvent(nameof(events.CurrentBranchUpdated), received);

                // we don't expect these events
                AssertDidNotReceiveEvent(nameof(events.LocalBranchesUpdated), received);
                AssertDidNotReceiveEvent(nameof(events.RemoteBranchesUpdated), received);
                AssertDidNotReceiveEvent(nameof(events.GitAheadBehindStatusUpdated), received);
                //AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), received);
            }
        }

        [Test]
        public async Task ShouldDetectBranchDelete()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                await test.RepositoryManager.DeleteBranch("feature/document", true).StartAsAsync();
                //await TaskManager.Wait();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                var received = await ProcessEvents(events);

                // we expect these events
                AssertReceivedEvent(nameof(events.LocalBranchesUpdated), received);
                AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), received);
                AssertDidNotReceiveEvent(nameof(events.GitAheadBehindStatusUpdated), received);
                AssertReceivedEvent(nameof(events.GitLogUpdated), received);
                AssertReceivedEvent(nameof(events.CurrentBranchUpdated), received);

                // we don't expect these events
                AssertDidNotReceiveEvent(nameof(events.GitStatusUpdated), received);
                //AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), received);
            }
        }

        [Test]
        public async Task ShouldDetectBranchCreate()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                {
                    // prepopulate repository info cache
                    var b = test.Repository.CurrentBranch;
                    test.RepositoryManager.WaitForEvents();
                    var received = await ProcessEvents(events);
                    AssertReceivedEvent(nameof(events.CurrentBranchUpdated), received);
                    listener.ClearReceivedCalls();
                    events.Reset();
                }

                {
                    var createdBranch1 = "feature/document2";
                    await test.RepositoryManager.CreateBranch(createdBranch1, "feature/document").StartAsAsync();

                    test.RepositoryManager.WaitForEvents();
                    await events.WaitForNotBusy();
                    var received = await ProcessEvents(events);

                    // we expect these events
                    AssertReceivedEvent(nameof(events.LocalBranchesUpdated), received);
                    AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), received);
                    AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), received);

                    // we don't expect these events
                    // TODO: log should not be getting called, but it is because when branches get changed we're blindly calling log
                    //AssertDidNotReceiveEvent(events.GitLogUpdated, received);
                    AssertDidNotReceiveEvent(nameof(events.GitStatusUpdated), received);
                    AssertDidNotReceiveEvent(nameof(events.CurrentBranchUpdated), received);
                    //AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), received);

                    listener.ClearReceivedCalls();
                    events.Reset();

                    await test.RepositoryManager.CreateBranch("feature2/document2", "feature/document").StartAsAsync();

                    test.RepositoryManager.WaitForEvents();

                    await events.WaitForNotBusy();
                    received = await ProcessEvents(events);

                    // we expect these events
                    AssertReceivedEvent(nameof(events.LocalBranchesUpdated), received);
                    AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), received);
                    AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), received);

                    // we don't expect these events
                    AssertDidNotReceiveEvent(nameof(events.GitStatusUpdated), received);
                    // TODO: log should not be getting called, but it is because when branches get changed we're blindly calling log
                    //AssertDidNotReceiveEvent(events.GitLogUpdated, received);
                    AssertDidNotReceiveEvent(nameof(events.CurrentBranchUpdated), received);
                    //AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), received);
                }
            }
        }

        [Test]
        public async Task ShouldDetectChangesToRemotes()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                await test.RepositoryManager.RemoteRemove("origin").StartAsAsync();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                var received = await ProcessEvents(events);

                // we expect these events
                AssertReceivedEvent(nameof(events.LocalBranchesUpdated), received);
                AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), received);
                AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), received);
                AssertReceivedEvent(nameof(events.GitLogUpdated), received);
                AssertReceivedEvent(nameof(events.CurrentBranchUpdated), received);

                // we don't expect these events
                AssertDidNotReceiveEvent(nameof(events.GitStatusUpdated), received);
                //AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), received);

                listener.ClearReceivedCalls();
                events.Reset();

                await test.RepositoryManager.RemoteAdd("origin", test.TestRepo.RepoPath.Parent.Combine("bare")).StartAsAsync();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                received = await ProcessEvents(events);

                // we expect these events
                AssertReceivedEvent(nameof(events.LocalBranchesUpdated), received);
                AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), received);
                AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), received);
                AssertReceivedEvent(nameof(events.GitLogUpdated), received);
                AssertReceivedEvent(nameof(events.CurrentBranchUpdated), received);

                // we don't expect these events
                AssertDidNotReceiveEvent(nameof(events.GitStatusUpdated), received);
                //AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), received);
            }
        }

        [Test]
        public async Task ShouldDetectChangesToRemotesWhenSwitchingBranches()
        {
            using (var test = StartTest(TestData.TestRepoMasterTwoRemotes))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                await test.RepositoryManager.CreateBranch("branch2", "another/master").StartAsAsync();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                var received = await ProcessEvents(events);

                // we expect these events
                AssertReceivedEvent(nameof(events.LocalBranchesUpdated), received);
                AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), received);
                AssertReceivedEvent(nameof(events.GitLogUpdated), received);
                AssertReceivedEvent(nameof(events.CurrentBranchUpdated), received);

                // we don't expect these events
                AssertDidNotReceiveEvent(nameof(events.GitStatusUpdated), received);
                AssertDidNotReceiveEvent(nameof(events.GitAheadBehindStatusUpdated), received);
                //AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), received);

                listener.ClearReceivedCalls();
                events.Reset();

                await test.RepositoryManager.SwitchBranch("branch2").StartAsAsync();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                received = await ProcessEvents(events);

                // we expect these events
                AssertReceivedEvent(nameof(events.GitStatusUpdated), received);
                AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), received);
                AssertReceivedEvent(nameof(events.GitLogUpdated), received);
                AssertReceivedEvent(nameof(events.CurrentBranchUpdated), received);

                // we don't expect these events
                AssertDidNotReceiveEvent(nameof(events.LocalBranchesUpdated), received);
                AssertDidNotReceiveEvent(nameof(events.RemoteBranchesUpdated), received);
                AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), received);
            }
        }

        [Test]
        public async Task ShouldDetectGitPull()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                await test.RepositoryManager.Pull("origin", "master").StartAsAsync();
                //await TaskManager.Wait();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                var received = await ProcessEvents(events);

                // we expect these events
                AssertReceivedEvent(nameof(events.GitStatusUpdated), received);
                AssertReceivedEvent(nameof(events.LocalBranchesUpdated), received);
                AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), received);
                AssertReceivedEvent(nameof(events.GitLogUpdated), received);
                AssertReceivedEvent(nameof(events.CurrentBranchUpdated), received);

                // we don't expect these events
                // TODO: this should not happen but it's happening right now because when local branches get updated in the cache, remotes get updated too
                //AssertDidNotReceiveEvent(nameof(events.RemoteBranchesUpdated), received);
                //AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), received);
            }
        }

        [Test]
        public async Task ShouldDetectGitFetch()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanUnsynchronized))
            {
                var events = new RepositoryManagerEvents();
                var listener = Substitute.For<IRepositoryManagerListener>();
                listener.AttachListener(test.RepositoryManager, events);
                listener.ClearReceivedCalls();

                await test.RepositoryManager.Fetch("origin").StartAsAsync();

                test.RepositoryManager.WaitForEvents();
                await events.WaitForNotBusy();
                var received = await ProcessEvents(events);

                // we expect these events
                AssertReceivedEvent(nameof(events.LocalBranchesUpdated), received);
                AssertReceivedEvent(nameof(events.RemoteBranchesUpdated), received);
                AssertReceivedEvent(nameof(events.GitAheadBehindStatusUpdated), received);
                AssertReceivedEvent(nameof(events.CurrentBranchUpdated), received);

                // we don't expect these events
                AssertDidNotReceiveEvent(nameof(events.GitStatusUpdated), received);
                // TODO: log should not be getting called, but it is because when branches get changed we're blindly calling log
                //AssertDidNotReceiveEvent(nameof(events.GitLogUpdated), received);
                //AssertDidNotReceiveEvent(nameof(events.GitAheadBehindStatusUpdated), received);
                //AssertDidNotReceiveEvent(nameof(events.GitLocksUpdated), received);
            }
        }

        private void AssertReceivedEvent(string eventName, Dictionary<string, bool> events)
        {
            Assert.IsTrue(events[eventName], $"{eventName} should have been raised");
        }

        private void AssertDidNotReceiveEvent(string eventName, Dictionary<string, bool> events)
        {
            Assert.IsFalse(events[eventName], $"{eventName} should not have been raised");
        }

        private async Task<Dictionary<string, bool>> ProcessEvents(RepositoryManagerEvents events)
        {
            int timeout = 1000;
            var received = new Dictionary<string, bool>
            {
                { "CurrentBranchUpdated", (await Task.WhenAny(events.CurrentBranchUpdated, Task.Delay(timeout))) is Task<object> },
                { "GitAheadBehindStatusUpdated", (await Task.WhenAny(events.GitAheadBehindStatusUpdated, Task.Delay(timeout))) is Task<object>},
                { "GitLocksUpdated", (await Task.WhenAny(events.GitLocksUpdated, Task.Delay(timeout))) is Task<object>},
                { "GitLogUpdated", (await Task.WhenAny(events.GitLogUpdated, Task.Delay(timeout))) is Task<object>},
                { "GitStatusUpdated", (await Task.WhenAny(events.GitStatusUpdated, Task.Delay(timeout))) is Task<object>},
                { "LocalBranchesUpdated", (await Task.WhenAny(events.LocalBranchesUpdated, Task.Delay(timeout))) is Task<object>},
                { "RemoteBranchesUpdated", (await Task.WhenAny(events.RemoteBranchesUpdated, Task.Delay(timeout))) is Task<object>},
            };
            return received;
        }

    }
}
