// TitleSceneUI.cs
// 타이틀 씬 버튼 컨트롤러
// 게임 시작, 이어하기, 종료 버튼을 처리
// 설정 버튼은 인스펙터 OnClick으로 KeySettingPanel을 직접 SetActive하므로 여기서 처리하지 않음
// 이어하기는 저장 파일이 있을 때만 표시 (저장 슬롯은 없음)

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
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
            continueButton.gameObject.SetActive(SaveSystem.HasSave);
        }
    }

    // 이어하기, 저장된 층으로 전환
    private void OnContinueClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ContinueGame();
        }
    }

    // 게임 시작, 마을 씬으로 전환
    private void OnNewGameClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
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