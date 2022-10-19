using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.VersionControl.Git
{
    [Serializable]
    public struct GitStatus
    {
        // these are public so Unity can serialize them
        // we don't have access here to the Unity attribute
        // that allows private fields to be serialized

        public string localBranch;
        public string remoteBranch;
        public int ahead;
        public int behind;
        public List<GitStatusEntry> entries;

        private int? hashcode;

        public GitStatus(string localBranch, string remoteBranch, int ahead, int behind, List<GitStatusEntry> entries)
        {
            this.localBranch = localBranch;
            this.remoteBranch = remoteBranch;
            this.ahead = ahead;
            this.behind = behind;
            this.entries = entries;
            hashcode = null;
        }

        public override int GetHashCode()
        {
            if (hashcode.HasValue)
                return hashcode.Value;

            unchecked
            {
                hashcode = (int)2166136261;
                hashcode = hashcode * 1677619 + (LocalBranch?.GetHashCode() ?? 0);
                hashcode = hashcode * 1677619 + (RemoteBranch?.GetHashCode() ?? 0);
                hashcode = hashcode * 1677619 + Ahead.GetHashCode();
                hashcode = hashcode * 1677619 + Behind.GetHashCode();
                foreach (var entry in Entries)
                    hashcode = hashcode * 1677619 + entry.GetHashCode();
                return hashcode.Value;
            }
        }

        public override bool Equals(object other)
        {
            if (other is GitStatus status)
                return Equals(status);
            return false;
        }

        public bool Equals(GitStatus other)
        {
            var equals =
                string.Equals(LocalBranch, other.LocalBranch) &&
                string.Equals(RemoteBranch, other.RemoteBranch) &&
                Ahead == other.Ahead &&
                Behind == other.Behind;

            if (!equals) return false;
            if (Entries == null) return Entries == other.Entries;

            // compare the entries in an unordered fashion
            var left = Entries.Except(other.Entries);
            var right = other.Entries.Except(Entries);
            return !left.Any() && !right.Any();
;        }

        public static bool operator ==(GitStatus lhs, GitStatus rhs)
        {
            // Return true if the fields match:
            return lhs.Equals(rhs);
        }

        public static bool operator !=(GitStatus lhs, GitStatus rhs)
        {
            return !(lhs == rhs);
        }

        public override string ToString()
        {
            var remoteBranchString = string.IsNullOrEmpty(RemoteBranch) ? "?" : string.Format("\"{0}\"", RemoteBranch);
            var entriesString = Entries == null ? "NULL" : Entries.Count.ToString();

            return string.Format("{{GitStatus: \"{0}\"->{1} +{2}/-{3} {4} entries}}", LocalBranch, remoteBranchString, Ahead,
                Behind, entriesString);
        }


        public string LocalBranch => localBranch;
        public string RemoteBranch => remoteBranch;
        public int Ahead => ahead;
        public int Behind => behind;
        public List<GitStatusEntry> Entries => entries;
    }
}
