using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject shopPanel;
    public TMP_Text dialogueText;
    public TMP_Text goldText;
    public Button buyTabButton;
    public Button sellTabButton;
    public Button closeButton;
    
    [Header("Shop Content")]
    public Transform shopItemContainer;
    public GameObject shopItemPrefab;
    public Transform playerItemContainer;
    
    [Header("Shop Items")]
    public List<ShopItemData> shopItems = new List<ShopItemData>();
    
    private bool isOpen = false;
    private ShopTab currentTab = ShopTab.Buy;
    
    private enum ShopTab
    {
        Buy,
        Sell
    }
    
    void Start()
    {
        shopPanel.SetActive(false);
        
        buyTabButton.onClick.AddListener(() => SwitchTab(ShopTab.Buy));
        sellTabButton.onClick.AddListener(() => SwitchTab(ShopTab.Sell));
        closeButton.onClick.AddListener(CloseShop);
        
        InitializeShop();
    }
    
    private void InitializeShop()
    {
        // 상점 아이템 초기화
        // 실제로는 데이터베이스나 ScriptableObject에서 로드
    }
    
    public void OpenShop()
    {
        if (isOpen) return;
        shopPanel.SetActive(true);
        isOpen = true;
        
        dialogueText.text = "어서오게! 좋은 물건들이 많다네!";
        
        UpdateGoldDisplay();
        SwitchTab(ShopTab.Buy);
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
    }
    
    public void CloseShop()
    {
        shopPanel.SetActive(false);
        isOpen = false;
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }
    
    private void SwitchTab(ShopTab tab)
    {
        currentTab = tab;
        
        if (tab == ShopTab.Buy)
        {
            ShowBuyTab();
        }
        else
        {
            ShowSellTab();
        }
    }
    
    private void ShowBuyTab()
    {
        ClearContainer(shopItemContainer);
        
        foreach (ShopItemData shopItem in shopItems)
        {
            GameObject itemObj = Instantiate(shopItemPrefab, shopItemContainer);
            ShopItemUI itemUI = itemObj.GetComponent<ShopItemUI>();
            
            itemUI.SetupBuyItem(shopItem, this);
        }
        
        dialogueText.text = "무엇을 사겠나?";
    }
    
    private void ShowSellTab()
    {
        ClearContainer(playerItemContainer);
        
        List<ItemData> playerItems = InventorySystem.Instance.items;
        
        foreach (ItemData item in playerItems)
        {
            GameObject itemObj = Instantiate(shopItemPrefab, playerItemContainer);
            ShopItemUI itemUI = itemObj.GetComponent<ShopItemUI>();
            
            itemUI.SetupSellItem(item, this);
        }
        
        dialogueText.text = "무엇을 팔겠나?";
    }
    
    public void BuyItem(ShopItemData shopItem)
    {
        if (PlayerStats.Instance.gold >= shopItem.buyPrice)
        {
            // 인벤토리 공간 확인
            if (InventorySystem.Instance.AddItem(shopItem.itemData))
            {
                PlayerStats.Instance.gold -= shopItem.buyPrice;
                
                dialogueText.text = $"{shopItem.itemData.itemName}을(를) 구매했네!";
                
                UpdateGoldDisplay();
                
                AudioManager.Instance?.PlaySFX(SFXClip.ItemPickup);
            }
            else
            {
                dialogueText.text = "인벤토리가 가득 찼네!";
                AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            }
        }
        else
        {
            dialogueText.text = "골드가 부족하군!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
        }
    }
    
    public void SellItem(ItemData item)
    {
        int sellPrice = CalculateSellPrice(item);
        
        PlayerStats.Instance.gold += sellPrice;
        InventorySystem.Instance.RemoveItem(item);
        
        dialogueText.text = $"{item.itemName}을(를) {sellPrice} 골드에 팔았네!";
        
        UpdateGoldDisplay();
        ShowSellTab(); // 판매 목록 갱신
        
        AudioManager.Instance?.PlaySFX(SFXClip.ItemPickup);
    }
    
    private int CalculateSellPrice(ItemData item)
    {
        // 기본 가격의 50%로 판매
        int basePrice = item.basePrice / 2;
        
        // 각인된 장비는 일반 가격으로 판매 (기획서 규칙)
        if (item is EquipmentData equipment && equipment.isInscribed)
        {
            return item.basePrice / 2;
        }
        
        return basePrice;
    }
    
    private void UpdateGoldDisplay()
    {
        goldText.text = $"보유 골드: {PlayerStats.Instance.gold}";
    }
    
    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }
}

[System.Serializable]
public class ShopItemData
{
    public ItemData itemData;
    public int buyPrice;
    public int stock = -1; // -1 = 무제한
}