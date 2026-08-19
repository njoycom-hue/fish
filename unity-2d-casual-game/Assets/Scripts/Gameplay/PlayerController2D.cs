using System.Collections.Generic;
using Game.Core;
using Game.InputSystem;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// 원터치 점프 캐릭터. 코요테 타임과 점프 버퍼를 넣어
    /// "분명 눌렀는데 안 뛰었다" 는 체감 문제를 없앤다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("점프")]
        [SerializeField] private float jumpVelocity = 11f;
        [SerializeField] private int maxJumps = 2;

        [Tooltip("버튼을 떼면 상승 속도를 이 비율로 깎아 짧은 점프를 만든다")]
        [SerializeField, Range(0.1f, 1f)] private float shortHopMultiplier = 0.45f;

        [Header("중력 보정")]
        [Tooltip("하강 시 중력 배수 — 클수록 착지가 빠르고 조작감이 경쾌하다")]
        [SerializeField] private float fallGravityScale = 3.2f;
        [SerializeField] private float riseGravityScale = 2.1f;

        [Header("접지 판정")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float groundCheckDistance = 0.12f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.12f;

        private readonly List<RaycastHit2D> groundHits = new List<RaycastHit2D>(8);
        private ContactFilter2D groundFilter;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private IInputSource input;
        private Vector3 startPosition;

        private int jumpsUsed;
        private float lastGroundedTime = float.NegativeInfinity;
        private float lastJumpPressedTime = float.NegativeInfinity;

        /// <summary>현재 바닥에 닿아 있는가.</summary>
        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            input = new TouchInputSource();
            startPosition = transform.position;

            groundFilter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = true,
                layerMask = groundLayers
            };
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            if (input.JumpPressed)
            {
                lastJumpPressedTime = Time.time;
            }

            // 상승 중에 버튼을 떼면 즉시 감속 — 탭 길이로 점프 높이를 조절한다.
            if (!input.JumpHeld && body.linearVelocityY > 0f)
            {
                body.linearVelocityY *= shortHopMultiplier;
            }
        }

        private void FixedUpdate()
        {
            bool playing = GameManager.Instance != null
                           && GameManager.Instance.State == GameState.Playing;

            UpdateGrounded();

            if (!playing)
            {
                return;
            }

            body.gravityScale = body.linearVelocityY > 0f ? riseGravityScale : fallGravityScale;

            bool bufferedJump = Time.time - lastJumpPressedTime <= jumpBufferTime;
            bool canCoyoteJump = Time.time - lastGroundedTime <= coyoteTime;

            if (bufferedJump && (canCoyoteJump || jumpsUsed < maxJumps))
            {
                Jump();
            }
        }

        private void UpdateGrounded()
        {
            Bounds bounds = bodyCollider != null
                ? bodyCollider.bounds
                : new Bounds(transform.position, Vector3.one * 0.5f);

            int count = Physics2D.BoxCast(
                bounds.center,
                new Vector2(bounds.size.x * 0.9f, 0.05f),
                0f,
                Vector2.down,
                groundFilter,
                groundHits,
                (bounds.extents.y - 0.02f) + groundCheckDistance);

            bool touchingGround = false;
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = groundHits[i].collider;

                // 캐스트가 자기 콜라이더 안에서 시작하므로 자기 자신은 반드시 걸러야 한다.
                if (hit == null || hit.attachedRigidbody == body || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                touchingGround = true;
                break;
            }

            IsGrounded = touchingGround && body.linearVelocityY <= 0.01f;

            if (IsGrounded)
            {
                lastGroundedTime = Time.time;
                jumpsUsed = 0;
            }
        }

        private void Jump()
        {
            // 코요테 점프도 1회로 세야 공중에서 최대 점프 수를 초과하지 않는다.
            jumpsUsed = IsGrounded ? 1 : jumpsUsed + 1;

            lastJumpPressedTime = float.NegativeInfinity;
            lastGroundedTime = float.NegativeInfinity;

            body.linearVelocityY = jumpVelocity;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayJump();
            }
        }

        /// <summary>새 판을 시작할 때 위치와 속도를 초기화한다.</summary>
        public void ResetPlayer()
        {
            transform.position = startPosition;
            transform.rotation = Quaternion.identity;

            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = fallGravityScale;

            jumpsUsed = 0;
            lastGroundedTime = float.NegativeInfinity;
            lastJumpPressedTime = float.NegativeInfinity;
        }
    }
}
