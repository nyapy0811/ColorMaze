using NUnit.Framework;
using UnityEngine;

namespace ColorMaze
{
    public class MapSnapshotTests
    {
        [Test]
        public void JsonRoundTrip_KeepsHeldOrbsAndFixtureStates()
        {
            var inventory = new OrbInventory(5, new OrbValue(8, 7, 9));
            inventory.TryAdd(new OrbValue(2, 0, 0), out _);
            inventory.TryAdd(new OrbValue(0, 3, 1), out _);

            var snapshot = new MapSnapshot
            {
                mapId = "test_core_loop",
                mapVersion = 1,
                playerPosition = new Vector3(1f, 0f, 2f),
                playerYaw = 90f,
                playerPitch = -10f,
                heldOrbs = inventory.ExportValues(),
                channelLimit = inventory.ChannelLimit,
                playTime = 12.5f,
            };
            snapshot.fixtures.Add(new FixtureStateEntry { fixtureId = 3, state = "{\"storedOrb\":\"2 1 0\"}" });
            snapshot.activatedCheckpointIds.Add(7);

            var loaded = JsonUtility.FromJson<MapSnapshot>(JsonUtility.ToJson(snapshot));
            var restored = new OrbInventory(5, new OrbValue(5, 5, 5));
            restored.Import(loaded.heldOrbs, loaded.channelLimit);

            CollectionAssert.AreEqual(inventory.ExportValues(), restored.ExportValues());
            Assert.AreEqual(new OrbValue(8, 7, 9), restored.ChannelLimit);
            Assert.AreEqual("test_core_loop", loaded.mapId);
            Assert.AreEqual(snapshot.playerPosition, loaded.playerPosition);
            Assert.AreEqual(1, loaded.fixtures.Count);
            Assert.AreEqual(3, loaded.fixtures[0].fixtureId);
            Assert.AreEqual(snapshot.fixtures[0].state, loaded.fixtures[0].state);
            CollectionAssert.AreEqual(new[] { 7 }, loaded.activatedCheckpointIds);
        }
    }
}
