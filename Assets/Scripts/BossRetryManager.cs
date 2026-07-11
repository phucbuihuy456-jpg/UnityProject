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
    [SerializeField] private BossRoomTrigger bossRoomTrigger;
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Button restartButton;

    [Header("Respawn")]
    [Tooltip("Điểm hồi sinh — mặc định là cửa vào Phòng 5")]
    [SerializeField] private Vector2 respawnPoint = new Vector2(43.5f, 22.5f);

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
        if (bossRoomTrigger == null)
            bossRoomTrigger = FindAnyObjectByType<BossRoomTrigger>();
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
            playerTransform.position = respawnPoint;

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

        if (boss != null)
        {
            boss.ResetBoss();
        }

        if (bossRoomTrigger != null)
        {
            bossRoomTrigger.ResetTrigger();
        }
    }
}
