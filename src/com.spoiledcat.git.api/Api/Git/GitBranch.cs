using System;

namespace Unity.VersionControl.Git
{
    [Serializable]
    public struct GitBranch
    {
        public static GitBranch Default = new GitBranch();

        public string name;
        public string tracking;

        public GitBranch(string name, string tracking = null)
        {
            Guard.ArgumentNotNullOrWhiteSpace(name, "name");

            this.name = name;
            this.tracking = tracking ?? string.Empty;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (name?.GetHashCode() ?? 0);
            hash = hash * 23 + (tracking?.GetHashCode() ?? 0);
            return hash;
        }

        public override bool Equals(object other)
        {
            if (other is GitBranch)
                return Equals((GitBranch)other);
            return false;
        }

        public bool Equals(GitBranch other)
        {
            return
                string.Equals(name, other.name) &&
                string.Equals(tracking, other.tracking);
        }

        public static bool operator ==(GitBranch lhs, GitBranch rhs)
        {
            // Return true if the fields match:
            return lhs.Equals(rhs);
        }

        public static bool operator !=(GitBranch lhs, GitBranch rhs)
        {
            return !(lhs == rhs);
        }

        public string Name => name;
        public string Tracking => tracking;

        public override string ToString()
        {
            return $"{Name} Tracking? {Tracking ?? "[NULL]"}";
        }
    }
}
