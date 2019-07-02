namespace Unity.VersionControl.Git
{
    sealed class RepositoryInfoCacheData : IRepositoryInfoCacheData
    {
        public GitRemote? CurrentGitRemote { get; set; }
        public GitBranch? CurrentGitBranch { get; set; }
        public ConfigRemote? CurrentConfigRemote { get; set; }
        public ConfigBranch? CurrentConfigBranch { get; set; }
        public string CurrentHead { get; set; }
    }
}
