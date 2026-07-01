using System.Collections;
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

    [Header("Attack Detection")]
    public float attackRange = 1.0f;
    public float attackDamage = 1f;
    public Transform attackPoint;
    public float attackDelay = 0.15f;

    [Header("Step Climb")]
    public float stepHeight = 0.4f;
    public float stepLookAhead = 0.4f;

    private bool isGrounded;
    private bool isAttacking = false;
    private int jumpCount = 0;

    // Leo dốc
    private float moveInput;
    private bool onSlope;
    private Vector2 slopeNormal;
    private float defaultGravity;

    [Header("Special Attack")]
    public GameObject specialAttackPrefab;
    public Transform firePoint;

    public float specialCooldown = 5f;
    private bool canSpecial = true;
    private bool isSpecialAttacking = false;

    [Header("Footstep")]
    public AudioClip footstepSound;

    void Start()
    {
        defaultGravity = rb.gravityScale;
    }

    void Update()
    {
        // Ground Check
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.3f, groundLayer);

        if (isGrounded && rb.linearVelocity.y <= 0.01f)
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

        if (!isSpecialAttacking)
        {
            HandleAttack();
        }

        if (!isAttacking)
        {
            HandleSpecialAttack();
        }

        if (!isAttacking && !isSpecialAttacking)
        {
            HandleJump();
        }
    }

    public void PlayFootstep()
    {
        if (audioSource != null &&
            footstepSound != null &&
            isGrounded)
        {
            audioSource.PlayOneShot(footstepSound, 0.3f);
        }
    }

    private void HandleSpecialAttack()
    {
        if (Input.GetKeyDown(KeyCode.K) && canSpecial && !isSpecialAttacking && !isAttacking)
        {
            isSpecialAttacking = true;
            canSpecial = false;

            animator.SetTrigger("SpecialAttack");

            StartCoroutine(SpecialCooldown());
        }
    }

    private IEnumerator SpecialCooldown()
    {
        yield return new WaitForSeconds(specialCooldown);

        canSpecial = true;
    }

    public void SpawnSwordWave()
    {
        if (specialAttackPrefab == null || firePoint == null)
            return;

        GameObject wave =
            Instantiate(specialAttackPrefab,
                        firePoint.position,
                        Quaternion.identity);

        float dir = spriteRenderer.flipX ? -1f : 1f;

        SwordWave sword = wave.GetComponent<SwordWave>();

        if (sword != null)
            sword.SetDirection(dir);
    }

    public void FinishSpecialAttack()
    {
        isSpecialAttacking = false;
    }

    void FixedUpdate()
    {
        if (isSpecialAttacking)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        CheckSlope();

        if (moveInput != 0f)
        {
            StepClimb();
        }

        // Only apply slope physics if the player is grounded and not jumping
        if (onSlope && isGrounded && moveInput != 0f && rb.linearVelocity.y <= 0.1f)
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

        // Only disable gravity if the player is actually grounded on the slope
        rb.gravityScale = (onSlope && isGrounded && rb.linearVelocity.y <= 0.1f) ? 0f : defaultGravity;
    }

    private void StepClimb()
    {
        if (moveInput == 0f)
            return;

        float direction = Mathf.Sign(moveInput);
        Vector2 feetPos = groundCheck.position;

        // Shoot raycast slightly above the ground (e.g. 0.05f) to detect step
        Vector2 lowerOrigin = feetPos + Vector2.up * 0.05f;
        RaycastHit2D hitLower = Physics2D.Raycast(
            lowerOrigin,
            new Vector2(direction, 0f),
            stepLookAhead,
            groundLayer
        );

        if (hitLower.collider != null && !hitLower.collider.isTrigger)
        {
            // Shoot raycast at maximum step height to see if it's clear
            Vector2 upperOrigin = feetPos + Vector2.up * stepHeight;
            RaycastHit2D hitUpper = Physics2D.Raycast(
                upperOrigin,
                new Vector2(direction, 0f),
                stepLookAhead,
                groundLayer
            );

            if (hitUpper.collider == null)
            {
                // Lift player slightly higher and nudge them slightly further forward
                rb.position += new Vector2(direction * 0.15f, stepHeight + 0.1f);
            }
        }
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

            // Option C: Call damage check after a short delay to sync with animation swing
            Invoke("CheckAttackHit", attackDelay);
        }
    }

    // Animation Event
    public void FinishAttack()
    {
        isAttacking = false;
    }

    public void ResetAttack()
    {
        isAttacking = false;
        CancelInvoke("CheckAttackHit"); // Cancel pending attack check if interrupted
    }

    private void CheckAttackHit()
    {
        Vector2 position =
            attackPoint != null
                ? (Vector2)attackPoint.position
                : (Vector2)transform.position
                    + (
                        spriteRenderer != null && spriteRenderer.flipX
                            ? Vector2.left
                            : Vector2.right
                    ) * 0.8f;

        // Find all colliders within the attack range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(position, attackRange);
        foreach (Collider2D enemy in hitEnemies)
        {
            // Gây sát thương nếu đối tượng là Zombie
            ZombieAI zombie = enemy.GetComponent<ZombieAI>();
            if (zombie != null)
            {
                zombie.TakeDamage(attackDamage);
            }

            // Gây sát thương nếu đối tượng là Skeleton
            SkeletonAI skeleton = enemy.GetComponent<SkeletonAI>();
            if (skeleton != null)
            {
                skeleton.TakeDamage(attackDamage);
            }

            // Gây sát thương nếu đối tượng là Bat (Dơi)
            BatAI bat = enemy.GetComponent<BatAI>();
            if (bat != null)
            {
                bat.TakeDamage(attackDamage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 position =
            attackPoint != null
                ? (Vector2)attackPoint.position
                : (Vector2)transform.position
                    + (
                        spriteRenderer != null && spriteRenderer.flipX
                            ? Vector2.left
                            : Vector2.right
                    ) * 0.8f;
        Gizmos.DrawWireSphere(position, attackRange);
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
            groundLayer
        );

        if (hit.collider != null)
        {
            slopeNormal = hit.normal;

            float angle = Vector2.Angle(hit.normal, Vector2.up);

            bool isStairs = hit.collider.CompareTag("Stairs");
            onSlope = (angle > 1f && angle <= maxSlopeAngle) || isStairs;

            // If they are blocky stairs (flat top surfaces), synthesize a slope normal to climb smoothly
            if (isStairs && angle <= 1f)
            {
                float dir =
                    moveInput != 0f ? Mathf.Sign(moveInput) : (spriteRenderer.flipX ? -1f : 1f);
                slopeNormal = new Vector2(-dir, 1f).normalized;
            }
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
