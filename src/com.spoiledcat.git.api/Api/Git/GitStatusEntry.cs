using System;
using Unity.VersionControl.Git.IO;

namespace Unity.VersionControl.Git
{
    [Serializable]
    public struct GitStatusEntry
    {
        public static GitStatusEntry Default = new GitStatusEntry();

        public string path;
        public string fullPath;
        public string projectPath;
        public string originalPath;
        public GitFileStatus indexStatus;
        public GitFileStatus workTreeStatus;

        public GitStatusEntry(string path, string fullPath, string projectPath,
            GitFileStatus indexStatus, GitFileStatus workTreeStatus,
            string originalPath = null)
        {
            Guard.ArgumentNotNullOrWhiteSpace(path, "path");
            Guard.ArgumentNotNullOrWhiteSpace(fullPath, "fullPath");

            this.path = path;
            this.indexStatus = indexStatus;
            this.workTreeStatus = workTreeStatus;
            this.fullPath = fullPath;
            this.projectPath = projectPath;
            this.originalPath = originalPath;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)2166136261;
                hash = hash * 1677619 + GetPath(path)?.GetHashCode() ?? 0;
                hash = hash * 1677619 + GetPath(fullPath)?.GetHashCode() ?? 0;
                hash = hash * 1677619 + GetPath(projectPath)?.GetHashCode() ?? 0;
                hash = hash * 1677619 + GetPath(originalPath)?.GetHashCode() ?? 0;
                hash = hash * 1677619 + indexStatus.GetHashCode();
                hash = hash * 1677619 + workTreeStatus.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object other)
        {
            if (other is GitStatusEntry entry)
                return Equals(entry);
            return false;
        }

        public bool Equals(GitStatusEntry other)
        {
            return
                ComparePath(path, other.path) != null &&
                ComparePath(fullPath, other.fullPath) != null &&
                ComparePath(projectPath, other.projectPath) != null &&
                ComparePath(originalPath, other.originalPath) != null &&
                indexStatus == other.indexStatus &&
                workTreeStatus == other.workTreeStatus
                ;
        }

        public static bool operator ==(GitStatusEntry lhs, GitStatusEntry rhs)
        {
            // Return true if the fields match:
            return lhs.Equals(rhs);
        }

        public static bool operator !=(GitStatusEntry lhs, GitStatusEntry rhs)
        {
            return !(lhs == rhs);
        }

        public static GitFileStatus ParseStatusMarker(char changeFlag)
        {
            GitFileStatus status = GitFileStatus.None;
            switch (changeFlag)
            {
                case 'M':
                    status = GitFileStatus.Modified;
                    break;
                case 'A':
                    status = GitFileStatus.Added;
                    break;
                case 'D':
                    status = GitFileStatus.Deleted;
                    break;
                case 'R':
                    status = GitFileStatus.Renamed;
                    break;
                case 'C':
                    status = GitFileStatus.Copied;
                    break;
                case 'U':
                    status = GitFileStatus.Unmerged;
                    break;
                case 'T':
                    status = GitFileStatus.TypeChange;
                    break;
                case 'X':
                    status = GitFileStatus.Unknown;
                    break;
                case 'B':
                    status = GitFileStatus.Broken;
                    break;
                case '?':
                    status = GitFileStatus.Untracked;
                    break;
                case '!':
                    status = GitFileStatus.Ignored;
                    break;
                default: break;
            }
            return status;
        }

        private static SPath? GetPath(string left)
        {
            var (leftp, lefts) = SPath.TryParse(left);
            if (lefts) return leftp;
            return null;
        }

        private static SPath? ComparePath(string left, string right)
        {
            var (leftp, lefts) = SPath.TryParse(left);
            var (rightp, rights) = SPath.TryParse(right);
            if (lefts == rights && leftp == rightp) return leftp;
            return null;
        }

        public string Path => GetPath(path);

        public string FullPath => GetPath(fullPath);

        public string ProjectPath => GetPath(projectPath);

        public string OriginalPath => GetPath(originalPath);

        public GitFileStatus Status => workTreeStatus != GitFileStatus.None ? workTreeStatus : indexStatus;
        public GitFileStatus IndexStatus => indexStatus;
        public GitFileStatus WorkTreeStatus => workTreeStatus;

        public bool Staged => indexStatus != GitFileStatus.None && !Unmerged && !Untracked && !Ignored;

        public bool Unmerged => (indexStatus == workTreeStatus && (indexStatus == GitFileStatus.Added || indexStatus == GitFileStatus.Deleted)) ||
                                 indexStatus == GitFileStatus.Unmerged || workTreeStatus == GitFileStatus.Unmerged;

        public bool Untracked => workTreeStatus == GitFileStatus.Untracked;
        public bool Ignored => workTreeStatus == GitFileStatus.Ignored;

        public override string ToString()
        {
            return $"Path:'{Path}' Status:'{Status}' FullPath:'{FullPath}' ProjectPath:'{ProjectPath}' OriginalPath:'{OriginalPath}' Staged:'{Staged}' Unmerged:'{Unmerged}' Status:'{IndexStatus}'  Status:'{WorkTreeStatus}' ";
        }
    }
}
