using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class HoverLightEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [Tooltip("Kéo object dải sáng (LightBand) vào đây")]
    public Image lightBand;

    [Header("Animation Settings")]
    [Tooltip("Thời gian để dải sáng hiện ra và biến mất (giây)")]
    public float fadeDuration = 0.2f;

    [Tooltip("Kích thước dải sáng phình ra khi hover")]
    public Vector3 expandedScale = new Vector3(1.2f, 1f, 1f);

    [Header("Pulsing (Nhấp nháy) Settings")]
    [Tooltip("Độ sáng tối đa (Từ 0 đến 1)")]
    public float maxAlpha = 0.8f;

    [Tooltip("Độ sáng tối thiểu khi nhấp nháy (Từ 0 đến 1)")]
    public float minAlpha = 0.3f;

    [Tooltip("Tốc độ nhấp nháy (Càng lớn càng nhanh)")]
    public float pulseSpeed = 0.8f;

    private Coroutine currentCoroutine;
    private Vector3 originalScale = Vector3.one;
    private bool isHovering = false; // Biến kiểm tra xem chuột có đang nằm trên chữ không

    void Start()
    {
        if (lightBand != null)
        {
            SetAlpha(0f); // Tàng hình lúc ban đầu
            if (originalScale == Vector3.one) originalScale = lightBand.transform.localScale;
        }
    }

    // Khi chuột LƯỚT VÀO
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(HoverRoutine());
    }

    // Khi chuột RỜI KHỎI
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(FadeOutRoutine());
    }

    // Coroutine xử lý hiện ra và nhấp nháy
    private IEnumerator HoverRoutine()
    {
        if (lightBand == null) yield break;

        // --- GIAI ĐOẠN 1: Mọc ra và sáng lên ---
        float elapsedTime = 0f;
        Color startColor = lightBand.color;
        Vector3 startScale = lightBand.transform.localScale;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            t = t * t * (3f - 2f * t); // Mượt mà hơn

            SetAlpha(Mathf.Lerp(startColor.a, maxAlpha, t));
            lightBand.transform.localScale = Vector3.Lerp(startScale, expandedScale, t);
            yield return null;
        }

        // --- GIAI ĐOẠN 2: Nhấp nháy liên tục khi giữ chuột ---
        while (isHovering)
        {
            // Hàm PingPong sẽ chạy lên chạy xuống liên tục giữa 0 và (max-min)
            float currentAlpha = Mathf.PingPong(Time.time * pulseSpeed, maxAlpha - minAlpha) + minAlpha;
            SetAlpha(currentAlpha);
            yield return null;
        }
    }

    // Coroutine xử lý mờ đi khi bỏ chuột ra
    private IEnumerator FadeOutRoutine()
    {
        if (lightBand == null) yield break;

        float elapsedTime = 0f;
        Color startColor = lightBand.color;
        Vector3 startScale = lightBand.transform.localScale;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            t = t * t * (3f - 2f * t);

            SetAlpha(Mathf.Lerp(startColor.a, 0f, t)); // Fade về 0
            lightBand.transform.localScale = Vector3.Lerp(startScale, originalScale, t); // Thu nhỏ lại
            yield return null;
        }

        SetAlpha(0f);
        lightBand.transform.localScale = originalScale;
    }

    // Hàm phụ trợ để đổi Alpha nhanh gọn
    private void SetAlpha(float alpha)
    {
        if (lightBand == null) return;
        Color c = lightBand.color;
        c.a = alpha;
        lightBand.color = c;
    }
}