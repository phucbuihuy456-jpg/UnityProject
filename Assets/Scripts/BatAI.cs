using System.Collections;
using UnityEngine;

public class BatAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float patrolSpeed = 1.5f;     // Tốc độ bay tuần tra lơ lửng
    public float flySpeed = 3.5f;        // Tốc độ phi đến Player khi đuổi bắt
    public float returnSpeed = 2f;       // Tốc độ bay quay lại tổ
    public bool invertFlip = false;      // Tích chọn nếu Sprite bị lỗi ngược hướng đầu

    [Header("Health Settings")]
    public float maxHealth = 2f;         // Máu tối đa của Dơi
    public float staggerDuration = 0.3f; // Thời gian bị khựng khi trúng đòn
    private float currentHealth;
    private bool isDead = false;
    private float staggerTimer = 0f;

    [Header("Attack & Retreat Settings")]
    public float attackDamage = 1f;      // Sát thương gây ra cho Player
    public float attackCooldown = 2f;    // Thời gian chờ trước khi lao vào lần tiếp theo
    public float retreatSpeed = 4f;       // Tốc độ bay giật lùi ra xa
    public float retreatDuration = 0.8f;  // Thời gian bay giật lùi ra xa (giây)
    private float nextAttackTime = 0f;
    private bool isRetreating = false;    // Đang trong trạng thái bay giật lùi ra xa
    private float retreatTimer = 0f;
    private Vector2 retreatDirection;

    [Header("Detection Settings")]
    public float detectionRange = 6f;    // Khoảng cách phát hiện Player để đuổi theo

    [Header("Patrol Settings")]
    public float patrolRange = 3f;       // Khoảng cách tuần tra tối đa quanh tổ
    public float wallCheckDistance = 0.5f;
    public LayerMask obstacleLayer;      // Lớp mặt đất/tường để tránh đâm tường

    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Transform playerTransform;

    private Vector2 startPosition;       // Vị trí tổ ban đầu
    private bool isFacingRight = true;
    private bool isChasing = false;
    private bool isReturning = false;    // Đang bay về tổ

    void Start()
    {
        currentHealth = maxHealth;
        startPosition = transform.position;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // Khóa trọng lực của Dơi khi đang bay
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // Tự động tìm Player bằng Tag
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

        // Nếu đang bị khựng (choáng) do trúng đòn -> Dừng bay tại chỗ
        if (Time.time < staggerTimer)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Cập nhật hoạt ảnh lơ lửng bay liên tục khi còn sống
        if (animator != null)
        {
            animator.SetBool("isFlying", true);
        }

        // --- TRẠNG THÁI 1: Bay giật lùi ra xa (Retreating) sau khi chạm vào Player ---
        if (isRetreating)
        {
            if (Time.time < retreatTimer)
            {
                rb.linearVelocity = retreatDirection * retreatSpeed;

                // Luôn hướng mặt nhìn Player trong lúc đang bay giật lùi
                if (playerTransform != null)
                {
                    float dirX = playerTransform.position.x - transform.position.x;
                    if (dirX > 0.05f && !isFacingRight) Flip();
                    else if (dirX < -0.05f && isFacingRight) Flip();
                }
                return; // Bỏ qua tất cả hành động di chuyển khác
            }
            else
            {
                isRetreating = false;
            }
        }

        // Nếu không tìm thấy Player -> Di chuyển về tổ hoặc tuần tra
        if (playerTransform == null)
        {
            if (isReturning) ReturnToNest();
            else Patrol();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // --- TRẠNG THÁI 2: Đuổi bắt Player ---
        if (distanceToPlayer <= detectionRange)
        {
            // Nếu vừa mới nhìn thấy Player lần đầu (trước đó đang không đuổi và không giật lùi)
            if (!isChasing && !isRetreating)
            {
                // Reset cooldown để dơi ngay lập tức phi vào tấn công luôn
                nextAttackTime = 0f;
            }

            // Chỉ phi vào đuổi bắt nếu đã hết thời gian chờ cooldown (sau khi đâm trúng hoặc sau khi bị đánh trúng)
            if (Time.time >= nextAttackTime)
            {
                isChasing = true;
                isReturning = false;
                ChasePlayer();
            }
            else
            {
                // Nếu đang chờ hồi chiêu -> Tiếp tục đi tuần
                if (isChasing)
                {
                    isChasing = false;
                    isReturning = true;
                }

                if (isReturning) ReturnToNest();
                else Patrol();
            }
        }
        else
        {
            // Ngoài tầm phát hiện -> Quay về tổ
            if (isChasing)
            {
                isChasing = false;
                isReturning = true; 
            }

            if (isReturning)
            {
                ReturnToNest();
            }
            else
            {
                Patrol();
            }
        }
    }

    // --- Bay tuần tra lơ lửng (Patrol) ---
    void Patrol()
    {
        // Bay ngang qua lại theo cao độ Y ban đầu
        float targetY = startPosition.y;
        float moveDir = isFacingRight ? 1f : -1f;

        // Giữ thăng bằng cao độ Y ban đầu trong khi bay ngang
        float yDiff = targetY - transform.position.y;
        rb.linearVelocity = new Vector2(moveDir * patrolSpeed, yDiff * 2f);

        bool hitWall = CheckWall();
        bool outOfMinBounds = transform.position.x < startPosition.x - patrolRange;
        bool outOfMaxBounds = transform.position.x > startPosition.x + patrolRange;

        // Quay đầu nếu đâm tường hoặc bay vượt quá giới hạn tuần tra
        if (hitWall || (outOfMinBounds && !isFacingRight) || (outOfMaxBounds && isFacingRight))
        {
            Flip();
        }
    }

    // --- Bay đuổi theo Player (Chase) ---
    void ChasePlayer()
    {
        // Bay tự do cả hướng X và Y thẳng đến vị trí Player
        Vector2 direction = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * flySpeed;

        // Quay mặt về hướng bay đuổi
        if (direction.x > 0.05f && !isFacingRight) Flip();
        else if (direction.x < -0.05f && isFacingRight) Flip();
    }

    // --- Bay về tổ (Vị trí xuất phát) ---
    void ReturnToNest()
    {
        Vector2 direction = (startPosition - (Vector2)transform.position);
        float distance = direction.magnitude;

        if (distance > 0.2f)
        {
            rb.linearVelocity = direction.normalized * returnSpeed;

            if (direction.x > 0.05f && !isFacingRight) Flip();
            else if (direction.x < -0.05f && isFacingRight) Flip();
        }
        else
        {
            // Đã về tổ -> Chuyển sang bay tuần tra quanh tổ
            rb.linearVelocity = Vector2.zero;
            transform.position = startPosition;
            isReturning = false;
        }
    }

    // --- Xử lý va chạm vật lý (Khi chạm body vào Player) ---
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            HandlePlayerHit(collision.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            HandlePlayerHit(other.gameObject);
        }
    }

    // Gây sát thương và nhảy lùi ra xa
    void HandlePlayerHit(GameObject player)
    {
        if (isDead) return;

        // Nếu đang bị choáng (stagger) thì không được phép gây sát thương
        if (Time.time < staggerTimer) return;

        // Chỉ cắn nếu đã hết thời gian chờ (cooldown)
        if (Time.time >= nextAttackTime)
        {
            // 1. Gây sát thương lên Player
            HealthManager playerHealth = player.GetComponent<HealthManager>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }

            // 2. Kích hoạt animation cắn
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            // 3. Tính toán hướng và lực bay giật lùi ngược lại ra xa Player
            isRetreating = true;
            retreatTimer = Time.time + retreatDuration;
            
            // Hướng giật lùi = Tọa độ Dơi trừ đi tọa độ Player
            retreatDirection = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
            
            // Đề phòng trường hợp trùng khít vị trí (khoảng cách = 0) -> mặc định bay ngược hướng đang nhìn
            if (retreatDirection == Vector2.zero)
            {
                retreatDirection = isFacingRight ? Vector2.left : Vector2.right;
            }

            // 4. Thiết lập mốc thời gian hồi chiêu trước khi có thể phi vào lần tiếp theo
            nextAttackTime = Time.time + attackCooldown;
            isChasing = false;
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

        // Bị khựng lại trên không khi bị trúng đòn
        staggerTimer = Time.time + staggerDuration;
        rb.linearVelocity = Vector2.zero;
        
        // Bị ngắt trạng thái bay giật lùi cũ nếu đang có
        isRetreating = false; 

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // --- Dơi chết (Rơi tự do + Animation Death) ---
    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        
        // Bật trọng lực để xác rơi xuống
        rb.gravityScale = 1.5f;

        // Vô hiệu hóa va chạm
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Tắt hoạt ảnh bay và kích hoạt hoạt ảnh chết
        if (animator != null)
        {
            animator.SetBool("isFlying", false);
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

    // Kiểm tra đâm tường
    bool CheckWall()
    {
        Vector2 checkOrigin = (Vector2)transform.position + new Vector2(isFacingRight ? 0.4f : -0.4f, 0);
        RaycastHit2D hit = Physics2D.Raycast(checkOrigin, isFacingRight ? Vector2.right : Vector2.left, wallCheckDistance, obstacleLayer);
        Debug.DrawRay(checkOrigin, (isFacingRight ? Vector2.right : Vector2.left) * wallCheckDistance, Color.red);
        return hit.collider != null;
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

    // Vẽ trực quan phạm vi trên Scene
    private void OnDrawGizmosSelected()
    {
        // Tầm nhìn phát hiện (Xanh lá)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Vùng tuần tra lơ lửng (Xanh lam)
        Vector2 centerPos = Application.isPlaying ? startPosition : (Vector2)transform.position;
        Gizmos.color = Color.cyan;
        Vector3 startLeft = new Vector3(centerPos.x - patrolRange, centerPos.y, transform.position.z);
        Vector3 startRight = new Vector3(centerPos.x + patrolRange, centerPos.y, transform.position.z);
        
        Gizmos.DrawLine(startLeft, startRight);
        Gizmos.DrawLine(startLeft + Vector3.up * 0.3f, startLeft + Vector3.down * 0.3f);
        Gizmos.DrawLine(startRight + Vector3.up * 0.3f, startRight + Vector3.down * 0.3f);
    }
}
