using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 상점 아이템 슬롯 UI — ShopSystem 에서 Instantiate 후 SetupBuyItem / SetupSellItem 호출
/// </summary>
public class ShopItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image    itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button   actionButton;

    private ShopSystem    _shopSystem;
    private ShopItemData  _shopItemData;
    private ItemData      _sellItemData;
    private bool          _isBuyMode;

    /// <summary>구매 슬롯 초기화 — ShopSystem.ShowBuyTab() 에서 호출</summary>
    public void SetupBuyItem(ShopItemData shopItem, ShopSystem shop)
    {
        _shopSystem   = shop;
        _shopItemData = shopItem;
        _isBuyMode    = true;

        if (itemIcon     != null && shopItem.itemData != null)
            itemIcon.sprite = shopItem.itemData.itemIcon;
        if (itemNameText != null && shopItem.itemData != null)
            itemNameText.text = shopItem.itemData.itemName;
        if (priceText    != null)
            priceText.text = $"{shopItem.buyPrice} G";
        if (actionButton != null)
            actionButton.onClick.AddListener(OnClicked);
    }

    /// <summary>판매 슬롯 초기화 — ShopSystem.ShowSellTab() 에서 호출</summary>
    public void SetupSellItem(ItemData item, ShopSystem shop)
    {
        _shopSystem  = shop;
        _sellItemData = item;
        _isBuyMode   = false;

        if (itemIcon     != null) itemIcon.sprite     = item.itemIcon;
        if (itemNameText != null) itemNameText.text   = item.itemName;
        if (priceText    != null) priceText.text      = $"{item.basePrice / 2} G";
        if (actionButton != null) actionButton.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        if (_isBuyMode)
            _shopSystem?.BuyItem(_shopItemData);
        else
            _shopSystem?.SellItem(_sellItemData);
    }
}
