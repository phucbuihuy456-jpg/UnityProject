using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    [Header("Boss Setup")]
    [Tooltip("Kéo object chứa thanh máu của Boss (trên Canvas) vào đây")]
    public GameObject bossHealthBarUI;

    [Tooltip("Kéo file nhạc nền (mp3, wav) của Boss vào đây")]
    public AudioClip bossMusic;

    [Tooltip("Âm lượng của nhạc Boss (0 là tắt hẳn, 1 là to nhất)")]
    [Range(0f, 1f)]
    public float bossMusicVolume = 0.5f;

    // Biến này để đảm bảo sự kiện chỉ kích hoạt 1 lần duy nhất khi mới bước vào
    private bool hasTriggered = false;

    void Start()
    {
        // Tắt thanh máu boss đi khi mới vào scene
        if (bossHealthBarUI != null)
        {
            bossHealthBarUI.SetActive(false);
        }
    }

    // Hàm này tự động chạy khi có một vật thể chạm vào vùng Trigger 2D
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Nếu người chạm vào có dán nhãn là "Player" và sự kiện chưa từng kích hoạt
        if (collision.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true; // Đánh dấu là đã kích hoạt rồi, không chạy lại nữa

            // 1. Bật UI thanh máu Boss lên
            if (bossHealthBarUI != null)
            {
                bossHealthBarUI.SetActive(true);
            }

            // 2. Tự động tìm máy phát nhạc nền (BackgroundMusic) và đổi sang nhạc Boss
            BackgroundMusic bgmManager = FindAnyObjectByType<BackgroundMusic>();
            if (bgmManager != null && bossMusic != null)
            {
                AudioSource bgmSource = bgmManager.GetComponent<AudioSource>();

                // Nếu tìm thấy AudioSource và bài hát hiện tại khác bài nhạc boss
                if (bgmSource != null && bgmSource.clip != bossMusic)
                {
                    bgmSource.Stop();
                    bgmSource.clip = bossMusic;

                    // Chỉnh âm lượng nhạc Boss tại đây trước khi phát
                    bgmSource.volume = bossMusicVolume;

                    bgmSource.Play();
                }
            }
        }
    }
}