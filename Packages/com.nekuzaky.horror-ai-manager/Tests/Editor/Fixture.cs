using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Nekuzaky.HorrorAI.Tests
{
    public abstract class Fixture
    {
        #region Private and Protected

        private readonly List<Object> _created = new();

        internal HorrorWorld World { get; private set; }

        protected const float Frame = 1f / 60f;

        [SetUp]
        public void CreateWorld()
        {
            World = HorrorAI.ResetWorld();
        }

        [TearDown]
        public void DestroyCreated()
        {
            for (var i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);
            }
            _created.Clear();
            HorrorAI.ResetWorld();
        }

        protected T Track<T>(T obj) where T : Object
        {
            _created.Add(obj);
            return obj;
        }

        protected T Asset<T>() where T : ScriptableObject => Track(ScriptableObject.CreateInstance<T>());

        protected HorrorArchetype Archetype(out PerceptionProfile perception, out BehaviorProfile behavior)
        {
            perception = Asset<PerceptionProfile>();
            behavior = Asset<BehaviorProfile>();
            var archetype = Asset<HorrorArchetype>();
            archetype.perception = perception;
            archetype.behavior = behavior;
            return archetype;
        }

        protected HorrorEnemy Enemy(Vector3 position, Vector3 forward, HorrorArchetype archetype, bool register = true)
        {
            var go = Track(new GameObject("Enemy"));
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
            var enemy = go.AddComponent<HorrorEnemy>();
            enemy.archetype = archetype;
            if (register)
                World.Add(enemy);
            return enemy;
        }

        protected HorrorTarget Target(Vector3 position, bool collider = true)
        {
            var go = Track(new GameObject("Target"));
            go.transform.position = position;
            if (collider)
            {
                var capsule = go.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0f, 0.9f, 0f);
                capsule.height = 1.8f;
                capsule.radius = 0.3f;
            }
            var target = go.AddComponent<HorrorTarget>();
            World.Add(target);
            return target;
        }

        protected HorrorLightSource Lamp(Vector3 position, float range, bool on = true)
        {
            var go = Track(new GameObject("Lamp"));
            go.transform.position = position;
            var light = go.AddComponent<HorrorLightSource>();
            light.range = range;
            light.SetOn(on);
            World.Add(light);
            return light;
        }

        protected GameObject Wall(Vector3 position, Vector3 size)
        {
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = position;
            wall.transform.localScale = size;
            Physics.SyncTransforms();
            return wall;
        }

        protected void Tick(int frames, float deltaTime = Frame)
        {
            Physics.SyncTransforms();
            for (var i = 0; i < frames; i++)
                World.Tick(deltaTime);
        }

        protected float SecondsUntil(System.Func<bool> condition, float maxSeconds)
        {
            Physics.SyncTransforms();
            var frames = Mathf.CeilToInt(maxSeconds / Frame);
            for (var i = 0; i < frames; i++)
            {
                World.Tick(Frame);
                if (condition())
                    return (i + 1) * Frame;
            }
            return float.PositiveInfinity;
        }

        #endregion
    }
}
