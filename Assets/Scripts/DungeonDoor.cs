using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cửa hầm (Dungeon Door): khi người chơi lại gần và bấm phím tương tác (E)
/// thì chuyển sang scene mới.
///
/// Cách dùng:
///  - Gắn script này vào GameObject của cánh cửa.
///  - Không cần thêm Collider hay Tag: script tự đo khoảng cách tới người chơi
///    bằng "interactRadius".
///  - (Tùy chọn) gán "prompt" là một object UI/sprite kiểu "Bấm E" để tự hiện/ẩn.
/// </summary>
public class DungeonDoor : MonoBehaviour
{
    [Header("Chuyển scene")]
    [Tooltip("Tên scene sẽ load (phải có trong Build Settings)")]
    public string targetScene = "Level1";

    [Header("Tương tác")]
    [Tooltip("Phím để vào cửa")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("Khoảng cách tối đa để có thể tương tác (đơn vị world)")]
    public float interactRadius = 1.5f;

    [Tooltip("(Tùy chọn) Object gợi ý 'Bấm E' — tự bật khi tới gần, tắt khi đi xa")]
    public GameObject prompt;

    private Transform player;

    void Start()
    {
        // Tự tìm người chơi qua component PlayerMovement (không cần đặt Tag)
        PlayerMovement pm = Object.FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
            player = pm.transform;

        if (prompt != null)
            prompt.SetActive(false);
    }

    void Update()
    {
        if (player == null)
            return;

        bool inRange = Vector2.Distance(transform.position, player.position) <= interactRadius;

        if (prompt != null && prompt.activeSelf != inRange)
            prompt.SetActive(inRange);

        if (inRange && Input.GetKeyDown(interactKey))
        {
            EnterDoor();
        }
    }

    void EnterDoor()
    {
        // Phòng trường hợp game đang tạm dừng (Time.timeScale = 0 ở màn thắng)
        Time.timeScale = 1f;
        SceneManager.LoadScene(targetScene);
    }

    // Vẽ vòng tròn bán kính tương tác trong Scene view để dễ canh chỉnh
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
