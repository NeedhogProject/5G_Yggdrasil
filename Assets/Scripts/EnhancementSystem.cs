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
    public float baseSuccessRate = 0.75f; // 75%
    public float successRateDecreasePerLevel = 0.05f; // 레벨당 5% 감소
    
    private EquipmentData selectedEquipment;
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
        
        selectedEquipment = null;
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }
    
    public void SelectEquipment(EquipmentData equipment)
    {
        selectedEquipment = equipment;
        UpdateEnhancementUI();
    }
    
    private void UpdateEnhancementUI()
    {
        if (selectedEquipment != null)
        {
            // 현재 능력치 표시
            currentStatsText.text = $"현재 레벨: +{selectedEquipment.upgradeLevel}\n" +
                                   $"공격력: {selectedEquipment.attack}\n" +
                                   $"방어력: {selectedEquipment.defense}";
            
            // 다음 레벨 능력치 표시
            int nextAttack = selectedEquipment.attack + Mathf.RoundToInt(selectedEquipment.attack * 0.1f);
            int nextDefense = selectedEquipment.defense + Mathf.RoundToInt(selectedEquipment.defense * 0.1f);
            
            nextStatsText.text = $"다음 레벨: +{selectedEquipment.upgradeLevel + 1}\n" +
                                $"공격력: {nextAttack}\n" +
                                $"방어력: {nextDefense}";
            
            // 성공 확률 계산 및 표시
            float successRate = CalculateSuccessRate(selectedEquipment.upgradeLevel);
            successRateText.text = $"성공 확률: {successRate * 100:F1}%";
            successRateSlider.value = successRate;
            
            // 강화 비용
            int cost = CalculateEnhancementCost(selectedEquipment.upgradeLevel);
            
            enhanceButton.interactable = PlayerStats.Instance.gold >= cost && !isEnhancing;
        }
    }
    
    private void OnEnhanceClicked()
    {
        if (selectedEquipment == null)
        {
            dialogueText.text = "먼저 강화할 장비를 선택해주게.";
            return;
        }
        
        if (isEnhancing)
        {
            dialogueText.text = "이미 강화 중일세!";
            return;
        }
        
        int cost = CalculateEnhancementCost(selectedEquipment.upgradeLevel);
        
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
        
        // 코인 플립 연출
        float successRate = CalculateSuccessRate(selectedEquipment.upgradeLevel);
        bool success = coinFlipUI.PlayCoinFlip(successRate);
        
        // 코인 애니메이션 대기
        yield return new WaitForSeconds(2f);
        
        if (success)
        {
            // 강화 성공
            selectedEquipment.upgradeLevel++;
            selectedEquipment.UpgradeStats();
            
            dialogueText.text = $"강화 성공! {selectedEquipment.itemName}이(가) +{selectedEquipment.upgradeLevel}이 되었네!";
            
            AudioManager.Instance?.PlaySFX(SFXClip.EnhanceSuccess);
        }
        else
        {
            // 강화 실패
            dialogueText.text = "강화 실패... 아쉽지만 다음 기회를 노려보게.";
            
            AudioManager.Instance?.PlaySFX(SFXClip.EnhanceFail);
        }
        
        UpdateEnhancementUI();
        
        isEnhancing = false;
    }
    
    private float CalculateSuccessRate(int currentLevel)
    {
        float rate = baseSuccessRate - (currentLevel * successRateDecreasePerLevel);
        return Mathf.Clamp(rate, 0.1f, 1f); // 최소 10%, 최대 100%
    }
    
    private int CalculateEnhancementCost(int currentLevel)
    {
        return baseCost * (currentLevel + 1);
    }
}