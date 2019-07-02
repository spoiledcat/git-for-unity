namespace Unity.VersionControl.Git
{
    public enum GitFileStatus
    {
        None,
        Untracked,
        Ignored,
        Modified,
        Added,
        Deleted,
        Renamed,
        Copied,
        TypeChange,
        Unmerged,
        Unknown,
        Broken
    }
}