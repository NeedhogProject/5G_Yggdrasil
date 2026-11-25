/*
 * CoinFlipUI.cs
 * 강화 시도 시 코인 앞면/뒷면 연출 및 결과 표시
 * 담당: 김보민
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class CoinFlipUI : MonoBehaviour
{
    public static CoinFlipUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject coinObject;
    public Image coinImage;
    public Sprite headsSprite; // 앞면 (성공)
    public Sprite tailsSprite; // 뒷면 (실패)

    [Header("Animation Settings")]
    public float flipDuration = 2f;
    // 초당 앞뒤 전환 횟수 (높을수록 빠르게 돌아가 보임)
    public float flipFrequency = 8f;
    public AnimationCurve flipCurve;

    [Header("Result Display")]
    public GameObject resultPanel;
    public TMP_Text resultText;
    public Color successColor = Color.green;
    public Color failColor = Color.red;

    [Header("Scale Animation")]
    // 애니메이션 중 코인 크기 진동폭
    public float scaleAmplitude = 0.15f;

    private bool _isFlipping = false;

    // 애니메이션 완료 후 결과를 전달받을 콜백
    // true = 성공(앞면), false = 실패(뒷면)
    private System.Action<bool> _onComplete = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        coinObject.SetActive(false);
        resultPanel.SetActive(false);
    }

    // ─────────────────────── 공개 API ───────────────────────

    /// <summary>
    /// 코인 플립 연출 시작
    /// successRate: 0~1 범위 성공 확률
    /// onComplete: 애니메이션 완료 후 호출 (true = 성공, false = 실패)
    /// 강화 실제 처리는 onComplete 콜백 안에서 해야 함
    /// </summary>
    public bool PlayCoinFlip(float successRate, System.Action<bool> onComplete = null)
    {
        if (_isFlipping == true)
        {
            return false;
        }

        // 결과를 미리 결정해두고 애니메이션 후 콜백으로 전달
        bool result = Random.value <= successRate;
        _onComplete = onComplete;

        StartCoroutine(CoinFlipAnimation(result));
        return true;
    }

    /// <summary>외부에서 강제로 닫을 때 사용 (씬 전환 등)</summary>
    public void ForceHide()
    {
        StopAllCoroutines();
        coinObject.SetActive(false);
        resultPanel.SetActive(false);
        _isFlipping = false;
        _onComplete = null;
    }

    // ─────────────────────── 애니메이션 코루틴 ───────────────────────

    private IEnumerator CoinFlipAnimation(bool result)
    {
        _isFlipping = true;
        coinObject.SetActive(true);
        resultPanel.SetActive(false);

        AudioManager.Instance?.PlaySFX(SFXClip.CoinFlip);

        float elapsed = 0f;
        float spriteTimer = 0f;
        // 앞면부터 시작
        bool showingHeads = true;
        coinImage.sprite = headsSprite;

        Vector3 originalScale = coinObject.transform.localScale;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            spriteTimer += Time.deltaTime;

            float progress = elapsed / flipDuration;

            // flipCurve: 처음엔 빠르게, 끝으로 갈수록 느려지도록 설정
            float currentFrequency = flipFrequency * (flipCurve != null ? flipCurve.Evaluate(progress) : 1f);
            float interval = currentFrequency > 0f ? 1f / currentFrequency : 0.1f;

            // 일정 간격마다 앞뒷면 전환
            if (spriteTimer >= interval)
            {
                spriteTimer = 0f;
                showingHeads = showingHeads == false;
                coinImage.sprite = showingHeads == true ? headsSprite : tailsSprite;
            }

            // 코인 크기 진동 (돌아가는 느낌)
            float scaleX = 1f + Mathf.Sin(elapsed * currentFrequency * Mathf.PI) * scaleAmplitude;
            coinObject.transform.localScale = new Vector3(
                originalScale.x * scaleX,
                originalScale.y,
                originalScale.z);

            yield return null;
        }

        // 크기 원복
        coinObject.transform.localScale = originalScale;

        // 최종 결과 스프라이트 고정
        if (result == true)
        {
            coinImage.sprite = headsSprite;
            ShowResult("성공!", successColor);
            AudioManager.Instance?.PlaySFX(SFXClip.EnhanceSuccess);
        }
        else
        {
            coinImage.sprite = tailsSprite;
            ShowResult("실패...", failColor);
            AudioManager.Instance?.PlaySFX(SFXClip.EnhanceFail);
        }

        // 결과 표시 후 콜백 실행 (강화 실제 처리 시점)
        if (_onComplete != null)
        {
            _onComplete.Invoke(result);
            _onComplete = null;
        }

        yield return new WaitForSeconds(1.5f);

        coinObject.SetActive(false);
        resultPanel.SetActive(false);

        _isFlipping = false;
    }

    // ─────────────────────── 내부 유틸 ───────────────────────

    private void ShowResult(string message, Color color)
    {
        resultPanel.SetActive(true);

        if (resultText != null)
        {
            resultText.text = message;
            resultText.color = color;
        }
    }
}