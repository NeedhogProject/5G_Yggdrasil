using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Slot Settings")]
    public int slotIndex;
    public Vector2Int slotSize = new Vector2Int(1, 1);
    
    [Header("UI References")]
    public Image itemIcon;
    public Image highlightImage;
    public GameObject occupiedIndicator;
    
    [Header("Slot Data")]
    public ItemData currentItem;
    public bool isOccupied = false;
    
    private InventorySystem inventorySystem;
    
    void Start()
    {
        inventorySystem = GetComponentInParent<InventorySystem>();
        UpdateSlotUI();
    }
    
    public void SetItem(ItemData item)
    {
        currentItem = item;
        isOccupied = item != null;
        UpdateSlotUI();
    }
    
    public void ClearSlot()
    {
        currentItem = null;
        isOccupied = false;
        UpdateSlotUI();
    }
    
    private void UpdateSlotUI()
    {
        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(isOccupied);
            if (isOccupied && currentItem != null)
            {
                itemIcon.sprite = currentItem.itemIcon;
                itemIcon.color = Color.white;
            }
        }
        
        if (occupiedIndicator != null)
        {
            occupiedIndicator.SetActive(isOccupied);
        }
        
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(false);
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(true);
        }
        
        if (isOccupied && currentItem != null && UIItemTooltip.Instance != null)
        {
            UIItemTooltip.Instance.ShowTooltip(currentItem, transform.position);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(false);
        }
        
        if (UIItemTooltip.Instance != null)
        {
            UIItemTooltip.Instance.HideTooltip();
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventorySystem == null) return;
        
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            inventorySystem.OnSlotClicked(this);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (isOccupied && currentItem != null)
            {
                inventorySystem.UseItem(currentItem);
            }
        }
    }
    
    public bool CanPlaceItem(ItemData item)
    {
        if (item == null) return false;
        return !isOccupied && slotSize.x >= item.itemSize.x && slotSize.y >= item.itemSize.y;
    }
} 