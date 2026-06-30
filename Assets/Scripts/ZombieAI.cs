using System.Collections;
using UnityEngine;

public class ZombieAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed;       // Tốc độ đi tuần tra
    public float chaseSpeed;        // Tốc độ đuổi theo Player
    public bool invertFlip = false;      // Tích chọn nếu Sprite bị lỗi moonwalk (đi tiến nhưng mặt quay lùi)
    private float currentSpeed;

    [Header("Health Settings")]
    public float maxHealth;         // Máu tối đa của Zombie
    public float staggerDuration; // Thời gian bị khựng (choáng) khi trúng đòn
    private float currentHealth;
    private bool isDead = false;
    private float staggerTimer;     // Mốc thời gian kết thúc trạng thái khựng

    [Header("Attack Settings")]
    public float attackRange;     // Khoảng cách để tấn công Player
    public float attackDamage;      // Sát thương gây ra cho Player
    public float attackCooldown;  // Thời gian hồi chiêu giữa mỗi đòn đánh
    private float nextAttackTime;

    [Header("Detection Settings")]
    public float detectionRange;    // Khoảng cách phát hiện Player
    public LayerMask obstacleLayer;      // Lớp các chướng ngại vật (nền đất, tường)
    
    [Header("Patrol Settings")]
    public float patrolRange;       // Khoảng cách tuần tra tối đa quanh vị trí ban đầu
    public bool avoidFalling = true;     // Tránh rơi khỏi vực (chỉ cho game đi cảnh 2D)
    public float wallCheckDistance = 0.5f;
    public float ledgeCheckDistance = 1f;

    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Transform playerTransform;    // Tham chiếu tới Player

    private Vector2 startPosition;       // Vị trí ban đầu khi xuất hiện
    private bool isFacingRight = true;
    private bool isChasing = false;
    private bool isReturning = false;    // Đánh dấu Zombie đang đi bộ về vị trí cũ
    private bool wasAttacking = false;   // Lưu trạng thái tấn công của frame trước

    void Start()
    {
        currentSpeed = walkSpeed;
        currentHealth = maxHealth;
        
        // Lưu lại vị trí ban đầu của Zombie
        startPosition = transform.position;

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

        // Nếu đang bị khựng (choáng) do trúng đòn -> Dừng mọi hành động di chuyển/tấn công
        if (Time.time < staggerTimer)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            UpdateMovementAnimation(false);
            return;
        }

        // Nếu không tìm thấy Player -> Di chuyển về vị trí ban đầu hoặc đi tuần tra
        if (playerTransform == null)
        {
            if (isReturning)
            {
                ReturnToStartPosition();
            }
            else
            {
                Patrol();
            }
            UpdateMovementAnimation(false);
            return;
        }

        // Tính khoảng cách đến Player
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        bool isAttacking = false;

        if (distanceToPlayer <= attackRange)
        {
            // Nếu frame trước chưa ở trạng thái tấn công (vừa mới tiếp cận Player)
            if (!wasAttacking)
            {
                // Bắt Zombie phải chờ hết thời gian hồi chiêu rồi mới tung đòn đánh đầu tiên
                nextAttackTime = Time.time + attackCooldown;
            }
            // Trong tầm đánh -> Tấn công
            isAttacking = true;
            isChasing = false;
            isReturning = false; // Ngắt trạng thái quay về vị trí cũ
            AttackPlayer();
        }
        else if (distanceToPlayer <= detectionRange)
        {
            // Trong tầm nhìn nhưng ngoài tầm đánh -> Đuổi theo
            isChasing = true;
            isReturning = false; // Ngắt trạng thái quay về vị trí cũ
            ChasePlayer();
        }
        else
        {
            // Ngoài tầm nhìn của Zombie
            if (isChasing)
            {
                // Vừa mất dấu Player -> Bắt đầu quay về vị trí ban đầu
                isChasing = false;
                isReturning = true;
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

        // Cập nhật trạng thái Animator (isMoving và isAttacking)
        UpdateMovementAnimation(isAttacking);

        // Ghi nhận trạng thái tấn công của frame này để so sánh ở frame sau
        wasAttacking = isAttacking;
    }

    // Cập nhật hoạt ảnh di chuyển trong Animator
    void UpdateMovementAnimation(bool isAttacking)
    {
        if (animator != null)
        {
            // Nếu mất dấu Player, đảm bảo tắt trigger "Attack" và bool "isAttacking"
            if (!isAttacking)
            {
                animator.ResetTrigger("Attack");
                animator.SetBool("isAttacking", false);
            }
            else
            {
                animator.SetBool("isAttacking", true);
            }

            bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            animator.SetBool("isMoving", isMoving);
        }
    }

    // --- Hành vi đi tuần tra (Patrol) ---
    void Patrol()
    {
        float moveDir = isFacingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDir * walkSpeed, rb.linearVelocity.y);

        bool hitWall = CheckWall();
        bool hitLedge = CheckLedge();

        // Kiểm tra giới hạn phạm vi tuần tra xung quanh vị trí xuất phát ban đầu
        bool outOfMinBounds = transform.position.x < startPosition.x - patrolRange;
        bool outOfMaxBounds = transform.position.x > startPosition.x + patrolRange;

        // Quay đầu nếu đâm tường, gặp vực hoặc đi vượt quá giới hạn tuần tra
        if (hitWall || (avoidFalling && hitLedge) || (outOfMinBounds && !isFacingRight) || (outOfMaxBounds && isFacingRight))
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

    // --- Hành vi quay trở về vị trí cũ (Return to Spawn Location) ---
    void ReturnToStartPosition()
    {
        currentSpeed = walkSpeed;
        float directionX = startPosition.x - transform.position.x;

        // Nếu khoảng cách đến vị trí cũ lớn hơn 0.2 thì tiếp tục di chuyển về
        if (Mathf.Abs(directionX) > 0.2f)
        {
            float moveDir = directionX > 0f ? 1f : -1f;
            rb.linearVelocity = new Vector2(moveDir * walkSpeed, rb.linearVelocity.y);

            // Quay mặt đúng hướng di chuyển về vị trí cũ
            if (moveDir > 0f && !isFacingRight) Flip();
            else if (moveDir < 0f && isFacingRight) Flip();

            // Nếu đi về mà bị chặn bởi tường thì dừng trạng thái Return và chuyển sang đi tuần
            if (CheckWall())
            {
                isReturning = false;
            }
        }
        else
        {
            // Đã về đến vị trí ban đầu -> Đứng yên 1 nhịp và chuyển lại sang tuần tra bình thường
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            isReturning = false;
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

    // --- Nhận sát thương ---
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        // Kích hoạt animation bị thương
        if (animator != null)
        {
            animator.SetTrigger("Hurt");
        }

        // Tạo hiệu ứng nháy trắng đỏ khi trúng đòn
        StartCoroutine(FlashHitEffect());

        // Gán thời gian bị khựng (choáng) ngăn cản mọi hành động di chuyển/tấn công
        staggerTimer = Time.time + staggerDuration;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        // Reset lại thời gian hồi đòn đánh (bị ngắt đòn, bắt chờ lại từ đầu)
        nextAttackTime = Time.time + attackCooldown;

        // Kiểm tra xem máu đã hết chưa
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Coroutine tạo hiệu ứng nháy đỏ - trắng (bình thường) khi trúng đòn
    private IEnumerator FlashHitEffect()
    {
        if (spriteRenderer != null)
        {
            // Nháy đỏ
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);

            // Nháy về bình thường (Unity sử dụng màu trắng Color.white để hiển thị sprite nguyên bản)
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);

            // Nháy đỏ lần hai
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);

            // Trở về bình thường
            spriteRenderer.color = Color.white;
        }
    }

    // --- Hành vi khi chết ---
    void Die()
    {
        isDead = true;

        // Dừng mọi chuyển động vật lý
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Vô hiệu hóa va chạm
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

    // Hiển thị các công cụ hỗ trợ trong cửa sổ Scene
    private void OnDrawGizmosSelected()
    {
        // 1. Tầm nhìn (Xanh lá) và Tầm đánh (Đỏ)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 2. Phạm vi tuần tra (Xanh dương)
        // Khi game đang chạy, sử dụng startPosition làm tâm. Khi chưa chạy, sử dụng vị trí hiện tại của Zombie làm tâm.
        Vector2 centerPos = Application.isPlaying ? startPosition : (Vector2)transform.position;
        Gizmos.color = Color.cyan;
        Vector3 startLeft = new Vector3(centerPos.x - patrolRange, centerPos.y, transform.position.z);
        Vector3 startRight = new Vector3(centerPos.x + patrolRange, centerPos.y, transform.position.z);
        
        Gizmos.DrawLine(startLeft, startRight);
        Gizmos.DrawLine(startLeft + Vector3.up * 0.3f, startLeft + Vector3.down * 0.3f);
        Gizmos.DrawLine(startRight + Vector3.up * 0.3f, startRight + Vector3.down * 0.3f);
    }
}
