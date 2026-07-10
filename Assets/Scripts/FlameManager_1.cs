using UnityEngine;
using UnityEngine.UI;
public class FlameManager_1 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int flameCount = 0;
    public Text flameText;
    public GameObject Door;
    public bool isDoorOpen = false;
    public GameObject winPanel;
    public int targetFlames = 9;
    void Start()
    {
        // Nạp lại số flame đã nhặt từ scene trước (giữ đồng nhất giữa các màn).
        if (GameProgress.HasData)
        {
            flameCount = GameProgress.FlameCount;
        }

        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Đồng bộ số flame để giữ khi chuyển scene.
        GameProgress.SaveFlames(flameCount);

        if (flameText != null)
        {
            flameText.text = " : " + flameCount.ToString();
        }
        if (flameCount >= 4 && !isDoorOpen)
        {
            isDoorOpen = true;
            if(Door != null)
            {
                Destroy(Door);
            }
        }
        // [TẠM TẮT] Logic thắng khi nhặt đủ lửa - tạm thời không dùng để win nữa.
        // Bật lại bằng cách bỏ comment khối dưới.
        // if (winPanel != null && flameCount >= targetFlames)
        // {
        //     winPanel.SetActive(true);
        //     Time.timeScale = 0f;
        // }
    }
}
