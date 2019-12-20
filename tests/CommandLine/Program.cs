using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Mono.Options;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Helpers;
using Unity.Editor.Tasks.Logging;
using Unity.VersionControl.Git;
using Unity.VersionControl.Git.IO;

namespace Tests.CommandLine
{
    class ConsoleLogAdapter : LogAdapterBase
    {
        public override void Info(string context, string message)
        {
            WriteLine(context, message);
        }

        public override void Debug(string context, string message)
        {
            WriteLine(context, message);
        }

        public override void Trace(string context, string message)
        {
            WriteLine(context, message);
        }

        public override void Warning(string context, string message)
        {
            WriteLine(context, message);
        }

        public override void Error(string context, string message)
        {
            WriteLine(context, message);
        }

        private string GetMessage(string context, string message)
        {
            var time = DateTime.Now.ToString("HH:mm:ss.fff tt");
            var threadId = Thread.CurrentThread.ManagedThreadId;
            return string.Format("{0} [{1,2}] {2} {3}", time, threadId, context, message);
        }

        private void WriteLine(string context, string message)
        {
            Console.WriteLine(GetMessage(context, message));
        }
    }

    static class Program
    {
        private static string ReadAllTextIfFileExists(this string path)
        {
            var file = path.ToSPath();
            if (!file.IsInitialized || !file.FileExists())
                return null;
            return file.ReadAllText();
        }

        private static ILogging Logger = LogHelper.GetLogger();

        static void RunWebServer(SPath path, int port)
        {
            if (!path.IsInitialized)
            {
                path = typeof(Program).Assembly.Location.ToSPath().Parent.Combine("files");
            }

            var server = new TestWebServer.HttpServer(path, port);
            var thread = new Thread(() =>
            {
                Logger.Error($"Press any key to exit");
                server.Start();
            });
            thread.Start();
            Console.Read();
            server.Stop();
        }

        static TestWebServer.HttpServer RunWebServer(int port)
        {
            var path = typeof(Program).Assembly.Location.ToSPath().Parent.Combine("files");
            var server = new TestWebServer.HttpServer(path, port);
            var thread = new Thread(() =>
            {
                server.Start();
            });
            thread.Start();
            return server;
        }

        static int Main(string[] args)
        {
            LogHelper.LogAdapter = new ConsoleLogAdapter();

            int retCode = 0;
            string data = null;
            string error = null;
            int sleepms = 0;
            var p = new OptionSet();
            var readInputToEof = false;
            var lines = new List<string>();
            bool runWebServer = false;
            SPath outfile = SPath.Default;
            SPath path = SPath.Default;
            string releaseNotes = null;
            int webServerPort = -1;
            bool generateVersion = false;
            bool generatePackage = false;
            string version = null;
            string url = null;
            string readVersion = null;
            string msg = null;
            string host = null;
            bool runUsage = false;

            var arguments = new List<string>(args);
            if (arguments.Contains("usage"))
            {
                runUsage = true;
                arguments.RemoveRange(0, 2);
            }

            p = p
                .Add("r=", (int v) => retCode = v)
                .Add("d=|data=", v => data = v)
                .Add("e=|error=", v => error = v)
                .Add("f=|file=", v => data = File.ReadAllText(v))
                .Add("ef=|errorFile=", v => error = File.ReadAllText(v))
                .Add("sleep=", (int v) => sleepms = v)
                .Add("i|input", v => readInputToEof = true)
                .Add("w|web", v => runWebServer = true)
                .Add("p|port=", "Port", (int v) => webServerPort = v)
                .Add("g|generateVersion", v => generateVersion = true)
                .Add("v=|version=", v => version = v)
                .Add("gen-package", "Pass --version --url --path --md5 --rn --msg to generate a package", v => generatePackage = true)
                .Add("u=|url=", v => url = v)
                .Add("path=", v => path = v.ToSPath())
                .Add("rn=", "Path to file with release notes", v => releaseNotes = v.ReadAllTextIfFileExists())
                .Add("msg=", "Path to file with message for package", v => msg = v.ReadAllTextIfFileExists())
                .Add("readVersion=", v => readVersion = v)
                .Add("o=|outfile=", v => outfile = v.ToSPath().MakeAbsolute())
                .Add("h=", "Host", v => host = v)
                .Add("help", v => p.WriteOptionDescriptions(Console.Out));

            var extra = p.Parse(arguments);
            if (runUsage)
            {
                extra.Remove("usage");
                p.Parse(extra);

                path = extra[extra.Count - 1].ToSPath();
                var server = RunWebServer(webServerPort);
                var webRequest = (HttpWebRequest)WebRequest.Create(new UriString("http://localhost:" + webServerPort + "/api/usage/unity"));
                webRequest.Method = "POST";
                using (var sw = new StreamWriter(webRequest.GetRequestStream()))
                {
                    foreach (var line in path.ReadAllLines())
                    {
                        sw.WriteLine(line);
                    }
                }
                using (var webResponse = (HttpWebResponse)webRequest.GetResponseWithoutException())
                {
                    MemoryStream ms = new MemoryStream();
                    var responseLength = webResponse.ContentLength;
                    using (var sr = new StreamWriter(ms))
                    using (var responseStream = webResponse.GetResponseStream())
                    {
                        Utils.Copy(responseStream, ms, responseLength);
                    }
                    Console.WriteLine(Encoding.ASCII.GetString(ms.ToArray()));
                }

                server.Stop();
                return 0;
            }

            if (generatePackage)
            {
                var md5 = path.CalculateMD5();
                url += "/" + path.FileName;
                var package = new Package
                {
                    Message = msg,
                    Md5 = md5,
                    ReleaseNotes = releaseNotes,
                    ReleaseNotesUrl = null,
                    Url = url,
                    Version = TheVersion.Parse(version),
                };

                var json = package.ToJson(lowerCase: true, onlyPublic: false);
                if (outfile.IsInitialized)
                    outfile.WriteAllText(json);
                else
                    Console.WriteLine(json);
                return 0;
            }

            if (readVersion != null)
            {
                var json = File.ReadAllText(readVersion);
                var package = json.FromJson<Package>(lowerCase: true, onlyPublic: false);
                Console.WriteLine(package);
                Console.WriteLine($"{package.Url} {package.Version}");
                return 0;
            }

            if (generateVersion)
            {
                Logger.Error($"Generating version json {version} to {(outfile.IsInitialized ? outfile : "console")}");
                var vv = TheVersion.Parse(version);
                url += $"/unity/releases/github-for-unity-{version}.unitypackage";
                var package = new Package { Url = url, Version = vv};
                var json = package.ToJson(lowerCase: true, onlyPublic: false);
                if (outfile.IsInitialized)
                    outfile.WriteAllText(json);
                else
                    Logger.Info(json);
                return 0;
            }

            if (runWebServer)
            {
                if (webServerPort < 0)
                    webServerPort = 50000;
                RunWebServer(outfile, webServerPort);
                return 0;
            }

            if (readInputToEof)
            {
                string line;
                while ((line = Console.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            if (sleepms > 0)
                Thread.Sleep(sleepms);

            if (!String.IsNullOrEmpty(data))
                Console.WriteLine(data);
            else if (readInputToEof)
                Console.WriteLine(String.Join(Environment.NewLine, lines.ToArray()));

            if (!String.IsNullOrEmpty(error))
                Console.Error.WriteLine(error);

            return retCode;
        }
    }

    public static class SPathExtensions
    {
        public static string CalculateMD5(this SPath file)
        {
            byte[] computeHash;
            using (var md5 = MD5.Create())
            {
                using (var stream = file.OpenRead())
                {
                    computeHash = md5.ComputeHash(stream);
                }
            }

            return BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
        }
    }
}
