using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiển thị các chỉ dẫn điều khiển theo thứ tự khi bắt đầu màn chơi:
/// 1) Di chuyển trái/phải (A + D)  -> bấm đủ cả 2 phím thì qua bước sau
/// 2) Nhảy (W)                     -> bấm W thì qua
/// 3) Đánh thường (J)              -> bấm J thì qua
/// 4) Chiêu đặc biệt (K)           -> bấm K thì ẩn hướng dẫn
///
/// Chỉ cần gắn script này vào 1 GameObject rỗng trong scene PlayScreen.
/// UI (Canvas + Text) sẽ được tự tạo lúc chạy.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Header("Nội dung chỉ dẫn (có thể sửa)")]
    [TextArea] public string moveMessage = "Nhấn A và D để di chuyển trái / phải";
    [TextArea] public string jumpMessage = "Nhấn W để nhảy";
    [TextArea] public string attackMessage = "Nhấn J để đánh thường";
    [TextArea] public string specialMessage = "Nhấn K để dùng chiêu đặc biệt";

    [Header("Hiển thị")]
    public int fontSize = 40;
    public Color textColor = Color.white;
    [Tooltip("Vị trí theo chiều dọc màn hình: 0 = đáy, 1 = đỉnh")]
    [Range(0f, 1f)] public float verticalAnchor = 0.85f;
    [Tooltip("Thời gian (giây) chờ trước khi ẩn sau khi hoàn thành bước cuối")]
    public float hideDelayAfterFinish = 1.0f;

    private Text tutorialText;

    // Trạng thái bước 1 (di chuyển)
    private bool pressedA = false;
    private bool pressedD = false;

    private enum Step { Move, Jump, Attack, Special, Done }
    private Step currentStep = Step.Move;

    void Start()
    {
        BuildUI();
        ShowStep(Step.Move);
    }

    void Update()
    {
        switch (currentStep)
        {
            case Step.Move:
                if (Input.GetKeyDown(KeyCode.A)) pressedA = true;
                if (Input.GetKeyDown(KeyCode.D)) pressedD = true;

                if (pressedA && pressedD)
                {
                    ShowStep(Step.Jump);
                }
                break;

            case Step.Jump:
                if (Input.GetKeyDown(KeyCode.W))
                {
                    ShowStep(Step.Attack);
                }
                break;

            case Step.Attack:
                if (Input.GetKeyDown(KeyCode.J))
                {
                    ShowStep(Step.Special);
                }
                break;

            case Step.Special:
                if (Input.GetKeyDown(KeyCode.K))
                {
                    currentStep = Step.Done;
                    StartCoroutine(HideAfterDelay());
                }
                break;
        }
    }

    private void ShowStep(Step step)
    {
        currentStep = step;

        if (tutorialText == null)
            return;

        switch (step)
        {
            case Step.Move:
                tutorialText.text = moveMessage;
                break;
            case Step.Jump:
                tutorialText.text = jumpMessage;
                break;
            case Step.Attack:
                tutorialText.text = attackMessage;
                break;
            case Step.Special:
                tutorialText.text = specialMessage;
                break;
        }
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelayAfterFinish);

        if (tutorialText != null)
            tutorialText.transform.parent.gameObject.SetActive(false);
    }

    /// <summary>
    /// Tự tạo Canvas + Text để không phải dựng UI bằng tay trong Editor.
    /// </summary>
    private void BuildUI()
    {
        // Canvas
        GameObject canvasGO = new GameObject("TutorialCanvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // nổi lên trên các UI khác

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Text
        GameObject textGO = new GameObject("TutorialText");
        textGO.transform.SetParent(canvasGO.transform, false);

        tutorialText = textGO.AddComponent<Text>();
        tutorialText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tutorialText.fontSize = fontSize;
        tutorialText.color = textColor;
        tutorialText.alignment = TextAnchor.MiddleCenter;
        tutorialText.horizontalOverflow = HorizontalWrapMode.Overflow;
        tutorialText.verticalOverflow = VerticalWrapMode.Overflow;

        // Viền chữ cho dễ đọc trên nền sáng/tối
        Outline outline = textGO.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Đặt vị trí: căn giữa ngang, theo verticalAnchor dọc
        RectTransform rt = tutorialText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, verticalAnchor);
        rt.anchorMax = new Vector2(0.5f, verticalAnchor);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(1600, 120);
    }
}
