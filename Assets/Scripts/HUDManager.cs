using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUDManager
/// - 체력 / 방어력 / 정신력 바 실시간 갱신
/// - 데미지 비네트 (화면 테두리 빨개지는 효과)
///   · 체력 감소 시 강하게 / 정신력 감소 시 어둡게 (색상 구분)
///   · 정신력 임계값 이하 → 상시 맥동 효과
/// - 세트 효과 발동 상태 표시 (최대 2세트)
///   · 발동 중 : 강조 / 미발동 : 투명
///
/// [외부 호출]
///   HUDManager.Instance.UpdateHP(current, max);
///   HUDManager.Instance.UpdateDef(current, max);
///   HUDManager.Instance.UpdateSanity(current, max);
///   HUDManager.Instance.TriggerDamageVignette();
///   HUDManager.Instance.TriggerSanityVignette();
///   HUDManager.Instance.UpdateSetEffects(activeSetList);
/// </summary>

// ─────────────────────────────────────────
// 세트 효과 표시용 데이터
// ─────────────────────────────────────────

[System.Serializable]
public class SetEffectHUDEntry
{
    public string   setName;   // ex) "불 2세트"
    public string   description;
    public Color    color;     // 속성별 대표 색상
}

// ─────────────────────────────────────────
// HUDManager 본체
// ─────────────────────────────────────────

public class HUDManager : MonoBehaviour
{
    // ── 싱글톤 ──────────────────────────────
    public static HUDManager Instance { get; private set; }

    // ════════════════════════════════════════
    // Inspector 바인딩
    // ════════════════════════════════════════

    [Header("── 체력 바 ──────────────────────")]
    [SerializeField] private Slider   hpSlider;
    [SerializeField] private Image    hpFill;
    [SerializeField] private TMP_Text hpText;       // "75 / 100" 형태 (선택)
    [SerializeField] private Color    hpColorFull  = new Color(0.22f, 0.82f, 0.42f);
    [SerializeField] private Color    hpColorLow   = new Color(0.90f, 0.20f, 0.20f);
    [SerializeField] [Range(0f, 1f)] private float hpLowThreshold = 0.3f;

    [Header("── 방어력 바 ─────────────────────")]
    [SerializeField] private Slider   defSlider;
    [SerializeField] private Image    defFill;
    [SerializeField] private TMP_Text defText;
    [SerializeField] private Color    defColor     = new Color(0.40f, 0.70f, 1.00f);

    [Header("── 정신력 바 ─────────────────────")]
    [SerializeField] private Slider   sanitySlider;
    [SerializeField] private Image    sanityFill;
    [SerializeField] private TMP_Text sanityText;
    [SerializeField] private Color    sanityColorFull = new Color(0.70f, 0.45f, 1.00f);
    [SerializeField] private Color    sanityColorLow  = new Color(0.30f, 0.10f, 0.50f);
    [SerializeField] [Range(0f, 1f)] private float sanityLowThreshold  = 0.3f;
    [SerializeField] [Range(0f, 1f)] private float sanityPulseThreshold = 0.5f;

    [Header("── 바 감소 지연 (Delay Bar) ──────")]
    [Tooltip("체력 바 아래 서서히 줄어드는 흰색 지연 바")]
    [SerializeField] private Image hpDelayFill;
    [SerializeField] [Min(0f)] private float delayBarSpeed = 2.5f;

    [Header("── 데미지 비네트 ──────────────────")]
    [SerializeField] private Image    vignetteImage;        // 화면 테두리 Image (색상만 교체)
    [SerializeField] private Color    vignetteColorDamage   = new Color(0.75f, 0.00f, 0.00f, 0.55f);
    [SerializeField] private Color    vignetteColorSanity   = new Color(0.20f, 0.00f, 0.30f, 0.60f);
    [SerializeField] [Min(0f)] private float vignetteFadeInTime  = 0.08f;
    [SerializeField] [Min(0f)] private float vignetteHoldTime    = 0.12f;
    [SerializeField] [Min(0f)] private float vignetteFadeOutTime = 0.45f;
    [SerializeField] [Min(0f)] private float sanityPulseSpeed    = 1.8f;

    [Header("── 세트 효과 ────────────────────")]
    [SerializeField] private List<SetEffectSlotUI> setEffectSlots; // Inspector에서 2개 연결

    // ════════════════════════════════════════
    // 내부 상태
    // ════════════════════════════════════════

    // 현재 스탯값 (0~1 정규화)
    private float hpRatio     = 1f;
    private float defRatio    = 1f;
    private float sanityRatio = 1f;

    // 지연 바
    private float hpDelayRatio = 1f;

    // 비네트 코루틴
    private Coroutine vignetteCoroutine;
    private Coroutine sanityPulseCoroutine;

    // ════════════════════════════════════════
    // 초기화
    // ════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // 비네트 초기 투명
        if (vignetteImage != null)
        {
            vignetteImage.color = Color.clear;
            vignetteImage.raycastTarget = false;
        }

        // 방어력 바 색상 고정
        if (defFill != null) defFill.color = defColor;

        // 세트 슬롯 초기화
        foreach (var slot in setEffectSlots)
            slot?.SetInactive();
    }

    private void Update()
    {
        UpdateDelayBar();
    }

    // ════════════════════════════════════════
    // 스탯 업데이트 — 공개 API
    // ════════════════════════════════════════

    /// <summary>체력 바 갱신</summary>
    public void UpdateHP(float current, float max)
    {
        float prev = hpRatio;
        hpRatio = Mathf.Clamp01(current / max);

        SetSlider(hpSlider, hpRatio);
        SetText(hpText, current, max);
        hpFill.color = Color.Lerp(hpColorLow, hpColorFull, hpRatio);

        // 체력이 줄었을 때만 비네트 & 지연 바 갱신
        if (hpRatio < prev)
            TriggerDamageVignette();

        // 임계값 이하 → 경고 색상 강조
        if (hpFill != null)
            hpFill.color = hpRatio <= hpLowThreshold
                ? Color.Lerp(Color.red, hpColorLow, hpRatio / hpLowThreshold)
                : Color.Lerp(hpColorLow, hpColorFull, hpRatio);
    }

    /// <summary>방어력 바 갱신</summary>
    public void UpdateDef(float current, float max)
    {
        defRatio = Mathf.Clamp01(current / max);
        SetSlider(defSlider, defRatio);
        SetText(defText, current, max);
    }

    /// <summary>정신력 바 갱신</summary>
    public void UpdateSanity(float current, float max)
    {
        float prev = sanityRatio;
        sanityRatio = Mathf.Clamp01(current / max);

        SetSlider(sanitySlider, sanityRatio);
        SetText(sanityText, current, max);

        // 임계값 이하 → 경고 색상
        if (sanityFill != null)
            sanityFill.color = sanityRatio <= sanityLowThreshold
                ? Color.Lerp(Color.magenta, sanityColorLow, sanityRatio / sanityLowThreshold)
                : Color.Lerp(sanityColorLow, sanityColorFull, sanityRatio);

        // 감소 시 비네트
        if (sanityRatio < prev)
            TriggerSanityVignette();

        // 임계값 이하 → 맥동 시작 / 초과 → 맥동 중지
        if (sanityRatio <= sanityPulseThreshold)
            StartSanityPulse();
        else
            StopSanityPulse();
    }

    // ════════════════════════════════════════
    // 데미지 비네트 — 공개 API
    // ════════════════════════════════════════

    /// <summary>체력 피해 비네트 (빨간색)</summary>
    public void TriggerDamageVignette()
    {
        if (vignetteImage == null) return;
        if (vignetteCoroutine != null) StopCoroutine(vignetteCoroutine);
        vignetteCoroutine = StartCoroutine(PlayVignette(vignetteColorDamage));
    }

    /// <summary>정신력 피해 비네트 (보라색)</summary>
    public void TriggerSanityVignette()
    {
        if (vignetteImage == null) return;
        // 정신력 맥동 중이라면 잠시 강조만 하고 맥동으로 복귀
        if (vignetteCoroutine != null) StopCoroutine(vignetteCoroutine);
        vignetteCoroutine = StartCoroutine(PlayVignette(vignetteColorSanity));
    }

    // ════════════════════════════════════════
    // 세트 효과 — 공개 API
    // ════════════════════════════════════════

    /// <summary>
    /// 현재 발동 중인 세트 효과 목록으로 HUD 갱신
    /// list가 비어 있으면 슬롯 전부 비활성
    /// </summary>
    public void UpdateSetEffects(List<SetEffectHUDEntry> activeList)
    {
        for (int i = 0; i < setEffectSlots.Count; i++)
        {
            if (setEffectSlots[i] == null) continue;

            if (i < activeList.Count)
                setEffectSlots[i].SetActive(activeList[i]);
            else
                setEffectSlots[i].SetInactive();
        }
    }

    // ════════════════════════════════════════
    // 내부 유틸
    // ════════════════════════════════════════

    private void SetSlider(Slider slider, float ratio)
    {
        if (slider == null) return;
        slider.value = ratio;
    }

    private void SetText(TMP_Text label, float current, float max)
    {
        if (label == null) return;
        label.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    // ── 지연 바 ─────────────────────────────

    private void UpdateDelayBar()
    {
        if (hpDelayFill == null) return;

        // 지연 비율이 현재 HP보다 높으면 서서히 따라 내려옴
        if (hpDelayRatio > hpRatio)
        {
            hpDelayRatio = Mathf.MoveTowards(
                hpDelayRatio, hpRatio, delayBarSpeed * Time.deltaTime);
            hpDelayFill.fillAmount = hpDelayRatio;
        }
        else
        {
            // HP가 회복되면 즉시 맞춤
            hpDelayRatio = hpRatio;
            hpDelayFill.fillAmount = hpDelayRatio;
        }
    }

    // ── 비네트 코루틴 ────────────────────────

    private IEnumerator PlayVignette(Color targetColor)
    {
        // 페이드 인
        yield return FadeVignette(Color.clear, targetColor, vignetteFadeInTime);
        // 홀드
        yield return new WaitForSecondsRealtime(vignetteHoldTime);
        // 페이드 아웃
        yield return FadeVignette(targetColor, Color.clear, vignetteFadeOutTime);

        vignetteCoroutine = null;

        // 정신력 임계값 이하라면 맥동 재개
        if (sanityRatio <= sanityPulseThreshold)
            StartSanityPulse();
    }

    private IEnumerator FadeVignette(Color from, Color to, float duration)
    {
        if (duration <= 0f) { vignetteImage.color = to; yield break; }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            vignetteImage.color = Color.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        vignetteImage.color = to;
    }

    // ── 정신력 맥동 ──────────────────────────

    private void StartSanityPulse()
    {
        if (sanityPulseCoroutine != null) return; // 이미 실행 중
        sanityPulseCoroutine = StartCoroutine(SanityPulseLoop());
    }

    private void StopSanityPulse()
    {
        if (sanityPulseCoroutine == null) return;
        StopCoroutine(sanityPulseCoroutine);
        sanityPulseCoroutine = null;

        // 비네트가 재생 중이 아닐 때만 투명으로
        if (vignetteCoroutine == null && vignetteImage != null)
            vignetteImage.color = Color.clear;
    }

    private IEnumerator SanityPulseLoop()
    {
        // 정신력 임계값 이하인 동안 계속 맥동
        while (sanityRatio <= sanityPulseThreshold)
        {
            // 비네트 애니메이션이 진행 중이면 양보
            if (vignetteCoroutine != null)
            {
                yield return null;
                continue;
            }

            // 정신력이 낮을수록 강하게 (alpha 0.1 ~ 0.45)
            float intensity = Mathf.Lerp(0.45f, 0.10f, sanityRatio / sanityPulseThreshold);
            Color pulseColor = new Color(
                vignetteColorSanity.r,
                vignetteColorSanity.g,
                vignetteColorSanity.b,
                intensity);

            float t = Mathf.PingPong(Time.unscaledTime * sanityPulseSpeed, 1f);
            vignetteImage.color = Color.Lerp(Color.clear, pulseColor, t);
            yield return null;
        }

        // 루프 종료 후 정리
        if (vignetteCoroutine == null && vignetteImage != null)
            vignetteImage.color = Color.clear;

        sanityPulseCoroutine = null;
    }
}

// ═════════════════════════════════════════════
// 세트 효과 슬롯 UI 컴포넌트
// (HUD 프리팹 안에 세트 슬롯 개수만큼 배치)
// ═════════════════════════════════════════════

[System.Serializable]
public class SetEffectSlotUI : MonoBehaviour
{
    [Header("바인딩")]
    [SerializeField] private Image    iconBackground;
    [SerializeField] private TMP_Text setNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("발동 / 미발동 투명도")]
    [SerializeField] [Range(0f, 1f)] private float activeAlpha   = 1.0f;
    [SerializeField] [Range(0f, 1f)] private float inactiveAlpha = 0.25f;

    [Header("전환 속도")]
    [SerializeField] [Min(0f)] private float transitionSpeed = 6f;

    private float targetAlpha;
    private Coroutine transCoroutine;

    // ── 공개 메서드 ──────────────────────────

    public void SetActive(SetEffectHUDEntry entry)
    {
        if (setNameText    != null) setNameText.text    = entry.setName;
        if (descriptionText != null) descriptionText.text = entry.description;
        if (iconBackground  != null) iconBackground.color = entry.color;

        TransitionAlpha(activeAlpha);
    }

    public void SetInactive()
    {
        TransitionAlpha(inactiveAlpha);
    }

    // ── 내부 ─────────────────────────────────

    private void TransitionAlpha(float target)
    {
        if (canvasGroup == null) return;
        targetAlpha = target;
        if (transCoroutine != null) StopCoroutine(transCoroutine);
        transCoroutine = StartCoroutine(FadeAlpha(target));
    }

    private IEnumerator FadeAlpha(float target)
    {
        while (!Mathf.Approximately(canvasGroup.alpha, target))
        {
            canvasGroup.alpha = Mathf.MoveTowards(
                canvasGroup.alpha, target, transitionSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
        canvasGroup.alpha = target;
        transCoroutine = null;
    }
}
