using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioSettingUI : MonoBehaviour
{
    [Header("패널 루트")]
    [SerializeField] private GameObject panelRoot;

    [Header("볼륨 슬라이더")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("볼륨 수치 텍스트")]
    [SerializeField] private TMP_Text masterValueText;
    [SerializeField] private TMP_Text bgmValueText;
    [SerializeField] private TMP_Text sfxValueText;

    [Header("확인 버튼")]
    [SerializeField] private Button saveButton;

    [Header("취소 버튼")]
    [SerializeField] private Button closeButton;

    private bool isOpen = false;

    private void Start()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        // BGM 슬라이더
        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.onValueChanged.AddListener(OnBGMSliderChanged);
        }

        // SFX 슬라이더
        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        }

        // 저장 버튼
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(Close);
        }

        // 닫기 버튼
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
    }

    public void Open()
    {
        if (panelRoot == null)
            return;

        panelRoot.SetActive(true);
        isOpen = true;

        if (AudioManager.Instance != null)
        {
            if (bgmSlider != null)
            {
                bgmSlider.SetValueWithoutNotify(
                    AudioManager.Instance.BGMVolume
                );
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(
                    AudioManager.Instance.SFXVolume
                );
            }
        }

        RefreshValueText();
    }

    public void Close()
    {
        if (panelRoot == null)
            return;

        panelRoot.SetActive(false);
        isOpen = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SaveSettings();
        }
    }

    public bool IsOpen
    {
        get { return isOpen; }
    }

    private void OnBGMSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(value);
        }

        RefreshValueText();
    }

    private void OnSFXSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }

        RefreshValueText();
    }

    private void RefreshValueText()
    {
        if (bgmValueText != null && bgmSlider != null)
        {
            int percent =
                Mathf.RoundToInt(bgmSlider.value * 100f);

            bgmValueText.text = percent.ToString();
        }

        if (sfxValueText != null && sfxSlider != null)
        {
            int percent =
                Mathf.RoundToInt(sfxSlider.value * 100f);

            sfxValueText.text = percent.ToString();
        }
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }
}