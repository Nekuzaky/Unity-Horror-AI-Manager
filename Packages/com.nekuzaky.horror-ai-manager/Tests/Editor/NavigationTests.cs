using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Nekuzaky.HorrorAI.Tests
{
    public sealed class NavigationTests : Fixture
    {
        [Test]
        public void Project_SnapsAPointNearTheFloorOntoTheNavMesh()
        {
            BuildFloor();

            var found = NavMeshMover.Project(new Vector3(1f, 1.2f, 1f), NavMesh.AllAreas, out var projected, out var exact);

            Assert.IsTrue(found);
            Assert.IsTrue(exact);
            Assert.AreEqual(1f, projected.x, 0.05f);
            Assert.AreEqual(0f, projected.y, 0.3f);
        }

        [Test]
        public void Project_AnOffMeshSpotGivesTheNearestReachablePoint()
        {
            BuildFloor();

            var found = NavMeshMover.Project(new Vector3(9f, 0f, 0f), NavMesh.AllAreas, out var projected, out var exact);

            Assert.IsTrue(found);
            Assert.IsFalse(exact);
            Assert.That(projected.x, Is.InRange(4f, 5.01f));
        }

        [Test]
        public void Project_NothingNearbyIsNotFound()
        {
            BuildFloor();

            Assert.IsFalse(NavMeshMover.Project(new Vector3(60f, 0f, 60f), NavMesh.AllAreas, out _, out _));
        }

        [Test]
        public void NavMeshAgents_AreNotPlacedOnTheNavMeshInEditMode()
        {
            BuildFloor();
            var agent = Track(new GameObject("Agent")).AddComponent<NavMeshAgent>();
            agent.transform.position = new Vector3(0f, 0.1f, 0f);

            Assert.IsFalse(agent.isOnNavMesh, "Agents now work in Edit Mode: add an Edit Mode allocation test with real NavMesh agents.");
        }

        private void BuildFloor()
        {
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(10f, 0.2f, 10f);
            var surface = floor.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();
        }
    }
}
