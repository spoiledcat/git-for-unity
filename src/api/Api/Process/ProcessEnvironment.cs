using Unity.VersionControl.Git;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Resources;

namespace Unity.VersionControl.Git
{
    public class ProcessEnvironment : IProcessEnvironment
    {
        protected IEnvironment Environment { get; private set; }
        protected ILogging Logger { get; private set; }

        private NPath basePath;
        private string[] envPath;
        private NPath gitInstallPath;
        public NPath LibExecPath { get; private set; }


        public ProcessEnvironment(IEnvironment environment)
        {
            Logger = LogHelper.GetLogger(GetType());
            Environment = environment;
        }

        private void Reset()
        {
            basePath = LibExecPath = NPath.Default;
            envPath = null;
            gitInstallPath = Environment.GitInstallPath;

            if (!gitInstallPath.IsInitialized)
                return;

            basePath = ResolveBasePath();
            envPath = CreateEnvPath().ToArray();
            if (ResolveGitExecPath(out NPath p))
                LibExecPath = p;
        }

        private void GeneralConfigure(ProcessStartInfo psi, NPath workingDirectory)
        {
            Guard.ArgumentNotNull(psi, "psi");

            psi.WorkingDirectory = workingDirectory;
            psi.EnvironmentVariables["HOME"] = NPath.HomeDirectory;
            psi.EnvironmentVariables["TMP"] = psi.EnvironmentVariables["TEMP"] = NPath.SystemTemp;

            var path = Environment.Path;
            psi.EnvironmentVariables["PROCESS_WORKINGDIR"] = workingDirectory;

            var pathEnvVarKey = Environment.GetEnvironmentVariableKey("PATH");
            psi.EnvironmentVariables["PROCESS_FULLPATH"] = path;
            psi.EnvironmentVariables[pathEnvVarKey] = path;

        }

        public void Configure(ProcessStartInfo psi, NPath workingDirectory, bool dontSetupGit = false)
        {
            GeneralConfigure(psi, workingDirectory);

            if (dontSetupGit)
                return;

            if (gitInstallPath == NPath.Default || gitInstallPath != Environment.GitInstallPath)
                Reset();

            var pathEntries = new List<string>(envPath);
            string separator = Environment.IsWindows ? ";" : ":";

            // we can only set this env var if there is a libexec/git-core. git will bypass internally bundled tools if this env var
            // is set, which will break Apple's system git on certain tools (like osx-credentialmanager)
            if (LibExecPath.IsInitialized)
                psi.EnvironmentVariables["GIT_EXEC_PATH"] = LibExecPath.ToString();

            pathEntries.Add("END");

            var path = string.Join(separator, pathEntries.ToArray()) + separator + Environment.Path;

            var pathEnvVarKey = Environment.GetEnvironmentVariableKey("PATH");
            psi.EnvironmentVariables[pathEnvVarKey] = path;

            //if (Environment.IsWindows)
            //{
            //    psi.EnvironmentVariables["PLINK_PROTOCOL"] = "ssh";
            //    psi.EnvironmentVariables["TERM"] = "msys";
            //}

            var httpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            if (!string.IsNullOrEmpty(httpProxy))
                psi.EnvironmentVariables["HTTP_PROXY"] = httpProxy;

            var httpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            if (!string.IsNullOrEmpty(httpsProxy))
                psi.EnvironmentVariables["HTTPS_PROXY"] = httpsProxy;
            psi.EnvironmentVariables["DISPLAY"] = "0";

            if (!Environment.IsWindows)
            {
                psi.EnvironmentVariables["GIT_TEMPLATE_DIR"] = Environment.GitInstallPath.Combine("share/git-core/templates");
            }

            if (Environment.IsLinux)
            {
                psi.EnvironmentVariables["PREFIX"] = Environment.GitExecutablePath.Parent;
            }

            var sslCAInfo = Environment.GetEnvironmentVariable("GIT_SSL_CAINFO");
            if (string.IsNullOrEmpty(sslCAInfo))
            {
                var certFile = basePath.Combine("ssl/cacert.pem");
                if (certFile.FileExists())
                    psi.EnvironmentVariables["GIT_SSL_CAINFO"] = certFile.ToString();
            }
/*
            psi.WorkingDirectory = workingDirectory;
            psi.EnvironmentVariables["HOME"] = NPath.HomeDirectory;
            psi.EnvironmentVariables["TMP"] = psi.EnvironmentVariables["TEMP"] = NPath.SystemTemp;

            var path = Environment.Path;
            psi.EnvironmentVariables["GHU_WORKINGDIR"] = workingDirectory;
            var pathEnvVarKey = Environment.GetEnvironmentVariableKey("PATH");

            if (dontSetupGit)
            {
                psi.EnvironmentVariables["GHU_FULLPATH"] = path;
                psi.EnvironmentVariables[pathEnvVarKey] = path;
                return;
            }

            Guard.ArgumentNotNull(psi, "psi");

            var pathEntries = new List<string>();
            string separator = Environment.IsWindows ? ";" : ":";

            NPath libexecPath = NPath.Default;
            List<string> gitPathEntries = new List<string>();
            if (Environment.GitInstallPath.IsInitialized)
            {
                var gitPathRoot = Environment.GitExecutablePath.Resolve().Parent.Parent;
                var gitExecutableDir = Environment.GitExecutablePath.Parent; // original path to git (might be different from install path if it's a symlink)

                var baseExecPath = gitPathRoot;
                var binPath = baseExecPath;
                if (Environment.IsWindows)
                {
                    if (baseExecPath.DirectoryExists("mingw32"))
                        baseExecPath = baseExecPath.Combine("mingw32");
                    else
                        baseExecPath = baseExecPath.Combine("mingw64");
                    binPath = baseExecPath.Combine("bin");
                }

                libexecPath = baseExecPath.Combine("libexec", "git-core");
                if (!libexecPath.DirectoryExists())
                    libexecPath = NPath.Default;

                if (Environment.IsWindows)
                {
                    gitPathEntries.AddRange(new[] { gitPathRoot.Combine("cmd").ToString(), gitPathRoot.Combine("usr", "bin") });
                }
                else
                {
                    gitPathEntries.Add(gitExecutableDir.ToString());
                }

                if (libexecPath.IsInitialized)
                    gitPathEntries.Add(libexecPath);
                gitPathEntries.Add(binPath);

                // we can only set this env var if there is a libexec/git-core. git will bypass internally bundled tools if this env var
                // is set, which will break Apple's system git on certain tools (like osx-credentialmanager)
                if (libexecPath.IsInitialized)
                    psi.EnvironmentVariables["GIT_EXEC_PATH"] = libexecPath.ToString();
            }

            if (Environment.GitLfsInstallPath.IsInitialized && libexecPath != Environment.GitLfsInstallPath)
            {
                pathEntries.Add(Environment.GitLfsInstallPath);
            }
            if (gitPathEntries.Count > 0)
                pathEntries.AddRange(gitPathEntries);

            pathEntries.Add("END");

            path = string.Join(separator, pathEntries.ToArray()) + separator + path;

            psi.EnvironmentVariables["GHU_FULLPATH"] = path;
            psi.EnvironmentVariables[pathEnvVarKey] = path;

            //TODO: Remove with Git LFS Locking becomes standard
            psi.EnvironmentVariables["GITLFSLOCKSENABLED"] = "1";

            if (Environment.IsWindows)
            {
                psi.EnvironmentVariables["PLINK_PROTOCOL"] = "ssh";
                psi.EnvironmentVariables["TERM"] = "msys";
            }

            var httpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            if (!string.IsNullOrEmpty(httpProxy))
                psi.EnvironmentVariables["HTTP_PROXY"] = httpProxy;

            var httpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            if (!string.IsNullOrEmpty(httpsProxy))
                psi.EnvironmentVariables["HTTPS_PROXY"] = httpsProxy;
            psi.EnvironmentVariables["DISPLAY"] = "0";
*/
        }


        private bool ResolveGitExecPath(out NPath path)
        {
            path = ResolveBasePath().Combine("libexec", "git-core");
            return path.DirectoryExists();
        }

        private NPath ResolveBasePath()
        {
            var path = Environment.GitInstallPath;
            if (Environment.IsWindows)
            {
                if (Environment.Is32Bit)
                    path = Environment.GitInstallPath.Combine("mingw32");
                else
                    path = Environment.GitInstallPath.Combine("mingw64");
            }
            return path;
        }

        private IEnumerable<string> CreateEnvPath()
        {
            yield return Environment.GitExecutablePath.Parent.ToString();
            var basePath = ResolveBasePath();
            yield return basePath.Combine("bin").ToString();
            if (Environment.IsWindows)
                yield return Environment.GitInstallPath.Combine("usr/bin").ToString();
            if (Environment.GitInstallPath.IsInitialized && Environment.GitLfsExecutablePath.Parent != Environment.GitExecutablePath.Parent)
                yield return Environment.GitLfsExecutablePath.Parent.ToString();
        }
    }
}
