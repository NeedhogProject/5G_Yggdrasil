using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public GameObject itemInventoryPanel;
    public GameObject resourceInventoryPanel;
    
    [Header("Tab Buttons")]
    public Button itemTabButton;
    public Button resourceTabButton;
    public Button closeButton;
    
    [Header("Info Display")]
    public TMP_Text inventoryInfoText;
    public TMP_Text weightText;
    
    private bool isOpen = false;
    private InventoryTab currentTab = InventoryTab.Item;
    
    private enum InventoryTab
    {
        Item,
        Resource
    }
    
    void Start()
    {
        inventoryPanel.SetActive(false);
        
        itemTabButton.onClick.AddListener(() => SwitchTab(InventoryTab.Item));
        resourceTabButton.onClick.AddListener(() => SwitchTab(InventoryTab.Resource));
        closeButton.onClick.AddListener(CloseInventory);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape) && isOpen)
        {
            CloseInventory();
        }
    }
    
    public void ToggleInventory()
    {
        if (isOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }
    
    public void OpenInventory()
    {
        inventoryPanel.SetActive(true);
        isOpen = true;
        
        SwitchTab(InventoryTab.Item);
        UpdateInventoryInfo();
        
        Time.timeScale = 0f; // 게임 일시정지
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
    }
    
    public void CloseInventory()
    {
        inventoryPanel.SetActive(false);
        isOpen = false;
        
        Time.timeScale = 1f; // 게임 재개
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }
    
    private void SwitchTab(InventoryTab tab)
    {
        currentTab = tab;
        
        if (tab == InventoryTab.Item)
        {
            itemInventoryPanel.SetActive(true);
            resourceInventoryPanel.SetActive(false);
        }
        else
        {
            itemInventoryPanel.SetActive(false);
            resourceInventoryPanel.SetActive(true);
        }
        
        UpdateInventoryInfo();
    }
    
    private void UpdateInventoryInfo()
    {
        InventorySystem inventory = InventorySystem.Instance;
        
        if (inventory != null)
        {
            int usedSlots = inventory.items.Count;
            int maxSlots = inventory.maxSlots;
            
            inventoryInfoText.text = $"아이템: {usedSlots}/{maxSlots}";
            
            // 무게 시스템이 있다면
            // weightText.text = $"무게: {currentWeight}/{maxWeight}";
        }
    }
}