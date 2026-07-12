using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Xử lý nút Restart trên DeathPanel khi chết ở trận boss Level 2:
/// thay vì load lại cả scene, hồi sinh player tại điểm respawn (Phòng 5)
/// trong cùng run và reset boss để đánh lại.
///
/// Tự khởi tạo khi scene "Level 2" được nạp — không cần đặt sẵn trong scene,
/// và tự tạo EventSystem nếu scene thiếu (không có EventSystem thì mọi nút UI đều chết).
/// </summary>
public class BossRetryManager : MonoBehaviour
{
    private const string TargetSceneName = "Level 2";

    [Header("References (để trống sẽ tự tìm khi vào scene)")]
    [SerializeField] private HealthManager playerHealth;
    [SerializeField] private LampkeeperBoss boss;
    [SerializeField] private VampireLordBoss vampireBoss;
    [SerializeField] private BossRoomTrigger bossRoomTrigger;
    [SerializeField] private BossRoomTrigger vampireRoomTrigger;
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Button restartButton;

    [Header("Respawn")]
    [Tooltip("Điểm hồi sinh cho trận Lampkeeper — mặc định là cửa vào Phòng 5")]
    [SerializeField] private Vector2 respawnPoint = new Vector2(43.5f, 22.5f);

    [Tooltip("Điểm hồi sinh khi chết ở các tầng trên (Phòng 6-9) — cửa vào phòng Checkpoint")]
    [SerializeField] private Vector2 checkpointRespawnPoint = new Vector2(-6.5f, 70.5f);

    [Tooltip("Điểm hồi sinh cho trận boss cuối — cửa vào Phòng Ngai Vàng")]
    [SerializeField] private Vector2 vampireRespawnPoint = new Vector2(-6.5f, 142.5f);

    [Tooltip("Chết ở độ cao y lớn hơn ngưỡng này = đang đánh boss cuối")]
    [SerializeField] private float vampireZoneMinY = 135f;

    [Tooltip("Chết ở độ cao y lớn hơn ngưỡng này (nhưng dưới vùng boss cuối) = hồi sinh ở Checkpoint")]
    [SerializeField] private float checkpointZoneMinY = 66f;

    // --- BOOTSTRAP: tự sinh khi vào Level 2, không phụ thuộc scene file ---

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureInstance(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance(scene);
    }

    private static void EnsureInstance(Scene scene)
    {
        if (scene.name != TargetSceneName)
            return;

        if (FindAnyObjectByType<BossRetryManager>() == null)
        {
            new GameObject("BossRetryManager (auto)").AddComponent<BossRetryManager>();
        }
    }

    // --- SETUP ---

    private void Start()
    {
        EnsureEventSystem();
        AutoWireReferences();
        HookRestartButton();
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        var go = new GameObject("EventSystem (auto)");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
        Debug.Log("BossRetryManager: scene thiếu EventSystem — đã tự tạo để UI nhận click.");
    }

    private void AutoWireReferences()
    {
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<HealthManager>();
        if (boss == null)
            boss = FindAnyObjectByType<LampkeeperBoss>();
        if (vampireBoss == null)
            vampireBoss = FindAnyObjectByType<VampireLordBoss>();

        // Scene có 2 trigger phòng boss — phân biệt theo tên GameObject
        if (bossRoomTrigger == null || vampireRoomTrigger == null)
        {
            foreach (var trigger in FindObjectsByType<BossRoomTrigger>(FindObjectsSortMode.None))
            {
                if (trigger.name.Contains("NgaiVang"))
                {
                    if (vampireRoomTrigger == null)
                        vampireRoomTrigger = trigger;
                }
                else if (bossRoomTrigger == null)
                {
                    bossRoomTrigger = trigger;
                }
            }
        }

        if (deathPanel == null && playerHealth != null)
            deathPanel = playerHealth.deathPanel;
    }

    private void HookRestartButton()
    {
        if (restartButton == null && deathPanel != null)
        {
            // Tìm cả khi DeathPanel đang inactive
            foreach (var btn in deathPanel.GetComponentsInChildren<Button>(true))
            {
                if (btn.name == "RestartButton")
                {
                    restartButton = btn;
                    break;
                }
            }
        }

        if (restartButton == null)
        {
            Debug.LogWarning("BossRetryManager: không tìm thấy RestartButton trong DeathPanel.");
            return;
        }

        // Tắt handler mặc định trong prefab (PauseMenu.RestartLevel — load lại cả scene)
        int persistentCount = restartButton.onClick.GetPersistentEventCount();
        for (int i = 0; i < persistentCount; i++)
        {
            restartButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        }

        restartButton.onClick.RemoveListener(RespawnAndResetBoss);
        restartButton.onClick.AddListener(RespawnAndResetBoss);
    }

    // --- RESPAWN ---

    public void RespawnAndResetBoss()
    {
        // Gỡ đóng băng thời gian do HealthManager đặt khi chết
        Time.timeScale = 1f;

        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }

        if (playerHealth != null)
        {
            Transform playerTransform = playerHealth.transform;
            Vector3 oldPos = playerTransform.position;
            playerTransform.position = PickRespawnPoint(oldPos.y);

            Rigidbody2D playerRb = playerHealth.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
            }

            playerHealth.ResetHealth();

            // Báo cho Cinemachine biết mục tiêu vừa "dịch chuyển tức thời"
            CinemachineCore.OnTargetObjectWarped(
                playerTransform,
                playerTransform.position - oldPos
            );
        }

        // Chỉ reset boss còn sống — reset boss đã chết sẽ hồi sinh nó và khóa cửa lại
        if (boss != null && boss.CurrentHealth > 0)
        {
            boss.ResetBoss();
            if (bossRoomTrigger != null)
            {
                bossRoomTrigger.ResetTrigger();
            }
        }

        if (vampireBoss != null && vampireBoss.CurrentHealth > 0)
        {
            vampireBoss.ResetBoss();
            if (vampireRoomTrigger != null)
            {
                vampireRoomTrigger.ResetTrigger();
            }
        }
    }

    /// <summary>
    /// Chọn điểm hồi sinh theo độ cao lúc chết: tầng ngai vàng -> cửa Phòng Ngai Vàng,
    /// các tầng trên Checkpoint -> cửa Checkpoint, còn lại -> cửa Phòng 5 (trước Boss 1).
    /// </summary>
    private Vector2 PickRespawnPoint(float deathY)
    {
        if (deathY >= vampireZoneMinY)
            return vampireRespawnPoint;
        if (deathY >= checkpointZoneMinY)
            return checkpointRespawnPoint;
        return respawnPoint;
    }
}
