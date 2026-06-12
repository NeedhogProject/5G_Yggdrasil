/*
 * HUDManager.cs
 * 체력 / 정신력 바 실시간 갱신
 * 데미지 비네트 / 정신력 맥동 / 세트 효과 표시
 * 담당: 김보민
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 세트 효과 표시용 데이터
[System.Serializable]
public class SetEffectHUDEntry
{
    public string setName;
    public string description;
    public Color color;
}

public class HUDManager : MonoBehaviour
{
    // ── 싱글톤 ────────────────────────────────────
    public static HUDManager Instance { get; private set; }

    // ── 체력 바 ───────────────────────────────────
    [Header("체력 바")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Image hpFill;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Color hpColorFull = new Color(0.22f, 0.82f, 0.42f);
    [SerializeField] private Color hpColorLow = new Color(0.90f, 0.20f, 0.20f);
    [SerializeField][Range(0f, 1f)] private float hpLowThreshold = 0.3f;

    // ── 정신력 바 ─────────────────────────────────
    [Header("정신력 바")]
    [SerializeField] private Slider sanitySlider;
    [SerializeField] private Image sanityFill;
    [SerializeField] private TMP_Text sanityText;
    [SerializeField] private Color sanityColorFull = new Color(0.70f, 0.45f, 1.00f);
    [SerializeField] private Color sanityColorLow = new Color(0.30f, 0.10f, 0.50f);
    [SerializeField][Range(0f, 1f)] private float sanityLowThreshold = 0.3f;
    [SerializeField][Range(0f, 1f)] private float sanityPulseThreshold = 0.5f;

    // ── 지연 바 ───────────────────────────────────
    [Header("바 감소 지연 (Delay Bar)")]
    [SerializeField] private Image hpDelayFill;
    [SerializeField][Min(0f)] private float delayBarSpeed = 2.5f;

    // ── 데미지 비네트 ─────────────────────────────
    [Header("데미지 비네트")]
    [SerializeField] private Image vignetteImage;
    [SerializeField] private Color vignetteColorDamage = new Color(0.75f, 0.00f, 0.00f, 0.55f);
    [SerializeField] private Color vignetteColorSanity = new Color(0.20f, 0.00f, 0.30f, 0.60f);
    [SerializeField][Min(0f)] private float vignetteFadeInTime = 0.08f;
    [SerializeField][Min(0f)] private float vignetteHoldTime = 0.12f;
    [SerializeField][Min(0f)] private float vignetteFadeOutTime = 0.45f;
    [SerializeField][Min(0f)] private float sanityPulseSpeed = 1.8f;

    // ── 세트 효과 ─────────────────────────────────
    [Header("세트 효과")]
    [SerializeField] private List<SetEffectSlotUI> setEffectSlots;

    // ── 내부 상태 ─────────────────────────────────
    private float hpRatio = 1f;
    private float sanityRatio = 1f;
    private float hpDelayRatio = 1f;

    private Coroutine vignetteCoroutine;
    private Coroutine sanityPulseCoroutine;

    // ──────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        // 씬 전환/오브젝트 파괴 시 PlayerStats 이벤트에서 안전하게 해제
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnHealthChanged -= HandlePlayerHealthChanged;
            PlayerStats.Instance.OnMentalChanged -= HandlePlayerMentalChanged;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (vignetteImage != null)
        {
            vignetteImage.color = Color.clear;
            vignetteImage.raycastTarget = false;
        }

        foreach (SetEffectSlotUI slot in setEffectSlots)
        {
            if (slot != null)
            {
                slot.SetInactive();
            }
        }

        // PlayerStats 이벤트 구독 및 초기값 반영
        // PlayerStats 가 [DefaultExecutionOrder(-100)] 으로 먼저 Awake 됨이 보장됨
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnHealthChanged += HandlePlayerHealthChanged;
            PlayerStats.Instance.OnMentalChanged += HandlePlayerMentalChanged;

            UpdateHP(PlayerStats.Instance.Health, PlayerStats.MAX_STAT);
            UpdateSanity(PlayerStats.Instance.Mental, PlayerStats.MAX_STAT);
        }
    }

    // ── PlayerStats 이벤트 핸들러 ─────────────────

    private void HandlePlayerHealthChanged(float hp)
    {
        UpdateHP(hp, PlayerStats.MAX_STAT);
    }

    private void HandlePlayerMentalChanged(float men)
    {
        UpdateSanity(men, PlayerStats.MAX_STAT);
    }

    private void Update()
    {
        UpdateDelayBar();
    }

    // ── 스탯 갱신 공개 API ────────────────────────

    // 체력 바 갱신 (PlayerStats에서 호출)
    public void UpdateHP(float current, float max)
    {
        float prev = hpRatio;
        hpRatio = Mathf.Clamp01(current / max);

        SetSlider(hpSlider, hpRatio);
        SetText(hpText, current, max);

        // 체력 비율에 따라 색상 결정
        if (hpFill != null)
        {
            bool isDangerZone = hpRatio <= hpLowThreshold;
            if (isDangerZone == true)
            {
                hpFill.color = Color.Lerp(Color.red, hpColorLow, hpRatio / hpLowThreshold);
            }
            else
            {
                hpFill.color = Color.Lerp(hpColorLow, hpColorFull, hpRatio);
            }
        }

        // 체력이 줄었을 때만 비네트 발동
        if (hpRatio < prev)
        {
            TriggerDamageVignette();
        }
    }

    // 정신력 바 갱신 (PlayerStats에서 호출)
    public void UpdateSanity(float current, float max)
    {
        float prev = sanityRatio;
        sanityRatio = Mathf.Clamp01(current / max);

        SetSlider(sanitySlider, sanityRatio);
        SetText(sanityText, current, max);

        // 정신력 비율에 따라 색상 결정
        if (sanityFill != null)
        {
            bool isLowZone = sanityRatio <= sanityLowThreshold;
            if (isLowZone == true)
            {
                sanityFill.color = Color.Lerp(Color.magenta, sanityColorLow, sanityRatio / sanityLowThreshold);
            }
            else
            {
                sanityFill.color = Color.Lerp(sanityColorLow, sanityColorFull, sanityRatio);
            }
        }

        // 정신력이 줄었을 때 비네트 발동
        if (sanityRatio < prev)
        {
            TriggerSanityVignette();
        }

        // 임계값 이하: 맥동 시작 / 초과: 맥동 중지
        if (sanityRatio <= sanityPulseThreshold)
        {
            StartSanityPulse();
        }
        else
        {
            StopSanityPulse();
        }
    }

    // ── 비네트 공개 API ───────────────────────────

    // 체력 피해 비네트 (빨간색)
    public void TriggerDamageVignette()
    {
        if (vignetteImage == null)
        {
            return;
        }
        if (vignetteCoroutine != null)
        {
            StopCoroutine(vignetteCoroutine);
        }
        vignetteCoroutine = StartCoroutine(PlayVignette(vignetteColorDamage));
    }

    // 정신력 피해 비네트 (보라색)
    public void TriggerSanityVignette()
    {
        if (vignetteImage == null)
        {
            return;
        }
        if (vignetteCoroutine != null)
        {
            StopCoroutine(vignetteCoroutine);
        }
        vignetteCoroutine = StartCoroutine(PlayVignette(vignetteColorSanity));
    }

    // ── 세트 효과 공개 API ────────────────────────

    // 현재 발동 중인 세트 효과 목록으로 HUD 갱신
    public void UpdateSetEffects(List<SetEffectHUDEntry> activeList)
    {
        for (int i = 0; i < setEffectSlots.Count; i++)
        {
            if (setEffectSlots[i] == null)
            {
                continue;
            }

            if (i < activeList.Count)
            {
                setEffectSlots[i].SetActive(activeList[i]);
            }
            else
            {
                setEffectSlots[i].SetInactive();
            }
        }
    }

    // ── 내부 유틸 ─────────────────────────────────

    private void SetSlider(Slider slider, float ratio)
    {
        if (slider == null)
        {
            return;
        }
        slider.value = ratio;
    }

    private void SetText(TMP_Text label, float current, float max)
    {
        if (label == null)
        {
            return;
        }
        label.text = Mathf.CeilToInt(current).ToString() + " / " + Mathf.CeilToInt(max).ToString();
    }

    // ── 지연 바 ───────────────────────────────────

    private void UpdateDelayBar()
    {
        if (hpDelayFill == null)
        {
            return;
        }

        if (hpDelayRatio > hpRatio)
        {
            hpDelayRatio = Mathf.MoveTowards(
                hpDelayRatio, hpRatio, delayBarSpeed * Time.deltaTime);
            hpDelayFill.fillAmount = hpDelayRatio;
        }
        else
        {
            hpDelayRatio = hpRatio;
            hpDelayFill.fillAmount = hpDelayRatio;
        }
    }

    // ── 비네트 코루틴 ─────────────────────────────

    private IEnumerator PlayVignette(Color targetColor)
    {
        yield return FadeVignette(Color.clear, targetColor, vignetteFadeInTime);
        yield return new WaitForSecondsRealtime(vignetteHoldTime);
        yield return FadeVignette(targetColor, Color.clear, vignetteFadeOutTime);

        vignetteCoroutine = null;

        // 정신력 임계값 이하라면 맥동 재개
        if (sanityRatio <= sanityPulseThreshold)
        {
            StartSanityPulse();
        }
    }

    private IEnumerator FadeVignette(Color from, Color to, float duration)
    {
        if (duration <= 0f)
        {
            vignetteImage.color = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            vignetteImage.color = Color.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        vignetteImage.color = to;
    }

    // ── 정신력 맥동 ───────────────────────────────

    private void StartSanityPulse()
    {
        // 비네트 이미지가 미할당이면 맥동 자체를 건너뜀
        if (vignetteImage == null)
        {
            return;
        }
        if (sanityPulseCoroutine != null)
        {
            return;
        }
        sanityPulseCoroutine = StartCoroutine(SanityPulseLoop());
    }

    private void StopSanityPulse()
    {
        if (sanityPulseCoroutine == null)
        {
            return;
        }
        StopCoroutine(sanityPulseCoroutine);
        sanityPulseCoroutine = null;

        if (vignetteCoroutine == null && vignetteImage != null)
        {
            vignetteImage.color = Color.clear;
        }
    }

    private IEnumerator SanityPulseLoop()
    {
        while (sanityRatio <= sanityPulseThreshold)
        {
            // 비네트 재생 중이면 양보
            if (vignetteCoroutine != null)
            {
                yield return null;
                continue;
            }

            // 정신력이 낮을수록 강하게 맥동
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

        if (vignetteCoroutine == null && vignetteImage != null)
        {
            vignetteImage.color = Color.clear;
        }
        sanityPulseCoroutine = null;
    }
}

// ── 세트 효과 슬롯 UI 컴포넌트 ───────────────────

[System.Serializable]
public class SetEffectSlotUI : MonoBehaviour
{
    [Header("바인딩")]
    [SerializeField] private Image iconBackground;
    [SerializeField] private TMP_Text setNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("발동 / 미발동 투명도")]
    [SerializeField][Range(0f, 1f)] private float activeAlpha = 1.0f;
    [SerializeField][Range(0f, 1f)] private float inactiveAlpha = 0.25f;

    [Header("전환 속도")]
    [SerializeField][Min(0f)] private float transitionSpeed = 6f;

    private float _targetAlpha;
    private Coroutine _transCoroutine;

    // ── 공개 메서드 ───────────────────────────────

    public void SetActive(SetEffectHUDEntry entry)
    {
        if (setNameText != null)
        {
            setNameText.text = entry.setName;
        }
        if (descriptionText != null)
        {
            descriptionText.text = entry.description;
        }
        if (iconBackground != null)
        {
            iconBackground.color = entry.color;
        }

        TransitionAlpha(activeAlpha);
    }

    public void SetInactive()
    {
        TransitionAlpha(inactiveAlpha);
    }

    // ── 내부 ──────────────────────────────────────

    private void TransitionAlpha(float target)
    {
        if (canvasGroup == null)
        {
            return;
        }
        _targetAlpha = target;
        if (_transCoroutine != null)
        {
            StopCoroutine(_transCoroutine);
        }
        _transCoroutine = StartCoroutine(FadeAlpha(target));
    }

    private IEnumerator FadeAlpha(float target)
    {
        while (Mathf.Approximately(canvasGroup.alpha, target) == false)
        {
            canvasGroup.alpha = Mathf.MoveTowards(
                canvasGroup.alpha, target, transitionSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
        canvasGroup.alpha = target;
        _transCoroutine = null;
    }
}