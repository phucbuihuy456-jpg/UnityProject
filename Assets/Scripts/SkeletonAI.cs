using System.Collections;
using UnityEngine;

public class SkeletonAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 1.0f;       // Tốc độ đi tuần tra (Skeleton thường đi chậm)
    public float chaseSpeed = 2.0f;      // Tốc độ di chuyển áp sát Player
    public bool invertFlip = false;      // Đảo ngược hướng quay mặt
    private float currentSpeed;

    [Header("Health Settings")]
    public float maxHealth = 3f;         // Máu tối đa của Skeleton
    public float staggerDuration = 0.5f; // Thời gian bị khựng khi trúng đòn
    private float currentHealth;
    private bool isDead = false;
    private float staggerTimer = 0f;

    [Header("Ranged Attack Settings")]
    public GameObject bonePrefab;        // Prefab của chiếc xương ném đi
    public Transform throwPoint;         // Vị trí xuất phát của chiếc xương khi ném
    public float throwDelay = 0.25f;     // Thời gian chờ trễ của đòn ném (khớp với hoạt ảnh vung tay)
    public float stopRange = 3.5f;       // Khoảng cách an toàn để đứng ném (không cần chạy lại quá sát Player)
    public float attackCooldown = 2.5f;  // Thời gian chờ giữa các lần ném xương
    private float nextAttackTime = 0f;

    [Header("Detection Settings")]
    public float detectionRange = 7f;    // Khoảng cách phát hiện Player (xa hơn zombie để ném xương)
    public LayerMask obstacleLayer;      // Lớp mặt đất/tường
    
    [Header("Patrol Settings")]
    public float patrolRange = 4f;       // Phạm vi tuần tra tối đa quanh vị trí xuất phát
    public bool avoidFalling = true;     // Tránh rơi vực
    public float wallCheckDistance = 0.5f;
    public float ledgeCheckDistance = 1f;

    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Transform playerTransform;

    private Vector2 startPosition;
    private bool isFacingRight = true;
    private bool isChasing = false;
    private bool isReturning = false;

    void Start()
    {
        currentSpeed = walkSpeed;
        currentHealth = maxHealth;
        startPosition = transform.position;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // Tự động tìm Player
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }

    void Update()
    {
        if (isDead) return;

        // Nếu đang bị choáng/khựng -> Đứng im tại chỗ
        if (Time.time < staggerTimer)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            UpdateMovementAnimation();
            return;
        }

        // Nếu không có Player -> Quay về tổ hoặc tuần tra
        if (playerTransform == null)
        {
            if (isReturning) ReturnToStartPosition();
            else Patrol();
            UpdateMovementAnimation();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRange)
        {
            // Nhìn thấy Player -> Đứng lại đối mặt hoặc đi áp sát
            isReturning = false;
            isChasing = true;
            HandleRangedCombat(distanceToPlayer);
        }
        else
        {
            // Mất dấu Player -> Quay về tổ
            if (isChasing)
            {
                isChasing = false;
                isReturning = true;
                currentSpeed = walkSpeed;
            }

            if (isReturning)
            {
                ReturnToStartPosition();
            }
            else
            {
                Patrol();
            }
        }

        UpdateMovementAnimation();
    }

    // Cập nhật hoạt ảnh đi bộ/đứng yên
    void UpdateMovementAnimation()
    {
        if (animator != null)
        {
            bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            animator.SetBool("isMoving", isMoving);
        }
    }

    // --- Hành vi tuần tra (Patrol) ---
    void Patrol()
    {
        float moveDir = isFacingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDir * walkSpeed, rb.linearVelocity.y);

        bool hitWall = CheckWall();
        bool hitLedge = CheckLedge();

        bool outOfMinBounds = transform.position.x < startPosition.x - patrolRange;
        bool outOfMaxBounds = transform.position.x > startPosition.x + patrolRange;

        if (hitWall || (avoidFalling && hitLedge) || (outOfMinBounds && !isFacingRight) || (outOfMaxBounds && isFacingRight))
        {
            Flip();
        }
    }

    // --- Xử lý logic chiến đấu tầm xa (Ranged Combat) ---
    void HandleRangedCombat(float distanceToPlayer)
    {
        // 1. Đảm bảo Skeleton luôn quay mặt về phía Player
        float directionX = playerTransform.position.x - transform.position.x;
        if (directionX > 0.1f && !isFacingRight) Flip();
        else if (directionX < -0.1f && isFacingRight) Flip();

        // 2. Kiểm tra khoảng cách di chuyển
        if (distanceToPlayer > stopRange)
        {
            // Nếu ngoài khoảng cách ném -> Đi bộ áp sát từ từ
            currentSpeed = chaseSpeed;
            float moveDir = directionX > 0f ? 1f : -1f;
            rb.linearVelocity = new Vector2(moveDir * currentSpeed, rb.linearVelocity.y);
        }
        else
        {
            // Nếu đã ở khoảng cách an toàn -> Đứng im để ném xương
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        // 3. Tấn công ném xương khi hết cooldown
        if (Time.time >= nextAttackTime)
        {
            // Kích hoạt hoạt ảnh ném
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            // Gọi Coroutine ném xương để khớp với thời gian tay vung ra
            StartCoroutine(ThrowBoneRoutine());

            // Thiết lập hồi chiêu
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    // Coroutine trì hoãn ném xương cho khớp với Animation
    private IEnumerator ThrowBoneRoutine()
    {
        yield return new WaitForSeconds(throwDelay);

        if (isDead || Time.time < staggerTimer) yield break; // Nếu đã chết hoặc đang bị choáng thì hủy ném

        if (bonePrefab != null)
        {
            // Xác định điểm xuất phát ném (nếu chưa gán throwPoint thì lấy vị trí hiện tại lệch lên trên một chút)
            Vector2 spawnPos = throwPoint != null ? (Vector2)throwPoint.position : (Vector2)transform.position + new Vector2(isFacingRight ? 0.5f : -0.5f, 0.5f);
            
            // Tạo quả xương
            GameObject bone = Instantiate(bonePrefab, spawnPos, Quaternion.identity);
            SkeletonBone boneScript = bone.GetComponent<SkeletonBone>();

            if (boneScript != null)
            {
                // Cho xương bay theo hướng mặt của Skeleton
                Vector2 throwDir = isFacingRight ? Vector2.right : Vector2.left;
                boneScript.SetDirection(throwDir);

                // Bỏ qua va chạm vật lý giữa bản thân Skeleton và quả xương vừa tạo
                Collider2D skeletonCol = GetComponent<Collider2D>();
                Collider2D boneCol = bone.GetComponent<Collider2D>();
                if (skeletonCol != null && boneCol != null)
                {
                    Physics2D.IgnoreCollision(skeletonCol, boneCol);
                }
            }
        }
    }

    // --- Quay trở lại vị trí cũ ---
    void ReturnToStartPosition()
    {
        currentSpeed = walkSpeed;
        float directionX = startPosition.x - transform.position.x;

        if (Mathf.Abs(directionX) > 0.2f)
        {
            float moveDir = directionX > 0f ? 1f : -1f;
            rb.linearVelocity = new Vector2(moveDir * walkSpeed, rb.linearVelocity.y);

            if (moveDir > 0f && !isFacingRight) Flip();
            else if (moveDir < 0f && isFacingRight) Flip();

            if (CheckWall())
            {
                isReturning = false;
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            isReturning = false;
        }
    }

    // --- Nhận sát thương ---
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        if (animator != null)
        {
            animator.SetTrigger("Hurt");
        }

        StartCoroutine(FlashHitEffect());

        // Bị choáng/khựng
        staggerTimer = Time.time + staggerDuration;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        // Reset lại thời gian hồi đòn ném (bị ngắt đòn ném dở)
        nextAttackTime = Time.time + attackCooldown;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // --- Khi chết ---
    void Die()
    {
        isDead = true;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        Destroy(gameObject, 2f);
    }

    // --- Quay mặt ---
    void Flip()
    {
        isFacingRight = !isFacingRight;
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = invertFlip ? isFacingRight : !isFacingRight;
        }
        else
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    // Kiểm tra tường/vực
    bool CheckWall()
    {
        Vector2 checkOrigin = (Vector2)transform.position + new Vector2(isFacingRight ? 0.5f : -0.5f, 0);
        RaycastHit2D hit = Physics2D.Raycast(checkOrigin, isFacingRight ? Vector2.right : Vector2.left, wallCheckDistance, obstacleLayer);
        Debug.DrawRay(checkOrigin, (isFacingRight ? Vector2.right : Vector2.left) * wallCheckDistance, Color.red);
        return hit.collider != null;
    }

    bool CheckLedge()
    {
        Vector2 checkOrigin = (Vector2)transform.position + new Vector2(isFacingRight ? 0.5f : -0.5f, 0);
        RaycastHit2D hit = Physics2D.Raycast(checkOrigin, Vector2.down, ledgeCheckDistance, obstacleLayer);
        Debug.DrawRay(checkOrigin, Vector2.down * ledgeCheckDistance, Color.yellow);
        return hit.collider == null;
    }

    // Coroutine nháy đỏ trắng khi trúng đòn
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

    // Công cụ trực quan trong cửa sổ Scene
    private void OnDrawGizmosSelected()
    {
        // 1. Tầm phát hiện Player (Xanh lá)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 2. Khoảng cách đứng ném (Vàng)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopRange);

        // 3. Phạm vi đi tuần tra (Xanh lam)
        Vector2 centerPos = Application.isPlaying ? startPosition : (Vector2)transform.position;
        Gizmos.color = Color.cyan;
        Vector3 startLeft = new Vector3(centerPos.x - patrolRange, centerPos.y, transform.position.z);
        Vector3 startRight = new Vector3(centerPos.x + patrolRange, centerPos.y, transform.position.z);
        
        Gizmos.DrawLine(startLeft, startRight);
        Gizmos.DrawLine(startLeft + Vector3.up * 0.3f, startLeft + Vector3.down * 0.3f);
        Gizmos.DrawLine(startRight + Vector3.up * 0.3f, startRight + Vector3.down * 0.3f);
    }
}
