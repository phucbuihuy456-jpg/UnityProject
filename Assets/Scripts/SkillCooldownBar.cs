using UnityEngine;
using UnityEngine.UI;

public class SkillCooldownBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kéo Object Player (chứa PlayerMovement) vào đây")]
    [SerializeField] private PlayerMovement player;

    [Header("UI Images")]
    [Tooltip("Lớp 1 (Dưới cùng): Ảnh nền trống khi kỹ năng đang hồi")]
    [SerializeField] private Image emptyCooldownImage;

    [Tooltip("Lớp 2 (Trên cùng): Ảnh thanh kỹ năng đã hồi đầy")]
    [SerializeField] private Image fullCooldownImage;

    void Start()
    {
        // Khởi tạo ban đầu đảm bảo thanh đầy
        if (fullCooldownImage != null)
        {
            fullCooldownImage.fillAmount = 1f;
        }
    }

    void Update()
    {
        if (player == null || fullCooldownImage == null) return;

        // Đọc phần trăm hồi chiêu (từ 0 đến 1) trực tiếp từ PlayerMovement và gán vào lớp trên cùng
        fullCooldownImage.fillAmount = player.CooldownPercentage;
    }
}