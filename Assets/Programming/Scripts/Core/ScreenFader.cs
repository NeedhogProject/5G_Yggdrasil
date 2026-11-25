// ScreenFader.cs
// 화면 페이드 인/아웃 오버레이 — 순간이동 연출용
// 런타임에 전용 Canvas + 검은 Image 를 직접 생성하므로 GameCore 에 컴포넌트만 부착하면 됨
// FadeOutIn(액션): 어두워짐, 가장 어두운 시점에 액션 실행(이동 등), 다시 밝아짐

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("페이드 시간 (초)")]
    [Tooltip("어두워지는 시간과 밝아지는 시간 각각에 적용")]
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("암전 유지 시간 (초)")]
    [Tooltip("가장 어두운 상태를 유지하는 시간. 0 이면 즉시 밝아짐")]
    [SerializeField] private float holdDuration = 0.1f;

    private Image _fadeImage;
    private Coroutine _fadeCoroutine;

    /// <summary>페이드 진행 중 여부 (중복 실행 방지용)</summary>
    public bool IsFading => _fadeCoroutine != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateOverlay();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // 전용 캔버스와 전체 화면 검은 이미지를 생성
    private void CreateOverlay()
    {
        GameObject objCanvas = new GameObject("FadeCanvas");
        objCanvas.transform.SetParent(transform, false);

        Canvas canvas = objCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // 모든 UI 위에 렌더

        GameObject objImage = new GameObject("FadeImage");
        objImage.transform.SetParent(objCanvas.transform, false);

        _fadeImage = objImage.AddComponent<Image>();
        _fadeImage.color = new Color(0f, 0f, 0f, 0f);
        _fadeImage.raycastTarget = false;

        // 화면 전체로 늘리기
        RectTransform trRect = _fadeImage.rectTransform;
        trRect.anchorMin = Vector2.zero;
        trRect.anchorMax = Vector2.one;
        trRect.offsetMin = Vector2.zero;
        trRect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 어두워짐, 가장 어두운 시점에 액션 실행, 다시 밝아짐
    /// 이미 페이드 중이면 무시
    /// </summary>
    public void FadeOutIn(System.Action _onBlack)
    {
        if (IsFading == true)
        {
            return;
        }
        _fadeCoroutine = StartCoroutine(FadeOutInRoutine(_onBlack));
    }

    private IEnumerator FadeOutInRoutine(System.Action _onBlack)
    {
        yield return FadeAlpha(0f, 1f);

        if (_onBlack != null)
        {
            _onBlack.Invoke();
        }

        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }

        yield return FadeAlpha(1f, 0f);

        _fadeCoroutine = null;
    }

    // 알파를 지정 구간으로 fadeDuration 동안 변경 (timeScale 무관)
    private IEnumerator FadeAlpha(float _fFrom, float _fTo)
    {
        float fElapsed = 0f;
        while (fElapsed < fadeDuration)
        {
            fElapsed += Time.unscaledDeltaTime;
            float fAlpha = Mathf.Lerp(_fFrom, _fTo, fElapsed / fadeDuration);
            _fadeImage.color = new Color(0f, 0f, 0f, fAlpha);
            yield return null;
        }
        _fadeImage.color = new Color(0f, 0f, 0f, _fTo);
    }
}
