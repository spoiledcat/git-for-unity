using System.Diagnostics;

namespace Unity.VersionControl.Git
{
    using IO;

    public interface IProcessEnvironment
    {
        void Configure(ProcessStartInfo psi, SPath workingDirectory, bool dontSetupGit = false);
    }
}
