using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SegmentedHealthBar : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Kéo object HealthBarContainer vào đây")]
    public Transform container;

    [Tooltip("Kéo Prefab ô máu (HealthCell_Prefab) vào đây")]
    public GameObject healthCellPrefab;

    [Header("Sprite Settings")]
    [Tooltip("Hình ảnh ô máu khi CÒN MÁU (VD: Trái tim đỏ)")]
    public Sprite fullHealthSprite;

    [Tooltip("Hình ảnh ô máu khi MẤT MÁU (VD: Trái tim rỗng/đen)")]
    public Sprite emptyHealthSprite;

    // Danh sách lưu trữ các ô máu đã tạo ra
    private List<Image> healthCells = new List<Image>();

    /// <summary>
    /// Hàm này gọi lúc bắt đầu game để tạo ra số lượng ô bằng với Max HP
    /// </summary>
    public void SetupHealthBar(int maxHealth, int currentHealth)
    {
        // Xóa các ô cũ nếu có (tránh bị lỗi khi chơi lại)
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
        healthCells.Clear();

        // Tạo ra các ô máu mới
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject cell = Instantiate(healthCellPrefab, container);
            Image cellImage = cell.GetComponent<Image>();
            healthCells.Add(cellImage);
        }

        // Cập nhật hiển thị ngay lập tức
        UpdateHealth(currentHealth);
    }

    /// <summary>
    /// Gọi hàm này mỗi khi nhân vật bị mất máu hoặc hồi máu
    /// </summary>
    public void UpdateHealth(int currentHealth)
    {
        for (int i = 0; i < healthCells.Count; i++)
        {
            // Đảm bảo màu của Image luôn là màu trắng để hiển thị đúng màu gốc của hình ảnh (Sprite)
            healthCells[i].color = Color.white;

            // Nếu vị trí của ô (i) nhỏ hơn lượng máu hiện tại, gán hình ảnh "Còn máu"
            // Ngược lại, gán hình ảnh "Mất máu"
            if (i < currentHealth)
            {
                healthCells[i].sprite = fullHealthSprite;
            }
            else
            {
                healthCells[i].sprite = emptyHealthSprite;
            }
        }
    }
}