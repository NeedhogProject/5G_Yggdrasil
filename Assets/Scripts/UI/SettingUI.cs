// SettingsUI.cs
// 타이틀 및 인게임에서 공용으로 호출되는 설정창
// BGM/SFX 볼륨 슬라이더를 AudioManager에 반영하고 PlayerPrefs로 저장

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI: MonoBehaviour
{
    [Header("패널 루트")]
    [SerializeField] private GameObject panelRoot;

    [Header("볼륨 슬라이더")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("볼륨 수치 텍스트")]
    [SerializeField] private TMP_Text MasterValueText;
    [SerializeField] private TMP_Text bgmValueText;
    [SerializeField] private TMP_Text sfxValueText;


    [Header("확인 버튼")]
    [SerializeField] private Button saveButton;

    [Header("취소 버튼")]
    [SerializeField] private Button closeButton;

    // 현재 열려있는 상태
    private bool isOpen = false;

private void Start()
{
    // 시작 시 꺼진 상태로 유지
    if (panelRoot != null)
    {
        panelRoot.SetActive(false);
    }

    // 슬라이더 콜백 등록
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

    // 닫기 버튼 콜백
    if (closeButton != null)
    {
        closeButton.onClick.AddListener(Close);
    }
}

// 설정창 열기, 타이틀 설정 버튼이 호출
public void Open()
{
    if (panelRoot == null)
    {
        return;
    }

    panelRoot.SetActive(true);
    isOpen = true;

    // 현재 저장된 볼륨 값으로 슬라이더 초기화
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

// 설정창 닫기
public void Close()
{
    if (panelRoot == null)
    {
        return;
    }

    panelRoot.SetActive(false);
    isOpen = false;

    // 닫을 때 PlayerPrefs 강제 저장
    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.SaveSettings();
    }
}

// 설정창이 열려있는지 외부 조회용
public bool IsOpen
{
    get { return isOpen; }
}

// BGM 슬라이더 변경 시
private void OnBGMSliderChanged(float value)
{
    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.SetBGMVolume(value);
    }
    RefreshValueText();
}

// SFX 슬라이더 변경 시
private void OnSFXSliderChanged(float value)
{
    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.SetSFXVolume(value);
    }
    RefreshValueText();
}

// 0~1 값을 0~100 정수로 표시
private void RefreshValueText()
{
    if (bgmValueText != null && bgmSlider != null)
    {
        int percent = Mathf.RoundToInt(bgmSlider.value * 100f);
        bgmValueText.text = percent.ToString();
    }
    if (sfxValueText != null && sfxSlider != null)
    {
        int percent = Mathf.RoundToInt(sfxSlider.value * 100f);
        sfxValueText.text = percent.ToString();
    }
}

// ESC 키로도 닫기 가능
private void Update()
{
    if (isOpen == false)
    {
        return;
    }

    if (Input.GetKeyDown(KeyCode.Escape) == true)
    {
        Close();
    }
}
}