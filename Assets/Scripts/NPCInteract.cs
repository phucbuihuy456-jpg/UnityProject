using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NPC đứng yên (chạy animation idle) và cho phép người chơi tương tác:
///  - Player lại gần (vào vùng trigger) => hiện gợi ý "Nhấn E để nói chuyện".
///  - Nhấn E => hiện từng câu thoại với hiệu ứng GÕ CHỮ (streaming) kèm tiếng nói.
///  - Đang gõ mà nhấn E => hiện ngay hết câu; câu đã xong nhấn E => qua câu kế.
///  - Hết thoại thì đóng lại.
///
/// UI (gợi ý + hộp thoại) tự tạo lúc chạy nên không cần dựng Canvas tay.
/// </summary>
public class NPCInteract : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        public string speaker;
        [TextArea] public string text;
    }

    [Header("Tương tác")]
    [Tooltip("Phím bắt đầu nói chuyện khi lại gần")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("Phím skip nhanh / qua câu khi đang nói chuyện")]
    public KeyCode advanceKey = KeyCode.Return;
    public string hintText = "Nhấn E để nói chuyện";
    [Tooltip("Khóa di chuyển của player trong lúc nói chuyện")]
    public bool lockPlayerWhileTalking = true;

    [Header("Lời thoại")]
    public DialogueLine[] dialogue = new DialogueLine[]
    {
        new DialogueLine {
            speaker = "Bà sơ",
            text = "Con là người thứ tám, Elara. Bảy người đi trước đã mang theo ngọn nến vào lâu đài Wickwood… và không ai quay lại."
        },
        new DialogueLine {
            speaker = "Bà sơ",
            text = "Chừng nào Cây Nến Vesper còn cháy, con còn sống. Khi nó tắt — con tắt theo nó. Đừng bao giờ để bóng tối chạm tới ngọn lửa ấy."
        },
        new DialogueLine {
            speaker = "Bà sơ",
            text = "Đi đi, con của ta. Đi vào ánh nến, và đừng quay đầu lại… dù cho lâu đài có gọi tên con bằng một cái tên con tưởng mình chưa từng nghe."
        },
        new DialogueLine {
            speaker = "Elara",
            text = "Con sẽ cẩn thận, người hãy an tâm."
        },
    };

    [Header("Hiệu ứng gõ chữ")]
    [Tooltip("Thời gian (giây) giữa mỗi ký tự hiện ra")]
    public float typeSpeed = 0.04f;

    [Header("Âm thanh khi nói")]
    [Tooltip("Tiếng phát trong lúc chữ đang chạy (mô phỏng đang nói)")]
    public AudioClip typingSound;
    [Range(0f, 1f)] public float voiceVolume = 0.7f;

    [Header("Hiển thị")]
    public int hintFontSize = 26;
    public int nameFontSize = 28;
    public int dialogueFontSize = 32;
    public Color textColor = Color.white;
    public Color nameColor = new Color(1f, 0.85f, 0.4f, 1f);

    private bool playerInRange = false;
    private bool isTalking = false;
    private bool isStreaming = false;
    private int lineIndex = 0;
    private Coroutine typingRoutine;

    private PlayerMovement player;
    private AudioSource audioSource;
    private Canvas canvas;
    private Text hintLabel;
    private GameObject dialogueBox;
    private Text nameLabel;
    private Text dialogueLabel;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // âm 2D

        BuildUI();
        SetHintVisible(false);
        dialogueBox.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange)
            return;

        if (!isTalking)
        {
            // Chưa nói chuyện: bấm E để bắt đầu
            if (Input.GetKeyDown(interactKey))
                StartDialogue();
            return;
        }

        // Đang nói chuyện: bấm Enter để skip nhanh / qua câu
        if (Input.GetKeyDown(advanceKey) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (isStreaming)
                CompleteLine();   // đang gõ -> hiện ngay hết câu
            else
                NextLine();       // câu đã xong -> qua câu kế
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other))
        {
            playerInRange = true;
            if (player == null)
                player = other.GetComponentInParent<PlayerMovement>();
            if (!isTalking)
                SetHintVisible(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other))
        {
            playerInRange = false;
            SetHintVisible(false);
            EndDialogue();
        }
    }

    private bool IsPlayer(Collider2D other)
    {
        return other.GetComponentInParent<PlayerMovement>() != null
            || other.CompareTag("Player");
    }

    private void StartDialogue()
    {
        isTalking = true;
        lineIndex = 0;
        SetHintVisible(false);
        dialogueBox.SetActive(true);

        if (lockPlayerWhileTalking && player != null)
            player.SetInputLocked(true);

        ShowCurrentLine();
    }

    private void NextLine()
    {
        lineIndex++;
        if (lineIndex >= dialogue.Length)
        {
            EndDialogue();
            if (playerInRange)
                SetHintVisible(true);
        }
        else
        {
            ShowCurrentLine();
        }
    }

    private void ShowCurrentLine()
    {
        if (dialogue.Length == 0)
            return;

        DialogueLine line = dialogue[Mathf.Clamp(lineIndex, 0, dialogue.Length - 1)];
        nameLabel.text = line.speaker;

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeLine(line));
    }

    private IEnumerator TypeLine(DialogueLine line)
    {
        isStreaming = true;
        dialogueLabel.text = "";
        StartVoice();

        string full = line.text ?? "";
        for (int i = 0; i < full.Length; i++)
        {
            dialogueLabel.text += full[i];
            yield return new WaitForSeconds(typeSpeed);
        }

        StopVoice();
        isStreaming = false;
    }

    private void CompleteLine()
    {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        DialogueLine line = dialogue[Mathf.Clamp(lineIndex, 0, dialogue.Length - 1)];
        dialogueLabel.text = line.text;
        StopVoice();
        isStreaming = false;
    }

    private void EndDialogue()
    {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);
        StopVoice();
        isStreaming = false;
        isTalking = false;
        if (dialogueBox != null)
            dialogueBox.SetActive(false);

        if (player != null)
            player.SetInputLocked(false);
    }

    private void StartVoice()
    {
        if (audioSource != null && typingSound != null)
        {
            audioSource.clip = typingSound;
            audioSource.loop = true;
            audioSource.volume = voiceVolume;
            audioSource.Play();
        }
    }

    private void StopVoice()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    private void SetHintVisible(bool visible)
    {
        if (hintLabel != null)
            hintLabel.gameObject.SetActive(visible);
    }

    // ------------------------------------------------------------------
    // Tự dựng UI lúc runtime
    // ------------------------------------------------------------------
    private void BuildUI()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasGO = new GameObject("NPCDialogueCanvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // --- Gợi ý ---
        GameObject hintGO = new GameObject("Hint");
        hintGO.transform.SetParent(canvasGO.transform, false);
        hintLabel = hintGO.AddComponent<Text>();
        hintLabel.font = font;
        hintLabel.fontSize = hintFontSize;
        hintLabel.color = textColor;
        hintLabel.alignment = TextAnchor.MiddleCenter;
        hintLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        hintLabel.verticalOverflow = VerticalWrapMode.Overflow;
        hintLabel.text = hintText;
        AddOutline(hintGO);
        RectTransform hintRT = hintLabel.rectTransform;
        hintRT.anchorMin = new Vector2(0.5f, 0.78f);
        hintRT.anchorMax = new Vector2(0.5f, 0.78f);
        hintRT.pivot = new Vector2(0.5f, 0.5f);
        hintRT.anchoredPosition = Vector2.zero;
        hintRT.sizeDelta = new Vector2(800, 60);

        // --- Hộp thoại ---
        dialogueBox = new GameObject("DialogueBox");
        dialogueBox.transform.SetParent(canvasGO.transform, false);
        Image boxBg = dialogueBox.AddComponent<Image>();
        boxBg.color = new Color(0f, 0f, 0f, 0.8f);
        RectTransform boxRT = boxBg.rectTransform;
        boxRT.anchorMin = new Vector2(0.5f, 0f);
        boxRT.anchorMax = new Vector2(0.5f, 0f);
        boxRT.pivot = new Vector2(0.5f, 0f);
        boxRT.anchoredPosition = new Vector2(0f, 40f);
        boxRT.sizeDelta = new Vector2(1500, 260);

        // Tên người nói
        GameObject nameGO = new GameObject("NameText");
        nameGO.transform.SetParent(dialogueBox.transform, false);
        nameLabel = nameGO.AddComponent<Text>();
        nameLabel.font = font;
        nameLabel.fontStyle = FontStyle.Bold;
        nameLabel.fontSize = nameFontSize;
        nameLabel.color = nameColor;
        nameLabel.alignment = TextAnchor.UpperLeft;
        AddOutline(nameGO);
        RectTransform nameRT = nameLabel.rectTransform;
        nameRT.anchorMin = new Vector2(0.05f, 0.72f);
        nameRT.anchorMax = new Vector2(0.95f, 0.98f);
        nameRT.offsetMin = Vector2.zero;
        nameRT.offsetMax = Vector2.zero;

        // Nội dung thoại
        GameObject dlgGO = new GameObject("DialogueText");
        dlgGO.transform.SetParent(dialogueBox.transform, false);
        dialogueLabel = dlgGO.AddComponent<Text>();
        dialogueLabel.font = font;
        dialogueLabel.fontSize = dialogueFontSize;
        dialogueLabel.color = textColor;
        dialogueLabel.alignment = TextAnchor.UpperLeft;
        RectTransform dlgRT = dialogueLabel.rectTransform;
        dlgRT.anchorMin = new Vector2(0.05f, 0.08f);
        dlgRT.anchorMax = new Vector2(0.95f, 0.70f);
        dlgRT.offsetMin = Vector2.zero;
        dlgRT.offsetMax = Vector2.zero;

        // Gợi ý nhỏ "[E] tiếp" góc phải dưới
        GameObject contGO = new GameObject("ContinueHint");
        contGO.transform.SetParent(dialogueBox.transform, false);
        Text contLabel = contGO.AddComponent<Text>();
        contLabel.font = font;
        contLabel.fontSize = 20;
        contLabel.color = new Color(1f, 1f, 1f, 0.6f);
        contLabel.alignment = TextAnchor.LowerRight;
        contLabel.text = "[Enter] tiếp";
        RectTransform contRT = contLabel.rectTransform;
        contRT.anchorMin = new Vector2(0.05f, 0.02f);
        contRT.anchorMax = new Vector2(0.97f, 0.2f);
        contRT.offsetMin = Vector2.zero;
        contRT.offsetMax = Vector2.zero;
    }

    private void AddOutline(GameObject go)
    {
        Outline o = go.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.85f);
        o.effectDistance = new Vector2(2f, -2f);
    }
}
