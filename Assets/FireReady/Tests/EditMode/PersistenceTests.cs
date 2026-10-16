using System.IO;
using FireReady.Core;
using FireReady.Data;
using NUnit.Framework;

namespace FireReady.Tests
{
    public class PersistenceTests
    {
        class MemoryStorage : IDataStorage
        {
            public string Content;
            public string BackupContent;
            public int Writes;

            public bool Exists() => Content != null;
            public string Read() => Content;
            public void Write(string json) { Content = json; Writes++; }
            public void Backup() { BackupContent = Content; Content = null; }
            public void Delete() { Content = null; }
        }

        static SessionRecord Record(int score)
        {
            return new SessionRecord { sessionId = score.ToString(), score = score, Outcome = SessionOutcome.Completed };
        }

        [Test]
        public void Newest_session_comes_first()
        {
            var repository = new SessionRepository(new MemoryStorage());
            repository.Load();
            repository.Save(Record(10));
            repository.Save(Record(20));

            var recent = repository.LoadRecent(5);
            Assert.AreEqual(2, recent.Count);
            Assert.AreEqual(20, recent[0].score);
        }

        [Test]
        public void History_is_trimmed_to_the_maximum()
        {
            var repository = new SessionRepository(new MemoryStorage(), maxSessions: 3);
            repository.Load();
            for (int i = 0; i < 6; i++) repository.Save(Record(i));

            Assert.AreEqual(3, repository.Count);
            Assert.AreEqual(5, repository.LoadRecent(10)[0].score);
        }

        [Test]
        public void Saved_sessions_survive_a_reload()
        {
            var storage = new MemoryStorage();
            var first = new SessionRepository(storage);
            first.Load();
            first.Save(Record(77));

            var second = new SessionRepository(storage);
            second.Load();
            Assert.AreEqual(1, second.Count);
            Assert.AreEqual(77, second.LoadRecent(1)[0].score);
            Assert.AreEqual(SessionOutcome.Completed, second.LoadRecent(1)[0].Outcome);
        }

        [Test]
        public void Corrupt_file_is_backed_up_and_history_starts_empty()
        {
            var storage = new MemoryStorage { Content = "{ not valid json" };
            var repository = new SessionRepository(storage);
            repository.Load();

            Assert.AreEqual(0, repository.Count);
            Assert.IsTrue(repository.RecoveredFromCorruptFile);
            Assert.AreEqual("{ not valid json", storage.BackupContent);
        }

        [Test]
        public void Clear_empties_the_history()
        {
            var repository = new SessionRepository(new MemoryStorage());
            repository.Load();
            repository.Save(Record(1));
            repository.Clear();
            Assert.AreEqual(0, repository.Count);
        }

        [Test]
        public void File_storage_replaces_the_file_and_leaves_no_temp_file()
        {
            var folder = Path.Combine(Path.GetTempPath(), "FireReadyTests_" + System.Guid.NewGuid().ToString("N"));
            var path = Path.Combine(folder, "sessions.json");
            try
            {
                var storage = new JsonFileStorage(path);
                storage.Write("first");
                storage.Write("second");

                Assert.AreEqual("second", storage.Read());
                Assert.IsFalse(File.Exists(path + ".tmp"));

                storage.Backup();
                Assert.IsFalse(storage.Exists());
                Assert.IsTrue(File.Exists(path + ".bak"));
            }
            finally
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }
    }
}
