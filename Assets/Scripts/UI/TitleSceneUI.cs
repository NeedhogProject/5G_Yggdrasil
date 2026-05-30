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

    // 이어하기, 가장 최근에 저장된 슬롯을 자동으로 로드
    private void OnContinueClicked()
    {
        if (SaveSystem.Instance == null)
        {
            return;
        }
        if (GameManager.Instance == null)
        {
            return;
        }

        int latestSlot = FindLatestSaveSlot();
        if (latestSlot < 0)
        {
            Debug.LogWarning("[TitleSceneUI] 저장 데이터 없음");
            return;
        }

        GameManager.Instance.ContinueGame(latestSlot);
    }

    // 가장 최근에 저장된 슬롯 인덱스 반환, 없으면 -1
    private int FindLatestSaveSlot()
    {
        string latestDateTime = string.Empty;
        int latestSlot = -1;

        for (int i = 0; i < SaveSystem.SLOT_COUNT; i++)
        {
            if (SaveSystem.Instance.HasSave(i) == false)
            {
                continue;
            }

            SaveData meta = SaveSystem.Instance.GetSaveMeta(i);
            if (meta == null)
            {
                continue;
            }

            // saveDateTime 은 "yyyy-MM-dd HH:mm" 형식, 문자열 비교로 최신 판별 가능
            if (string.IsNullOrEmpty(latestDateTime) == true
                || string.Compare(meta.saveDateTime, latestDateTime) > 0)
            {
                latestDateTime = meta.saveDateTime;
                latestSlot = i;
            }
        }

        return latestSlot;
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