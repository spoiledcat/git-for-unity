using Unity.VersionControl.Git;

namespace TestUtils
{
    public class CreateEnvironmentOptions
    {
        public const string DefaultExtensionFolder = "ExtensionFolder";
        public const string DefaultUserProfilePath = "UserProfile";
        public const string DefaultUnityProjectPathAndRepositoryPath = "UnityProject";

        public CreateEnvironmentOptions(NPath? basePath = null)
        {
            NPath path = basePath ?? NPath.SystemTemp.Combine(ApplicationInfo.ApplicationName);
            path.EnsureDirectoryExists();
            Extensionfolder = path.Combine(DefaultExtensionFolder);
            UserProfilePath = path.Combine(DefaultUserProfilePath);
            UnityProjectPath = path.Combine(DefaultUnityProjectPathAndRepositoryPath);
            RepositoryPath = path.Combine(DefaultUnityProjectPathAndRepositoryPath);
            Extensionfolder.EnsureDirectoryExists();
            UserProfilePath.EnsureDirectoryExists();
            UnityProjectPath.EnsureDirectoryExists();
        }

        public NPath Extensionfolder { get; set; }
        public NPath UserProfilePath { get; set; }
        public NPath UnityProjectPath { get; set; }
        public string RepositoryPath { get; set; }
    }
}
