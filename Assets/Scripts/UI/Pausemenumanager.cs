/*
 * PauseMenuManager.cs
 * ESC 키 입력 처리 — 다른 UI 창이 없을 때만 설정창(KeySettingPanel) 열고 닫기
 * 담당: 김보민
 */

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ESC 키로 설정창을 여닫는 매니저
///
/// [동작]
/// - 인벤토리/상점/창고/저장슬롯 등 다른 창이 떠있으면 ESC 무시 (각 창이 자기 ESC 처리)
/// - 아무 창도 없으면 ESC 로 설정창 열기
/// - 설정창이 떠있으면 ESC 로 설정창 닫기
///
/// [씬 설정]
/// Title 씬에 빈 오브젝트 만들고 부착 (DontDestroyOnLoad 로 유지)
/// settingPanel = KeySettingPanel 연결
/// 감시할 다른 창들을 watchedPanels 에 등록
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    // 싱글턴
    public static PauseMenuManager Instance { get; private set; }

    [Header("설정 패널 (ESC 로 여닫을 대상)")]
    [SerializeField] private GameObject settingPanel;

    [Header("감시할 다른 창들 (이 중 하나라도 켜져있으면 ESC 무시)")]
    [SerializeField] private GameObject[] watchedPanels;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // 키보드가 없으면 무시
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame == false)
        {
            return;
        }

        HandleEscape();
    }

    // ESC 입력 처리
    private void HandleEscape()
    {
        if (settingPanel == null)
        {
            return;
        }

        // 설정창이 이미 열려있으면 닫기
        if (settingPanel.activeSelf == true)
        {
            CloseSetting();
            return;
        }

        // 다른 창이 열려있으면 ESC 무시 (그 창이 자기 ESC 처리)
        if (IsAnyWatchedPanelOpen() == true)
        {
            return;
        }

        // 아무것도 안 열려있으면 설정창 열기
        OpenSetting();
    }

    // 감시 대상 창 중 하나라도 켜져있는지
    private bool IsAnyWatchedPanelOpen()
    {
        if (watchedPanels == null)
        {
            return false;
        }

        for (int i = 0; i < watchedPanels.Length; i++)
        {
            if (watchedPanels[i] != null && watchedPanels[i].activeSelf == true)
            {
                return true;
            }
        }

        return false;
    }

    // 설정창 열기
    private void OpenSetting()
    {
        settingPanel.SetActive(true);

        // 게임 일시정지 (마을/던전일 때만)
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying == true)
        {
            GameManager.Instance.Pause();
        }
    }

    // 설정창 닫기
    private void CloseSetting()
    {
        settingPanel.SetActive(false);

        // 일시정지 해제
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Paused)
        {
            GameManager.Instance.Resume();
        }
    }
}