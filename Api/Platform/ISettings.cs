namespace Unity.VersionControl.Git
{
    using IO;

    public interface ISettings
    {
        void Initialize();
        bool Exists(string key);
        string Get(string key, string fallback = "");
        T Get<T>(string key, T fallback = default(T));
        void Set<T>(string key, T value);
        void Unset(string key);
        void Rename(string oldKey, string newKey);
        SPath SettingsPath { get; }
    }
}
