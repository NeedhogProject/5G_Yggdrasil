// TitleSceneUI.cs
// 타이틀 씬 버튼 컨트롤러
// 싱글 플레이, 멀티 플레이, 종료 버튼과 방 코드 입력 창을 처리
// 설정 버튼은 인스펙터 OnClick으로 KeySettingPanel을 직접 SetActive하므로 여기서 처리하지 않음
// 싱글은 Host 로 방을 만들고 저장이 있으면 이어하기, 없으면 새 캐릭터로 시작 (Host 실패 시 오프라인)
// 멀티는 방 코드를 입력해 Client 로 참가 (저장이 없으면 새 캐릭터, 마을에 저장된 캐릭터만 참가 가능)

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleSceneUI : MonoBehaviour
{
    [Header("타이틀 버튼")]
    [SerializeField] private Button singlePlayButton;
    [SerializeField] private Button multiPlayButton;
    [SerializeField] private Button quitButton;

    [Header("방 코드 입력 창")]
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Button joinConfirmButton;
    [SerializeField] private Button joinCancelButton;
    [SerializeField] private TMP_Text joinMessageText;   // 접속 중, 실패 안내

    private void Start()
    {
        // 버튼 콜백 등록
        if (singlePlayButton != null)
        {
            singlePlayButton.onClick.AddListener(OnSinglePlayClicked);
        }
        if (multiPlayButton != null)
        {
            multiPlayButton.onClick.AddListener(OnMultiPlayClicked);
        }
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
        if (joinConfirmButton != null)
        {
            joinConfirmButton.onClick.AddListener(OnJoinConfirmClicked);
        }
        if (joinCancelButton != null)
        {
            joinCancelButton.onClick.AddListener(OnJoinCancelClicked);
        }
        if (joinPanel != null)
        {
            joinPanel.SetActive(false);
        }
    }

    // 싱글 플레이, Host 접속 후 이어하기 또는 새 캐릭터 (실패해도 오프라인으로 시작)
    private void OnSinglePlayClicked()
    {
        if (NetworkLauncher.Instance == null)
        {
            Debug.LogWarning("[TitleSceneUI] NetworkLauncher 가 없어 오프라인으로 시작합니다. GameCore 를 확인하세요.");
            StartSingleGame();
            return;
        }
        if (NetworkLauncher.Instance.IsStarting == true)
        {
            return;
        }

        SetButtonsInteractable(false);
        NetworkLauncher.Instance.StartHost(OnHostStarted);
    }

    private void OnHostStarted(bool _bOnline)
    {
        StartSingleGame();
    }

    private void StartSingleGame()
    {
        if (GameManager.Instance == null)
        {
            return;
        }
        if (SaveSystem.HasSave == true)
        {
            GameManager.Instance.ContinueGame();
        }
        else
        {
            GameManager.Instance.StartNewGame();
        }
    }

    // 멀티 플레이, 방 코드 입력 창 열기
    private void OnMultiPlayClicked()
    {
        if (joinPanel == null)
        {
            return;
        }
        joinPanel.SetActive(true);
        SetJoinMessage("");
        if (roomCodeInput != null)
        {
            roomCodeInput.text = "";
            roomCodeInput.ActivateInputField();
        }
    }

    private void OnJoinConfirmClicked()
    {
        if (NetworkLauncher.Instance == null || NetworkLauncher.Instance.IsStarting == true)
        {
            return;
        }

        // 저장된 캐릭터가 던전에 있으면 참가 불가
        if (SaveSystem.HasSave == true)
        {
            SaveSystem.SaveData data = SaveSystem.Load();
            if (data != null && data.floor != 0)
            {
                SetJoinMessage("마을에 저장된 캐릭터만 참가할 수 있습니다");
                return;
            }
        }

        SetButtonsInteractable(false);
        SetJoinMessage("접속 중...");
        string sCode = roomCodeInput != null ? roomCodeInput.text : "";
        NetworkLauncher.Instance.JoinRoom(sCode, OnJoinFinished);
    }

    private void OnJoinFinished(string _sError)
    {
        if (_sError != null)
        {
            SetJoinMessage(_sError);
            SetButtonsInteractable(true);
            return;
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartAsJoiner();
        }
    }

    private void OnJoinCancelClicked()
    {
        if (NetworkLauncher.Instance != null && NetworkLauncher.Instance.IsStarting == true)
        {
            return;
        }
        if (joinPanel != null)
        {
            joinPanel.SetActive(false);
        }
    }

    // 접속 중에는 버튼 잠금
    private void SetButtonsInteractable(bool _bInteractable)
    {
        Button[] buttons = { singlePlayButton, multiPlayButton, quitButton, joinConfirmButton, joinCancelButton };
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                button.interactable = _bInteractable;
            }
        }
    }

    private void SetJoinMessage(string _sMessage)
    {
        if (joinMessageText != null)
        {
            joinMessageText.text = _sMessage;
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
