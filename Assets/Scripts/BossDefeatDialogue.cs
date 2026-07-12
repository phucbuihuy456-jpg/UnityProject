using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Hội thoại cốt truyện tự động chạy sau khi đánh bại boss:
///  - Boss chết (giữ nguyên xác trong lúc nói) => hộp thoại hiện ra sau một khoảng trễ ngắn.
///  - Từng câu thoại hiện với hiệu ứng gõ chữ; nhấn Enter/E để hiện hết câu hoặc qua câu kế.
///  - Hết thoại => mở khóa di chuyển, xác boss mờ dần rồi biến mất.
///
/// Gắn component này lên cùng GameObject với script boss. Script boss chỉ cần gọi Play()
/// trong hàm Die() thay vì tự Destroy. UI tự dựng lúc chạy nên không cần dựng Canvas tay.
/// </summary>
public class BossDefeatDialogue : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        public string speaker;
        [TextArea] public string text;
    }

    [Header("Lời thoại sau khi boss bị hạ")]
    public DialogueLine[] dialogue = new DialogueLine[]
    {
        new DialogueLine {
            speaker = "Bộ Xương",
            text = "Khoan... đã... Ánh nến ấy... Ngươi cũng là người của Hội Canh Giữ... phải không...?"
        },
        new DialogueLine {
            speaker = "Elara",
            text = "Tấm huy hiệu rơi ra từ bộ giáp này... biểu tượng ngọn lửa của Hội?! 'Brother Tobias — Hiệp sĩ Canh Đêm, năm 1782'... Ngươi từng là thợ săn của Hội sao?"
        },
        new DialogueLine {
            speaker = "Brother Tobias",
            text = "Đã từng... Ta ôm cây nến của mình bước qua cánh cổng Wickwood trước ngươi... hơn hai trăm năm. Và ta chưa bao giờ... bước ra."
        },
        new DialogueLine {
            speaker = "Elara",
            text = "Chuyện gì đã xảy ra với ngươi? Tại sao một hiệp sĩ của Hội lại thành ra thế này?"
        },
        new DialogueLine {
            speaker = "Brother Tobias",
            text = "Cây nến của ta... nó dẫn đường cho ta... Nó soi vào những đứa trẻ trong làng... và thì thầm rằng chúng là 'cái ác'..."
        },
        new DialogueLine {
            speaker = "Brother Tobias",
            text = "Ta đã giết mười bảy đứa trẻ... vì ta tin tuyệt đối vào ánh sáng của mình. Nhưng nghe cho rõ đây, hỡi người mang nến... cái ác... chính là ta."
        },
        new DialogueLine {
            speaker = "Elara",
            text = "Không thể nào... Nến của Hội không bao giờ nói dối! Ánh sáng là thứ duy nhất ta có thể tin!"
        },
        new DialogueLine {
            speaker = "Brother Tobias",
            text = "Ta cũng từng nói... y hệt như vậy... Hãy nhớ lấy: trong lâu đài này, thứ phát sáng chưa chắc đã là thiện... và thứ ẩn trong bóng tối... chưa chắc đã là ác..."
        },
        new DialogueLine {
            speaker = "Elara",
            text = "...Ta không hiểu ngươi nói gì. Nhưng ta sẽ không dừng lại. Yên nghỉ đi, Brother Tobias."
        },
    };

    [Header("Thời gian")]
    [Tooltip("Chờ bao lâu sau khi boss chết (để animation chết chạy xong) mới hiện thoại")]
    public float startDelay = 1.2f;
    [Tooltip("Thời gian (giây) giữa mỗi ký tự hiện ra")]
    public float typeSpeed = 0.04f;
    [Tooltip("Thời gian xác boss mờ dần rồi biến mất sau khi thoại kết thúc")]
    public float fadeOutDuration = 1.5f;

    [Header("Điều khiển")]
    [Tooltip("Phím skip nhanh / qua câu")]
    public KeyCode advanceKey = KeyCode.Return;
    [Tooltip("Khóa di chuyển của player trong lúc nói chuyện")]
    public bool lockPlayerWhileTalking = true;

    [Header("Sau khi thoại xong")]
    [Tooltip("Tự hủy GameObject boss sau khi mờ dần")]
    public bool destroyBossWhenDone = true;
    [Tooltip("Sự kiện gọi khi thoại kết thúc (mở cửa, phát nhạc...)")]
    public UnityEvent onDialogueComplete;

    [Header("Âm thanh khi nói")]
    public AudioClip typingSound;
    [Range(0f, 1f)] public float voiceVolume = 0.7f;

    [Header("Hiển thị")]
    public int nameFontSize = 28;
    public int dialogueFontSize = 32;
    public Color textColor = Color.white;
    public Color nameColor = new Color(1f, 0.85f, 0.4f, 1f);

    private bool isPlaying = false;
    private bool isStreaming = false;
    private int lineIndex = 0;
    private Coroutine typingRoutine;

    private PlayerMovement player;
    private AudioSource audioSource;
    private GameObject dialogueBox;
    private Text nameLabel;
    private Text dialogueLabel;
    private SpriteRenderer bossSprite;

    /// <summary>Gọi từ script boss khi boss chết. Tự lo toàn bộ trình tự thoại + hủy xác.</summary>
    public void Play()
    {
        if (isPlaying)
            return;
        StartCoroutine(PlaySequence());
    }

    void Update()
    {
        if (!isPlaying || dialogueBox == null || !dialogueBox.activeSelf)
            return;

        if (Input.GetKeyDown(advanceKey)
            || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetKeyDown(KeyCode.E))
        {
            if (isStreaming)
                CompleteLine();
            else
                NextLine();
        }
    }

    private IEnumerator PlaySequence()
    {
        isPlaying = true;

        bossSprite = GetComponent<SpriteRenderer>();
        if (bossSprite == null)
            bossSprite = GetComponentInChildren<SpriteRenderer>();

        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
            player = pm;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        yield return new WaitForSeconds(startDelay);

        if (lockPlayerWhileTalking && player != null)
            player.SetInputLocked(true);

        BuildUI();
        dialogueBox.SetActive(true);
        lineIndex = 0;
        ShowCurrentLine();
    }

    private void NextLine()
    {
        lineIndex++;
        if (lineIndex >= dialogue.Length)
            EndDialogue();
        else
            ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (dialogue.Length == 0)
        {
            EndDialogue();
            return;
        }

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

        if (dialogueBox != null)
            dialogueBox.SetActive(false);

        if (player != null)
            player.SetInputLocked(false);

        onDialogueComplete?.Invoke();

        StartCoroutine(FadeOutAndDestroy());
    }

    private IEnumerator FadeOutAndDestroy()
    {
        if (bossSprite != null && fadeOutDuration > 0f)
        {
            Color startColor = bossSprite.color;
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / fadeOutDuration);
                bossSprite.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }

        isPlaying = false;

        if (destroyBossWhenDone)
            Destroy(gameObject);
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

    // ------------------------------------------------------------------
    // Tự dựng UI lúc runtime (cùng phong cách với NPCInteract)
    // ------------------------------------------------------------------
    private void BuildUI()
    {
        if (dialogueBox != null)
            return;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasGO = new GameObject("BossDialogueCanvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

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
        Outline nameOutline = nameGO.AddComponent<Outline>();
        nameOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        nameOutline.effectDistance = new Vector2(2f, -2f);
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

        // Gợi ý nhỏ góc phải dưới
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

        dialogueBox.SetActive(false);
    }
}
