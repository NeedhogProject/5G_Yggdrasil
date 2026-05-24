/*
 * BlacksmithSystem.cs
 * 대장장이 NPC 패널 — 무기 선택 후 EnhancementSystem 패널로 넘겨 코인 플립 강화 진행
 * 담당: 김보민
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlacksmithSystem : MonoBehaviour
{
    [Header("UI 참조")]
    public GameObject blacksmithPanel;
    public TMP_Text dialogueText;
    public Button enhanceButton;
    public Button closeButton;

    [Header("강화 UI")]
    public TMP_Text currentLevelText;
    public TMP_Text successRateText;

    [Header("시스템 연동")]
    [SerializeField] private EnhancementSystem enhancementSystem;

    // WeaponData 대신 WeaponInstance 사용 (강화 단계는 런타임에 있음)
    private WeaponInstance _selectedWeapon = null;
    private bool _isOpen = false;

    private void Start()
    {
        blacksmithPanel.SetActive(false);

        enhanceButton.onClick.AddListener(OnEnhanceClicked);
        closeButton.onClick.AddListener(CloseBlacksmith);
    }

    // ─────────────────────── 열기 / 닫기 ───────────────────────

    public void OpenBlacksmith()
    {
        if (_isOpen == true)
        {
            return;
        }

        blacksmithPanel.SetActive(true);
        _isOpen = true;

        dialogueText.text = "어서오게! 무기를 벼릴 준비가 됐나?";

        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
    }

    public void CloseBlacksmith()
    {
        blacksmithPanel.SetActive(false);
        _isOpen = false;

        _selectedWeapon = null;

        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }

    // ─────────────────────── 무기 선택 ───────────────────────

    /// <summary>
    /// 인벤토리 슬롯에서 무기 클릭 시 외부에서 호출
    /// WeaponInstance 를 받아야 강화 단계 정보가 유지됨
    /// </summary>
    public void SelectWeapon(WeaponInstance weapon)
    {
        if (weapon == null)
        {
            dialogueText.text = "무기를 선택해주게.";
            return;
        }

        _selectedWeapon = weapon;
        UpdateUI();
    }

    // ─────────────────────── UI 갱신 ───────────────────────

    private void UpdateUI()
    {
        if (_selectedWeapon == null)
        {
            return;
        }

        int level = _selectedWeapon.EnhancementLevel;

        if (level >= 5)
        {
            currentLevelText.text = "현재 강화: +" + level.ToString() + " (최대)";
            successRateText.text = "더 이상 강화할 수 없다네.";
            enhanceButton.interactable = false;
            return;
        }

        currentLevelText.text = "현재 강화: +" + level.ToString();
        successRateText.text = "성공 확률: " + _selectedWeapon.CurrentSuccessRate.ToString("F0") + "%";
        enhanceButton.interactable = true;
    }

    // ─────────────────────── 강화 버튼 ───────────────────────

    private void OnEnhanceClicked()
    {
        if (_selectedWeapon == null)
        {
            dialogueText.text = "먼저 강화할 무기를 선택해주게.";
            return;
        }

        if (_selectedWeapon.EnhancementLevel >= 5)
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
        enhancementSystem.SelectWeapon(_selectedWeapon);
        enhancementSystem.OpenEnhancement();
    }
}