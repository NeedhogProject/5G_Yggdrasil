// TitleSceneUI.cs
// 타이틀 씬 버튼 컨트롤러
// 새 게임, 이어하기, 종료 버튼을 처리
// 설정 버튼은 인스펙터 OnClick으로 KeySettingPanel을 직접 SetActive하므로 여기서 처리하지 않음

using UnityEngine;
using UnityEngine.UI;

public class TitleSceneUI : MonoBehaviour
{
    [Header("타이틀 버튼")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button quitButton;

    [Header("이어하기 슬롯 선택 패널")]
    [SerializeField] private SaveSlotPanelUI saveSlotPanel;

    private void Start()
    {
        // 버튼 콜백 등록
        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(OnNewGameClicked);
        }
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        // 저장 슬롯이 하나도 없으면 이어하기 비활성화
        RefreshContinueButton();
    }

    // 저장 데이터 존재 여부에 따라 이어하기 버튼 활성 / 비활성
    private void RefreshContinueButton()
    {
        if (continueButton == null)
        {
            return;
        }
        if (SaveSystem.Instance == null)
        {
            continueButton.interactable = false;
            return;
        }

        bool hasAnySave = false;
        for (int i = 0; i < SaveSystem.SLOT_COUNT; i++)
        {
            if (SaveSystem.Instance.HasSave(i) == true)
            {
                hasAnySave = true;
                break;
            }
        }
        continueButton.interactable = hasAnySave;
    }

    // 새 게임 시작, 마을 씬으로 전환
    private void OnNewGameClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
    }

    // 이어하기 — 저장 슬롯 패널을 불러오기 모드로 열기
    // 플레이어가 카드에서 불러올 슬롯을 직접 선택
    // public 으로 둬서 Inspector On Click 으로도 연결 가능
    public void OnContinueClicked()
    {
        Debug.Log("[TitleSceneUI] 이어하기 클릭됨");

        if (saveSlotPanel == null)
        {
            Debug.LogWarning("[TitleSceneUI] SaveSlotPanel 이 연결되지 않음");
            return;
        }

        saveSlotPanel.OpenForLoad();
    }

    // 게임 종료
    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}