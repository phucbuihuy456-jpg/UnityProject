using UnityEngine;
using UnityEngine.UI;
using TMPro; // Xóa dòng này nếu bạn dùng Text thường của Unity thay vì TextMeshPro

public class TutorialManagerMenu : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Kéo Component Image dùng để hiển thị hình ảnh hướng dẫn vào đây")]
    public Image tutorialDisplayImage;

    [Tooltip("Kéo nút Next (Tiến) vào đây")]
    public Button nextButton;

    [Tooltip("Kéo nút Previous (Lùi) vào đây")]
    public Button prevButton;

    [Tooltip("(Tùy chọn) Kéo Text hiển thị số trang (VD: 1/3) vào đây")]
    public TextMeshProUGUI pageNumberText; // Đổi thành public Text pageNumberText; nếu không dùng TextMeshPro

    [Header("Tutorial Data")]
    [Tooltip("Khóa ổ khóa ở đây, chỉnh Size và kéo các bức ảnh hướng dẫn vào")]
    public Sprite[] tutorialPages;

    // Biến lưu trữ trang hiện tại (Bắt đầu từ 0)
    private int currentPageIndex = 0;

    void Start()
    {
        // Khi bật màn hình hướng dẫn lên, luôn bắt đầu từ trang đầu tiên (trang 0)
        currentPageIndex = 0;
        UpdateTutorialUI();
    }

    // Hàm này sẽ được gán vào sự kiện OnClick của nút Next
    public void GoToNextPage()
    {
        // Nếu chưa phải là trang cuối cùng
        if (currentPageIndex < tutorialPages.Length - 1)
        {
            currentPageIndex++;
            UpdateTutorialUI();
        }
    }

    // Hàm này sẽ được gán vào sự kiện OnClick của nút Previous
    public void GoToPreviousPage()
    {
        // Nếu chưa phải là trang đầu tiên
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdateTutorialUI();
        }
    }

    // Hàm này làm nhiệm vụ thay ảnh và làm mờ nút khi hết trang
    private void UpdateTutorialUI()
    {
        // 1. Nếu không có ảnh nào trong danh sách thì báo lỗi rồi thoát
        if (tutorialPages.Length == 0)
        {
            Debug.LogWarning("Chưa có bức ảnh nào trong Tutorial Manager!");
            return;
        }

        // 2. Thay đổi hình ảnh trên UI thành hình ảnh ở trang hiện tại
        tutorialDisplayImage.sprite = tutorialPages[currentPageIndex];

        // 3. Tắt/Bật nút Prev: Bật nếu index > 0 (không phải trang đầu)
        prevButton.interactable = (currentPageIndex > 0);

        // 4. Tắt/Bật nút Next: Bật nếu chưa tới trang cuối cùng
        nextButton.interactable = (currentPageIndex < tutorialPages.Length - 1);

        // 5. Cập nhật chữ số trang (VD: "1 / 3")
        if (pageNumberText != null)
        {
            // Cộng 1 vì máy tính đếm từ 0, nhưng người bình thường đếm từ 1
            pageNumberText.text = (currentPageIndex + 1) + " / " + tutorialPages.Length;
        }
    }

    // Hàm dùng để gán vào nút "Close" (Tắt màn hình hướng dẫn)
    public void CloseTutorial()
    {
        gameObject.SetActive(false);
    }
}