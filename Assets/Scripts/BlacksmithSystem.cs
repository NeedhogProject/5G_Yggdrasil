using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlacksmithSystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject blacksmithPanel;
    public TMP_Text dialogueText;
    public Button upgradeButton;
    public Button repairButton;
    public Button closeButton;
    
    [Header("Upgrade Settings")]
    public Transform equipmentSlot;
    public TMP_Text upgradeCostText;
    public TMP_Text currentLevelText;
    
    private EquipmentData selectedEquipment;
    private bool isOpen = false;
    
    void Start()
    {
        blacksmithPanel.SetActive(false);
        
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
        repairButton.onClick.AddListener(OnRepairClicked);
        closeButton.onClick.AddListener(CloseBlacksmith);
    }
    
    public void OpenBlacksmith()
    {
        if (isOpen) return;
        blacksmithPanel.SetActive(true);
        isOpen = true;
        
        dialogueText.text = "어서오게! 무엇을 도와줄까?";
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
    }
    
    public void CloseBlacksmith()
    {
        blacksmithPanel.SetActive(false);
        isOpen = false;
        
        selectedEquipment = null;
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }
    
    public void SelectEquipment(EquipmentData equipment)
    {
        selectedEquipment = equipment;
        UpdateUpgradeUI();
    }
    
    private void UpdateUpgradeUI()
    {
        if (selectedEquipment != null)
        {
            currentLevelText.text = $"현재 레벨: {selectedEquipment.upgradeLevel}";
            
            int upgradeCost = CalculateUpgradeCost(selectedEquipment);
            upgradeCostText.text = $"강화 비용: {upgradeCost} 골드";
            
            upgradeButton.interactable = PlayerStats.Instance.gold >= upgradeCost;
        }
    }
    
    private void OnUpgradeClicked()
    {
        if (selectedEquipment == null)
        {
            dialogueText.text = "먼저 강화할 장비를 선택해주게!";
            return;
        }
        
        int upgradeCost = CalculateUpgradeCost(selectedEquipment);
        
        if (PlayerStats.Instance.gold >= upgradeCost)
        {
            // 골드 차감
            PlayerStats.Instance.gold -= upgradeCost;
            
            // 장비 강화
            selectedEquipment.upgradeLevel++;
            selectedEquipment.UpgradeStats();
            
            dialogueText.text = $"{selectedEquipment.itemName}이(가) 강화되었네!";
            
            UpdateUpgradeUI();
            
            AudioManager.Instance?.PlaySFX(SFXClip.EnhanceSuccess);
        }
        else
        {
            dialogueText.text = "골드가 부족하군!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
        }
    }
    
    private void OnRepairClicked()
    {
        if (selectedEquipment == null)
        {
            dialogueText.text = "먼저 수리할 장비를 선택해주게!";
            return;
        }
        
        int repairCost = CalculateRepairCost(selectedEquipment);
        
        if (PlayerStats.Instance.gold >= repairCost)
        {
            PlayerStats.Instance.gold -= repairCost;
            
            selectedEquipment.durability = selectedEquipment.maxDurability;
            
            dialogueText.text = $"{selectedEquipment.itemName}을(를) 수리했네!";
            
            AudioManager.Instance?.PlaySFX(SFXClip.EnhanceSuccess);
        }
        else
        {
            dialogueText.text = "골드가 부족하군!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
        }
    }
    
    private int CalculateUpgradeCost(EquipmentData equipment)
    {
        // 강화 레벨에 따른 비용 계산
        return 100 * (equipment.upgradeLevel + 1);
    }
    
    private int CalculateRepairCost(EquipmentData equipment)
    {
        // 내구도에 따른 수리 비용 계산
        float damagePercent = 1f - (equipment.durability / equipment.maxDurability);
        return Mathf.RoundToInt(equipment.basePrice * 0.3f * damagePercent);
    }
}