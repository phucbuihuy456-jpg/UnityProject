using UnityEngine;
using UnityEngine.UIElements;

public class HealthManager : MonoBehaviour
{
    [SerializeField]private float startingHealth;
    public float currentHealth { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float StartingHealth => startingHealth;
    private Animator animator;
    public GameObject deathPanel;
    void Start()
    {
        animator = GetComponent<Animator>();

        // Nạp lại máu đã lưu khi chuyển scene; nếu chưa có thì dùng máu khởi tạo.
        if (GameProgress.HasData)
            currentHealth = Mathf.Clamp(GameProgress.Health, 0, startingHealth);
        else
            currentHealth = startingHealth;

        // Đồng bộ lại giá trị đã lưu.
        GameProgress.SaveHealth(currentHealth);

        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Hồi sinh player trong cùng run: hồi đầy máu, reset animation chết, tắt death panel.
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = startingHealth;
        GameProgress.SaveHealth(currentHealth);

        if (animator != null)
        {
            // Đưa animator về state mặc định (thoát khỏi animation Die)
            animator.Rebind();
            animator.Update(0f);
        }

        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }

        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.ResetState();
        }
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth = Mathf.Max(currentHealth - damageAmount, 0); // Đảm bảo không giảm dưới 0
        GameProgress.SaveHealth(currentHealth); // Lưu lại máu hiện tại để giữ khi chuyển scene
        if (currentHealth > 0)
        {
            // player hurt
            animator.SetTrigger("Hurt");
            PlayerMovement pm = GetComponent<PlayerMovement>();
            if (pm != null)
            {
                pm.ResetAttack();
            }
        }
        else
        {
            // player dead
            animator.SetTrigger("Die");
            deathPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }
}
