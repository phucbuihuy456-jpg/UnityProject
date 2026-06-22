using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody2D rb;       // Rigidbody2D của nhân vật
    public Animator animator;    // Animator cho chạy/idle
    public SpriteRenderer spriteRenderer; // SpriteRenderer để lật hướng
    public float moveSpeed = 3.5f;
    public float jumpForce = 8f;

    private bool isGrounded = true; // Giả lập đơn giản để nhảy
    public FlameManager_1 flameManager; // Tham chiếu đến FlameManager

    private int jumpCount = 0;
    public int maxJump = 2;

    public AudioSource audioSource;
    public AudioClip collectSound;
    void Update()
    {
        // --- Di chuyển ngang ---
        if (Input.GetKey(KeyCode.D))
        {
            // Di chuyển sang phải
            rb.linearVelocity = new Vector2(moveSpeed, rb.linearVelocity.y);
            animator.SetBool("isMoving", true);
            spriteRenderer.flipX = false; // Hướng mặt về phải
        }
        else if (Input.GetKey(KeyCode.A))
        {
            // Di chuyển sang trái
            rb.linearVelocity = new Vector2(-moveSpeed, rb.linearVelocity.y);
            animator.SetBool("isMoving", true);
            spriteRenderer.flipX = true; // Hướng mặt về trái
        }
        else
        {
            // Không nhấn gì → đứng yên
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            animator.SetBool("isMoving", false);
        }

        // --- Nhảy ---
        if (Input.GetKeyDown(KeyCode.W) && jumpCount < maxJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Reset vận tốc y trước khi nhảy
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpCount++;
            isGrounded = false;
        }
    }

    // Khi chạm bất kỳ collider nào → bật isGrounded
    void OnCollisionEnter2D(Collision2D collision)
    {
        isGrounded = true;
        jumpCount = 0; // Reset số lần nhảy khi chạm đất
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Nếu chạm vào collider có tag "Ground" → bật isGrounded
        if (other.gameObject.CompareTag("Flame"))
        {
            audioSource.PlayOneShot(collectSound);
            Destroy(other.gameObject); // Hủy đối tượng lửa
            flameManager.flameCount++;
        }
    }
}
