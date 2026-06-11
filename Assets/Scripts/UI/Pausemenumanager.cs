/*
 * PauseMenuManager.cs
 * ESC 키 입력 처리 — 다른 UI 창이 없을 때만 설정창(KeySettingPanel) 열고 닫기
 * 담당: 김보민
 */

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// ESC 키로 설정창을 여닫는 매니저
///
/// [동작]
/// - 인벤토리/상점/창고 등 다른 창이 떠있으면 ESC 무시 (각 창이 자기 ESC 처리)
/// - 아무 창도 없으면 ESC 로 설정창 열기
/// - 설정창이 떠있으면 ESC 로 설정창 닫기
///
/// [감시 창 탐지]
/// - 드래그 연결 대신 오브젝트 이름으로 자동 탐지 (씬 전환에도 작동)
/// - ESC 누른 순간에만 검사하므로 성능 부담 없음
///
/// [실행 순서]
/// - Script Execution Order 를 뒤로 미뤄 각 창의 ESC 처리가 먼저 일어나게 함
///   (인스펙터에서 직접 설정하거나 아래 DefaultExecutionOrder 속성 사용)
///
/// [씬 설정]
/// Title 씬에 빈 오브젝트 만들고 부착 (DontDestroyOnLoad 로 유지)
/// settingPanel = KeySettingPanel 연결
/// watchedPanelNames 에 감시할 창들의 오브젝트 이름 등록
/// </summary>
[DefaultExecutionOrder(1000)]
public class PauseMenuManager : MonoBehaviour
{
    // 싱글턴
    public static PauseMenuManager Instance { get; private set; }

    [Header("설정 패널 (ESC 로 여닫을 대상)")]
    [SerializeField] private GameObject settingPanel;

    [Header("감시할 창 이름들 (이 중 하나라도 켜져있으면 ESC 무시)")]
    [SerializeField]
    private List<string> watchedPanelNames = new List<string>
    {
        "Inventory_Canvas",
        "ShopMenuPanel",
        "ShopUI",
        "StoragePanel",
        "StorageUI",
        "SaveSlotPanel"
    };

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
        Debug.Log("[PauseMenuManager] ESC 감지됨");

        if (settingPanel == null)
        {
            Debug.LogWarning("[PauseMenuManager] settingPanel 이 null - 연결 끊김");
            return;
        }

        // 설정창이 이미 열려있으면 닫기
        if (settingPanel.activeSelf == true)
        {
            Debug.Log("[PauseMenuManager] 설정창 열려있음 - 닫기");
            CloseSetting();
            return;
        }

        // 다른 창이 열려있으면 ESC 무시 (그 창이 자기 ESC 처리)
        if (IsAnyWatchedPanelOpen() == true)
        {
            return;
        }

        // 아무것도 안 열려있으면 설정창 열기
        Debug.Log("[PauseMenuManager] 설정창 열기");
        OpenSetting();
    }

    // 감시 대상 창 중 하나라도 켜져있는지 (이름으로 탐지)
    private bool IsAnyWatchedPanelOpen()
    {
        for (int i = 0; i < watchedPanelNames.Count; i++)
        {
            string panelName = watchedPanelNames[i];

            if (string.IsNullOrEmpty(panelName) == true)
            {
                continue;
            }

            GameObject panel = GameObject.Find(panelName);

            // GameObject.Find 는 비활성 오브젝트를 못 찾으므로
            // 찾아졌다는 것 자체가 활성 상태라는 뜻
            if (panel != null && panel.activeInHierarchy == true)
            {
                Debug.Log("[PauseMenuManager] 감시 창 열림 감지: " + panelName + " → ESC 무시");
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