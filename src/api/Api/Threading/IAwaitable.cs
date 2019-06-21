namespace Unity.Git
{
    interface IAwaitable
    {
        IAwaiter GetAwaiter();
    }
}