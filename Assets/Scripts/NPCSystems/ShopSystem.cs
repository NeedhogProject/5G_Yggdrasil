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

    [Header("구매 확인 팝업")]
public GameObject buyConfirmPopup;
public TMP_Text confirmItemNameText;
public TMP_Text quantityText;
public TMP_Text totalPriceText;
public TMP_Text balanceAfterText;
public Button quantityUpButton;
public Button quantityDownButton;
public Button confirmBuyButton;
public Button cancelBuyButton;

private ShopItemData _pendingShopItem = null;
private int _pendingQuantity = 1;

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

        // 구매 확인 팝업 버튼 연결
        if (buyConfirmPopup != null)
        {
            buyConfirmPopup.SetActive(false);
        }
        if (quantityUpButton != null)
        {
            quantityUpButton.onClick.AddListener(() => ChangeBuyQuantity(1));
        }
        if (quantityDownButton != null)
        {
            quantityDownButton.onClick.AddListener(() => ChangeBuyQuantity(-1));
        }
        if (confirmBuyButton != null)
        {
            confirmBuyButton.onClick.AddListener(ConfirmBuy);
        }
        if (cancelBuyButton != null)
        {
            cancelBuyButton.onClick.AddListener(CancelBuy);
        }
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

        // 상점을 열면 인벤토리도 오른쪽에 같이 열기 (판매 드래그용)
        InventoryUI inventoryUI = ResolveInventoryUI();
        if (inventoryUI != null)
        {
            inventoryUI.OpenInventoryForShop();
        }
    }

    // 상점이 열려있는지 외부 확인용
    public bool IsOpen
    {
        get { return isOpen; }
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        isOpen = false;

        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);

        // 같이 열었던 인벤토리도 닫기
        InventoryUI inventoryUI = ResolveInventoryUI();
        if (inventoryUI != null)
        {
            inventoryUI.CloseInventory();
        }
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

    private InventoryUI _inventoryUI = null;

    // 인벤토리 UI 참조 확보
    private InventoryUI ResolveInventoryUI()
    {
        if (_inventoryUI != null)
        {
            return _inventoryUI;
        }

        if (InventorySystem.Instance != null && InventorySystem.Instance.inventoryUI != null)
        {
            _inventoryUI = InventorySystem.Instance.inventoryUI;
        }
        else
        {
            _inventoryUI = FindFirstObjectByType<InventoryUI>();
        }

        return _inventoryUI;
    }
    // 구매 확인 팝업 열기 (우클릭 시 ShopItemUI 가 호출)
    public void OpenBuyConfirm(ShopItemData shopItem)
    {
        if (shopItem == null || shopItem.itemData == null)
        {
            return;
        }

        _pendingShopItem = shopItem;
        _pendingQuantity = 1;

        if (buyConfirmPopup != null)
        {
            buyConfirmPopup.SetActive(true);
        }
        if (confirmItemNameText != null)
        {
            confirmItemNameText.text = shopItem.itemData.itemName;
        }

        RefreshBuyConfirm();
    }

    // 수량 증감 (1 이상, 최대 구매 가능 수량 이하)
    private void ChangeBuyQuantity(int delta)
    {
        if (_pendingShopItem == null)
        {
            return;
        }

        int maxQ = GetMaxBuyQuantity(_pendingShopItem);
        _pendingQuantity = _pendingQuantity + delta;

        if (_pendingQuantity < 1)
        {
            _pendingQuantity = 1;
        }
        if (_pendingQuantity > maxQ)
        {
            _pendingQuantity = maxQ;
        }

        RefreshBuyConfirm();
    }

    // 구매 가능 최대 수량 (골드/재고 기준)
    private int GetMaxBuyQuantity(ShopItemData shopItem)
    {
        int byGold = 9999;
        if (shopItem.buyPrice > 0)
        {
            byGold = PlayerStats.Instance.gold / shopItem.buyPrice;
        }

        int maxQ = byGold;

        if (shopItem.stock >= 0 && shopItem.stock < maxQ)
        {
            maxQ = shopItem.stock;
        }

        if (maxQ < 1)
        {
            maxQ = 1;
        }

        return maxQ;
    }

    // 팝업 텍스트 갱신
    private void RefreshBuyConfirm()
    {
        if (_pendingShopItem == null)
        {
            return;
        }

        int total = _pendingShopItem.buyPrice * _pendingQuantity;
        int balanceAfter = PlayerStats.Instance.gold - total;

        if (quantityText != null)
        {
            quantityText.text = _pendingQuantity.ToString();
        }
        if (totalPriceText != null)
        {
            totalPriceText.text = "구매 금액: " + total + " G";
        }
        if (balanceAfterText != null)
        {
            balanceAfterText.text = "구매 후 잔액: " + balanceAfter + " G";
        }
    }

    // 확인: 수량만큼 구매
    private void ConfirmBuy()
    {
        if (_pendingShopItem != null)
        {
            BuyItem(_pendingShopItem, _pendingQuantity);
        }
        CloseBuyConfirm();
    }

    // 취소
    private void CancelBuy()
    {
        CloseBuyConfirm();
    }

    // 팝업 닫기 + 목록 갱신
    private void CloseBuyConfirm()
    {
        _pendingShopItem = null;
        _pendingQuantity = 1;

        if (buyConfirmPopup != null)
        {
            buyConfirmPopup.SetActive(false);
        }
    }

    // 수량 구매 처리
    public void BuyItem(ShopItemData shopItem, int quantity)
    {
        if (shopItem == null || shopItem.itemData == null)
        {
            return;
        }

        int totalPrice = shopItem.buyPrice * quantity;

        if (PlayerStats.Instance.gold < totalPrice)
        {
            dialogueText.text = "골드가 부족합니다!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        int boughtCount = 0;
        for (int i = 0; i < quantity; i++)
        {
            bool added = InventorySystem.Instance.AddItem(shopItem.itemData);
            if (added == false)
            {
                break;
            }
            boughtCount = boughtCount + 1;
        }

        if (boughtCount == 0)
        {
            dialogueText.text = "인벤토리가 가득 찼습니다!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        int spent = shopItem.buyPrice * boughtCount;
        PlayerStats.Instance.gold -= spent;

        if (shopItem.stock > 0)
        {
            shopItem.stock -= boughtCount;
        }

        dialogueText.text = shopItem.itemData.itemName + " " + boughtCount + "개 구매!";

        UpdateGoldDisplay();
        AudioManager.Instance?.PlaySFX(SFXClip.ItemPickup);
    }
}


[System.Serializable]
public class ShopItemData
{
    public ItemData itemData;
    public int      buyPrice;
    public int      stock = -1; // -1 = 무제한
}
