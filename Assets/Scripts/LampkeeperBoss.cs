using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class LampkeeperBoss : MonoBehaviour
{
    public enum BossState
    {
        Idle,
        Patrol,
        Chase,
        SnuffOut,
        Dead,
    }

    [Header("Stats")]
    [SerializeField]
    private float maxHealth = 300f;

    [SerializeField]
    private float currentHealth;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;

    [SerializeField]
    private float patrolSpeed = 1.5f;

    [SerializeField]
    private float chaseSpeed = 3f;

    [Header("AI Behavior")]
    [SerializeField]
    private float detectionRange = 10f;

    [Tooltip("Tầm xa của luồng gió Snuff Out")]
    [SerializeField]
    private float snuffRange = 6f;

    [SerializeField]
    private float snuffCooldown = 3.5f;

    [Header("Patrol Settings")]
    [SerializeField]
    private Transform[] patrolWaypoints;

    [SerializeField]
    private float waypointTolerance = 0.5f;

    [SerializeField]
    private float patrolRange = 10f; // Walk distance if waypoints are empty

    [SerializeField]
    private bool avoidFalling = true;

    [SerializeField]
    private float wallCheckDistance = 0.5f;

    [SerializeField]
    private float ledgeCheckDistance = 1.5f;

    [SerializeField]
    private float patrolIdleDuration = 2f; // Pause duration when reaching limit/waypoint

    [Header("Attack Settings")]
    [Tooltip("Thời điểm luồng gió chạm player, tính từ lúc bắt đầu animation (frame 2-3 = ~0.3s)")]
    [SerializeField]
    private float snuffImpactDelay = 0.3f;

    [Tooltip("Tổng thời lượng animation Snuff Out (6 frame x 0.12s)")]
    [SerializeField]
    private float snuffDuration = 0.72f;

    [SerializeField]
    private float snuffDamage = 25f;

    [Tooltip("Prefab hiệu ứng/đạn gió (tùy chọn). Nếu để trống thì chỉ tính damage theo khoảng cách.")]
    [SerializeField]
    private GameObject windWavePrefab;

    [SerializeField]
    private Transform windSpawnPoint;

    [Header("Layer Masks")]
    [SerializeField]
    private LayerMask obstacleLayer;

    [Header("Death")]
    [Tooltip("Gọi sau khi animation chết chạy xong — nối cutscene lantern-shatter vào đây")]
    public UnityEvent onDeathAnimationComplete;

    [SerializeField]
    private float deathAnimationDuration = 1.1f;

    [SerializeField]
    private bool destroyAfterDeath = false;

    // Component References
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D bossCollider;
    private Transform player;

    // State Variables
    private BossState currentState = BossState.Idle;
    private int currentWaypointIndex = 0;
    private bool isFacingRight = true;
    private bool isAttacking = false;
    private float nextSnuffTime = 0f;

    private Vector2 startPosition;
    private float patrolDirection = 1f;
    private bool isPatrolIdling = false;
    private float patrolIdleTimer = 0f;

    // Animator Parameter Hashes (for performance)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int SnuffOutHash = Animator.StringToHash("SnuffOut");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bossCollider = GetComponent<Collider2D>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        currentHealth = maxHealth;
        startPosition = transform.position;

        FindPlayer();
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            return;
        }

        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
        {
            player = pm.transform;
        }
    }

    private void Update()
    {
        if (currentState == BossState.Dead || isAttacking)
            return;

        // Manage patrol idling timer
        if (isPatrolIdling && Time.time >= patrolIdleTimer)
        {
            isPatrolIdling = false;
        }

        if (player == null)
        {
            FindPlayer();
        }

        EvaluateState();
    }

    private void FixedUpdate()
    {
        if (currentState == BossState.Dead || isAttacking)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetFloat(SpeedHash, 0f);
            return;
        }

        HandleMovement();
    }

    private void EvaluateState()
    {
        float distanceToPlayer =
            player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;

        // Bà lão mù — không nhìn thấy, chỉ "nghe" được khi player lại gần
        if (distanceToPlayer > detectionRange)
        {
            if (isPatrolIdling)
            {
                currentState = BossState.Idle;
            }
            else
            {
                currentState =
                    (patrolWaypoints != null && patrolWaypoints.Length > 0) || patrolRange > 0f
                        ? BossState.Patrol
                        : BossState.Idle;
            }
            return;
        }

        FaceTarget(player.position);

        if (distanceToPlayer <= snuffRange)
        {
            if (Time.time >= nextSnuffTime)
            {
                StartCoroutine(PerformSnuffOut());
            }
            else
            {
                // Đang hồi chiêu: đứng yên hướng về player
                currentState = BossState.Idle;
            }
        }
        else
        {
            // Trong tầm nghe nhưng ngoài tầm thổi -> lê bước lại gần
            currentState = BossState.Chase;
        }
    }

    private void HandleMovement()
    {
        if (currentState == BossState.Idle)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetFloat(SpeedHash, 0f);
            return;
        }

        if (currentState == BossState.Patrol)
        {
            if (isPatrolIdling)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                animator.SetFloat(SpeedHash, 0f);
                return;
            }

            if (patrolWaypoints != null && patrolWaypoints.Length > 0)
            {
                Vector2 targetPosition = patrolWaypoints[currentWaypointIndex].position;

                if (Vector2.Distance(transform.position, targetPosition) < waypointTolerance)
                {
                    // Reached waypoint, pause/idle
                    isPatrolIdling = true;
                    patrolIdleTimer = Time.time + patrolIdleDuration;
                    currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;

                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                    animator.SetFloat(SpeedHash, 0f);
                    return;
                }

                MoveHorizontally(
                    targetPosition.x > transform.position.x ? 1f : -1f,
                    patrolSpeed
                );
                return;
            }

            if (patrolRange > 0f)
            {
                bool hitWall = CheckWall();
                bool hitLedge = avoidFalling && CheckLedge();

                bool outOfMinBounds = transform.position.x < startPosition.x - patrolRange;
                bool outOfMaxBounds = transform.position.x > startPosition.x + patrolRange;

                if (
                    hitWall
                    || hitLedge
                    || (outOfMinBounds && patrolDirection < 0f)
                    || (outOfMaxBounds && patrolDirection > 0f)
                )
                {
                    // Reached patrol boundary/obstacle, pause/idle and turn around
                    isPatrolIdling = true;
                    patrolIdleTimer = Time.time + patrolIdleDuration;
                    patrolDirection = -patrolDirection;

                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                    animator.SetFloat(SpeedHash, 0f);
                    return;
                }

                MoveHorizontally(patrolDirection, patrolSpeed);
            }
            return;
        }

        if (currentState == BossState.Chase && player != null)
        {
            MoveHorizontally(
                player.position.x > transform.position.x ? 1f : -1f,
                chaseSpeed
            );
        }
    }

    private void MoveHorizontally(float direction, float speed)
    {
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
        animator.SetFloat(SpeedHash, speed);

        if (direction > 0 && !isFacingRight)
            Flip();
        else if (direction < 0 && isFacingRight)
            Flip();
    }

    private bool CheckWall()
    {
        // Mask chưa gán thì bỏ qua kiểm tra (tránh raycast vô nghĩa)
        if (obstacleLayer.value == 0)
            return false;

        Vector2 checkOrigin =
            (Vector2)transform.position + new Vector2(isFacingRight ? 0.5f : -0.5f, 0);
        RaycastHit2D hit = Physics2D.Raycast(
            checkOrigin,
            isFacingRight ? Vector2.right : Vector2.left,
            wallCheckDistance,
            obstacleLayer
        );
        Debug.DrawRay(
            checkOrigin,
            (isFacingRight ? Vector2.right : Vector2.left) * wallCheckDistance,
            Color.red
        );
        return hit.collider != null;
    }

    private bool CheckLedge()
    {
        if (obstacleLayer.value == 0)
            return false;

        Vector2 checkOrigin =
            (Vector2)transform.position + new Vector2(isFacingRight ? 0.5f : -0.5f, 0);
        RaycastHit2D hit = Physics2D.Raycast(
            checkOrigin,
            Vector2.down,
            ledgeCheckDistance,
            obstacleLayer
        );
        Debug.DrawRay(checkOrigin, Vector2.down * ledgeCheckDistance, Color.yellow);
        return hit.collider == null;
    }

    // --- ATTACK MECHANICS ---

    private IEnumerator PerformSnuffOut()
    {
        isAttacking = true;
        currentState = BossState.SnuffOut;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        animator.SetFloat(SpeedHash, 0f);

        FaceTarget(player.position);
        animator.SetTrigger(SnuffOutHash);

        // Chờ tới frame thổi gió (frame 2-3)
        yield return new WaitForSeconds(snuffImpactDelay);

        if (currentState != BossState.Dead)
        {
            // Sinh hiệu ứng luồng gió nếu có prefab
            if (windWavePrefab != null)
            {
                Vector3 spawnPos = windSpawnPoint != null ? windSpawnPoint.position : transform.position;
                GameObject wave = Instantiate(windWavePrefab, spawnPos, Quaternion.identity);
                Vector3 waveScale = wave.transform.localScale;
                waveScale.x = Mathf.Abs(waveScale.x) * (isFacingRight ? 1f : -1f);
                wave.transform.localScale = waveScale;
            }

            // Tính damage: player còn trong tầm gió và đứng phía trước mặt boss
            if (player != null)
            {
                float distance = Vector2.Distance(transform.position, player.position);
                bool inFront =
                    (player.position.x >= transform.position.x) == isFacingRight
                    || Mathf.Abs(player.position.x - transform.position.x) < 1f;

                if (distance <= snuffRange && inFront)
                {
                    HealthManager playerHealth = player.GetComponent<HealthManager>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(snuffDamage);
                    }
                }
            }
        }

        // Chờ animation kết thúc
        yield return new WaitForSeconds(Mathf.Max(0f, snuffDuration - snuffImpactDelay));

        nextSnuffTime = Time.time + snuffCooldown;
        isAttacking = false;
        if (currentState != BossState.Dead)
        {
            currentState = BossState.Idle;
        }
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        if (targetPosition.x > transform.position.x && !isFacingRight)
            Flip();
        else if (targetPosition.x < transform.position.x && isFacingRight)
            Flip();
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    // --- HEALTH & DAMAGE MECHANICS ---

    /// <summary>
    /// Đưa boss về trạng thái ban đầu (dùng khi player hồi sinh đánh lại):
    /// đầy máu, về vị trí xuất phát, animation/vật lý/trigger như mới.
    /// </summary>
    public void ResetBoss()
    {
        StopAllCoroutines();

        currentHealth = maxHealth;
        isAttacking = false;
        nextSnuffTime = 0f;
        isPatrolIdling = false;
        patrolDirection = 1f;
        currentWaypointIndex = 0;
        currentState = BossState.Idle;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        if (bossCollider != null)
            bossCollider.enabled = true;

        if (!isFacingRight)
            Flip();
        transform.position = startPosition;

        // Về state mặc định của animator (thoát Death, xóa IsDead)
        animator.Rebind();
        animator.Update(0f);

        this.enabled = true;
    }

    public void TakeDamage(float amount)
    {
        if (currentState == BossState.Dead)
            return;

        currentHealth -= amount;
        StartCoroutine(FlashHitEffect());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashHitEffect()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white;
        }
    }

    private void Die()
    {
        currentState = BossState.Dead;
        StopAllCoroutines();
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (bossCollider != null)
            bossCollider.enabled = false;

        animator.SetBool(IsDeadHash, true);

        StartCoroutine(HandleDeathSequence());
    }

    private IEnumerator HandleDeathSequence()
    {
        // Chờ animation đèn vỡ chạy hết (6 frame x 0.18s)
        yield return new WaitForSeconds(deathAnimationDuration);

        // Nối cutscene lantern-shatter reveal tại đây (gán trong Inspector)
        onDeathAnimationComplete?.Invoke();

        if (destroyAfterDeath)
        {
            Destroy(gameObject, 2f);
        }

        this.enabled = false;
    }

    // --- EDITOR VISUALIZATION ---

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, snuffRange);
    }
}
