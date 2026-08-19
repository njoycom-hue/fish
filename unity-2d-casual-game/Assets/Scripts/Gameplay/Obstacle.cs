using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>플레이어와 닿으면 판을 끝내는 장애물.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Obstacle : MonoBehaviour, IPoolable
    {
        [SerializeField] private string playerTag = "Player";

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(playerTag))
            {
                return;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }

        public void OnSpawned()
        {
        }

        public void OnDespawned()
        {
        }
    }
}
