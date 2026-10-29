using NUnit.Framework;
using UnityEngine;

namespace ColorMaze
{
    public class NodeGraphRulesTests
    {
        static NodeData Node(NodeGraphData data, int x, int z)
        {
            var node = new NodeData { id = NodeGraphRules.IssueId(data), position = new Vector3Int(x, 0, z) };
            data.nodes.Add(node);
            return node;
        }

        static WallData Wall(NodeGraphData data, NodeData start, NodeData end)
        {
            var wall = new WallData { id = NodeGraphRules.IssueId(data), startNodeId = start.id, endNodeId = end.id };
            data.walls.Add(wall);
            return wall;
        }

        [Test]
        public void IssueId_StartsAtOneAndIncreases()
        {
            var data = new NodeGraphData();
            Assert.AreEqual(1, NodeGraphRules.IssueId(data));
            Assert.AreEqual(2, NodeGraphRules.IssueId(data));
        }

        [Test]
        public void RefreshNextId_ContinuesAfterLargestIdIncludingDoors()
        {
            var data = new NodeGraphData { nextId = 1 };
            data.nodes.Add(new NodeData { id = 4 });
            data.walls.Add(new WallData { id = 2, hasDoor = true, door = new DoorData { id = 9 } });
            NodeGraphRules.RefreshNextId(data);
            Assert.AreEqual(10, data.nextId);
        }

        [Test]
        public void Merge_KeepsFirstNodeAndRewiresWalls()
        {
            var data = new NodeGraphData();
            var a = Node(data, 0, 0);
            var b = Node(data, 3, 0);
            var c = Node(data, 0, 2);
            var moved = Node(data, 3, 0);
            var ab = Wall(data, a, b);
            var movedC = Wall(data, moved, c);

            Assert.AreEqual(1, NodeGraphRules.MergeNodesAtSamePosition(data));

            Assert.AreEqual(3, data.nodes.Count);
            Assert.IsFalse(data.nodes.Contains(moved));
            Assert.AreEqual(b.id, ab.endNodeId);
            Assert.AreEqual(b.id, movedC.startNodeId);
        }

        [Test]
        public void Merge_RemovesWallBetweenMergedNodes()
        {
            var data = new NodeGraphData();
            var a = Node(data, 1, 1);
            var b = Node(data, 1, 1);
            Wall(data, a, b);

            NodeGraphRules.MergeNodesAtSamePosition(data);

            Assert.AreEqual(1, data.nodes.Count);
            Assert.AreEqual(0, data.walls.Count);
        }

        [Test]
        public void ModuleCount_FillsWithQuarterModulesAndLeavesShortRemainder()
        {
            Assert.AreEqual(8, NodeGraphRules.ModuleCount(2f));
            Assert.AreEqual(0f, NodeGraphRules.Remainder(2f), 1e-5f);

            float diagonal = NodeGraphRules.SegmentLength(new Vector3Int(1, 0, 1), new Vector3Int(3, 0, 5));
            Assert.AreEqual(Mathf.Sqrt(20f), diagonal, 1e-5f);
            Assert.AreEqual(17, NodeGraphRules.ModuleCount(diagonal));
            Assert.Less(NodeGraphRules.Remainder(diagonal), NodeGraphRules.ModuleLength);
        }

        [Test]
        public void CanPlaceDoor_NeedsHalfDoorWidthOfWallOnBothSides()
        {
            // 문 폭 1(모듈 4개)이면 양옆에 모듈 2개씩 필요하다. 2유닛 구간(모듈 8개)에는 가운데만 된다.
            Assert.IsTrue(NodeGraphRules.CanPlaceDoor(2f, 1f, 2));
            Assert.IsFalse(NodeGraphRules.CanPlaceDoor(2f, 1f, 1));
            Assert.IsFalse(NodeGraphRules.CanPlaceDoor(2f, 1f, 3));
            Assert.IsFalse(NodeGraphRules.CanPlaceDoor(1.75f, 1f, 2));
            Assert.IsTrue(NodeGraphRules.CanPlaceDoor(4f, 1f, 10));
            Assert.IsFalse(NodeGraphRules.CanPlaceDoor(4f, 1f, 11));
        }
    }
}
