// ShopItemUI.cs
// 상점 아이템 슬롯 UI — 구매 모드에선 좌클릭 시 구매 확인 팝업을 연다
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ShopItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text priceText;

    private ShopSystem _shopSystem;
    private ShopItemData _shopItemData;
    private ItemData _sellItemData;
    private bool _isBuyMode;

    // 구매 슬롯 초기화
    public void SetupBuyItem(ShopItemData shopItem, ShopSystem shop)
    {
        _shopSystem = shop;
        _shopItemData = shopItem;
        _isBuyMode = true;

        if (itemIcon != null && shopItem.itemData != null)
        {
            itemIcon.sprite = shopItem.itemData.itemIcon;
        }
        if (itemNameText != null && shopItem.itemData != null)
        {
            itemNameText.text = shopItem.itemData.itemName;
        }
        if (priceText != null)
        {
            priceText.text = shopItem.buyPrice + " G";
        }
    }

    // 판매 슬롯 초기화 (판매 로직은 4단계에서 연결)
    public void SetupSellItem(ItemData item, ShopSystem shop)
    {
        _shopSystem = shop;
        _sellItemData = item;
        _isBuyMode = false;

        if (itemIcon != null)
        {
            itemIcon.sprite = item.itemIcon;
        }
        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
        }
        if (priceText != null)
        {
            priceText.text = item.SellPrice + " G";
        }
    }

    // 좌클릭: 구매 모드면 구매 팝업, 판매 모드면 판매창에서 빼기
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (_isBuyMode == true)
        {
            // 구매 모드: 좌클릭 시 구매 확인 팝업
            if (_shopSystem != null)
            {
                _shopSystem.OpenBuyConfirm(_shopItemData);
            }
        }
        else
        {
            // 판매 모드: 좌클릭 시 판매창에서 빼기 (마킹 해제)
            if (_shopSystem != null && _sellItemData != null)
            {
                _shopSystem.UnstageItem(_sellItemData);
            }
        }
    }
}