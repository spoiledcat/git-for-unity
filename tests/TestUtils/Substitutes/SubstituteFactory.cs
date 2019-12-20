using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NSubstitute;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Logging;
using Unity.VersionControl.Git;
using Unity.VersionControl.Git.IO;

namespace TestUtils
{
    public class TestSubstituteFactory
    {
        public IGitEnvironment CreateEnvironment(CreateEnvironmentOptions createEnvironmentOptions = null)
        {
            createEnvironmentOptions = createEnvironmentOptions ?? new CreateEnvironmentOptions();

            var userPath = createEnvironmentOptions.UserProfilePath;
            var localAppData = userPath.Parent.Combine("LocalAppData");
            var appData = userPath.Parent.Combine("AppData");

            var environment = Substitute.For<IGitEnvironment>();
            environment.RepositoryPath.Returns(createEnvironmentOptions.RepositoryPath.ToSPath());
            environment.ExtensionInstallPath.Returns(createEnvironmentOptions.Extensionfolder);
            environment.UnityProjectPath.Returns(createEnvironmentOptions.UnityProjectPath);
            environment.GetFolder(Folders.LocalApplicationData).Returns(localAppData);
            environment.GetFolder(Folders.Logs).Returns(localAppData);
            environment.GetFolder(Folders.CommonApplicationData).Returns(appData);
            environment.LocalAppData.Returns(localAppData);
            environment.CommonAppData.Returns(appData);
            return environment;
        }

        public IFileSystem CreateFileSystem(CreateFileSystemOptions createFileSystemOptions = null)
        {
            createFileSystemOptions = createFileSystemOptions ?? new CreateFileSystemOptions();

            var fileSystem = Substitute.For<IFileSystem>();
            var realFileSystem = new FileSystem();
            var logger = LogHelper.GetLogger("TestFileSystem");

            fileSystem.DirectorySeparatorChar.Returns(realFileSystem.DirectorySeparatorChar);
            fileSystem.CurrentDirectory.Returns(createFileSystemOptions.CurrentDirectory);

            fileSystem.FileExists(Args.String).Returns(info => {
                var path = (string)info[0];

                var result = false;
                if (createFileSystemOptions.FilesThatExist != null)
                {
                    result = createFileSystemOptions.FilesThatExist.Contains(path);
                }

                logger.Trace(@"FileExists(""{0}"") -> {1}", path, result);
                return result;
            });

            fileSystem.WhenForAnyArgs(system => system.FileCopy(Args.String, Args.String, Args.Bool))
                      .Do(
                          info => {
                              logger.Trace(@"FileCopy(""{0}"", ""{1}"", ""{2}"")", (string)info[0], (string)info[1],
                                  (bool)info[2]);
                          });

            fileSystem.DirectoryExists(Args.String).Returns(info => {
                var path1 = (string)info[0];

                var result = true;
                if (createFileSystemOptions.DirectoriesThatExist != null)
                {
                    result = createFileSystemOptions.DirectoriesThatExist.Contains(path1);
                }

                logger.Trace(@"DirectoryExists(""{0}"") -> {1}", path1, result);
                return result;
            });

            fileSystem.ExistingPathIsDirectory(Args.String).Returns(info => {
                var path = (string)info[0];

                var result = false;
                if (createFileSystemOptions.DirectoriesThatExist != null)
                {
                    result = createFileSystemOptions.DirectoriesThatExist.Contains(path);
                }

                logger.Trace(@"ExistingPathIsDirectory(""{0}"") -> {1}", path, result);
                return result;
            });

            fileSystem.ReadAllLines(Args.String).Returns(info => {
                var path = (string)info[0];

                IList<string> result = null;

                if (createFileSystemOptions.FileContents != null)
                {
                    if (createFileSystemOptions.FileContents.TryGetValue(path, out result))
                    {}
                }

                var resultLength = result != null ? $"{result.Count} lines" : "ERROR";

                logger.Trace(@"ReadAllLines(""{0}"") -> {1}", path, resultLength);

                return result;
            });

            fileSystem.ReadAllText(Args.String).Returns(info => {
                var path = (string)info[0];

                string result = null;
                IList<string> fileContent = null;

                if (createFileSystemOptions.FileContents != null)
                {
                    if (createFileSystemOptions.FileContents.TryGetValue(path, out fileContent))
                    {
                        result = string.Join(string.Empty, fileContent.ToArray());
                    }
                }

                var resultLength = fileContent != null ? $"{fileContent.Count} lines" : "ERROR";

                logger.Trace(@"ReadAllText(""{0}"") -> {1}", path, resultLength);

                return result;
            });

            var randomFileIndex = 0;
            fileSystem.GetRandomFileName().Returns(info => {
                string result = null;
                if (createFileSystemOptions.RandomFileNames != null)
                {
                    result = createFileSystemOptions.RandomFileNames[randomFileIndex];

                    randomFileIndex++;
                    randomFileIndex = randomFileIndex % createFileSystemOptions.RandomFileNames.Count;
                }

                logger.Trace(@"GetRandomFileName() -> {0}", result);

                return result;
            });

            fileSystem.TempPath.Returns(info => {
                logger.Trace(@"GetTempPath() -> {0}", createFileSystemOptions.TemporaryPath);

                return createFileSystemOptions.TemporaryPath;
            });

            fileSystem.GetFiles(Args.String).Returns(info => {
                var path = (string)info[0];

                string[] result = null;
                if (createFileSystemOptions.ChildFiles != null)
                {
                    var key = new ContentsKey(path);
                    if (createFileSystemOptions.ChildFiles.ContainsKey(key))
                    {
                        result = createFileSystemOptions.ChildFiles[key].ToArray();
                    }
                }

                var resultLength = result != null ? $"{result.Length} items" : "ERROR";

                logger.Trace(@"GetFiles(""{0}"") -> {1}", path, resultLength);

                return result;
            });

            fileSystem.GetFiles(Args.String, Args.String).Returns(info => {
                var path = (string)info[0];
                var pattern = (string)info[1];

                string[] result = null;
                if (createFileSystemOptions.ChildFiles != null)
                {
                    var key = new ContentsKey(path, pattern);
                    if (createFileSystemOptions.ChildFiles.ContainsKey(key))
                    {
                        result = createFileSystemOptions.ChildFiles[key].ToArray();
                    }
                }

                var resultLength = result != null ? $"{result.Length} items" : "ERROR";

                logger.Trace(@"GetFiles(""{0}"", ""{1}"") -> {2}", path, pattern, resultLength);

                return result;
            });

            fileSystem.GetFiles(Args.String, Args.String, Args.SearchOption).Returns(info => {
                var path = (string)info[0];
                var pattern = (string)info[1];
                var searchOption = (SearchOption)info[2];

                string[] result = null;
                if (createFileSystemOptions.ChildFiles != null)
                {
                    var key = new ContentsKey(path, pattern, searchOption);
                    if (createFileSystemOptions.ChildFiles.ContainsKey(key))
                    {
                        result = createFileSystemOptions.ChildFiles[key].ToArray();
                    }
                }

                var resultLength = result != null ? $"{result.Length} items" : "ERROR";

                logger.Trace(@"GetFiles(""{0}"", ""{1}"", {2}) -> {3}", path, pattern, searchOption, resultLength);

                return result;
            });

            fileSystem.GetDirectories(Args.String).Returns(info => {
                var path = (string)info[0];

                string[] result = null;
                if (createFileSystemOptions.ChildDirectories != null)
                {
                    var key = new ContentsKey(path);
                    if (createFileSystemOptions.ChildDirectories.ContainsKey(key))
                    {
                        result = createFileSystemOptions.ChildDirectories[key].ToArray();
                    }
                }

                var resultLength = result != null ? $"{result.Length} items" : "ERROR";

                logger.Trace(@"GetDirectories(""{0}"") -> {1}", path, resultLength);

                return result;
            });

            fileSystem.GetDirectories(Args.String, Args.String).Returns(info => {
                var path = (string)info[0];
                var pattern = (string)info[1];

                string[] result = null;
                if (createFileSystemOptions.ChildDirectories != null)
                {
                    var key = new ContentsKey(path, pattern);
                    if (createFileSystemOptions.ChildDirectories.ContainsKey(key))
                    {
                        result = createFileSystemOptions.ChildDirectories[key].ToArray();
                    }
                }

                var resultLength = result != null ? $"{result.Length} items" : "ERROR";

                logger.Trace(@"GetDirectories(""{0}"", ""{1}"") -> {2}", path, pattern, resultLength);

                return result;
            });

            fileSystem.GetDirectories(Args.String, Args.String, Args.SearchOption).Returns(info => {
                var path = (string)info[0];
                var pattern = (string)info[1];
                var searchOption = (SearchOption)info[2];

                string[] result = null;
                if (createFileSystemOptions.ChildDirectories != null)
                {
                    var key = new ContentsKey(path, pattern, searchOption);
                    if (createFileSystemOptions.ChildDirectories.ContainsKey(key))
                    {
                        result = createFileSystemOptions.ChildDirectories[key].ToArray();
                    }
                }

                var resultLength = result != null ? $"{result.Length} items" : "ERROR";

                logger.Trace(@"GetDirectories(""{0}"", ""{1}"", {2}) -> {3}", path, pattern, searchOption, resultLength);

                return result;
            });

            fileSystem.GetFullPath(Args.String).Returns(info => Path.GetFullPath((string)info[0]));

            fileSystem.GetFileNameWithoutExtension(Args.String)
                      .Returns(info => Path.GetFileNameWithoutExtension((string)info[0]));

            return fileSystem;
        }

        public IZipHelper CreateSharpZipLibHelper()
        {
            return Substitute.For<IZipHelper>();
        }

        public IGitObjectFactory CreateGitObjectFactory(string gitRepoPath)
        {
            var gitObjectFactory = Substitute.For<IGitObjectFactory>();

            gitObjectFactory.CreateGitStatusEntry(Args.String, Args.GitFileStatus, Args.GitFileStatus, Args.String)
                            .Returns(info => {
                                var path = (string)info[0];
                                var indexStatus = (GitFileStatus)info[1];
                                var workTreeStatus = (GitFileStatus)info[2];
                                var originalPath = (string)info[3];

                                return new GitStatusEntry(path, gitRepoPath + @"\" + path, null, indexStatus, workTreeStatus, originalPath);
                            });

            return gitObjectFactory;
        }

        public IProcessEnvironment CreateProcessEnvironment(SPath root)
        {
            var processEnvironment = Substitute.For<IProcessEnvironment>();
            return processEnvironment;
        }

        public IPlatform CreatePlatform()
        {
            return Substitute.For<IPlatform>();
        }

        public IRepositoryWatcher CreateRepositoryWatcher()
        {
            return Substitute.For<IRepositoryWatcher>();
        }

        public IGitConfig CreateGitConfig()
        {
            return Substitute.For<IGitConfig>();
        }

        public struct ContentsKey
        {
            public readonly string Path;
            public readonly string Pattern;
            public readonly SearchOption? SearchOption;

            public ContentsKey(string path, string pattern, SearchOption? searchOption)
            {
                Path = path;
                Pattern = pattern;
                SearchOption = searchOption;
            }

            public ContentsKey(string path, string pattern) : this(path, pattern, null)
            {}

            public ContentsKey(string path) : this(path, null)
            {}
        }
    }
}
