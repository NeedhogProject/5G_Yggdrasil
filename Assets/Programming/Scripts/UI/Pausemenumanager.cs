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
/// [타이밍 처리]
/// - 인벤/창고는 자기 Update 에서 ESC 로 닫히므로, PauseMenuManager 가 검사할 땐
///   이미 꺼져있어서 못 잡는 문제가 있음
/// - 그래서 매 프레임 감시 창 상태를 기록해두고, ESC 누른 순간
///   "이번 프레임 또는 직전 프레임에 창이 켜져있었으면" 설정창을 안 엶
///
/// [실행 순서]
/// - [DefaultExecutionOrder(1000)] 로 늦게 실행 (각 창 ESC 먼저 처리)
///
/// [씬 설정]
/// Title 씬에 빈 오브젝트 만들고 부착 (DontDestroyOnLoad 로 유지)
/// settingPanel = KeySettingPanel 연결
/// watchedPanelNames 에 감시할 창들의 오브젝트 이름 등록 (실제 토글되는 패널 이름)
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
        "InventoryPanel",
        "ShopMenuPanel",
        "ShopUI",
        "StoragePanel",
        "SaveSlotPanel"
    };

    // 직전 프레임에 감시 창이 켜져있었는지 기록
    private bool _watchedOpenLastFrame = false;

    /// <summary>설정창이 열려있는지 (인벤/창고/상점 입력 차단 판단용)</summary>
    public bool IsSettingOpen
    {
        get
        {
            if (settingPanel == null)
            {
                return false;
            }

            return settingPanel.activeSelf;
        }
    }

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
        // 키보드가 없으면 이번 프레임 상태만 기록하고 종료
        if (Keyboard.current == null)
        {
            _watchedOpenLastFrame = false;
            return;
        }

        // ESC 가 눌렸으면 먼저 판단 (이번 프레임 + 직전 프레임 상태 모두 고려)
        if (Keyboard.current.escapeKey.wasPressedThisFrame == true)
        {
            HandleEscape();
        }

        // 이번 프레임의 감시 창 상태를 다음 프레임을 위해 기록
        _watchedOpenLastFrame = IsAnyWatchedPanelOpen();
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

        // 이번 프레임 또는 직전 프레임에 다른 창이 켜져있었으면 설정창 안 엶
        // (인벤/창고가 이번 프레임에 ESC 로 막 닫혔어도, 직전 프레임엔 켜져있었음)
        bool watchedNow = IsAnyWatchedPanelOpen();

        if (watchedNow == true || _watchedOpenLastFrame == true)
        {
            return;
        }

        // 아무 창도 없었으면 설정창 열기
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

            if (IsPanelActiveByName(panelName) == true)
            {
                return true;
            }
        }

        return false;
    }

    // 이름이 일치하면서 화면에 실제 보이는(activeInHierarchy) 오브젝트가 있는지
    // 비활성 포함 전체 탐색 후 활성 여부 확인
    private bool IsPanelActiveByName(string panelName)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();

        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform t = allTransforms[i];

            if (t.name != panelName)
            {
                continue;
            }

            // 씬에 실제 배치된 오브젝트만 (프리팹 에셋 제외)
            if (t.gameObject.scene.IsValid() == false)
            {
                continue;
            }

            if (t.gameObject.activeInHierarchy == true)
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
