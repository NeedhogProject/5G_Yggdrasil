// SettingsUI.cs
// 오디오 설정 패널 (마스터/BGM/효과음 볼륨)
// 슬라이더 변경 시 AudioManager/AudioListener 에 반영, 라벨과 수치를 별도 텍스트로 표시
// 탭 모드(부모가 SetActive 토글) 와 팝업 모드(Open/Close) 둘 다 지원
// 게임 저장 버튼 추가 — SaveSlotPanel 을 열어줌 (마을에서만 활성화)
// 마을 판단은 CurrentFloor == 0 으로 함 (설정창 열 때 Paused 상태가 되므로 CurrentState 못 씀)

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    [Header("패널 루트 (팝업 모드 전용, 탭으로 쓰면 비움)")]
    [SerializeField] private GameObject panelRoot;

    [Header("볼륨 슬라이더")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("라벨 텍스트 (어떤 볼륨인지 표시)")]
    [SerializeField] private TMP_Text masterLabelText;
    [SerializeField] private TMP_Text bgmLabelText;
    [SerializeField] private TMP_Text sfxLabelText;

    [Header("라벨 문구")]
    [SerializeField] private string masterLabel = "마스터 볼륨";
    [SerializeField] private string bgmLabel = "BGM";
    [SerializeField] private string sfxLabel = "효과음";

    [Header("수치 텍스트 (% 실시간 표시)")]
    [SerializeField] private TMP_Text masterValueText;
    [SerializeField] private TMP_Text bgmValueText;
    [SerializeField] private TMP_Text sfxValueText;

    [Header("닫기 버튼 (팝업 모드 전용)")]
    [SerializeField] private Button closeButton;

    [Header("팝업 전체 루트 (있으면 그걸 닫고 없으면 자기 자신)")]
    [SerializeField] private GameObject popupRoot;

    [Header("게임 저장 버튼 (마을에서만 활성화)")]
    [SerializeField] private Button saveGameButton;
    [SerializeField] private SaveSlotPanelUI saveSlotPanel;

    [Header("타이틀로 나가기 버튼")]
    [SerializeField] private Button exitToTitleButton;

    private bool isOpen = false;

    private void Start()
    {
        // 라벨 문구 적용 (인스펙터 문구를 텍스트에 반영)
        ApplyLabels();

        // 슬라이더 콜백 등록
        if (masterSlider != null)
        {
            masterSlider.minValue = 0f;
            masterSlider.maxValue = 1f;
            masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        }

        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.onValueChanged.AddListener(OnBGMSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        }

        // 닫기 버튼 (팝업 모드)
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        // 게임 저장 버튼 — SaveSlotPanel 열기
        if (saveGameButton != null)
        {
            saveGameButton.onClick.AddListener(OnSaveGameClicked);
        }

        // 타이틀로 나가기 버튼
        if (exitToTitleButton != null)
        {
            exitToTitleButton.onClick.AddListener(OnExitToTitleClicked);
        }
    }

    // 타이틀로 나가기 버튼 클릭
    // 타이틀 화면이면 게임 종료, 게임 중이면 타이틀로 이동
    private void OnExitToTitleClicked()
    {
        // 일시정지 상태였으면 시간 복구 (씬 넘어가기 전)
        Time.timeScale = 1f;

        // 이미 타이틀 화면이면 게임 종료
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Title)
        {
            QuitGame();
            return;
        }

        // 게임 중이면 타이틀로 이동
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToTitle();
        }
    }

    // 게임 종료 (에디터에서는 플레이 정지, 빌드에서는 앱 종료)
    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 게임 저장 버튼 클릭 — 저장 슬롯 패널 열기
    private void OnSaveGameClicked()
    {
        // 마을(0층)에서만 저장 가능
        // 설정창 열 때 일시정지(Paused) 되므로 CurrentState 대신 CurrentFloor 로 판단
        if (GameManager.Instance != null && GameManager.Instance.CurrentFloor != 0)
        {
            Debug.Log("[SettingsUI] 마을에서만 저장할 수 있습니다.");
            return;
        }

        if (saveSlotPanel != null)
        {
            saveSlotPanel.Open();
        }
        else
        {
            Debug.LogWarning("[SettingsUI] SaveSlotPanel 이 연결되지 않음");
        }
    }

    // 게임 저장 버튼 활성화 상태 갱신 (마을에서만 활성화)
    private void UpdateSaveButtonState()
    {
        if (saveGameButton == null)
        {
            return;
        }

        // 마을(0층)에서만 활성화
        bool isInTown = GameManager.Instance != null && GameManager.Instance.CurrentFloor == 0;
        saveGameButton.interactable = isInTown;
    }

    // 오디오 설정을 기본값(100%) 으로 초기화
    // 슬라이더 값을 바꾸면 onValueChanged 가 자동으로 호출돼서
    // AudioManager / AudioListener / 수치 텍스트가 같이 갱신됨
    public void ResetAudio()
    {
        if (masterSlider != null)
        {
            masterSlider.value = 1f;
        }

        if (bgmSlider != null)
        {
            bgmSlider.value = 1f;
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = 1f;
        }
    }

    // ── 여기부터 오디오 저장/복원 기능 ──

    // 스냅샷 보관용 변수 (설정창 열 때 찍어둔 볼륨)
    private float _snapMaster = 1f;
    private float _snapBGM = 1f;
    private float _snapSFX = 1f;

    // 사진 찍기 — 설정창 열릴 때 현재 볼륨을 기억해둠
    public void TakeAudioSnapshot()
    {
        _snapMaster = AudioListener.volume;

        if (AudioManager.Instance != null)
        {
            _snapBGM = AudioManager.Instance.BGMVolume;
            _snapSFX = AudioManager.Instance.SFXVolume;
        }
    }

    // 진짜 저장 — 확인 버튼 누를 때
    public void ApplyAudio()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SaveSettings();
        }
    }

    // 되돌리기 — 확인 안 누르고 나갈 때, 찍어둔 사진으로 복원
    public void RestoreAudioSnapshot()
    {
        Debug.Log("[ResetAudio] 초기화 버튼 눌림!");

        if (masterSlider != null)
        {
            masterSlider.value = 1f;
        }

        if (bgmSlider != null)
        {
            bgmSlider.value = 1f;
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = 1f;
        }
    }

    // 탭이 켜질 때마다 현재 볼륨으로 슬라이더 동기화 + 저장 버튼 상태 갱신
    private void OnEnable()
    {
        SyncSlidersToCurrentVolume();
        UpdateSaveButtonState();
    }

    // 팝업 모드용 열기
    public void Open()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        isOpen = true;
        SyncSlidersToCurrentVolume();
        UpdateSaveButtonState();
    }

    // 팝업 모드용 닫기
    public void Close()
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
        else if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        isOpen = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SaveSettings();
        }

        // 설정창은 일시정지(timeScale=0) 상태로 열리므로, 닫을 때 게임을 반드시 재개한다.
        // 이걸 안 하면 패널만 사라지고 게임이 멈춘 채로 남아 클릭/이동이 먹지 않는다.
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Paused)
        {
            GameManager.Instance.Resume();
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    public bool IsOpen
    {
        get { return isOpen; }
    }

    // 라벨 텍스트에 인스펙터 문구를 채워넣음
    private void ApplyLabels()
    {
        if (masterLabelText != null)
        {
            masterLabelText.text = masterLabel;
        }

        if (bgmLabelText != null)
        {
            bgmLabelText.text = bgmLabel;
        }

        if (sfxLabelText != null)
        {
            sfxLabelText.text = sfxLabel;
        }
    }

    // 현재 볼륨 값으로 슬라이더 + 수치 텍스트 동기화
    private void SyncSlidersToCurrentVolume()
    {
        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(AudioListener.volume);
        }

        if (AudioManager.Instance != null)
        {
            if (bgmSlider != null)
            {
                bgmSlider.SetValueWithoutNotify(AudioManager.Instance.BGMVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SFXVolume);
            }
        }

        RefreshValueText();
    }

    // 마스터 볼륨 변경, AudioListener.volume 으로 게임 전체 볼륨 조절
    private void OnMasterSliderChanged(float value)
    {
        AudioListener.volume = value;
        RefreshValueText();
    }

    // BGM 슬라이더 변경
    private void OnBGMSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(value);
        }

        RefreshValueText();
    }

    // 효과음 슬라이더 변경
    private void OnSFXSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }

        RefreshValueText();
    }

    // 수치 텍스트를 슬라이더 값에 맞춰 % 로 표시
    private void RefreshValueText()
    {
        if (masterValueText != null && masterSlider != null)
        {
            int percent = Mathf.RoundToInt(masterSlider.value * 100f);
            masterValueText.text = percent.ToString() + "%";
        }

        if (bgmValueText != null && bgmSlider != null)
        {
            int percent = Mathf.RoundToInt(bgmSlider.value * 100f);
            bgmValueText.text = percent.ToString() + "%";
        }

        if (sfxValueText != null && sfxSlider != null)
        {
            int percent = Mathf.RoundToInt(sfxSlider.value * 100f);
            sfxValueText.text = percent.ToString() + "%";
        }
    }
}