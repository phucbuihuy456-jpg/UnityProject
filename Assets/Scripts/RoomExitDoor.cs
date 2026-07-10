using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Cửa qua phòng (Level 2): người chơi phải diệt hết quái trong phòng
/// thì mới có thể đứng ở cửa bên phải và bấm E để dịch chuyển sang phòng kế tiếp.
///
/// Cách dùng:
///  - Gắn script vào GameObject đặt tại vị trí cửa bên phải của phòng.
///  - Kéo container quái của phòng (vd "Enemies_Phong1") vào "enemiesRoot".
///    Quái của phòng nào thì đặt làm con của container phòng đó.
///  - "targetPosition" là tọa độ người chơi xuất hiện ở phòng kế tiếp (cửa bên trái).
///  - Quái được tính là "còn sống" khi GameObject còn active và còn Collider2D đang bật
///    (SkeletonAI/ZombieAI/BatAI đều tắt collider ngay trong Die() rồi mới Destroy sau 2s,
///    nên cửa sẽ mở ngay khi con quái cuối cùng gục xuống).
///  - Container chưa có quái (hoặc chưa gán) thì coi như phòng đã sạch — cửa mở sẵn.
/// </summary>
public class RoomExitDoor : MonoBehaviour
{
    [Header("Phòng")]
    [Tooltip("Container chứa toàn bộ quái của phòng này (mỗi quái là 1 object con)")]
    public Transform enemiesRoot;

    [Header("Dịch chuyển")]
    [Tooltip("Tọa độ người chơi xuất hiện ở phòng kế tiếp (ngay cửa bên trái)")]
    public Vector2 targetPosition;

    [Header("Tương tác")]
    [Tooltip("Phím để qua cửa")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("Khoảng cách tối đa để có thể tương tác (đơn vị world)")]
    public float interactRadius = 1.8f;

    [Header("Chữ gợi ý")]
    [Tooltip("(Tùy chọn) Object gợi ý có sẵn — để trống sẽ TỰ TẠO chữ nổi phía trên cửa")]
    public GameObject prompt;
    [Tooltip("Chữ hiện khi phòng đã sạch quái")]
    public string readyText = "Bấm E để sang phòng tiếp theo";
    [Tooltip("Chữ hiện khi còn quái ({0} = số quái còn sống)")]
    public string lockedText = "Diệt hết {0} quái để mở cửa!";
    [Tooltip("Vị trí chữ so với cửa")]
    public Vector2 promptOffset = new Vector2(0f, 2.6f);

    private Transform player;
    private Rigidbody2D playerRb;
    private TextMesh promptTextMesh; // chỉ có khi prompt do script tự tạo

    void Start()
    {
        // Tự tìm người chơi qua component PlayerMovement (không cần đặt Tag)
        PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
        {
            player = pm.transform;
            playerRb = pm.GetComponent<Rigidbody2D>();
        }

        if (prompt == null)
            prompt = CreatePrompt();

        prompt.SetActive(false);
    }

    void Update()
    {
        if (player == null)
            return;

        bool inRange = Vector2.Distance(transform.position, player.position) <= interactRadius;
        int alive = CountAliveEnemies();
        bool cleared = alive == 0;

        // Tới gần thì hiện chữ: phòng sạch -> hướng dẫn bấm E, còn quái -> báo cửa khóa
        if (prompt != null)
        {
            if (prompt.activeSelf != inRange)
                prompt.SetActive(inRange);

            if (promptTextMesh != null && inRange)
            {
                promptTextMesh.text = cleared ? readyText : string.Format(lockedText, alive);
                promptTextMesh.color = cleared ? new Color(1f, 0.95f, 0.6f) : new Color(1f, 0.45f, 0.4f);
            }
        }

        if (inRange && Input.GetKeyDown(interactKey))
        {
            if (cleared)
                Teleport();
            else
                Debug.Log($"[RoomExitDoor] Cửa còn khóa — còn {alive} quái trong phòng!");
        }
    }

    // Tự tạo chữ nổi (TextMesh) phía trên cửa — không cần chuẩn bị UI gì trong scene
    GameObject CreatePrompt()
    {
        GameObject go = new GameObject("Prompt_E");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = promptOffset;

        promptTextMesh = go.AddComponent<TextMesh>();
        promptTextMesh.text = readyText;
        promptTextMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptTextMesh.fontSize = 48;
        promptTextMesh.characterSize = 0.06f;
        promptTextMesh.anchor = TextAnchor.MiddleCenter;
        promptTextMesh.alignment = TextAlignment.Center;
        promptTextMesh.color = new Color(1f, 0.95f, 0.6f);

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.material = promptTextMesh.font.material;
        mr.sortingOrder = 20; // nổi trên tilemap và người chơi

        return go;
    }

    /// <summary>Phòng đã sạch quái chưa? (container trống/chưa gán cũng tính là sạch)</summary>
    public bool IsRoomCleared()
    {
        return CountAliveEnemies() == 0;
    }

    int CountAliveEnemies()
    {
        if (enemiesRoot == null)
            return 0;

        int alive = 0;
        foreach (Transform child in enemiesRoot)
        {
            if (!child.gameObject.activeInHierarchy)
                continue;

            // Quái chết thì AI tắt collider ngay lập tức (trước khi Destroy sau 2s)
            Collider2D col = child.GetComponentInChildren<Collider2D>();
            if (col != null && col.enabled)
                alive++;
        }
        return alive;
    }

    void Teleport()
    {
        Vector3 oldPos = player.position;
        player.position = targetPosition;
        if (playerRb != null)
            playerRb.linearVelocity = Vector2.zero;

        // Báo cho Cinemachine biết mục tiêu vừa "dịch chuyển tức thời"
        // để camera nhảy thẳng sang phòng mới thay vì lia ngang qua vùng map trống
        CinemachineCore.OnTargetObjectWarped(player, player.position - oldPos);
    }

    // Vẽ bán kính tương tác + điểm đến trong Scene view để dễ canh chỉnh
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPosition, 0.5f);
        Gizmos.DrawLine(transform.position, targetPosition);
    }
}
