using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("Player Reference")]
    [Tooltip("Kéo Object Nhân vật (có chứa script HealthManager) vào đây")]
    [SerializeField] private HealthManager playerHealth;

    [Header("UI Images")]
    [Tooltip("Lớp 1 (Dưới cùng): Ảnh hiển thị khi mất sạch máu")]
    [SerializeField] private Image emptyHealthImage;

    [Tooltip("Lớp 2 (Ở giữa): Ảnh dải máu lưu lại (Màu xanh lá/trắng...)")]
    [SerializeField] private Image damageTrailImage;

    [Tooltip("Lớp 3 (Trên cùng): Ảnh máu chính (Màu đỏ)")]
    [SerializeField] private Image fullHealthImage;

    [Header("Settings")]
    [Tooltip("Tốc độ tụt của dải máu phụ (Số càng to tụt càng nhanh)")]
    [SerializeField] private float shrinkSpeed = 1f;

    void Start()
    {
        // Khởi tạo ban đầu cho cả máu chính và máu phụ bằng nhau
        if (playerHealth != null && playerHealth.StartingHealth > 0)
        {
            float fillValue = playerHealth.currentHealth / playerHealth.StartingHealth;
            if (fullHealthImage != null) fullHealthImage.fillAmount = fillValue;
            if (damageTrailImage != null) damageTrailImage.fillAmount = fillValue;
        }
    }

    void Update()
    {
        if (playerHealth == null || playerHealth.StartingHealth <= 0) return;

        // Tính toán phần trăm máu hiện tại (Target)
        float targetFill = playerHealth.currentHealth / playerHealth.StartingHealth;

        // 1. Thanh máu chính luôn giật/tụt xuống ngay lập tức
        if (fullHealthImage != null)
        {
            fullHealthImage.fillAmount = targetFill;
        }

        // 2. Xử lý thanh máu lưu ảnh (Trail) ở giữa
        if (damageTrailImage != null)
        {
            // Nếu thanh trail đang dài hơn lượng máu thực tế (do vừa bị trừ máu)
            if (damageTrailImage.fillAmount > targetFill)
            {
                // Dùng MoveTowards để gọt dần thanh trail xuống bằng với máu thực tế một cách mượt mà
                damageTrailImage.fillAmount = Mathf.MoveTowards(damageTrailImage.fillAmount, targetFill, shrinkSpeed * Time.deltaTime);
            }
            // Nếu được hồi máu (targetFill lớn hơn trail)
            else
            {
                // Thanh trail cũng nhảy lên ngay lập tức để không bị lộ ra
                damageTrailImage.fillAmount = targetFill;
            }
        }
    }
}