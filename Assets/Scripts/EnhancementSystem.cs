using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    private WeaponData selectedWeapon;
    private bool isEnhancing = false;
    private bool isOpen = false;
    
    void Start()
    {
        enhancementPanel.SetActive(false);
        
        enhanceButton.onClick.AddListener(OnEnhanceClicked);
        closeButton.onClick.AddListener(CloseEnhancement);
    }
    
    public void OpenEnhancement()
    {
        if (isOpen) return;
        enhancementPanel.SetActive(true);
        isOpen = true;
        
        dialogueText.text = "운을 시험해보겠나? 코인의 결과가 네 운명을 결정하지.";
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
    }
    
    public void CloseEnhancement()
    {
        if (isEnhancing)
        {
            dialogueText.text = "강화 중에는 나갈 수 없네!";
            return;
        }
        
        enhancementPanel.SetActive(false);
        isOpen = false;

        selectedWeapon = null;
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }
    
    public void SelectWeapon(WeaponData weapon)
    {
        selectedWeapon = weapon;
        UpdateEnhancementUI();
    }
    
    private void UpdateEnhancementUI()
    {
        if (selectedWeapon == null)
        {
            return;
        }

        int level = selectedWeapon.EnhancementLevel;

        currentStatsText.text = $"현재 강화: +{level}\n" +
                                $"공격력: {selectedWeapon.FinalDamage:F1}\n" +
                                $"공격속도: {selectedWeapon.FinalAttackSpeed:F2}";

        // 다음 강화 단계 미리보기 (5강이면 표시 없음)
        if (level < 5)
        {
            nextStatsText.text = $"다음 강화: +{level + 1}\n" +
                                 $"성공 확률: {selectedWeapon.CurrentSuccessRate}%";
        }
        else
        {
            nextStatsText.text = "최대 강화 단계입니다.";
        }

        float rateNormalized = selectedWeapon.CurrentSuccessRate / 100f;
        successRateText.text = $"성공 확률: {selectedWeapon.CurrentSuccessRate}%";
        successRateSlider.value = rateNormalized;

        int cost = CalculateEnhancementCost(level);
        enhanceButton.interactable = PlayerStats.Instance.gold >= cost && isEnhancing == false;
    }
    
    private void OnEnhanceClicked()
    {
        if (selectedWeapon == null)
        {
            dialogueText.text = "먼저 강화할 무기를 선택해주게.";
            return;
        }

        if (isEnhancing)
        {
            dialogueText.text = "이미 강화 중일세!";
            return;
        }

        int cost = CalculateEnhancementCost(selectedWeapon.EnhancementLevel);

        if (PlayerStats.Instance.gold >= cost)
        {
            PlayerStats.Instance.gold -= cost;
            StartCoroutine(EnhancementProcess());
        }
        else
        {
            dialogueText.text = "골드가 부족하군!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
        }
    }
    
    private IEnumerator EnhancementProcess()
    {
        isEnhancing = true;
        enhanceButton.interactable = false;

        dialogueText.text = "코인을 던지는 중...";

        float rateNormalized = selectedWeapon.CurrentSuccessRate / 100f;
        bool success = coinFlipUI.PlayCoinFlip(rateNormalized);

        yield return new WaitForSeconds(2f);

        EnhanceResult result = selectedWeapon.TryEnhance(success);

        switch (result)
        {
            case EnhanceResult.Success:
                dialogueText.text = $"강화 성공! {selectedWeapon.itemName}이(가) +{selectedWeapon.EnhancementLevel}이 되었네!";
                AudioManager.Instance?.PlaySFX(SFXClip.EnhanceSuccess);
                break;

            case EnhanceResult.MaxReached:
                dialogueText.text = $"최대 강화 달성! {selectedWeapon.itemName}이(가) +5가 되었네!";
                AudioManager.Instance?.PlaySFX(SFXClip.EnhanceSuccess);
                break;

            case EnhanceResult.Downgrade:
                dialogueText.text = $"강화 실패... {selectedWeapon.itemName}이(가) +{selectedWeapon.EnhancementLevel}으로 낮아졌네.";
                AudioManager.Instance?.PlaySFX(SFXClip.EnhanceFail);
                break;

            case EnhanceResult.AlreadyMax:
                dialogueText.text = "이미 최대 강화 단계일세.";
                break;
        }

        UpdateEnhancementUI();
        isEnhancing = false;
    }
    
    private int CalculateEnhancementCost(int currentLevel)
    {
        return baseCost * (currentLevel + 1);
    }
}