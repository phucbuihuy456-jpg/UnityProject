using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody2D rb;       // Rigidbody2D của nhân vật
    public Animator animator;    // Animator cho chạy/idle
    public SpriteRenderer spriteRenderer; // SpriteRenderer để lật hướng
    public float moveSpeed = 3.5f;
    public float jumpForce = 8f;

    [Header("Kiểm tra mặt đất / dốc (cầu thang)")]
    [Tooltip("Chọn đúng Layer của nền/cầu thang (vd: Ground)")]
    public LayerMask groundLayer;
    [Tooltip("Tầm bắn tia xuống dưới để dò mặt đất/dốc")]
    public float groundCheckDistance = 0.7f;
    [Tooltip("Góc dốc tối đa mà nhân vật đi lên được (độ)")]
    public float maxSlopeAngle = 60f;

    private bool isGrounded = true; // Dùng để giới hạn nhảy
    public FlameManager_1 flameManager; // Tham chiếu đến FlameManager

    private int jumpCount = 0;
    public int maxJump = 2;

    public AudioSource audioSource;
    public AudioClip collectSound;

    // --- Trạng thái dốc ---
    private float moveInput;        // -1, 0, 1
    private bool onSlope;           // đang đứng trên dốc?
    private Vector2 slopeNormal;    // pháp tuyến mặt dốc
    private float defaultGravity;   // lưu gravityScale ban đầu

    void Start()
    {
        defaultGravity = rb.gravityScale;
    }

    void Update()
    {
        // --- Đọc input ngang ---
        moveInput = 0f;
        if (Input.GetKey(KeyCode.D))
        {
            moveInput = 1f;
            spriteRenderer.flipX = false; // Hướng mặt về phải
        }
        else if (Input.GetKey(KeyCode.A))
        {
            moveInput = -1f;
            spriteRenderer.flipX = true;  // Hướng mặt về trái
        }
        animator.SetBool("isMoving", moveInput != 0f);

        // --- Nhảy ---
        if (Input.GetKeyDown(KeyCode.W) && jumpCount < maxJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Reset vận tốc y trước khi nhảy
            rb.gravityScale = defaultGravity; // bảo đảm có trọng lực khi nhảy khỏi dốc
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpCount++;
            isGrounded = false;
            onSlope = false;
        }
    }

    void FixedUpdate()
    {
        CheckSlope();

        if (onSlope && moveInput != 0f)
        {
            // Di chuyển DỌC THEO mặt dốc -> tự đi lên cầu thang, không cần nhảy.
            Vector2 slopeDir = new Vector2(slopeNormal.y, -slopeNormal.x).normalized;
            // Đảm bảo hướng đi trùng với phím bấm (trái/phải)
            if (Mathf.Sign(slopeDir.x) != Mathf.Sign(moveInput))
                slopeDir = -slopeDir;

            rb.linearVelocity = slopeDir * moveSpeed;
        }
        else
        {
            // Mặt phẳng / đang ở trên không: giữ vận tốc Y (rơi, nhảy) như bình thường
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }

        // Khi đứng trên dốc thì tắt trọng lực để KHÔNG bị trượt xuống;
        // rời dốc (đi trên phẳng / nhảy / rơi) thì bật lại trọng lực.
        rb.gravityScale = onSlope ? 0f : defaultGravity;
    }

    // Bắn tia xuống dưới để biết đang đứng trên dốc hay không
    void CheckSlope()
    {
        RaycastHit2D hit = Physics2D.Raycast(rb.position, Vector2.down, groundCheckDistance, groundLayer);
        if (hit.collider != null)
        {
            slopeNormal = hit.normal;
            float angle = Vector2.Angle(hit.normal, Vector2.up);
            // Coi là "dốc" khi nghiêng đáng kể nhưng vẫn trong giới hạn leo được
            onSlope = angle > 1f && angle <= maxSlopeAngle;
        }
        else
        {
            onSlope = false;
        }
    }

    // Khi chạm bất kỳ collider nào → bật isGrounded, reset số lần nhảy
    void OnCollisionEnter2D(Collision2D collision)
    {
        isGrounded = true;
        jumpCount = 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ăn lửa
        if (other.gameObject.CompareTag("Flame"))
        {
            audioSource.PlayOneShot(collectSound);
            Destroy(other.gameObject); // Hủy đối tượng lửa
            flameManager.flameCount++;
        }
    }
}
