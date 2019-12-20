using System;
using System.IO;
using NSubstitute;
using Unity.VersionControl.Git;
using TestUtils;
using Unity.VersionControl.Git.IO;

namespace UnitTests
{
    public class GitEnvironmentTestsBase
    {
        //Intentionally returning object here
        protected object BuildFindRootFileSystem()
        {
            var filesystem = Substitute.For<IFileSystem>();

            filesystem.DirectorySeparatorChar.Returns('\\');

            filesystem.DirectoryExists(Args.String)
                .Returns(info =>
                {
                    switch ((string) info[0])
                    {
                        case @"c:\Source\.git":
                            return true;
                        case @"c:\Source":
                            return true;
                        default:
                            return false;
                    }
                });

            filesystem.FileExists(Args.String)
                .Returns(info =>
                {
                    switch ((string) info[0])
                    {
                        case @"c:\Source\file.txt":
                            return true;

                        default:
                            return false;
                    }
                });
            return filesystem;
        }
    }
}
