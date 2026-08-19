using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class ObjectPoolTests
    {
        /// <summary>풀 콜백 호출 횟수를 세는 테스트 전용 컴포넌트.</summary>
        private class PooledProbe : MonoBehaviour, IPoolable
        {
            public int SpawnedCount { get; private set; }
            public int DespawnedCount { get; private set; }

            public void OnSpawned() => SpawnedCount++;

            public void OnDespawned() => DespawnedCount++;
        }

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private PooledProbe prefab;
        private Transform poolRoot;

        [SetUp]
        public void SetUp()
        {
            prefab = NewProbe("Prefab");
            prefab.gameObject.SetActive(false);

            // 풀이 만든 인스턴스를 한 부모 아래 모아 두면 테스트 종료 시 한 번에 정리된다.
            var root = new GameObject("PoolRoot");
            spawnedObjects.Add(root);
            poolRoot = root.transform;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawnedObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            spawnedObjects.Clear();
        }

        private PooledProbe NewProbe(string name)
        {
            var go = new GameObject(name);
            spawnedObjects.Add(go);
            return go.AddComponent<PooledProbe>();
        }

        [Test]
        public void Prewarm_CreatesIdleInstances()
        {
            var pool = new ObjectPool<PooledProbe>(prefab, poolRoot, prewarm: 3);

            Assert.AreEqual(3, pool.IdleCount);
            Assert.AreEqual(0, pool.ActiveCount);
        }

        [Test]
        public void Get_ReusesReleasedInstanceInsteadOfInstantiating()
        {
            var pool = new ObjectPool<PooledProbe>(prefab, poolRoot, prewarm: 1);

            PooledProbe first = pool.Get(Vector3.zero, Quaternion.identity);
            Track(first);
            pool.Release(first);

            PooledProbe second = pool.Get(Vector3.one, Quaternion.identity);
            Track(second);

            Assert.AreSame(first, second);
            Assert.AreEqual(0, pool.IdleCount);
            Assert.AreEqual(1, pool.ActiveCount);
        }

        [Test]
        public void Get_ActivatesAndPositionsInstance()
        {
            var pool = new ObjectPool<PooledProbe>(prefab, poolRoot);

            PooledProbe instance = pool.Get(new Vector3(4f, 2f, 0f), Quaternion.identity);
            Track(instance);

            Assert.IsTrue(instance.gameObject.activeSelf);
            Assert.AreEqual(new Vector3(4f, 2f, 0f), instance.transform.position);
        }

        [Test]
        public void Release_DeactivatesAndNotifiesPoolable()
        {
            var pool = new ObjectPool<PooledProbe>(prefab, poolRoot, prewarm: 1);

            PooledProbe instance = pool.Get(Vector3.zero, Quaternion.identity);
            Track(instance);
            pool.Release(instance);

            Assert.IsFalse(instance.gameObject.activeSelf);
            Assert.AreEqual(1, instance.SpawnedCount);
            Assert.AreEqual(1, instance.DespawnedCount);
        }

        [Test]
        public void Release_IgnoresDoubleRelease()
        {
            var pool = new ObjectPool<PooledProbe>(prefab, poolRoot, prewarm: 1);

            PooledProbe instance = pool.Get(Vector3.zero, Quaternion.identity);
            Track(instance);

            pool.Release(instance);
            pool.Release(instance);

            Assert.AreEqual(1, pool.IdleCount, "이중 반환으로 같은 인스턴스가 두 번 쌓이면 안 된다.");
            Assert.AreEqual(1, instance.DespawnedCount);
        }

        [Test]
        public void ReleaseAll_ReturnsEveryActiveInstance()
        {
            var pool = new ObjectPool<PooledProbe>(prefab, poolRoot, prewarm: 2);

            Track(pool.Get(Vector3.zero, Quaternion.identity));
            Track(pool.Get(Vector3.zero, Quaternion.identity));
            Track(pool.Get(Vector3.zero, Quaternion.identity));

            pool.ReleaseAll();

            Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual(3, pool.IdleCount);
        }

        [Test]
        public void Constructor_RejectsNullPrefab()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => new ObjectPool<PooledProbe>(null));
        }

        private void Track(PooledProbe instance)
        {
            if (instance != null)
            {
                spawnedObjects.Add(instance.gameObject);
            }
        }
    }
}
