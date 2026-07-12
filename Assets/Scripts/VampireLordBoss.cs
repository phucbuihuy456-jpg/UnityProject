using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class VampireLordBoss : MonoBehaviour
{
    public enum BossState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        SpecialAttack,
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
    private float chaseSpeed = 3.5f;

    [Header("AI Behavior")]
    [SerializeField]
    private float detectionRange = 12f;

    [Header("Normal Attack (chém rapier cận chiến)")]
    [SerializeField]
    private float attackRange = 2f;

    [SerializeField]
    private float attackCooldown = 2f;

    [Tooltip("Thời điểm lưỡi kiếm chạm player, tính từ lúc bắt đầu animation (frame 5/9 x 0.08s)")]
    [SerializeField]
    private float attackImpactDelay = 0.4f;

    [Tooltip("Tổng thời lượng animation chém thường (9 frame x 0.08s)")]
    [SerializeField]
    private float attackDuration = 0.72f;

    [SerializeField]
    private float attackDamage = 15f;

    [Header("Special Attack (đòn kiếm khí tầm xa)")]
    [SerializeField]
    private float specialRange = 4f;

    [SerializeField]
    private float specialCooldown = 8f;

    [Tooltip("Thời điểm đòn đánh chạm player, tính từ lúc bắt đầu animation (frame 5/9 x 0.12s)")]
    [SerializeField]
    private float specialImpactDelay = 0.6f;

    [Tooltip("Tổng thời lượng animation đòn đặc biệt (9 frame x 0.12s)")]
    [SerializeField]
    private float specialDuration = 1.08f;

    [SerializeField]
    private float specialDamage = 30f;

    [Header("Patrol Settings")]
    [SerializeField]
    private Transform[] patrolWaypoints;

    [SerializeField]
    private float waypointTolerance = 0.5f;

    [SerializeField]
    private float patrolRange = 12f; // Walk distance if waypoints are empty

    [SerializeField]
    private bool avoidFalling = true;

    [SerializeField]
    private float wallCheckDistance = 0.5f;

    [SerializeField]
    private float ledgeCheckDistance = 1.5f;

    [SerializeField]
    private float patrolIdleDuration = 2f; // Pause duration when reaching limit/waypoint

    [Header("Layer Masks")]
    [SerializeField]
    private LayerMask obstacleLayer;

    [Header("Enrage (dưới nửa máu)")]
    [Tooltip("Ngưỡng máu (tỷ lệ) kích hoạt cuồng nộ")]
    [SerializeField]
    private float enrageHealthFraction = 0.5f;

    [SerializeField]
    private float enrageSpeedMultiplier = 1.3f;

    [Tooltip("Hồi chiêu nhân với hệ số này khi cuồng nộ (nhỏ hơn 1 = đánh dày hơn)")]
    [SerializeField]
    private float enrageCooldownMultiplier = 0.6f;

    [Header("Death")]
    [Tooltip("Gọi sau khi animation chết chạy xong — nối cutscene/kết màn vào đây")]
    public UnityEvent onDeathAnimationComplete;

    [SerializeField]
    private float deathAnimationDuration = 1.44f;

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

    // Sprite gốc quay mặt sang TRÁI (hướng south-west) — scale.x dương = nhìn sang trái
    private bool isFacingRight = false;
    private bool isAttacking = false;
    private bool isEnraged = false;
    private float nextAttackTime = 0f;
    private float nextSpecialTime = 0f;

    private Vector2 startPosition;
    private float patrolDirection = 1f;
    private bool isPatrolIdling = false;
    private float patrolIdleTimer = 0f;

    // Animator Parameter Hashes (for performance)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int SpecialAttackHash = Animator.StringToHash("SpecialAttack");
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

        // Ưu tiên đòn đặc biệt khi player trong tầm kiếm khí và đã hồi chiêu
        if (distanceToPlayer <= specialRange && Time.time >= nextSpecialTime)
        {
            StartCoroutine(PerformSpecialAttack());
            return;
        }

        if (distanceToPlayer <= attackRange)
        {
            if (Time.time >= nextAttackTime)
            {
                StartCoroutine(PerformAttack());
            }
            else
            {
                // Đang hồi chiêu: đứng thủ thế hướng về player
                currentState = BossState.Idle;
            }
        }
        else
        {
            // Trong tầm phát hiện nhưng ngoài tầm chém -> áp sát
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
            float speed = isEnraged ? chaseSpeed * enrageSpeedMultiplier : chaseSpeed;
            MoveHorizontally(
                player.position.x > transform.position.x ? 1f : -1f,
                speed
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
        return hit.collider == null;
    }

    // --- ATTACK MECHANICS ---

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        currentState = BossState.Attack;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        animator.SetFloat(SpeedHash, 0f);

        FaceTarget(player.position);
        animator.SetTrigger(AttackHash);

        yield return new WaitForSeconds(attackImpactDelay);

        if (currentState != BossState.Dead)
        {
            // Lưỡi kiếm quét: player còn trong tầm chém (nới nhẹ) và đứng phía trước mặt
            DealDamageIfHit(attackRange + 0.5f, attackDamage);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, attackDuration - attackImpactDelay));

        float cooldown = isEnraged ? attackCooldown * enrageCooldownMultiplier : attackCooldown;
        nextAttackTime = Time.time + cooldown;
        isAttacking = false;
        if (currentState != BossState.Dead)
        {
            currentState = BossState.Idle;
        }
    }

    private IEnumerator PerformSpecialAttack()
    {
        isAttacking = true;
        currentState = BossState.SpecialAttack;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        animator.SetFloat(SpeedHash, 0f);

        FaceTarget(player.position);
        animator.SetTrigger(SpecialAttackHash);

        yield return new WaitForSeconds(specialImpactDelay);

        if (currentState != BossState.Dead)
        {
            DealDamageIfHit(specialRange + 0.5f, specialDamage);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, specialDuration - specialImpactDelay));

        float cooldown = isEnraged ? specialCooldown * enrageCooldownMultiplier : specialCooldown;
        nextSpecialTime = Time.time + cooldown;
        // Sau đòn đặc biệt, đòn thường cũng phải chờ một nhịp ngắn
        nextAttackTime = Mathf.Max(nextAttackTime, Time.time + 0.5f);
        isAttacking = false;
        if (currentState != BossState.Dead)
        {
            currentState = BossState.Idle;
        }
    }

    private void DealDamageIfHit(float range, float damage)
    {
        if (player == null)
            return;

        float distance = Vector2.Distance(transform.position, player.position);
        bool inFront =
            (player.position.x >= transform.position.x) == isFacingRight
            || Mathf.Abs(player.position.x - transform.position.x) < 1f;

        if (distance <= range && inFront)
        {
            HealthManager playerHealth = player.GetComponent<HealthManager>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
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
        isEnraged = false;
        nextAttackTime = 0f;
        nextSpecialTime = 0f;
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

        if (isFacingRight)
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
        else if (!isEnraged && currentHealth <= maxHealth * enrageHealthFraction)
        {
            isEnraged = true;
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
        // Tắt collider để RoomExitDoor tính boss là đã chết -> mở cửa lên Đỉnh tháp
        if (bossCollider != null)
            bossCollider.enabled = false;

        animator.SetBool(IsDeadHash, true);

        StartCoroutine(HandleDeathSequence());
    }

    private IEnumerator HandleDeathSequence()
    {
        // Chờ animation chết chạy hết (9 frame x 0.16s)
        yield return new WaitForSeconds(deathAnimationDuration);

        onDeathAnimationComplete?.Invoke();

        // Hội thoại kết màn + chọn ending (nếu có gắn component)
        VampireLordEndingDialogue endingDialogue = GetComponent<VampireLordEndingDialogue>();
        if (endingDialogue != null)
        {
            endingDialogue.Play();
        }
        else if (destroyAfterDeath)
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

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, specialRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
