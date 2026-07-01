using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    private static BackgroundMusic instance;

    void Awake()
    {
        // Kiểm tra xem đã có máy phát nhạc nào đang chạy từ Scene trước chưa
        if (instance == null)
        {
            // Nếu chưa có, giữ lại cái này và cấm Unity xóa nó khi đổi Scene
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Nếu đã có máy phát nhạc từ Scene trước rồi, hãy hủy cái mới này đi
            // để 2 bài nhạc không bị phát đè lên nhau (dội âm)
            Destroy(gameObject);
        }
    }
}