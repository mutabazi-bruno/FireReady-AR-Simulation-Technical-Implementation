namespace FireReady.Data
{
    // Hides where session data lives, so the JSON file can be swapped for a test double.
    public interface IDataStorage
    {
        bool Exists();
        string Read();
        void Write(string json);

        // Keeps a copy of an unreadable file and removes it so the app can start fresh (NFR-08).
        void Backup();
        void Delete();
    }
}
