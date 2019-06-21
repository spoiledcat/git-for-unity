using System.Diagnostics;

namespace Unity.Git
{
    public interface IProcessEnvironment
    {
        void Configure(ProcessStartInfo psi, NPath workingDirectory, bool dontSetupGit = false);
    }
}