using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>풀에서 꺼내지거나 반환될 때 알림을 받고 싶은 오브젝트가 구현한다.</summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }

    /// <summary>
    /// 프리팹 기반 컴포넌트 풀. 러너처럼 초당 수 개씩 생성/파괴되는 오브젝트의
    /// GC 부하를 없애기 위해 사용한다.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Stack<T> idle = new Stack<T>();
        private readonly HashSet<T> active = new HashSet<T>();

        public ObjectPool(T prefab, Transform parent = null, int prewarm = 0)
        {
            this.prefab = prefab != null
                ? prefab
                : throw new System.ArgumentNullException(nameof(prefab));
            this.parent = parent;

            for (int i = 0; i < prewarm; i++)
            {
                T instance = CreateInstance();
                instance.gameObject.SetActive(false);
                idle.Push(instance);
            }
        }

        /// <summary>대기 중인 인스턴스 수 — 테스트에서 재사용 여부를 확인할 때 쓴다.</summary>
        public int IdleCount => idle.Count;

        /// <summary>현재 사용 중인 인스턴스 수.</summary>
        public int ActiveCount => active.Count;

        public T Get(Vector3 position, Quaternion rotation)
        {
            T instance = idle.Count > 0 ? idle.Pop() : CreateInstance();

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            active.Add(instance);

            if (instance is IPoolable poolable)
            {
                poolable.OnSpawned();
            }

            return instance;
        }

        public void Release(T instance)
        {
            if (instance == null || !active.Remove(instance))
            {
                // 이미 반환됐거나 이 풀의 것이 아니면 무시한다 — 이중 반환은 흔한 실수다.
                return;
            }

            if (instance is IPoolable poolable)
            {
                poolable.OnDespawned();
            }

            instance.gameObject.SetActive(false);
            idle.Push(instance);
        }

        /// <summary>사용 중인 인스턴스를 모두 반환한다 — 재시작 시 호출.</summary>
        public void ReleaseAll()
        {
            if (active.Count == 0)
            {
                return;
            }

            var snapshot = new List<T>(active);
            foreach (T instance in snapshot)
            {
                Release(instance);
            }
        }

        private T CreateInstance()
        {
            return Object.Instantiate(prefab, parent);
        }
    }
}
