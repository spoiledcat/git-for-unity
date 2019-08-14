namespace Unity.VersionControl.Git
{
    interface IAwaitable
    {
        IAwaiter GetAwaiter();
    }
}