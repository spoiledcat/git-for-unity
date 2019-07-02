using System.Diagnostics;

namespace Unity.VersionControl.Git
{
    public interface IProcessEnvironment
    {
        void Configure(ProcessStartInfo psi, NPath workingDirectory, bool dontSetupGit = false);
    }
}