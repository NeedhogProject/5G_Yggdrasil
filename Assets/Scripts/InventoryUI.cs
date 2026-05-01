using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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

    [Header("바깥 클릭 닫기")]
    [Tooltip("InventoryPanel 뒤에 배치할 전체화면 투명 버튼")]
    public Button outsideCloseButton;

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

        if (outsideCloseButton != null)
        {
            outsideCloseButton.onClick.AddListener(CloseInventory);
            outsideCloseButton.gameObject.SetActive(false);
        }
    }
    
    void Update()
    {
        if (Keyboard.current.iKey.wasPressedThisFrame)
            ToggleInventory();

        if (Keyboard.current.escapeKey.wasPressedThisFrame && isOpen)
            CloseInventory();
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
        if (outsideCloseButton != null) outsideCloseButton.gameObject.SetActive(true);

        SwitchTab(InventoryTab.Item);
        UpdateInventoryInfo();

        Time.timeScale = 0f;
        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
    }

    public void CloseInventory()
    {
        inventoryPanel.SetActive(false);
        isOpen = false;
        if (outsideCloseButton != null) outsideCloseButton.gameObject.SetActive(false);

        Time.timeScale = 1f;
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