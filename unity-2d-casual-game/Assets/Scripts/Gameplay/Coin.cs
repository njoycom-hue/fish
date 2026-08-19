using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>먹으면 점수를 주고 풀로 돌아가는 수집 아이템.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Coin : MonoBehaviour, IPoolable
    {
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private int value = 1;
        [SerializeField] private float spinDegreesPerSecond = 180f;

        private Scroller scroller;
        private bool collected;

        private void Awake()
        {
            scroller = GetComponent<Scroller>();
        }

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
            {
                transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.Self);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || !other.CompareTag(playerTag))
            {
                return;
            }

            collected = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.Score.AddCoins(value);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCoin();
            }

            if (scroller != null)
            {
                scroller.Despawn();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void OnSpawned()
        {
            collected = false;
            transform.rotation = Quaternion.identity;
        }

        public void OnDespawned()
        {
        }
    }
}
