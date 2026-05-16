using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 대장장이 NPC 패널.
/// 무기 선택 후 EnhancementSystem 패널로 넘겨 코인 플립 강화를 진행한다.
/// </summary>
public class BlacksmithSystem : MonoBehaviour
{
    [Header("UI 참조")]
    public GameObject blacksmithPanel;
    public TMP_Text   dialogueText;
    public Button     enhanceButton;
    public Button     closeButton;

    [Header("강화 UI")]
    public TMP_Text currentLevelText;
    public TMP_Text successRateText;

    [Header("시스템 연동")]
    [SerializeField] private EnhancementSystem enhancementSystem;

    private WeaponData selectedWeapon;
    private bool isOpen = false;

    private void Start()
    {
        blacksmithPanel.SetActive(false);

        enhanceButton.onClick.AddListener(OnEnhanceClicked);
        closeButton.onClick.AddListener(CloseBlacksmith);
    }

    public void OpenBlacksmith()
    {
        if (isOpen)
        {
            return;
        }
        blacksmithPanel.SetActive(true);
        isOpen = true;

        dialogueText.text = "어서오게! 무기를 벼릴 준비가 됐나?";

        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
    }

    public void CloseBlacksmith()
    {
        blacksmithPanel.SetActive(false);
        isOpen = false;

        selectedWeapon = null;

        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }

    // 인벤토리 슬롯 클릭 시 외부에서 호출
    public void SelectWeapon(WeaponData weapon)
    {
        if (weapon == null)
        {
            dialogueText.text = "무기를 선택해주게.";
            return;
        }

        selectedWeapon = weapon;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (selectedWeapon == null)
        {
            return;
        }

        int level = selectedWeapon.EnhancementLevel;

        if (level >= 5)
        {
            currentLevelText.text      = $"현재 강화: +{level} (최대)";
            successRateText.text       = "더 이상 강화할 수 없다네.";
            enhanceButton.interactable = false;
            return;
        }

        currentLevelText.text      = $"현재 강화: +{level}";
        successRateText.text       = $"성공 확률: {selectedWeapon.CurrentSuccessRate}%";
        enhanceButton.interactable = true;
    }

    private void OnEnhanceClicked()
    {
        if (selectedWeapon == null)
        {
            dialogueText.text = "먼저 강화할 무기를 선택해주게.";
            return;
        }

        if (selectedWeapon.EnhancementLevel >= 5)
        {
            dialogueText.text = "이미 최대 강화 단계일세.";
            return;
        }

        if (enhancementSystem == null)
        {
            Debug.LogWarning("[BlacksmithSystem] EnhancementSystem 참조가 없습니다.");
            return;
        }

        // 대장간 패널 닫고 코인 플립 강화 패널 열기
        blacksmithPanel.SetActive(false);
        enhancementSystem.SelectWeapon(selectedWeapon);
        enhancementSystem.OpenEnhancement();
    }
}
