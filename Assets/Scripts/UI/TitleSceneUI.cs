// TitleSceneUI.cs
// 타이틀 씬 버튼 컨트롤러
// 새 게임, 이어하기, 설정, 종료 버튼을 처리

using UnityEngine;
using UnityEngine.UI;

public class TitleSceneUI : MonoBehaviour
{
    [Header("타이틀 버튼")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("설정 UI 참조")]
    [SerializeField] private SettingsUI settingsUI;

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
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }

    // 새 게임 시작, 마을 씬으로 전환
    private void OnNewGameClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
    }

    // 이어하기, 슬롯 선택 UI 연결 예정
    private void OnContinueClicked()
    {
        // 슬롯 선택 패널 추후 연결
    }

    // 설정창 열기
    private void OnSettingsClicked()
    {
        if (settingsUI == null)
        {
            return;
        }

        // 이미 열려있으면 중복 호출 무시
        if (settingsUI.IsOpen == true)
        {
            return;
        }

        settingsUI.Open();
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