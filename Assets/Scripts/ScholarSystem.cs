using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScholarSystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject scholarPanel;
    public TMP_Text dialogueText;
    public Button identifyButton;
    public Button closeButton;
    
    [Header("Identification UI")]
    public Transform relicSlot;
    public TMP_Text identifyCostText;
    public TMP_Text relicInfoText;
    
    [Header("Identification Settings")]
    public int identifyCost = 50; // 감정 비용
    
    private ItemData selectedRelic;
    private bool isOpen = false;
    
    void Start()
    {
        scholarPanel.SetActive(false);
        
        identifyButton.onClick.AddListener(OnIdentifyClicked);
        closeButton.onClick.AddListener(CloseScholar);
    }
    
    public void OpenScholar()
    {
        if (isOpen) return;
        scholarPanel.SetActive(true);
         isOpen = true;
        
        dialogueText.text = "유물 감정이 필요한가? 내게 맡기게나.";
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
    }
    
    public void CloseScholar()
    {
        scholarPanel.SetActive(false);
        isOpen = false;
        
        selectedRelic = null;
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }
    
    public void SelectRelic(ItemData relic)
    {
        if (relic.itemType != ItemType.Relic)
        {
            dialogueText.text = "이건 유물이 아니네.";
            return;
        }
        
        selectedRelic = relic;
        UpdateIdentificationUI();
    }
    
    private void UpdateIdentificationUI()
    {
        if (selectedRelic != null)
        {
            RelicData relicData = selectedRelic as RelicData;
            
            if (relicData != null && relicData.isIdentified)
            {
                relicInfoText.text = $"{relicData.itemName}\n이미 감정된 유물이네.";
                identifyButton.interactable = false;
            }
            else
            {
                relicInfoText.text = $"미확인 유물\n감정이 필요합니다.";
                identifyCostText.text = $"감정 비용: {identifyCost} 골드";
                identifyButton.interactable = PlayerStats.Instance.gold >= identifyCost;
            }
        }
    }
    
    private void OnIdentifyClicked()
    {
        if (selectedRelic == null)
        {
            dialogueText.text = "먼저 감정할 유물을 선택해주게.";
            return;
        }
        
        RelicData relicData = selectedRelic as RelicData;
        
        if (relicData == null)
        {
            dialogueText.text = "이건 유물이 아니네.";
            return;
        }
        
        if (relicData.isIdentified)
        {
            dialogueText.text = "이미 감정된 유물일세.";
            return;
        }
        
        if (PlayerStats.Instance.gold >= identifyCost)
        {
            // 골드 차감
            PlayerStats.Instance.gold -= identifyCost;
            
            // 유물 감정
            relicData.isIdentified = true;
            relicData.RevealStats();
            
            dialogueText.text = $"감정 완료! 이것은 {relicData.itemName}이네!\n{relicData.itemDescription}";
            
            UpdateIdentificationUI();
            
            AudioManager.Instance?.PlaySFX(SFXClip.SetEffectActivate);
        }
        else
        {
            dialogueText.text = "골드가 부족하군!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
        }
    }
}