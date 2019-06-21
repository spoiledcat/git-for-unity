using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;

namespace GitHub.Unity
{
    enum ResourceType
    {
        Icon,
        Platform,
        Generic
    }

    class AssemblyResources
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
            var type = resourceType == ResourceType.Icon ? "IconsAndLogos"
                : resourceType == ResourceType.Platform ? "PlatformResources"
                : "Resources";

            return (type, os);
        }

        private static Stream TryGetResource(ResourceType resourceType, string type, string os, string resource)
        {
            // all the resources are embedded in GitHub.Api
            var asm = Assembly.GetCallingAssembly();
            if (resourceType != ResourceType.Icon)
                asm = typeof(AssemblyResources).Assembly;

            return asm.GetManifestResourceStream($"GitHub.Unity.{type}{(!string.IsNullOrEmpty(os) ? "." + os : os)}.{resource}");
        }

        private static Stream TryGetStream(ResourceType resourceType, string resource, IEnvironment environment)
        {
            /*
                This function attempts to get files embedded in the callers assembly.
                GitHub.Unity which tends to contain logos
                GitHub.Api which tends to contain application resources

                Each file's name is their physical path in the project.

                When running tests, we assume the tests are looking for application resources, and default to returning GitHub.Api 

                First check for the resource in the calling assembly.
                If the resource cannot be found, fallback to looking in GitHub.Api's assembly.
                If the resource is still not found, it attempts to find it in the file system
             */

            (string type, string os) = ParseResourceType(resourceType, environment);

            var stream = TryGetResource(resourceType, type, os, resource);
            if (stream != null)
                return stream;

            NPath possiblePath = environment.ExtensionInstallPath.Combine(type, os, resource);
            if (possiblePath.FileExists())
            {
                return new MemoryStream(possiblePath.ReadAllBytes());
            }

            var basePath = resourceType == ResourceType.Icon ? "GitHub.Unity" : "GitHub.Api";
            possiblePath = environment.ExtensionInstallPath.Parent.Combine(basePath, type, os, resource);
            if (possiblePath.FileExists())
            {
                return new MemoryStream(possiblePath.ReadAllBytes());
            }

            return null;
        }

        private static NPath TryGetFile(ResourceType resourceType, string resource, IEnvironment environment)
        {
            /*
                This function attempts to get files embedded in the callers assembly.
                GitHub.Unity which tends to contain logos
                GitHub.Api which tends to contain application resources

                Each file's name is their physical path in the project.

                When running tests, we assume the tests are looking for application resources, and default to returning GitHub.Api 

                First check for the resource in the calling assembly.
                If the resource cannot be found, fallback to looking in GitHub.Api's assembly.
                If the resource is still not found, it attempts to find it in the file system
             */

            (string type, string os) = ParseResourceType(resourceType, environment);

            var stream = TryGetResource(resourceType, type, os, resource);
            if (stream != null)
            {
                var target = NPath.GetTempFilename();
                return target.WriteAllBytes(stream.ToByteArray());
            }

            NPath possiblePath = environment.ExtensionInstallPath.Combine(type, os, resource);
            if (possiblePath.FileExists())
            {
                return possiblePath;
            }

            var basePath = resourceType == ResourceType.Icon ? "GitHub.Unity" : "GitHub.Api";
            possiblePath = environment.ExtensionInstallPath.Parent.Combine(basePath, type, os, resource);
            if (possiblePath.FileExists())
            {
                return possiblePath;
            }

            return NPath.Default;
        }


        public static NPath ToFile(ResourceType resourceType, string resource, NPath destinationPath, IEnvironment environment)
        {
            var target = destinationPath.Combine(resource);
            var source = TryGetFile(resourceType, resource, environment);
            if (source.IsInitialized)
            {
                target.DeleteIfExists();
                return source.Copy(target);
            }
            return NPath.Default;
        }

        public static Stream ToStream(ResourceType resourceType, string resource, IEnvironment environment)
        {
            return TryGetStream(resourceType, resource, environment);
        }

    }
}
