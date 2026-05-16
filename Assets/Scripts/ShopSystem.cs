using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 상인 NPC 패널.
/// 구매 탭: shopItems 목록에서 아이템 구매.
/// 판매 탭: 플레이어 인벤토리 아이템을 기본가의 절반에 판매.
/// </summary>
public class ShopSystem : MonoBehaviour
{
    [Header("UI 참조")]
    public GameObject shopPanel;
    public TMP_Text   dialogueText;
    public TMP_Text   goldText;
    public Button     buyTabButton;
    public Button     sellTabButton;
    public Button     closeButton;

    [Header("상품 목록")]
    public Transform  itemListContainer;
    public GameObject shopItemPrefab;

    [Header("판매 목록")]
    public List<ShopItemData> shopItems = new List<ShopItemData>();

    private bool     isOpen      = false;
    private ShopTab  currentTab  = ShopTab.Buy;

    private enum ShopTab
    {
        Buy,
        Sell
    }

    private void Start()
    {
        shopPanel.SetActive(false);

        buyTabButton.onClick.AddListener(() => SwitchTab(ShopTab.Buy));
        sellTabButton.onClick.AddListener(() => SwitchTab(ShopTab.Sell));
        closeButton.onClick.AddListener(CloseShop);
    }

    public void OpenShop()
    {
        if (isOpen)
        {
            return;
        }
        shopPanel.SetActive(true);
        isOpen = true;

        dialogueText.text = "어서오세요! 좋은 물건들이 있답니다.";

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
        ClearList();

        foreach (ShopItemData shopItem in shopItems)
        {
            GameObject itemObj = Instantiate(shopItemPrefab, itemListContainer);
            ShopItemUI itemUI  = itemObj.GetComponent<ShopItemUI>();
            itemUI.SetupBuyItem(shopItem, this);
        }

        dialogueText.text = "무엇을 구매하시겠어요?";
    }

    private void ShowSellTab()
    {
        ClearList();

        List<ItemData> playerItems = InventorySystem.Instance.items;

        foreach (ItemData item in playerItems)
        {
            GameObject itemObj = Instantiate(shopItemPrefab, itemListContainer);
            ShopItemUI itemUI  = itemObj.GetComponent<ShopItemUI>();
            itemUI.SetupSellItem(item, this);
        }

        dialogueText.text = "무엇을 판매하시겠어요?";
    }

    public void BuyItem(ShopItemData shopItem)
    {
        if (PlayerStats.Instance.gold < shopItem.buyPrice)
        {
            dialogueText.text = "골드가 부족합니다!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        if (InventorySystem.Instance.AddItem(shopItem.itemData) == false)
        {
            dialogueText.text = "인벤토리가 가득 찼습니다!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        PlayerStats.Instance.gold -= shopItem.buyPrice;

        dialogueText.text = $"{shopItem.itemData.itemName}을(를) 구매했습니다!";

        UpdateGoldDisplay();
        AudioManager.Instance?.PlaySFX(SFXClip.ItemPickup);
    }

    public void SellItem(ItemData item)
    {
        int sellPrice = item.basePrice / 2;

        PlayerStats.Instance.gold += sellPrice;
        InventorySystem.Instance.RemoveItem(item);

        dialogueText.text = $"{item.itemName}을(를) {sellPrice} 골드에 판매했습니다.";

        UpdateGoldDisplay();
        ShowSellTab();

        AudioManager.Instance?.PlaySFX(SFXClip.ItemPickup);
    }

    private void UpdateGoldDisplay()
    {
        goldText.text = $"보유 골드: {PlayerStats.Instance.gold}";
    }

    private void ClearList()
    {
        foreach (Transform child in itemListContainer)
        {
            Destroy(child.gameObject);
        }
    }
}

[System.Serializable]
public class ShopItemData
{
    public ItemData itemData;
    public int      buyPrice;
    public int      stock = -1; // -1 = 무제한
}
