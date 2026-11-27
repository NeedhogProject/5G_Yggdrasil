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
    private InventorySlot _sellSlot;
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

    // 판매창에 담긴 수량 (카드에 표시)
    private int _sellQuantity = 1;

    // 판매 카드 초기화 (판매창에 담긴 아이템 한 칸. quantity 는 담은 개수)
    public void SetupSellItem(ItemData item, InventorySlot slot, ShopSystem shop, int quantity)
    {
        _shopSystem = shop;
        _sellItemData = item;
        _sellSlot = slot;
        _isBuyMode = false;
        _sellQuantity = Mathf.Max(1, quantity);

        if (itemIcon != null)
        {
            itemIcon.sprite = item.itemIcon;
        }
        if (itemNameText != null)
        {
            // 수량이 2개 이상이면 이름 옆에 개수를 붙인다
            if (_sellQuantity > 1)
            {
                itemNameText.text = item.itemName + " x" + _sellQuantity;
            }
            else
            {
                itemNameText.text = item.itemName;
            }
        }
        if (priceText != null)
        {
            // 상점의 판매가 비율을 반영한 단가로 총액 계산 (판매창 합계와 일치)
            int unitPrice = (_shopSystem != null) ? _shopSystem.GetSellPrice(item) : item.SellPrice;
            priceText.text = (unitPrice * _sellQuantity) + " G";
        }
    }

    // 구매 모드: 좌클릭 = 구매 팝업 / 판매 모드: 우클릭 = 판매 확인 팝업, 좌클릭 = 판매창에서 빼기
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isBuyMode == true)
        {
            // 구매 모드: 좌클릭 시 구매 확인 팝업
            if (eventData.button == PointerEventData.InputButton.Left && _shopSystem != null)
            {
                _shopSystem.OpenBuyConfirm(_shopItemData);
            }
            return;
        }

        // 판매 모드
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // 우클릭: 판매 확인 팝업 열기
            if (_shopSystem != null && _sellSlot != null)
            {
                _shopSystem.OpenSellConfirm(_sellSlot);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 좌클릭: 판매창에서 빼기 (마킹 해제)
            if (_shopSystem != null && _sellItemData != null)
            {
                _shopSystem.UnstageItem(_sellItemData);
            }
        }
    }
}