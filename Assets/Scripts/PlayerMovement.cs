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

    [Header("Collect")]
    public FlameManager_1 flameManager;
    public AudioClip collectSound;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    private bool isGrounded;

    private int jumpCount = 0;
    private bool isAttacking = false;

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(
       groundCheck.position,
       0.3f,
       groundLayer);

        animator.SetBool("isGrounded", isGrounded);
        animator.SetFloat("yVelocity", rb.linearVelocity.y);

        HandleAttack();

        if (!isAttacking)
        {
            HandleMovement();
            HandleJump();
        }
    }

    //========================
    // Movement
    //========================
    private void HandleMovement()
    {
        if (Input.GetKey(KeyCode.D))
        {
            rb.linearVelocity = new Vector2(moveSpeed, rb.linearVelocity.y);
            animator.SetBool("isMoving", true);
            spriteRenderer.flipX = false;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            rb.linearVelocity = new Vector2(-moveSpeed, rb.linearVelocity.y);
            animator.SetBool("isMoving", true);
            spriteRenderer.flipX = true;
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetBool("isMoving", false);
        }
    }

    //========================
    // Jump
    //========================
    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.W) && jumpCount < maxJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            jumpCount++;
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

            // Dừng di chuyển trước khi đánh
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            animator.SetBool("isMoving", false);
            animator.SetTrigger("Attack");
        }
    }

    // Được gọi bằng Animation Event
    public void FinishAttack()
    {
        isAttacking = false;
    }

    //========================
    // Collision
    //========================
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            jumpCount = 0;
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Flame"))
        {
            audioSource.PlayOneShot(collectSound);

            Destroy(other.gameObject);

            flameManager.flameCount++;
        }
    }
}
