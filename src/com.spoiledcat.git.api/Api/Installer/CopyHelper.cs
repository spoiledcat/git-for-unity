using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Editor.Tasks.Logging;
using Unity.VersionControl.Git;

namespace Unity.VersionControl.Git
{
    using IO;

    public static class CopyHelper
    {
        private static readonly ILogging Logger = LogHelper.GetLogger(typeof(CopyHelper));

        public static void Copy(SPath fromPath, SPath toPath)
        {
            Logger.Trace("Copying from {0} to {1}", fromPath, toPath);

            try
            {
                CopyFolder(fromPath, toPath);
            }
            catch (Exception ex1)
            {
                Logger.Warning(ex1, "Error copying.");

                try
                {
                    CopyFolderContents(fromPath, toPath);
                }
                catch (Exception ex2)
                {
                    Logger.Error(ex2, "Error copying contents.");
                    throw;
                }
            }
            finally
            {
                fromPath.DeleteIfExists();
            }
        }
        public static void CopyFolder(SPath fromPath, SPath toPath)
        {
            Logger.Trace("CopyFolder from {0} to {1}", fromPath, toPath);
            toPath.DeleteIfExists();
            toPath.EnsureParentDirectoryExists();
            fromPath.Move(toPath);
        }

        public static void CopyFolderContents(SPath fromPath, SPath toPath)
        {
            Logger.Trace("CopyFolderContents from {0} to {1}", fromPath, toPath);
            toPath.DeleteContents();
            fromPath.MoveFiles(toPath, true);
        }
    }
}
