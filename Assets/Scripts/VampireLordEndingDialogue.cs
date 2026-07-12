using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Hội thoại kết màn với Lord Wickwood (Vampire Lord) — có CƠ CHẾ CHỌN CÂU TRẢ LỜI:
///  - Boss gục xuống => thoại mở đầu (hắn quỳ xuống, đưa ra đề nghị).
///  - Elara được chọn 1 trong các câu trả lời (W/S hoặc mũi tên để di chuyển, Enter để chọn):
///      + 2 lựa chọn cố định: Ánh Sáng (hy sinh) và Bóng Tối (nhận ngai vàng).
///      + 1 lựa chọn ẨN (Người Canh Giữ) chỉ mở khóa khi GameProgress.FlameCount
///        đạt đủ "flamesRequiredForSecret" (nhặt hết toàn bộ lửa trong game).
///  - Mỗi lựa chọn dẫn đến đoạn thoại kết riêng + màn hình ending riêng.
///  - Từ màn hình ending, nhấn Enter để xóa tiến trình và về Main Menu.
///
/// Gắn cùng GameObject với VampireLordBoss; boss gọi Play() trong chuỗi chết.
/// UI tự dựng lúc runtime, không cần dựng Canvas tay.
/// </summary>
public class VampireLordEndingDialogue : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        public string speaker;
        [TextArea] public string text;
    }

    [System.Serializable]
    public class EndingChoice
    {
        [Tooltip("Câu trả lời của Elara hiện trong bảng lựa chọn")]
        [TextArea] public string choiceText;
        [Tooltip("Đoạn thoại diễn ra sau khi chọn câu trả lời này")]
        public DialogueLine[] epilogue;
        [Tooltip("Tiêu đề màn hình kết thúc")]
        public string endingTitle;
        [Tooltip("Đoạn văn kể kết cục (hiện trên nền đen)")]
        [TextArea(5, 12)] public string endingNarration;
        [Tooltip("Sự kiện gọi khi người chơi chốt lựa chọn này")]
        public UnityEvent onChosen;
    }

    [Header("Thoại mở đầu (trước khi hiện lựa chọn)")]
    public DialogueLine[] introDialogue = new DialogueLine[]
    {
        new DialogueLine {
            speaker = "Lord Wickwood",
            text = "Khụ... Tuyệt vời... tuyệt vời lắm, cháu yêu của ta... Năm trăm năm rồi... dòng máu Wickwood chưa bao giờ chảy mạnh mẽ đến thế..."
        },
        new DialogueLine {
            speaker = "Elara",
            text = "Đủ rồi, Albrecht Wickwood! Ngươi đã nuốt chửng bảy cuộc đời. Bảy người con gái của chính dòng họ ngươi. Hôm nay, mọi chuyện kết thúc."
        },
        new DialogueLine {
            speaker = "Lord Wickwood",
            text = "Kết thúc...? Cháu vẫn chưa hiểu sao? Cây nến cháu cầm chưa bao giờ là vũ khí... Nó là CHÌA KHÓA. Mỗi thợ săn trở về, mỗi ngọn nến cháy hết... ta lại mạnh thêm một chút."
        },
        new DialogueLine {
            speaker = "Lord Wickwood",
            text = "Hội Canh Giữ không cử cháu đến để giết ta... Chúng MANG CHÁU VỀ cho ta. Như đã mang về bảy đứa trước. Chúng nuôi cháu, dạy cháu, rồi dâng cháu — đúng năm cháu hai mươi ba tuổi."
        },
        new DialogueLine {
            speaker = "Lord Wickwood",
            text = "Nhìn ta này, Vesper Wickwood... Ta đang quỳ trước cháu đây. Ngai vàng, lâu đài, sự bất tử — tất cả là của cháu. Cháu chỉ cần... đưa ngọn nến về nguồn của nó."
        },
    };

    [Header("Lựa chọn của Elara")]
    [Tooltip("Câu hỏi hiện phía trên các lựa chọn")]
    public string choicePrompt = "Elara sẽ trả lời thế nào?";

    public EndingChoice[] choices = new EndingChoice[]
    {
        // ----- 1. KẾT ÁNH SÁNG (cố định) -----
        new EndingChoice {
            choiceText = "Dòng họ Wickwood kết thúc tại đây — bằng ngọn lửa cuối cùng của nó.",
            epilogue = new DialogueLine[]
            {
                new DialogueLine {
                    speaker = "Elara",
                    text = "Bà nội đã tự đốt mình trong bóng tối để ta được sống. Giờ ta mới hiểu... Ánh sáng không phải thứ để giữ lấy. Nó là thứ để trao đi."
                },
                new DialogueLine {
                    speaker = "Lord Wickwood",
                    text = "Khoan... cháu định làm gì...? DỪNG LẠI! Cháu là TÀI SẢN của ta! Cháu chính là TA!"
                },
                new DialogueLine {
                    speaker = "Elara",
                    text = "Không. Ta là ánh sáng cuối cùng của dòng họ Wickwood. Và ánh sáng này... không thuộc về ngươi."
                },
            },
            endingTitle = "ÁNH SÁNG",
            endingNarration = "Elara áp Cây Nến Vesper vào ngực mình. Ngọn lửa lan ra — trắng xóa, ấm áp, không hề đau đớn.\n\nLord Wickwood gào lên rồi tan thành tro bụi. Một trăm ngọn ma trơi bay ra khỏi lâu đài, hướng về bầu trời như một dòng sao băng ngược. Trong ánh ban mai đầu tiên sau năm trăm năm, lâu đài Wickwood sụp đổ.\n\nTrên bia mộ thứ tám trong tu viện, có dòng chữ mới:\n\n\"Elara Vespera Wickwood.\nNgười cuối cùng của dòng họ.\nNgười đầu tiên chọn ánh sáng.\""
        },
        // ----- 2. KẾT BÓNG TỐI (cố định) -----
        new EndingChoice {
            choiceText = "Ngai vàng đó là của ta. Ta là Vesper Wickwood.",
            epilogue = new DialogueLine[]
            {
                new DialogueLine {
                    speaker = "Elara",
                    text = "Năm trăm năm... tám thế hệ... Ngươi nói đúng một điều, Albrecht: không ai trốn được cội nguồn của mình."
                },
                new DialogueLine {
                    speaker = "Lord Wickwood",
                    text = "Đúng vậy... đúng vậy... Về nhà đi, cháu yêu của ta..."
                },
                new DialogueLine {
                    speaker = "Elara",
                    text = "Nhưng ngươi nhầm một điều. Ngai vàng này không phải ngươi ban cho ta... Nó là CỦA TA. Và kẻ đầu tiên bóng tối này nuốt chửng — chính là ngươi."
                },
            },
            endingTitle = "BÓNG TỐI",
            endingNarration = "Elara ngồi lên ngai vàng. Cây Nến Vesper bùng cháy mãnh liệt hơn bao giờ hết — và bóng tối quỳ xuống dưới chân nó.\n\nỞ một nơi rất xa, một bé gái vừa chào đời trong một gia đình bình thường — mang trong mình dòng máu Wickwood mà không ai hay biết.\n\nTrong giấc ngủ đầu tiên của em, có tiếng thì thầm:\n\n\"Hẹn gặp lại sau 70 năm, cháu yêu.\""
        },
        // ----- 3. KẾT NGƯỜI CANH GIỮ (ẨN — cần nhặt đủ lửa) -----
        new EndingChoice {
            choiceText = "Ta không giết ngươi. Ta cũng không nhận ngai vàng. Ta sẽ ở lại — canh giữ nơi này, mãi mãi.",
            epilogue = new DialogueLine[]
            {
                new DialogueLine {
                    speaker = "Lord Wickwood",
                    text = "Cái gì...? Không giết... cũng không nhận lấy...? KHÔNG AI được phép từ chối dòng máu Wickwood!"
                },
                new DialogueLine {
                    speaker = "Elara",
                    text = "Một trăm đứa trẻ ngươi đã dâng đi — ta đã nhặt từng ngọn lửa, đưa từng đứa về với ánh sáng. Chúng dạy ta điều ngươi không bao giờ hiểu: bóng tối không cần bị tiêu diệt. Nó chỉ cần được thắp sáng."
                },
                new DialogueLine {
                    speaker = "Elara",
                    text = "Nếu ánh sáng cần một người canh giữ, thì người đó là ta. Không ai khác nữa phải đến đây."
                },
            },
            endingTitle = "NGƯỜI CANH GIỮ",
            endingNarration = "Elara ở lại. Không phải tù nhân — mà là Người Giữ Sáng mới của Wickwood Manor.\n\nTrong sảnh chính, cô ngồi giữa một trăm ngọn ma trơi nhỏ bay quanh như sao trời. Cây nến cháy bình yên. Cô khẽ hát bài hát ru bằng thứ ngôn ngữ cô chưa từng được học — ngôn ngữ của tổ tiên.\n\nVà lần đầu tiên trong đời,\nElara không còn cô đơn nữa."
        },
    };

    [Header("Điều kiện mở kết ẩn")]
    [Tooltip("Chỉ số (index) của lựa chọn ẩn trong mảng choices")]
    public int secretChoiceIndex = 2;
    [Tooltip("Số lửa (GameProgress.FlameCount) cần nhặt để mở lựa chọn ẩn — chỉnh cho khớp tổng số lửa trong game")]
    public int flamesRequiredForSecret = 75;

    [Header("Thời gian")]
    [Tooltip("Chờ bao lâu sau khi boss gục (chuỗi chết của boss đã chờ death anim rồi)")]
    public float startDelay = 0.5f;
    public float typeSpeed = 0.04f;

    [Header("Sau khi kết thúc")]
    [Tooltip("Tên scene Main Menu để quay về sau ending")]
    public string mainMenuSceneName = "MenuScreen";

    [Header("Âm thanh khi nói")]
    public AudioClip typingSound;
    [Range(0f, 1f)] public float voiceVolume = 0.7f;

    [Header("Hiển thị")]
    public int nameFontSize = 28;
    public int dialogueFontSize = 32;
    public Color textColor = Color.white;
    public Color nameColor = new Color(1f, 0.85f, 0.4f, 1f);
    public Color choiceNormalColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    public Color choiceSelectedColor = new Color(1f, 0.9f, 0.45f, 1f);
    public Color secretChoiceColor = new Color(0.55f, 0.9f, 1f, 1f);

    private enum Phase { Inactive, Intro, Choosing, Epilogue, EndingScreen }
    private Phase phase = Phase.Inactive;

    private DialogueLine[] currentLines;
    private int lineIndex = 0;
    private bool isStreaming = false;
    private Coroutine typingRoutine;

    private int chosenIndex = -1;
    private int selectedRow = 0;
    private int[] visibleChoiceIndices; // map hàng hiển thị -> index trong mảng choices

    private PlayerMovement player;
    private AudioSource audioSource;

    // UI
    private GameObject dialogueBox;
    private Text nameLabel;
    private Text dialogueLabel;
    private GameObject choiceBox;
    private Text choicePromptLabel;
    private Text[] choiceRowLabels;
    private Text flameCountLabel;
    private GameObject endingScreen;
    private Image endingBackground;
    private Text endingTitleLabel;
    private Text endingNarrationLabel;
    private Text endingHintLabel;

    /// <summary>Gọi từ VampireLordBoss sau khi animation chết chạy xong.</summary>
    public void Play()
    {
        if (phase != Phase.Inactive)
            return;
        StartCoroutine(PlaySequence());
    }

    void Update()
    {
        switch (phase)
        {
            case Phase.Intro:
            case Phase.Epilogue:
                if (PressedAdvance())
                {
                    if (isStreaming)
                        CompleteLine();
                    else
                        NextLine();
                }
                break;

            case Phase.Choosing:
                HandleChoiceInput();
                break;

            case Phase.EndingScreen:
                if (!isStreaming && PressedAdvance())
                {
                    GameProgress.Clear();
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(mainMenuSceneName);
                }
                else if (isStreaming && PressedAdvance())
                {
                    CompleteEndingNarration();
                }
                break;
        }
    }

    private bool PressedAdvance()
    {
        return Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetKeyDown(KeyCode.E);
    }

    private IEnumerator PlaySequence()
    {
        phase = Phase.Intro;

        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
            player = pm;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        yield return new WaitForSeconds(startDelay);

        if (player != null)
            player.SetInputLocked(true);

        BuildUI();
        dialogueBox.SetActive(true);

        currentLines = introDialogue;
        lineIndex = 0;
        ShowCurrentLine();
    }

    // ------------------------------------------------------------------
    // Thoại (dùng chung cho intro và epilogue)
    // ------------------------------------------------------------------
    private void NextLine()
    {
        lineIndex++;
        if (lineIndex >= currentLines.Length)
        {
            if (phase == Phase.Intro)
                ShowChoices();
            else if (phase == Phase.Epilogue)
                ShowEndingScreen();
        }
        else
        {
            ShowCurrentLine();
        }
    }

    private void ShowCurrentLine()
    {
        if (currentLines == null || currentLines.Length == 0)
        {
            NextLine();
            return;
        }

        DialogueLine line = currentLines[Mathf.Clamp(lineIndex, 0, currentLines.Length - 1)];
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

        DialogueLine line = currentLines[Mathf.Clamp(lineIndex, 0, currentLines.Length - 1)];
        dialogueLabel.text = line.text;
        StopVoice();
        isStreaming = false;
    }

    // ------------------------------------------------------------------
    // Bảng lựa chọn
    // ------------------------------------------------------------------
    private void ShowChoices()
    {
        phase = Phase.Choosing;
        dialogueBox.SetActive(false);

        bool secretUnlocked = GameProgress.FlameCount >= flamesRequiredForSecret;

        // Lập danh sách lựa chọn hiển thị (bỏ lựa chọn ẩn nếu chưa mở khóa)
        int count = 0;
        for (int i = 0; i < choices.Length; i++)
            if (i != secretChoiceIndex || secretUnlocked)
                count++;

        visibleChoiceIndices = new int[count];
        int row = 0;
        for (int i = 0; i < choices.Length; i++)
        {
            if (i == secretChoiceIndex && !secretUnlocked)
                continue;
            visibleChoiceIndices[row] = i;
            row++;
        }

        BuildChoiceUI(count);

        choicePromptLabel.text = choicePrompt;
        flameCountLabel.text = secretUnlocked
            ? $"Lửa đã thu thập: {GameProgress.FlameCount} — Một con đường khác đã mở ra..."
            : $"Lửa đã thu thập: {GameProgress.FlameCount}";

        selectedRow = 0;
        RefreshChoiceRows();
        choiceBox.SetActive(true);
    }

    private void HandleChoiceInput()
    {
        int rows = visibleChoiceIndices.Length;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedRow = (selectedRow - 1 + rows) % rows;
            RefreshChoiceRows();
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedRow = (selectedRow + 1) % rows;
            RefreshChoiceRows();
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmChoice(visibleChoiceIndices[selectedRow]);
        }
    }

    private void RefreshChoiceRows()
    {
        for (int row = 0; row < choiceRowLabels.Length; row++)
        {
            int choiceIdx = visibleChoiceIndices[row];
            bool isSecret = choiceIdx == secretChoiceIndex;
            bool isSelected = row == selectedRow;

            string prefix = isSelected ? "▶ " : "   ";
            string marker = isSecret ? "✦ " : ""; // ✦ đánh dấu lựa chọn ẩn
            choiceRowLabels[row].text = prefix + marker + choices[choiceIdx].choiceText;
            choiceRowLabels[row].color = isSelected
                ? (isSecret ? secretChoiceColor : choiceSelectedColor)
                : choiceNormalColor;
            choiceRowLabels[row].fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
        }
    }

    private void ConfirmChoice(int choiceIdx)
    {
        chosenIndex = choiceIdx;
        choiceBox.SetActive(false);

        choices[choiceIdx].onChosen?.Invoke();

        phase = Phase.Epilogue;
        dialogueBox.SetActive(true);
        currentLines = choices[choiceIdx].epilogue;
        lineIndex = 0;
        ShowCurrentLine();
    }

    // ------------------------------------------------------------------
    // Màn hình ending
    // ------------------------------------------------------------------
    private void ShowEndingScreen()
    {
        phase = Phase.EndingScreen;
        dialogueBox.SetActive(false);

        BuildEndingScreenUI();
        endingScreen.SetActive(true);
        StartCoroutine(PlayEndingScreen(choices[chosenIndex]));
    }

    private IEnumerator PlayEndingScreen(EndingChoice ending)
    {
        isStreaming = true;
        endingTitleLabel.text = "";
        endingNarrationLabel.text = "";
        endingHintLabel.gameObject.SetActive(false);

        // Màn hình tối dần
        float fadeDuration = 1.5f;
        float elapsed = 0f;
        Color bg = Color.black;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            bg.a = Mathf.Clamp01(elapsed / fadeDuration);
            endingBackground.color = bg;
            yield return null;
        }
        endingBackground.color = Color.black;

        yield return new WaitForSeconds(0.5f);
        endingTitleLabel.text = ending.endingTitle;
        yield return new WaitForSeconds(1f);

        // Đoạn văn kết chạy chữ chậm rãi
        StartVoice();
        string full = ending.endingNarration ?? "";
        for (int i = 0; i < full.Length; i++)
        {
            endingNarrationLabel.text += full[i];
            yield return new WaitForSeconds(typeSpeed);
        }
        StopVoice();

        isStreaming = false;
        endingHintLabel.gameObject.SetActive(true);
    }

    private void CompleteEndingNarration()
    {
        StopAllCoroutines();
        StopVoice();
        endingBackground.color = Color.black;
        endingTitleLabel.text = choices[chosenIndex].endingTitle;
        endingNarrationLabel.text = choices[chosenIndex].endingNarration;
        isStreaming = false;
        endingHintLabel.gameObject.SetActive(true);
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
    // Tự dựng UI lúc runtime
    // ------------------------------------------------------------------
    private Canvas canvas;
    private Font uiFont;

    private void BuildUI()
    {
        if (canvas != null)
            return;

        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasGO = new GameObject("EndingDialogueCanvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 130;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // --- Hộp thoại (giống các boss khác) ---
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

        GameObject nameGO = new GameObject("NameText");
        nameGO.transform.SetParent(dialogueBox.transform, false);
        nameLabel = nameGO.AddComponent<Text>();
        nameLabel.font = uiFont;
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

        GameObject dlgGO = new GameObject("DialogueText");
        dlgGO.transform.SetParent(dialogueBox.transform, false);
        dialogueLabel = dlgGO.AddComponent<Text>();
        dialogueLabel.font = uiFont;
        dialogueLabel.fontSize = dialogueFontSize;
        dialogueLabel.color = textColor;
        dialogueLabel.alignment = TextAnchor.UpperLeft;
        RectTransform dlgRT = dialogueLabel.rectTransform;
        dlgRT.anchorMin = new Vector2(0.05f, 0.08f);
        dlgRT.anchorMax = new Vector2(0.95f, 0.70f);
        dlgRT.offsetMin = Vector2.zero;
        dlgRT.offsetMax = Vector2.zero;

        GameObject contGO = new GameObject("ContinueHint");
        contGO.transform.SetParent(dialogueBox.transform, false);
        Text contLabel = contGO.AddComponent<Text>();
        contLabel.font = uiFont;
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

    private void BuildChoiceUI(int rowCount)
    {
        if (choiceBox != null)
            Destroy(choiceBox);

        float rowHeight = 64f;
        float boxHeight = 150f + rowCount * rowHeight;

        choiceBox = new GameObject("ChoiceBox");
        choiceBox.transform.SetParent(canvas.transform, false);
        Image bg = choiceBox.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.88f);
        RectTransform rt = bg.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 40f);
        rt.sizeDelta = new Vector2(1500, boxHeight);

        // Câu hỏi
        GameObject promptGO = new GameObject("Prompt");
        promptGO.transform.SetParent(choiceBox.transform, false);
        choicePromptLabel = promptGO.AddComponent<Text>();
        choicePromptLabel.font = uiFont;
        choicePromptLabel.fontStyle = FontStyle.Bold;
        choicePromptLabel.fontSize = nameFontSize;
        choicePromptLabel.color = nameColor;
        choicePromptLabel.alignment = TextAnchor.MiddleLeft;
        AddOutline(promptGO);
        RectTransform promptRT = choicePromptLabel.rectTransform;
        promptRT.anchorMin = new Vector2(0f, 1f);
        promptRT.anchorMax = new Vector2(1f, 1f);
        promptRT.pivot = new Vector2(0.5f, 1f);
        promptRT.anchoredPosition = new Vector2(0f, -14f);
        promptRT.sizeDelta = new Vector2(-150f, 44f);

        // Số lửa đã nhặt (góc phải trên)
        GameObject flameGO = new GameObject("FlameCount");
        flameGO.transform.SetParent(choiceBox.transform, false);
        flameCountLabel = flameGO.AddComponent<Text>();
        flameCountLabel.font = uiFont;
        flameCountLabel.fontSize = 22;
        flameCountLabel.color = new Color(1f, 0.75f, 0.35f, 0.9f);
        flameCountLabel.alignment = TextAnchor.MiddleRight;
        RectTransform flameRT = flameCountLabel.rectTransform;
        flameRT.anchorMin = new Vector2(0f, 1f);
        flameRT.anchorMax = new Vector2(1f, 1f);
        flameRT.pivot = new Vector2(0.5f, 1f);
        flameRT.anchoredPosition = new Vector2(-40f, -60f);
        flameRT.sizeDelta = new Vector2(-150f, 30f);

        // Các hàng lựa chọn
        choiceRowLabels = new Text[rowCount];
        for (int row = 0; row < rowCount; row++)
        {
            GameObject rowGO = new GameObject("Choice_" + row);
            rowGO.transform.SetParent(choiceBox.transform, false);
            Text label = rowGO.AddComponent<Text>();
            label.font = uiFont;
            label.fontSize = dialogueFontSize - 4;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            RectTransform rowRT = label.rectTransform;
            rowRT.anchorMin = new Vector2(0f, 1f);
            rowRT.anchorMax = new Vector2(1f, 1f);
            rowRT.pivot = new Vector2(0.5f, 1f);
            rowRT.anchoredPosition = new Vector2(20f, -104f - row * rowHeight);
            rowRT.sizeDelta = new Vector2(-160f, rowHeight);
            choiceRowLabels[row] = label;
        }

        // Gợi ý điều khiển
        GameObject hintGO = new GameObject("ControlHint");
        hintGO.transform.SetParent(choiceBox.transform, false);
        Text hint = hintGO.AddComponent<Text>();
        hint.font = uiFont;
        hint.fontSize = 20;
        hint.color = new Color(1f, 1f, 1f, 0.6f);
        hint.alignment = TextAnchor.LowerRight;
        hint.text = "[W/S] chọn   [Enter] xác nhận";
        RectTransform hintRT = hint.rectTransform;
        hintRT.anchorMin = new Vector2(0.05f, 0f);
        hintRT.anchorMax = new Vector2(0.97f, 0f);
        hintRT.pivot = new Vector2(0.5f, 0f);
        hintRT.anchoredPosition = new Vector2(0f, 8f);
        hintRT.sizeDelta = new Vector2(0f, 30f);

        choiceBox.SetActive(false);
    }

    private void BuildEndingScreenUI()
    {
        if (endingScreen != null)
            return;

        endingScreen = new GameObject("EndingScreen");
        endingScreen.transform.SetParent(canvas.transform, false);
        endingBackground = endingScreen.AddComponent<Image>();
        endingBackground.color = new Color(0f, 0f, 0f, 0f);
        RectTransform bgRT = endingBackground.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        GameObject titleGO = new GameObject("EndingTitle");
        titleGO.transform.SetParent(endingScreen.transform, false);
        endingTitleLabel = titleGO.AddComponent<Text>();
        endingTitleLabel.font = uiFont;
        endingTitleLabel.fontStyle = FontStyle.Bold;
        endingTitleLabel.fontSize = 64;
        endingTitleLabel.color = nameColor;
        endingTitleLabel.alignment = TextAnchor.MiddleCenter;
        endingTitleLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        RectTransform titleRT = endingTitleLabel.rectTransform;
        titleRT.anchorMin = new Vector2(0.5f, 0.82f);
        titleRT.anchorMax = new Vector2(0.5f, 0.82f);
        titleRT.pivot = new Vector2(0.5f, 0.5f);
        titleRT.anchoredPosition = Vector2.zero;
        titleRT.sizeDelta = new Vector2(1600, 90);

        GameObject narrGO = new GameObject("EndingNarration");
        narrGO.transform.SetParent(endingScreen.transform, false);
        endingNarrationLabel = narrGO.AddComponent<Text>();
        endingNarrationLabel.font = uiFont;
        endingNarrationLabel.fontSize = 30;
        endingNarrationLabel.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        endingNarrationLabel.alignment = TextAnchor.UpperCenter;
        endingNarrationLabel.lineSpacing = 1.25f;
        RectTransform narrRT = endingNarrationLabel.rectTransform;
        narrRT.anchorMin = new Vector2(0.5f, 0.16f);
        narrRT.anchorMax = new Vector2(0.5f, 0.74f);
        narrRT.pivot = new Vector2(0.5f, 1f);
        narrRT.anchoredPosition = Vector2.zero;
        narrRT.sizeDelta = new Vector2(1200, 0);

        GameObject hintGO = new GameObject("EndingHint");
        hintGO.transform.SetParent(endingScreen.transform, false);
        endingHintLabel = hintGO.AddComponent<Text>();
        endingHintLabel.font = uiFont;
        endingHintLabel.fontSize = 24;
        endingHintLabel.color = new Color(1f, 1f, 1f, 0.65f);
        endingHintLabel.alignment = TextAnchor.MiddleCenter;
        endingHintLabel.text = "[Enter] Về màn hình chính";
        RectTransform hintRT = endingHintLabel.rectTransform;
        hintRT.anchorMin = new Vector2(0.5f, 0.07f);
        hintRT.anchorMax = new Vector2(0.5f, 0.07f);
        hintRT.pivot = new Vector2(0.5f, 0.5f);
        hintRT.anchoredPosition = Vector2.zero;
        hintRT.sizeDelta = new Vector2(800, 40);
        endingHintLabel.gameObject.SetActive(false);

        endingScreen.SetActive(false);
    }

    private void AddOutline(GameObject go)
    {
        Outline o = go.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.85f);
        o.effectDistance = new Vector2(2f, -2f);
    }
}
