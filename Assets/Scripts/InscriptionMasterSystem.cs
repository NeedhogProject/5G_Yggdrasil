using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InscriptionMasterSystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inscriptionPanel;
    public TMP_Text dialogueText;
    public Button inscribeButton;
    public Button resetButton;
    public Button closeButton;
    
    [Header("Inscription UI")]
    public Transform equipmentSlot;
    public TMP_Dropdown inscriptionTypeDropdown;
    public TMP_Text resourceCostText;
    public TMP_Text currentInscriptionText;
    
    [Header("Resource Display")]
    public TMP_Text fireResourceText;
    public TMP_Text waterResourceText;
    public TMP_Text windResourceText;
    public TMP_Text earthResourceText;
    public TMP_Text darknessResourceText;
    
    [Header("Inscription Settings")]
    public int inscriptionCost = 3; // 각인 비용 (자원 개수)
    public int resetItemRequired = 1; // 각인 초기화권 필요 개수
    
    private EquipmentData selectedEquipment;
    private InscriptionType selectedInscriptionType;
    private bool isOpen = false;
    
    void Start()
    {
        inscriptionPanel.SetActive(false);
        
        inscribeButton.onClick.AddListener(OnInscribeClicked);
        resetButton.onClick.AddListener(OnResetClicked);
        closeButton.onClick.AddListener(CloseInscriptionMaster);
        
        inscriptionTypeDropdown.onValueChanged.AddListener(OnInscriptionTypeChanged);
        
        SetupDropdown();
    }
    
    private void SetupDropdown()
    {
        inscriptionTypeDropdown.ClearOptions();
        
        List<string> options = new List<string>
        {
            "불 (Fire)",
            "물 (Water)",
            "바람 (Wind)",
            "땅 (Earth)",
            "어둠 (Darkness)"
        };
        
        inscriptionTypeDropdown.AddOptions(options);
    }
    
    public void OpenInscriptionMaster()
    {
        if (isOpen) return;
        inscriptionPanel.SetActive(true);
        isOpen = true;
        
        dialogueText.text = "각인을 원하는가? 자원만 있다면 언제든 도와주지.";
        
        UpdateResourceDisplay();
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
    }
    
    public void CloseInscriptionMaster()
    {
        inscriptionPanel.SetActive(false);
        isOpen = false;
        
        selectedEquipment = null;
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }
    
    public void SelectEquipment(EquipmentData equipment)
    {
        // 무기는 각인 불가
        if (equipment.equipmentType == EquipmentType.Weapon)
        {
            dialogueText.text = "무기에는 각인을 부여할 수 없네.";
            return;
        }
        
        selectedEquipment = equipment;
        UpdateInscriptionUI();
    }
    
    private void UpdateInscriptionUI()
    {
        if (selectedEquipment != null)
        {
            if (selectedEquipment.isInscribed)
            {
                currentInscriptionText.text = $"현재 각인: {GetInscriptionName(selectedEquipment.inscriptionType)}";
                inscribeButton.interactable = false;
                resetButton.interactable = true;
            }
            else
            {
                currentInscriptionText.text = "현재 각인: 없음";
                inscribeButton.interactable = true;
                resetButton.interactable = false;
            }
            
            UpdateResourceCost();
        }
    }
    
    private void UpdateResourceDisplay()
    {
        ResourceInventory resourceInv = ResourceInventory.Instance;
        
        if (resourceInv != null)
        {
            fireResourceText.text = $"불: {resourceInv.GetResourceCount(InscriptionType.Fire)}";
            waterResourceText.text = $"물: {resourceInv.GetResourceCount(InscriptionType.Water)}";
            windResourceText.text = $"바람: {resourceInv.GetResourceCount(InscriptionType.Wind)}";
            earthResourceText.text = $"땅: {resourceInv.GetResourceCount(InscriptionType.Earth)}";
            darknessResourceText.text = $"어둠: {resourceInv.GetResourceCount(InscriptionType.Darkness)}";
        }
    }
    
    private void UpdateResourceCost()
    {
        resourceCostText.text = $"필요 자원: {GetInscriptionName(selectedInscriptionType)} x {inscriptionCost}";
    }
    
    private void OnInscriptionTypeChanged(int index)
    {
        selectedInscriptionType = (InscriptionType)(index + 1); // None 제외
        UpdateResourceCost();
    }
    
    private void OnInscribeClicked()
    {
        if (selectedEquipment == null)
        {
            dialogueText.text = "먼저 각인할 장비를 선택해주게.";
            return;
        }
        
        if (selectedEquipment.isInscribed)
        {
            dialogueText.text = "이미 각인된 장비일세. 초기화가 필요하네.";
            return;
        }
        
        ResourceInventory resourceInv = ResourceInventory.Instance;
        
        if (resourceInv.GetResourceCount(selectedInscriptionType) >= inscriptionCost)
        {
            // 자원 소모
            resourceInv.RemoveResource(selectedInscriptionType, inscriptionCost);
            
            // 각인 부여
            selectedEquipment.inscriptionType = selectedInscriptionType;
            selectedEquipment.isInscribed = true;
            
            dialogueText.text = $"{selectedEquipment.itemName}에 {GetInscriptionName(selectedInscriptionType)} 각인을 부여했네!";
            
            UpdateInscriptionUI();
            UpdateResourceDisplay();
            
            AudioManager.Instance?.PlaySFX(SFXClip.InscribeApply);
        }
        else
        {
            dialogueText.text = "자원이 부족하군!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
        }
    }
    
    private void OnResetClicked()
    {
        if (selectedEquipment == null)
        {
            dialogueText.text = "먼저 초기화할 장비를 선택해주게.";
            return;
        }
        
        if (selectedEquipment.isInscribed == false)
        {
            dialogueText.text = "각인되지 않은 장비일세.";
            return;
        }
        
        // 각인 초기화권 확인
        if (InventorySystem.Instance.HasItem("각인 초기화권"))
        {
            // 각인 초기화권 소모
            ItemData resetItem = InventorySystem.Instance.items.Find(item => item.itemName == "각인 초기화권");
            InventorySystem.Instance.RemoveItem(resetItem);
            
            // 각인 제거
            selectedEquipment.inscriptionType = InscriptionType.None;
            selectedEquipment.isInscribed = false;
            
            dialogueText.text = $"{selectedEquipment.itemName}의 각인을 초기화했네!";
            
            UpdateInscriptionUI();
            
            AudioManager.Instance?.PlaySFX(SFXClip.InscribeReset);
        }
        else
        {
            dialogueText.text = "각인 초기화권이 필요하네!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
        }
    }
    
    private string GetInscriptionName(InscriptionType type)
    {
        switch (type)
        {
            case InscriptionType.Fire: return "불";
            case InscriptionType.Water: return "물";
            case InscriptionType.Wind: return "바람";
            case InscriptionType.Earth: return "땅";
            case InscriptionType.Darkness: return "어둠";
            default: return "없음";
        }
    }
}