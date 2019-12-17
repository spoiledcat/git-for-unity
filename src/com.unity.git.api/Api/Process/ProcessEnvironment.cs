using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Unity.VersionControl.Git
{
    using IO;
    using Unity.Editor.Tasks;

    public class ProcessEnvironment : IProcessEnvironment
    {
        private readonly IProcessEnvironment defaultEnvironment;
        protected IGitEnvironment GitEnvironment { get; private set; }
        protected ILogging Logger { get; private set; }

        private SPath basePath;
        private string[] envPath;
        private SPath gitInstallPath;
        private SPath libExecPath;

        public ProcessEnvironment(IProcessEnvironment defaultEnvironment, IGitEnvironment environment)
        {
            this.defaultEnvironment = defaultEnvironment;
            GitEnvironment = environment;

            Logger = LogHelper.GetLogger(GetType());
        }

        private void Reset()
        {
            basePath = libExecPath = SPath.Default;
            envPath = null;
            gitInstallPath = GitEnvironment.GitInstallPath;

            if (!gitInstallPath.IsInitialized)
                return;

            basePath = ResolveBasePath();
            envPath = CreateEnvPath().ToArray();
            if (ResolveGitExecPath(out SPath p))
                libExecPath = p;
        }

        public void Configure(ProcessStartInfo psi)
        {
            defaultEnvironment.Configure(psi);

            //if (gitInstallPath == SPath.Default || gitInstallPath != Environment.GitInstallPath)
                Reset();

            var pathEntries = new List<string>(envPath);
            string separator = GitEnvironment.IsWindows ? ";" : ":";

            // we can only set this env var if there is a libexec/git-core. git will bypass internally bundled tools if this env var
            // is set, which will break Apple's system git on certain tools (like osx-credentialmanager)
            if (libExecPath.IsInitialized)
                psi.EnvironmentVariables["GIT_EXEC_PATH"] = libExecPath.ToString();

            pathEntries.Add("END");

            var path = string.Join(separator, pathEntries.ToArray()) + separator + GitEnvironment.Path;

            var pathEnvVarKey = GitEnvironment.GetEnvironmentVariableKey("PATH");
            psi.EnvironmentVariables[pathEnvVarKey] = path;

            //if (Environment.IsWindows)
            //{
            //    psi.EnvironmentVariables["PLINK_PROTOCOL"] = "ssh";
            //    psi.EnvironmentVariables["TERM"] = "msys";
            //}

            var httpProxy = GitEnvironment.GetEnvironmentVariable("HTTP_PROXY");
            if (!string.IsNullOrEmpty(httpProxy))
                psi.EnvironmentVariables["HTTP_PROXY"] = httpProxy;

            var httpsProxy = GitEnvironment.GetEnvironmentVariable("HTTPS_PROXY");
            if (!string.IsNullOrEmpty(httpsProxy))
                psi.EnvironmentVariables["HTTPS_PROXY"] = httpsProxy;
            psi.EnvironmentVariables["DISPLAY"] = "0";

            if (!GitEnvironment.IsWindows)
            {
                psi.EnvironmentVariables["GIT_TEMPLATE_DIR"] = GitEnvironment.GitInstallPath.Combine("share/git-core/templates");
            }

            if (GitEnvironment.IsLinux)
            {
                psi.EnvironmentVariables["PREFIX"] = GitEnvironment.GitExecutablePath.Parent;
            }

            var sslCAInfo = GitEnvironment.GetEnvironmentVariable("GIT_SSL_CAINFO");
            if (string.IsNullOrEmpty(sslCAInfo))
            {
                var certFile = basePath.Combine("ssl/cacert.pem");
                if (certFile.FileExists())
                    psi.EnvironmentVariables["GIT_SSL_CAINFO"] = certFile.ToString();
            }
/*
            psi.WorkingDirectory = workingDirectory;
            psi.EnvironmentVariables["HOME"] = SPath.HomeDirectory;
            psi.EnvironmentVariables["TMP"] = psi.EnvironmentVariables["TEMP"] = SPath.SystemTemp;

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

            SPath libexecPath = SPath.Default;
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
                    libexecPath = SPath.Default;

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


        private bool ResolveGitExecPath(out SPath path)
        {
            path = ResolveBasePath().Combine("libexec", "git-core");
            return path.DirectoryExists();
        }

        private SPath ResolveBasePath()
        {
            var path = GitEnvironment.GitInstallPath;
            if (GitEnvironment.IsWindows)
            {
                if (GitEnvironment.Is32Bit)
                    path = GitEnvironment.GitInstallPath.Combine("mingw32");
                else
                    path = GitEnvironment.GitInstallPath.Combine("mingw64");
            }
            return path;
        }

        private IEnumerable<string> CreateEnvPath()
        {
            yield return GitEnvironment.GitExecutablePath.Parent.ToString();
            var basep = ResolveBasePath();
            yield return basep.Combine("bin").ToString();
            if (GitEnvironment.IsWindows)
                yield return GitEnvironment.GitInstallPath.Combine("usr/bin").ToString();
            if (GitEnvironment.GitInstallPath.IsInitialized && GitEnvironment.GitLfsExecutablePath.Parent != GitEnvironment.GitExecutablePath.Parent)
                yield return GitEnvironment.GitLfsExecutablePath.Parent.ToString();
        }

        public IEnvironment Environment => defaultEnvironment.Environment;
    }
}
