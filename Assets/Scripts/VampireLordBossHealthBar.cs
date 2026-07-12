using UnityEngine;
using UnityEngine.UI;

public class VampireLordBossHealthBar : MonoBehaviour
{
    [Header("Boss Reference")]
    [Tooltip("Kéo Object Boss (chứa script VampireLordBoss) vào đây")]
    [SerializeField] private VampireLordBoss boss;

    [Header("UI Images")]
    [Tooltip("Lớp 1 (Dưới cùng): Ảnh hiển thị khi mất sạch máu")]
    [SerializeField] private Image emptyHealthImage;

    [Tooltip("Lớp 2 (Ở giữa): Ảnh dải máu lưu lại (Màu xanh/trắng...)")]
    [SerializeField] private Image damageTrailImage;

    [Tooltip("Lớp 3 (Trên cùng): Ảnh máu chính (Màu đỏ)")]
    [SerializeField] private Image fullHealthImage;

    [Header("Settings")]
    [Tooltip("Tốc độ tụt của dải máu phụ (Số càng to tụt càng nhanh)")]
    [SerializeField] private float shrinkSpeed = 1f;

    [Header("End Fight Settings")]
    [Tooltip("Kéo Object CHỨA CẢ CỤM thanh máu boss này vào đây để tắt đi khi thắng")]
    [SerializeField] private GameObject bossUIContainer;

    [Tooltip("Kéo file nhạc nền CŨ của màn chơi vào đây để nó phát lại khi thắng")]
    [SerializeField] private AudioClip normalLevelMusic;

    // Biến đánh dấu để sự kiện thắng boss chỉ chạy 1 lần
    private bool hasDefeated = false;

    void Start()
    {
        if (boss != null && boss.MaxHealth > 0)
        {
            float fillValue = boss.CurrentHealth / boss.MaxHealth;
            if (fullHealthImage != null) fullHealthImage.fillAmount = fillValue;
            if (damageTrailImage != null) damageTrailImage.fillAmount = fillValue;
        }
    }

    void Update()
    {
        // Nếu không có boss hoặc boss đã bị hạ trước đó, không làm gì cả
        if (boss == null || hasDefeated) return;

        // KIỂM TRA: Boss hết máu -> Kích hoạt dọn dẹp sau trận chiến
        if (boss.CurrentHealth <= 0)
        {
            hasDefeated = true;
            HandleBossDefeated();
            return;
        }

        // Tính toán phần trăm máu hiện tại của Boss
        float targetFill = boss.CurrentHealth / boss.MaxHealth;

        // 1. Thanh máu chính giật xuống ngay lập tức
        if (fullHealthImage != null)
        {
            fullHealthImage.fillAmount = targetFill;
        }

        // 2. Xử lý thanh máu lưu ảnh (Trail) ở giữa tụt từ từ
        if (damageTrailImage != null)
        {
            if (damageTrailImage.fillAmount > targetFill)
            {
                damageTrailImage.fillAmount = Mathf.MoveTowards(damageTrailImage.fillAmount, targetFill, shrinkSpeed * Time.deltaTime);
            }
            else
            {
                damageTrailImage.fillAmount = targetFill;
            }
        }
    }

    // Hàm xử lý việc tắt giao diện và chuyển nhạc
    private void HandleBossDefeated()
    {
        // 1. Tắt thanh máu Boss
        if (bossUIContainer != null)
        {
            bossUIContainer.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false); // Phương án dự phòng nếu bạn quên kéo thả ở Inspector
        }

        // 2. Tự động tìm máy phát nhạc và đổi về bài nhạc nền ban đầu
        BackgroundMusic bgmManager = FindAnyObjectByType<BackgroundMusic>();
        if (bgmManager != null && normalLevelMusic != null)
        {
            AudioSource bgmSource = bgmManager.GetComponent<AudioSource>();
            if (bgmSource != null && bgmSource.clip != normalLevelMusic)
            {
                bgmSource.Stop();
                bgmSource.clip = normalLevelMusic;
                bgmSource.Play();
            }
        }
    }
}
