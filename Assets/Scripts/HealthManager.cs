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
        currentHealth = startingHealth;
        animator = GetComponent<Animator>();
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            TakeDamage(1);
        }
    }
    public void TakeDamage(float damageAmount)
    {
       currentHealth = Mathf.Max(currentHealth - damageAmount, 0); // Đảm bảo không giảm dưới 0
        if (currentHealth > 0)
        {
            // player hurt
            animator.SetTrigger("Hurt");
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
