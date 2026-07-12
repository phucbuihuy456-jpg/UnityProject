using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class SkeletonBoss : MonoBehaviour
{
    public enum BossState
    {
        Idle,
        Patrol,
        Chase,
        SwingAttack,
        Hurt,
        Dead,
    }

    [Header("Stats")]
    [SerializeField]
    private float maxHealth = 200f;

    [SerializeField]
    private float currentHealth;

    // THÊM 2 DÒNG NÀY VÀO ĐÂY ĐỂ UI CÓ THỂ ĐỌC ĐƯỢC DỮ LIỆU MÁU
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;

    [SerializeField]
    private float patrolSpeed = 2f;

    [SerializeField]
    private float chaseSpeed = 4.5f;

    [Header("AI Behavior")]
    [SerializeField]
    private float detectionRange = 10f;

    [SerializeField]
    private float swingRange = 2f;

    [SerializeField]
    private float swingCooldown = 2f;

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
    [SerializeField]
    private Transform attackPoint;

    [SerializeField]
    private float swingRadius = 1.5f;

    [SerializeField]
    private float swingDamage = 20f;

    [Header("Layer Masks")]
    [SerializeField]
    private LayerMask playerLayer;

    [SerializeField]
    private LayerMask obstacleLayer;

    // Component References
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private Collider2D bossCollider;

    // State Variables
    private BossState currentState = BossState.Idle;
    private int currentWaypointIndex = 0;
    private bool isFacingRight = true;
    private float nextSwingTime = 0f;
    private bool isAttacking = false;
    private bool isHurt = false;

    private Vector2 startPosition;
    private float patrolDirection = 1f;
    private bool isPatrolIdling = false;
    private float patrolIdleTimer = 0f;

    // Animator Parameter Hashes (for performance)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int SwingHash = Animator.StringToHash("Swing");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
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
        // Try tag first
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            return;
        }

        // Fallback: search by PlayerMovement component
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
        {
            player = pm.transform;
        }
    }

    private void Update()
    {
        if (currentState == BossState.Dead || isAttacking || isHurt)
            return;

        // Manage patrol idling timer
        if (isPatrolIdling)
        {
            if (Time.time >= patrolIdleTimer)
            {
                isPatrolIdling = false;
            }
        }

        FindPlayerIfNeeded();
        EvaluateState();
    }

    private void FixedUpdate()
    {
        if (currentState == BossState.Dead || isAttacking || isHurt)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetFloat(SpeedHash, 0f);
            return;
        }

        HandleMovement();
    }

    private void FindPlayerIfNeeded()
    {
        if (player == null)
        {
            FindPlayer();
        }
    }

    private void EvaluateState()
    {
        if (player == null)
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

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

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

        // Player is within detection range
        float attackDistance = distanceToPlayer;
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (bossCollider != null && playerCollider != null)
        {
            ColliderDistance2D dist = bossCollider.Distance(playerCollider);
            if (dist.isValid)
            {
                attackDistance = dist.distance;
            }
        }

        if (attackDistance <= swingRange)
        {
            if (Time.time >= nextSwingTime)
            {
                StartCoroutine(PerformSwingAttack());
            }
            else
            {
                // Attack is on cooldown, stand still and face the player
                currentState = BossState.Idle;
                FaceTarget(player.position);
            }
        }
        else
        {
            // Player is outside attack range but within detection range -> Chase
            currentState = BossState.Chase;
        }
    }

    private void HandleMovement()
    {
        float targetSpeed = 0f;
        Vector2 targetPosition = transform.position;

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
                targetPosition = patrolWaypoints[currentWaypointIndex].position;
                targetSpeed = patrolSpeed;

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
            }
            else if (patrolRange > 0f)
            {
                targetSpeed = patrolSpeed;

                bool hitWall = CheckWall();
                bool hitLedge = CheckLedge();

                bool outOfMinBounds = transform.position.x < startPosition.x - patrolRange;
                bool outOfMaxBounds = transform.position.x > startPosition.x + patrolRange;

                if (
                    hitWall
                    || (avoidFalling && hitLedge)
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

                rb.linearVelocity = new Vector2(patrolDirection * targetSpeed, rb.linearVelocity.y);
                animator.SetFloat(SpeedHash, targetSpeed);

                // Flip sprite depending on patrol direction
                if (patrolDirection > 0 && !isFacingRight)
                    Flip();
                else if (patrolDirection < 0 && isFacingRight)
                    Flip();
                return;
            }
        }
        else if (currentState == BossState.Chase && player != null)
        {
            targetPosition = player.position;
            targetSpeed = chaseSpeed;
        }

        if (targetSpeed > 0f)
        {
            float direction = targetPosition.x > transform.position.x ? 1f : -1f;
            rb.linearVelocity = new Vector2(direction * targetSpeed, rb.linearVelocity.y);
            animator.SetFloat(SpeedHash, targetSpeed);

            // Flip sprite depending on moving direction
            if (direction > 0 && !isFacingRight)
                Flip();
            else if (direction < 0 && isFacingRight)
                Flip();
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetFloat(SpeedHash, 0f);
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    private bool CheckWall()
    {
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

    private IEnumerator PerformSwingAttack()
    {
        isAttacking = true;
        currentState = BossState.SwingAttack;
        rb.linearVelocity = Vector2.zero;
        animator.SetFloat(SpeedHash, 0f);

        // Face player before swinging
        FaceTarget(player.position);

        animator.SetTrigger(SwingHash);

        // Wait for swing impact frame
        yield return new WaitForSeconds(0.4f);

        // Execute Melee Damage Check using edge-to-edge distance for high scalability
        bool hitPlayer = false;
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (bossCollider != null && playerCollider != null)
        {
            ColliderDistance2D dist = bossCollider.Distance(playerCollider);
            if (dist.isValid)
            {
                if (dist.distance <= swingRadius)
                {
                    hitPlayer = true;
                }
            }
        }
        else
        {
            Vector2 hitCenter = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
            if (playerLayer.value == 0)
            {
                Collider2D playerCol = player.GetComponent<Collider2D>();
                if (playerCol != null && Vector2.Distance(hitCenter, playerCol.bounds.ClosestPoint(hitCenter)) <= swingRadius)
                {
                    hitPlayer = true;
                }
            }
            else
            {
                Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(hitCenter, swingRadius, playerLayer);
                foreach (Collider2D p in hitPlayers)
                {
                    if (p.transform == player) hitPlayer = true;
                }
            }
        }

        if (hitPlayer)
        {
            HealthManager playerHealth = player.GetComponent<HealthManager>();
            if (playerHealth != null)
            {
                Debug.Log("Skeleton Boss swung and hit player!");
                playerHealth.TakeDamage(swingDamage);
            }
        }

        // Wait for animation to finish
        yield return new WaitForSeconds(0.6f);

        nextSwingTime = Time.time + swingCooldown;
        isAttacking = false;
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        if (targetPosition.x > transform.position.x && !isFacingRight)
            Flip();
        else if (targetPosition.x < transform.position.x && isFacingRight)
            Flip();
    }

    // --- HEALTH & DAMAGE MECHANICS ---

    public void TakeDamage(float amount)
    {
        if (currentState == BossState.Dead)
            return;

        currentHealth -= amount;
        Debug.Log("Skeleton Boss took damage! Current Health: " + currentHealth);

        // Kích hoạt hiệu ứng nháy đỏ trắng khi trúng đòn
        StartCoroutine(FlashHitEffect());

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Only play hurt/stagger animation if not currently mid-attack (Hyper-armor)
            if (!isAttacking)
            {
                StartCoroutine(PlayHurtAnimation());
            }
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

    private IEnumerator PlayHurtAnimation()
    {
        isHurt = true;
        rb.linearVelocity = Vector2.zero;

        animator.SetTrigger(HurtHash);

        yield return new WaitForSeconds(0.4f);

        isHurt = false;
    }

    private void Die()
    {
        currentState = BossState.Dead;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic; // Disable physics interaction
        GetComponent<Collider2D>().enabled = false; // Disable collisions

        animator.SetBool(IsDeadHash, true);
        Debug.Log("Skeleton Boss has been defeated!");

        // Nếu có hội thoại cốt truyện sau khi boss chết thì để nó lo trình tự (thoại -> mờ dần -> hủy)
        BossDefeatDialogue defeatDialogue = GetComponent<BossDefeatDialogue>();
        if (defeatDialogue != null)
        {
            defeatDialogue.Play();
        }
        else
        {
            // Destroy the boss game object after 2 seconds (to let death animation play)
            Destroy(gameObject, 2f);
        }

        // Disable script to stop update loops
        this.enabled = false;
    }

    // --- EDITOR VISUALIZATION ---

    private void OnDrawGizmosSelected()
    {
        // Draw detection range (Yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw swing attack range (Red circle)
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, swingRadius);
        }
    }

}
