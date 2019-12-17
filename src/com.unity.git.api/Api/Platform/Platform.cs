using System.Threading.Tasks;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    public interface IPlatform
    {
        IPlatform Initialize();
        IProcessEnvironment ProcessEnvironment { get; }
        IGitEnvironment Environment { get; }
        IProcessManager ProcessManager { get; }
        ITaskManager TaskManager { get; }
    }

    public class Platform : IPlatform
    {
        public Platform(ITaskManager taskManager, IGitEnvironment environment, IProcessManager processManager)
        {
            ProcessManager = processManager;
            Environment = environment;
            Instance = this;
        }

        public IPlatform Initialize()
        {
            ProcessEnvironment = new ProcessEnvironment(ProcessManager.DefaultProcessEnvironment, Environment);
            return this;
        }

        public static IPlatform Instance { get; private set; }
        public IGitEnvironment Environment { get; private set; }
        public IProcessEnvironment ProcessEnvironment { get; private set; }
        public IProcessManager ProcessManager { get; private set; }
        public ITaskManager TaskManager { get; private set; }
    }
}
