using Unity.VersionControl.Git;

namespace TestUtils
{
    public class CreateEnvironmentOptions
    {
        public const string DefaultExtensionFolder = "ExtensionFolder";
        public const string DefaultUserProfilePath = "UserProfile";
        public const string DefaultUnityProjectPathAndRepositoryPath = "UnityProject";

        public CreateEnvironmentOptions(SPath? basePath = null)
        {
            SPath path = basePath ?? SPath.SystemTemp.Combine(ApplicationInfo.ApplicationName);
            path.EnsureDirectoryExists();
            Extensionfolder = path.Combine(DefaultExtensionFolder);
            UserProfilePath = path.Combine(DefaultUserProfilePath);
            UnityProjectPath = path.Combine(DefaultUnityProjectPathAndRepositoryPath);
            RepositoryPath = path.Combine(DefaultUnityProjectPathAndRepositoryPath);
            Extensionfolder.EnsureDirectoryExists();
            UserProfilePath.EnsureDirectoryExists();
            UnityProjectPath.EnsureDirectoryExists();
        }

        public SPath Extensionfolder { get; set; }
        public SPath UserProfilePath { get; set; }
        public SPath UnityProjectPath { get; set; }
        public string RepositoryPath { get; set; }
    }
}
