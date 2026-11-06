using NUnit.Framework;
using UnityEngine;

namespace ColorMaze
{
    public class ProgressSaveTests
    {
        static MapSnapshot Snapshot(string mapId, int mapVersion) => new() { mapId = mapId, mapVersion = mapVersion, playTime = 30f };

        [Test]
        public void RecordClear_KeepsBestTime()
        {
            var save = new ProgressSave();
            save.RecordClear("a", 50f);
            save.RecordClear("a", 70f);
            Assert.AreEqual(50f, save.FindMap("a").bestTime);
            save.RecordClear("a", 40f);
            Assert.AreEqual(40f, save.FindMap("a").bestTime);
            Assert.IsTrue(save.FindMap("a").cleared);
            Assert.IsNull(save.FindMap("b"));
        }

        [Test]
        public void ClearCheckpoint_OnlyForThatMap()
        {
            var save = new ProgressSave();
            save.SetCheckpoint(Snapshot("a", 1));
            Assert.IsFalse(save.ClearCheckpoint("b"));
            Assert.IsTrue(save.hasCheckpoint);
            Assert.IsTrue(save.ClearCheckpoint("a"));
            Assert.IsFalse(save.hasCheckpoint);
        }

        [Test]
        public void TryGetCheckpoint_DiscardsOtherMapVersion()
        {
            var save = new ProgressSave();
            save.SetCheckpoint(Snapshot("a", 1));

            Assert.IsTrue(save.TryGetCheckpoint("a", 1, out var snapshot, out bool discarded));
            Assert.AreEqual(30f, snapshot.playTime);
            Assert.IsFalse(discarded);

            Assert.IsFalse(save.TryGetCheckpoint("b", 1, out _, out discarded), "다른 맵의 기록은 꺼내지 않는다");
            Assert.IsFalse(discarded);

            Assert.IsFalse(save.TryGetCheckpoint("a", 2, out _, out discarded), "맵 버전이 다르면 버린다");
            Assert.IsTrue(discarded);
            Assert.IsFalse(save.hasCheckpoint);
        }

        [Test]
        public void JsonRoundTrip_KeepsRecordsAndCheckpoint()
        {
            var save = new ProgressSave { savedAt = "2026-11-06T00:00:00Z" };
            save.RecordClear("a", 42.5f);
            save.SetCheckpoint(Snapshot("b", 3));

            var loaded = JsonUtility.FromJson<ProgressSave>(JsonUtility.ToJson(save));

            Assert.AreEqual(ProgressSave.CurrentVersion, loaded.version);
            Assert.AreEqual(42.5f, loaded.FindMap("a").bestTime);
            Assert.IsTrue(loaded.hasCheckpoint);
            Assert.AreEqual("b", loaded.checkpoint.mapId);
            Assert.AreEqual(3, loaded.checkpoint.mapVersion);
        }
    }
}
