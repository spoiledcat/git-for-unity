using Unity.VersionControl.Git;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    public interface IGitProcessManager : IProcessManager
    {
        IProcessEnvironment GitProcessEnvironment { get; }
    }

    public class GitProcessManager : ProcessManager, IGitProcessManager
    {
        public IGitEnvironment GitEnvironment { get; }

        /// <summary>
        /// Creates an instance of the process manager and the <see cref="GitProcessEnvironment"/>.
        /// </summary>
        /// <param name="environment"></param>
        public GitProcessManager(IGitEnvironment environment) :  base(environment)
        {
            GitEnvironment = environment;
            GitProcessEnvironment = new ProcessEnvironment(DefaultProcessEnvironment, environment);
        }

        public override T Configure<T>(T processTask, string workingDirectory = null)
        {
            if (workingDirectory == null)
                workingDirectory = GitEnvironment.RepositoryPath;
            return base.Configure(processTask, workingDirectory);
        }

        //public T Configure<T>(T processTask, SPath? executable = null, string arguments = null,
        //    SPath? workingDirectory = null,
        //    bool withInput = false,
        //    bool dontSetupGit = false)
        //     where T : IProcess
        //{
        //    executable = executable ?? processTask.ProcessName?.ToSPath() ?? environment.GitExecutablePath;

        //    //If this null check fails, be sure you called Configure() on your task
        //    Guard.ArgumentNotNull(executable, nameof(executable));

        //    var startInfo = new ProcessStartInfo
        //    {
        //        RedirectStandardInput = withInput,
        //        RedirectStandardOutput = true,
        //        RedirectStandardError = true,
        //        UseShellExecute = false,
        //        CreateNoWindow = true,
        //        StandardOutputEncoding = Encoding.UTF8,
        //        StandardErrorEncoding = Encoding.UTF8
        //    };

        //    gitEnvironment.Configure(startInfo, workingDirectory ?? environment.RepositoryPath, dontSetupGit);

        //    string filename = executable.Value;
        //    if (executable.Value.IsRelative && filename.StartsWith("git"))
        //    {
        //        var file = FindExecutableInPath(executable.Value.FileName, false, startInfo.EnvironmentVariables["PATH"].ToSPathList(environment).ToArray());
        //        filename = file.IsInitialized ? file : executable.Value.FileName;
        //    }
        //    startInfo.FileName = filename;
        //    startInfo.Arguments = arguments ?? processTask.ProcessArguments;
        //    processTask.Configure(startInfo);
        //    processTask.OnStartProcess += p => processes.Add(p);
        //    processTask.OnEndProcess += p => {
        //        if (processes.Contains(p))
        //            processes.Remove(p);
        //    };
        //    return processTask;
        //}

        //public void RunCommandLineWindow(SPath workingDirectory)
        //{
        //    var startInfo = new ProcessStartInfo
        //    {
        //        RedirectStandardInput = false,
        //        RedirectStandardOutput = false,
        //        RedirectStandardError = false,
        //        UseShellExecute = false,
        //        CreateNoWindow = false
        //    };

        //    if (environment.IsWindows)
        //    {
        //        startInfo.FileName = "cmd";
        //        gitEnvironment.Configure(startInfo, workingDirectory);
        //    }
        //    else if (environment.IsMac)
        //    {
        //        // we need to create a temp bash script to set up the environment properly, because
        //        // osx terminal app doesn't inherit the PATH env var and there's no way to pass it in

        //        var envVarFile = SPath.GetTempFilename();
        //        startInfo.FileName = "open";
        //        startInfo.Arguments = $"-a Terminal {envVarFile}";
        //        gitEnvironment.Configure(startInfo, workingDirectory);

        //        var envVars = startInfo.EnvironmentVariables;
        //        var scriptContents = new[] {
        //            $"cd \"{envVars["GHU_WORKINGDIR"]}\"",
        //            $"PATH=\"{envVars["GHU_FULLPATH"]}\" /bin/bash"
        //        };
        //        environment.FileSystem.WriteAllLines(envVarFile, scriptContents);
        //        Mono.Unix.Native.Syscall.chmod(envVarFile, (Mono.Unix.Native.FilePermissions)493); // -rwxr-xr-x mode (0755)
        //    }
        //    else
        //    {
        //        startInfo.FileName = "sh";
        //        gitEnvironment.Configure(startInfo, workingDirectory);
        //    }

        //    Process.Start(startInfo);
        //}

        //public IProcess Reconnect(IProcess processTask, int pid)
        //{
        //    logger.Trace("Reconnecting process " + pid);
        //    var p = Process.GetProcessById(pid);
        //    p.StartInfo.RedirectStandardInput = true;
        //    p.StartInfo.RedirectStandardOutput = true;
        //    p.StartInfo.RedirectStandardError = true;
        //    processTask.Configure(p);
        //    return processTask;
        //}

        //public void Stop()
        //{
        //    foreach (var p in processes.ToArray())
        //        p.Stop();
        //}

        //public static SPath FindExecutableInPath(string executable, bool recurse = false, params SPath[] searchPaths)
        //{
        //    Guard.ArgumentNotNullOrWhiteSpace(executable, "executable");

        //    return searchPaths
        //        .Where(x => x.IsInitialized && !x.IsRelative && x.DirectoryExists())
        //        .SelectMany(x => x.Files(executable, recurse))
        //        .FirstOrDefault();
        //}

        public IProcessEnvironment GitProcessEnvironment { get; }

    }
}
