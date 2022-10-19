using System.Collections.Generic;
using System.Linq;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    class RemoteListOutputProcessor : BaseOutputListProcessor<GitRemote>
    {
        private string currentName;
        private string currentUrl;
        private List<string> currentModes;

        public RemoteListOutputProcessor()
        {
            Reset();
        }

        protected override bool ProcessLine(string line, out GitRemote result)
        {
            base.ProcessLine(line, out result);

            //origin https://github.com/github/VisualStudio.git (fetch)

            if (line == null)
            {
                result = ReturnRemote();
                return true;
            }

            var shouldRaiseEntry = false;

            var proc = new LineParser(line);
            var name = proc.ReadUntilWhitespace();
            proc.SkipWhitespace();

            var url = proc.ReadUntilWhitespace();
            proc.SkipWhitespace();

            proc.MoveNext();
            var mode = proc.ReadUntil(')');

            if (currentName == null)
            {
                currentName = name;
                currentUrl = url;
                currentModes.Add(mode);
            }
            else if (currentName == name)
            {
                currentModes.Add(mode);
            }
            else
            {
                shouldRaiseEntry = true;
                result = ReturnRemote();

                currentName = name;
                currentUrl = url;
                currentModes.Add(mode);
            }
            return shouldRaiseEntry;
        }

        private GitRemote ReturnRemote()
        {
            var modes = currentModes.Select(s => s.ToUpperInvariant()).ToArray();

            var isFetch = modes.Contains("FETCH");
            var isPush = modes.Contains("PUSH");

            GitRemoteFunction remoteFunction;
            if (isFetch && isPush)
            {
                remoteFunction = GitRemoteFunction.Both;
            }
            else if (isFetch)
            {
                remoteFunction = GitRemoteFunction.Fetch;
            }
            else if (isPush)
            {
                remoteFunction = GitRemoteFunction.Push;
            }
            else
            {
                remoteFunction = GitRemoteFunction.Unknown;
            }

            string host;
            string user = null;
            var proc = new LineParser(currentUrl);
            if (proc.Matches("http") || proc.Matches("https"))
            {
                proc.MoveToAfter(':');
                proc.MoveNext();
                proc.MoveNext();
                host = proc.ReadUntil('/');
            }
            else
            {
                //Assuming SSH here
                user = proc.ReadUntil('@');
                proc.MoveNext();
                host = proc.ReadUntil(':');

                currentUrl = currentUrl.Substring(user.Length + 1);
            }

            var remote = new GitRemote(currentName, host, currentUrl, remoteFunction, user, null, null);
            Reset();
            return remote;
        }

        private void Reset()
        {
            currentName = null;
            currentModes = new List<string>();
            currentUrl = null;
        }
    }
}
