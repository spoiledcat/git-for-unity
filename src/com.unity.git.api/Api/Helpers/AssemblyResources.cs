using System.IO;
using System.Reflection;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    using IO;

    public enum ResourceType
    {
        Icon,
        Platform,
        Generic
    }

    public class AssemblyResources
    {
        private static (string type, string os) ParseResourceType(ResourceType resourceType, IEnvironment environment)
        {
            var os = "";
            if (resourceType == ResourceType.Platform)
            {
                os =  environment.IsWindows ? "windows"
                    : environment.IsLinux ? "linux"
                    : "mac";
            }
            var type = resourceType == ResourceType.Icon ? "IconsAndLogos" : "PlatformResources";

            return (type, os);
        }

        private static Stream TryGetResource(ResourceType resourceType, string type, string os, string resource)
        {
            // all the resources are embedded in Git.Api
            var asm = Assembly.GetCallingAssembly();
            if (resourceType != ResourceType.Icon)
                asm = typeof(AssemblyResources).Assembly;

            return asm.GetManifestResourceStream($"Unity.VersionControl.Git.{type}{(!string.IsNullOrEmpty(os) ? "." + os : os)}.{resource}");
        }

        private static Stream TryGetStream(ResourceType resourceType, string resource, IGitEnvironment environment)
        {
            /*
                This function attempts to get files embedded in the callers assembly.
                Unity.VersionControl.Git which tends to contain logos
                Git.Api which tends to contain application resources

                Each file's name is their physical path in the project.

                When running tests, we assume the tests are looking for application resources, and default to returning Git.Api

                First check for the resource in the calling assembly.
                If the resource cannot be found, fallback to looking in Git.Api's assembly.
                If the resource is still not found, it attempts to find it in the file system
             */

            (string type, string os) = ParseResourceType(resourceType, environment);

            var stream = TryGetResource(resourceType, type, os, resource);
            if (stream != null)
                return stream;

            SPath possiblePath = environment.ExtensionInstallPath.Combine(type, os, resource);
            if (possiblePath.FileExists())
            {
                return new MemoryStream(possiblePath.ReadAllBytes());
            }

            var basePath = resourceType == ResourceType.Icon ? "Editor" : "Api";
            possiblePath = environment.ExtensionInstallPath.Parent.Combine(basePath, type, os, resource);
            if (possiblePath.FileExists())
            {
                return new MemoryStream(possiblePath.ReadAllBytes());
            }

            return null;
        }

        private static SPath TryGetFile(ResourceType resourceType, string resource, IGitEnvironment environment)
        {
            /*
                This function attempts to get files embedded in the callers assembly.
                Unity.VersionControl.Git which tends to contain logos
                Git.Api which tends to contain application resources

                Each file's name is their physical path in the project.

                When running tests, we assume the tests are looking for application resources, and default to returning Git.Api

                First check for the resource in the calling assembly.
                If the resource cannot be found, fallback to looking in Git.Api's assembly.
                If the resource is still not found, it attempts to find it in the file system
             */

            (string type, string os) = ParseResourceType(resourceType, environment);

            var stream = TryGetResource(resourceType, type, os, resource);
            if (stream != null)
            {
                var target = SPath.GetTempFilename();
                return target.WriteAllBytes(stream.ToByteArray());
            }

            SPath possiblePath = environment.ExtensionInstallPath.Combine(type, os, resource);
            if (possiblePath.FileExists())
            {
                return possiblePath;
            }

            var basePath = resourceType == ResourceType.Icon ? "Editor" : "Api";
            possiblePath = environment.ExtensionInstallPath.Parent.Combine(basePath, type, os, resource);
            if (possiblePath.FileExists())
            {
                return possiblePath;
            }

            return SPath.Default;
        }


        public static SPath ToFile(ResourceType resourceType, string resource, SPath destinationPath, IGitEnvironment environment)
        {
            var source = TryGetFile(resourceType, resource, environment);
            if (source.IsInitialized)
            {
                return source.Copy(destinationPath);
            }
            return SPath.Default;
        }

        public static Stream ToStream(ResourceType resourceType, string resource, IGitEnvironment environment)
        {
            return TryGetStream(resourceType, resource, environment);
        }

    }
}
