using System.IO;

namespace FireReady.Data
{
    /// <summary>
    /// Writes to a .tmp file first and then swaps it in, so a crash mid-write
    /// leaves the previous history untouched.
    /// </summary>
    public class JsonFileStorage : IDataStorage
    {
        readonly string path;

        public string Path => path;
        string TempPath => path + ".tmp";
        string BackupPath => path + ".bak";

        public JsonFileStorage(string path)
        {
            this.path = path;
        }

        public bool Exists() => File.Exists(path);

        public string Read() => File.Exists(path) ? File.ReadAllText(path) : null;

        public void Write(string json)
        {
            var directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            File.WriteAllText(TempPath, json);

            if (File.Exists(path)) File.Replace(TempPath, path, null);
            else File.Move(TempPath, path);
        }

        public void Backup()
        {
            if (!File.Exists(path)) return;
            if (File.Exists(BackupPath)) File.Delete(BackupPath);
            File.Move(path, BackupPath);
        }

        public void Delete()
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(TempPath)) File.Delete(TempPath);
        }
    }
}
