using UnityEngine;

public class ZombieAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 1.5f;       // Tốc độ đi tuần tra
    public float chaseSpeed = 3f;        // Tốc độ đuổi theo Player
    public bool invertFlip = false;      // Tích chọn nếu Sprite bị lỗi moonwalk (đi tiến nhưng mặt quay lùi)
    private float currentSpeed;

    [Header("Health Settings")]
    public float maxHealth = 3f;         // Máu tối đa của Zombie
    private float currentHealth;
    private bool isDead = false;

    [Header("Attack Settings")]
    public float attackRange = 1.2f;     // Khoảng cách để tấn công Player
    public float attackDamage = 1f;      // Sát thương gây ra cho Player
    public float attackCooldown = 1.5f;  // Thời gian hồi chiêu giữa mỗi đòn đánh
    private float nextAttackTime = 0f;

    [Header("Detection Settings")]
    public float detectionRange = 5f;    // Khoảng cách phát hiện Player
    public LayerMask obstacleLayer;      // Lớp các chướng ngại vật (nền đất, tường)
    
    [Header("Patrol Settings")]
    public bool avoidFalling = true;     // Tránh rơi khỏi vực (chỉ cho game đi cảnh 2D)
    public float wallCheckDistance = 0.5f;
    public float ledgeCheckDistance = 1f;

    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Transform playerTransform;    // Tham chiếu tới Player

    private bool isFacingRight = true;
    private bool isChasing = false;

    void Start()
    {
        currentSpeed = walkSpeed;
        currentHealth = maxHealth;

        // Tự động tìm Rigidbody2D, Animator, SpriteRenderer nếu chưa gán
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // Tự động tìm Player bằng Tag "Player"
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
        // Nếu Zombie đã chết, không làm gì cả
        if (isDead) return;

        if (playerTransform == null)
        {
            Patrol();
            return;
        }

        // Tính khoảng cách đến Player
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        bool isAttacking = false;

        if (distanceToPlayer <= attackRange)
        {
            // Trong tầm đánh -> Tấn công
            isAttacking = true;
            AttackPlayer();
        }
        else
        {
            // Bất cứ khi nào Player ra ngoài tầm đánh -> Dừng trạng thái tấn công ngay lập tức
            if (animator != null)
            {
                animator.ResetTrigger("Attack");
                animator.SetBool("isAttacking", false);
            }

            if (distanceToPlayer <= detectionRange)
            {
                // Trong tầm nhìn nhưng ngoài tầm đánh -> Đuổi theo
                isChasing = true;
                ChasePlayer();
            }
            else
            {
                // Ngoài tầm nhìn -> Đi tuần tra
                if (isChasing)
                {
                    isChasing = false;
                    currentSpeed = walkSpeed;
                    Flip();
                }
                Patrol();
            }
        }

        // Cập nhật hoạt ảnh di chuyển và tấn công
        if (animator != null)
        {
            bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            animator.SetBool("isMoving", isMoving);
            animator.SetBool("isAttacking", isAttacking);
        }
    }

    // --- Hành vi đi tuần tra (Patrol) ---
    void Patrol()
    {
        float moveDir = isFacingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDir * walkSpeed, rb.linearVelocity.y);

        bool hitWall = CheckWall();
        bool hitLedge = CheckLedge();

        if (hitWall || (avoidFalling && hitLedge))
        {
            Flip();
        }
    }

    // --- Hành vi đuổi theo Player (Chase) ---
    void ChasePlayer()
    {
        currentSpeed = chaseSpeed;
        float directionX = playerTransform.position.x - transform.position.x;

        if (directionX > 0.1f)
        {
            rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);
            if (!isFacingRight) Flip();
        }
        else if (directionX < -0.1f)
        {
            rb.linearVelocity = new Vector2(-currentSpeed, rb.linearVelocity.y);
            if (isFacingRight) Flip();
        }
    }

    // --- Hành vi tấn công Player (Attack) ---
    void AttackPlayer()
    {
        // Khi tấn công, Zombie sẽ dừng lại
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        // Đảm bảo Zombie luôn quay mặt về phía Player khi tấn công
        float directionX = playerTransform.position.x - transform.position.x;
        if (directionX > 0.1f && !isFacingRight) Flip();
        else if (directionX < -0.1f && isFacingRight) Flip();

        // Kích hoạt trạng thái tấn công trong Animator
        if (animator != null)
        {
            animator.SetBool("isAttacking", true);
        }

        // Kiểm tra xem đã hồi chiêu xong chưa
        if (Time.time >= nextAttackTime)
        {
            // Kích hoạt animation tấn công (dùng Trigger)
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            // Gây sát thương lên Player
            HealthManager playerHealth = playerTransform.GetComponent<HealthManager>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }

            // Đặt thời gian hồi chiêu cho đòn đánh tiếp theo
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    // --- Nhận sát thương (Sẽ được gọi bởi Player hoặc Bẫy) ---
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        // Kích hoạt animation bị thương (nếu có)
        if (animator != null)
        {
            animator.SetTrigger("Hurt");
        }

        // Kiểm tra xem máu đã hết chưa
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // --- Hành vi khi chết ---
    void Die()
    {
        isDead = true;

        // Dừng mọi chuyển động vật lý
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Vô hiệu hóa va chạm để Player có thể đi xuyên qua xác Zombie
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Kích hoạt animation chết
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // Hủy đối tượng Zombie khỏi game sau 2 giây
        Destroy(gameObject, 2f);
    }

    // --- Quay mặt (Flip) ---
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

    // --- Kiểm tra Va chạm / Vực bằng Raycast ---
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

    // Hiển thị vòng tròn tầm nhìn và tầm đánh trong cửa sổ Scene
    private void OnDrawGizmosSelected()
    {
        // Tầm phát hiện (Màu xanh lá)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Tầm tấn công (Màu đỏ)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
