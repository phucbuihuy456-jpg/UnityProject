using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public AudioSource audioSource;

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float jumpForce = 3f;
    public int maxJump = 2;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.7f;
    public float maxSlopeAngle = 60f;

    [Header("Collect")]
    public FlameManager_1 flameManager;
    public AudioClip collectSound;

    private bool isGrounded;
    private bool isAttacking = false;
    private int jumpCount = 0;

    // Leo dốc
    private float moveInput;
    private bool onSlope;
    private Vector2 slopeNormal;
    private float defaultGravity;

    void Start()
    {
        defaultGravity = rb.gravityScale;
    }

    void Update()
    {
        // Ground Check
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            0.3f,
            groundLayer);

        if (isGrounded)
        {
            jumpCount = 0;
        }

        animator.SetBool("isGrounded", isGrounded);
        animator.SetFloat("yVelocity", rb.linearVelocity.y);

        // Đọc input
        moveInput = 0f;

        if (Input.GetKey(KeyCode.D))
        {
            moveInput = 1f;
            spriteRenderer.flipX = false;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            moveInput = -1f;
            spriteRenderer.flipX = true;
        }

        animator.SetBool("isMoving", moveInput != 0);

        HandleAttack();

        if (!isAttacking)
        {
            HandleJump();
        }
    }

    void FixedUpdate()
    {
        CheckSlope();

        if (onSlope && moveInput != 0f)
        {
            // Di chuyển theo hướng của mặt dốc
            Vector2 slopeDir = new Vector2(slopeNormal.y, -slopeNormal.x).normalized;

            if (Mathf.Sign(slopeDir.x) != Mathf.Sign(moveInput))
                slopeDir = -slopeDir;

            rb.linearVelocity = slopeDir * moveSpeed;
        }
        else
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }

        // Không bị trượt khi đứng trên dốc
        rb.gravityScale = onSlope ? 0f : defaultGravity;
    }

    //========================
    // Jump
    //========================
    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.W) && jumpCount < maxJump)
        {
            jumpCount++;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);

            rb.gravityScale = defaultGravity;

            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            onSlope = false;
        }
    }

    //========================
    // Attack
    //========================
    private void HandleAttack()
    {
        if (Input.GetKeyDown(KeyCode.J) && !isAttacking)
        {
            isAttacking = true;

            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            animator.SetBool("isMoving", false);

            animator.SetTrigger("Attack");
        }
    }

    // Animation Event
    public void FinishAttack()
    {
        isAttacking = false;
    }

    //========================
    // Slope Check
    //========================
    private void CheckSlope()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            rb.position,
            Vector2.down,
            groundCheckDistance,
            groundLayer);

        if (hit.collider != null)
        {
            slopeNormal = hit.normal;

            float angle = Vector2.Angle(hit.normal, Vector2.up);

            onSlope = angle > 1f && angle <= maxSlopeAngle;
        }
        else
        {
            onSlope = false;
        }
    }

    //========================
    // Collision
    //========================

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            isGrounded = false;
        }
    }

    //========================
    // Collect Flame
    //========================
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Flame"))
        {
            if (audioSource != null && collectSound != null)
            {
                audioSource.PlayOneShot(collectSound);
            }

            Destroy(other.gameObject);

            if (flameManager != null)
            {
                flameManager.flameCount++;
            }
        }
    }
}
