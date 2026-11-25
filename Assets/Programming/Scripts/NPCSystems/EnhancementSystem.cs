/*
 * EnhancementSystem.cs
 * 대장장이 NPC 강화 UI — 코인 플립 연출 후 WeaponInstance 강화 처리
 * 담당: 김보민
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnhancementSystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject enhancementPanel;
    public TMP_Text dialogueText;
    public Button enhanceButton;
    public Button closeButton;

    [Header("Enhancement UI")]
    public Transform equipmentSlot;
    public TMP_Text currentStatsText;
    public TMP_Text nextStatsText;
    public TMP_Text successRateText;
    public Slider successRateSlider;

    [Header("Coin Flip UI")]
    public CoinFlipUI coinFlipUI;

    [Header("Enhancement Settings")]
    public int baseCost = 100;

    // WeaponData 대신 WeaponInstance 사용 (강화 단계는 런타임에 있음)
    private WeaponInstance _selectedWeapon = null;
    private bool _isEnhancing = false;
    private bool _isOpen = false;

    private void Start()
    {
        enhancementPanel.SetActive(false);

        enhanceButton.onClick.AddListener(OnEnhanceClicked);
        closeButton.onClick.AddListener(CloseEnhancement);
    }

    // ─────────────────────── 열기 / 닫기 ───────────────────────

    public void OpenEnhancement()
    {
        if (_isOpen == true)
        {
            return;
        }

        enhancementPanel.SetActive(true);
        _isOpen = true;

        dialogueText.text = "운을 시험해보겠나? 코인의 결과가 네 운명을 결정하지.";

        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
    }

    public void CloseEnhancement()
    {
        if (_isEnhancing == true)
        {
            dialogueText.text = "강화 중에는 나갈 수 없네!";
            return;
        }

        enhancementPanel.SetActive(false);
        _isOpen = false;
        _selectedWeapon = null;

        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }

    // ─────────────────────── 무기 선택 ───────────────────────

    /// <summary>
    /// 인벤토리 슬롯에서 무기 클릭 시 호출
    /// WeaponInstance 를 받아야 강화 단계 정보가 유지됨
    /// </summary>
    public void SelectWeapon(WeaponInstance weapon)
    {
        _selectedWeapon = weapon;
        UpdateEnhancementUI();
    }

    // ─────────────────────── UI 갱신 ───────────────────────

    private void UpdateEnhancementUI()
    {
        if (_selectedWeapon == null)
        {
            return;
        }

        int level = _selectedWeapon.EnhancementLevel;

        currentStatsText.text = "현재 강화: +" + level.ToString() + "\n"
                              + "공격력: " + _selectedWeapon.FinalDamage.ToString("F1");

        if (level < 5)
        {
            nextStatsText.text = "다음 강화: +" + (level + 1).ToString() + "\n"
                               + "성공 확률: " + _selectedWeapon.CurrentSuccessRate.ToString("F0") + "%";

            // 4강에서 실패하면 0강으로 초기화되므로 경고 표시
            if (_selectedWeapon.IsLastEnhance == true)
            {
                nextStatsText.text += "\n※ 실패 시 강화 단계 초기화!";
            }
        }
        else
        {
            nextStatsText.text = "최대 강화 단계입니다.";
        }

        float rateNormalized = _selectedWeapon.CurrentSuccessRate / 100f;
        successRateText.text = "성공 확률: " + _selectedWeapon.CurrentSuccessRate.ToString("F0") + "%";
        successRateSlider.value = rateNormalized;

        int cost = CalculateEnhancementCost(level);
        bool canAfford = PlayerStats.Instance != null && PlayerStats.Instance.gold >= cost;
        enhanceButton.interactable = canAfford == true && _isEnhancing == false && level < 5;
    }

    // ─────────────────────── 강화 버튼 ───────────────────────

    private void OnEnhanceClicked()
    {
        if (_selectedWeapon == null)
        {
            dialogueText.text = "먼저 강화할 무기를 선택해주게.";
            return;
        }

        if (_isEnhancing == true)
        {
            dialogueText.text = "이미 강화 중일세!";
            return;
        }

        if (_selectedWeapon.EnhancementLevel >= 5)
        {
            dialogueText.text = "이미 최대 강화 단계일세.";
            return;
        }

        int cost = CalculateEnhancementCost(_selectedWeapon.EnhancementLevel);

        if (PlayerStats.Instance == null || PlayerStats.Instance.gold < cost)
        {
            dialogueText.text = "골드가 부족하군!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        // 골드 차감
        PlayerStats.Instance.gold -= cost;

        StartEnhancement();
    }

    // ─────────────────────── 강화 처리 ───────────────────────

    private void StartEnhancement()
    {
        _isEnhancing = true;
        enhanceButton.interactable = false;
        dialogueText.text = "코인을 던지는 중...";

        float rateNormalized = _selectedWeapon.CurrentSuccessRate / 100f;

        // 코인 애니메이션 시작 — 끝난 후 콜백으로 강화 처리
        // 코루틴 대신 콜백 방식으로 변경 (애니메이션 전에 결과 처리되는 문제 해결)
        bool started = coinFlipUI.PlayCoinFlip(rateNormalized, OnCoinFlipComplete);

        if (started == false)
        {
            // CoinFlipUI 가 이미 실행 중이면 골드 환불 후 취소
            PlayerStats.Instance.gold += CalculateEnhancementCost(_selectedWeapon.EnhancementLevel);
            dialogueText.text = "잠시 후 다시 시도해주게.";
            _isEnhancing = false;
            enhanceButton.interactable = true;
        }
    }

    /// <summary>
    /// 코인 애니메이션 완료 후 호출되는 콜백
    /// result: true = 앞면(성공), false = 뒷면(실패)
    /// </summary>
    private void OnCoinFlipComplete(bool result)
    {
        if (_selectedWeapon == null)
        {
            _isEnhancing = false;
            return;
        }

        EnhanceResult enhanceResult = _selectedWeapon.TryEnhance(result);

        switch (enhanceResult)
        {
            case EnhanceResult.Success:
                dialogueText.text = _selectedWeapon.WeaponData.ItemName
                    + "이(가) +" + _selectedWeapon.EnhancementLevel.ToString() + "이 되었네!";
                break;

            case EnhanceResult.MaxReached:
                dialogueText.text = _selectedWeapon.WeaponData.ItemName + "이(가) +5 최대 강화 달성!";
                break;

            case EnhanceResult.Fail:
                dialogueText.text = _selectedWeapon.WeaponData.ItemName + " 강화에 실패했네. 다행히 등급은 그대로일세.";
                break;

            case EnhanceResult.Downgrade:
                dialogueText.text = _selectedWeapon.WeaponData.ItemName
                    + "이(가) +" + _selectedWeapon.EnhancementLevel.ToString() + "으로 낮아졌네.";
                break;

            case EnhanceResult.ResetToBase:
                dialogueText.text = _selectedWeapon.WeaponData.ItemName + "의 강화가 초기화되었네...";
                break;

            case EnhanceResult.AlreadyMax:
                dialogueText.text = "이미 최대 강화 단계일세.";
                break;
        }

        UpdateEnhancementUI();
        _isEnhancing = false;
    }

    // ─────────────────────── 유틸 ───────────────────────

    private int CalculateEnhancementCost(int currentLevel)
    {
        return baseCost * (currentLevel + 1);
    }
}