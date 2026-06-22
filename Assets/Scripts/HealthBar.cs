using UnityEngine;
using UnityEngine.UI;
public class HealthBar : MonoBehaviour
{
    [SerializeField] private HealthManager playerHealth;
    [SerializeField] private Image totalHealthBar;
    [SerializeField] private Image tcurrentHealthBar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        totalHealthBar.fillAmount = 1f;

        // ✅ Khởi tạo bar hiện tại đúng tỉ lệ
        tcurrentHealthBar.fillAmount = playerHealth.currentHealth
                                     / playerHealth.StartingHealth;
    }

    // Update is called once per frame
    void Update()
    {
        tcurrentHealthBar.fillAmount = playerHealth.currentHealth
                                     / playerHealth.StartingHealth;
    }
}
