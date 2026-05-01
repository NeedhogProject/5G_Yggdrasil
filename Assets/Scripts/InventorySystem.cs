using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class InventorySystem : MonoBehaviour
{
    // ─────────────────────── 싱글턴 ───────────────────────
    public static InventorySystem Instance { get; private set; }

    [Header("Inventory Settings")]
    public int gridWidth = 10;
    public int gridHeight = 8;
    public int maxSlots = 80;
    
    [Header("UI References")]
    public Transform slotContainer;
    public GameObject slotPrefab;
    public InventoryUI inventoryUI;

    [Header("참조")]
    [SerializeField] private PlayerEquipment playerEquipment;
    
    [Header("Inventory Data")]
    public List<InventorySlot> slots = new List<InventorySlot>();
    public List<ItemData> items = new List<ItemData>();
    
    private InventorySlot selectedSlot;
    private ItemData selectedItem;
    private bool isDragging = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (playerEquipment == null)
            playerEquipment = FindFirstObjectByType<PlayerEquipment>();
    }

    void Start()
    {
        InitializeInventory();
        Debug.Log($"[InventorySystem] 슬롯 {slots.Count}개 생성 완료");
    }
    
    void Update()
    {
        if (isDragging && selectedItem != null)
        {
            // 드래그 중 아이템 이동
            // 마우스 따라다니는 로직
        }
    }
    
    private void InitializeInventory()
    {
        // 격자형 슬롯 생성
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                GameObject slotObj = Instantiate(slotPrefab, slotContainer);
                InventorySlot slot = slotObj.GetComponent<InventorySlot>();
                
                slot.slotIndex = y * gridWidth + x;
                slot.slotSize = new Vector2Int(1, 1);
                
                slots.Add(slot);
            }
        }
    }
    
    public bool AddItem(ItemData item)
    {
        // 빈 슬롯 찾기
        InventorySlot emptySlot = FindEmptySlot(item);
        
        if (emptySlot != null)
        {
            emptySlot.SetItem(item);
            items.Add(item);
            
            Debug.Log($"아이템 추가: {item.itemName}");
            return true;
        }
        else
        {
            Debug.LogWarning("인벤토리가 가득 찼습니다!");
            return false;
        }
    }
    
    public void RemoveItem(ItemData item)
    {
        InventorySlot slot = slots.Find(s => s.currentItem == item);
        
        if (slot != null)
        {
            slot.ClearSlot();
            items.Remove(item);
            
            Debug.Log($"아이템 제거: {item.itemName}");
        }
    }
    
    public void OnSlotClicked(InventorySlot slot)
    {
        if (selectedSlot == null)
        {
            // 첫 번째 클릭 - 아이템 선택
            if (slot.isOccupied)
            {
                selectedSlot = slot;
                selectedItem = slot.currentItem;
                isDragging = true;
                
                Debug.Log($"아이템 선택: {selectedItem.itemName}");
            }
        }
        else
        {
            // 두 번째 클릭 - 아이템 이동
            if (slot.CanPlaceItem(selectedItem))
            {
                // 아이템 이동
                slot.SetItem(selectedItem);
                selectedSlot.ClearSlot();
                
                Debug.Log($"아이템 이동: {selectedItem.itemName}");
            }
            
            // 선택 해제
            selectedSlot = null;
            selectedItem = null;
            isDragging = false;
        }
    }
    
    public void DropItem(ItemData item)
    {
        // 아이템을 필드에 버리기
        RemoveItem(item);
        
        // 플레이어 위치에 아이템 생성
        Vector3 dropPosition = PlayerController.Instance != null
            ? PlayerController.Instance.transform.position + Vector3.forward * 2f
            : Vector3.zero;
        GameObject droppedItem = Instantiate(item.itemPrefab, dropPosition, Quaternion.identity);
        
        Debug.Log($"아이템 버림: {item.itemName}");
    }
    
    public void UseItem(ItemData item)
    {
        if (item.itemType == ItemType.Equipment)
        {
            // 장비 장착
            playerEquipment?.EquipItem(new WeaponInstance(item as WeaponData));
        }
        else if (item.itemType == ItemType.Consumable)
        {
            // 소비 아이템 사용
            item.UseItem();
            RemoveItem(item);
        }
    }
    
    private InventorySlot FindEmptySlot(ItemData item)
    {
        // 아이템 크기에 맞는 빈 슬롯 찾기 (디아블로식)
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].CanPlaceItem(item))
            {
                // 아이템이 차지할 영역 체크
                if (CheckAreaAvailable(i, item.itemSize))
                {
                    return slots[i];
                }
            }
        }
        
        return null;
    }
    
    private bool CheckAreaAvailable(int startIndex, Vector2Int itemSize)
    {
        int startX = startIndex % gridWidth;
        int startY = startIndex / gridWidth;
        
        // 아이템 크기만큼 영역 체크
        for (int y = 0; y < itemSize.y; y++)
        {
            for (int x = 0; x < itemSize.x; x++)
            {
                int checkX = startX + x;
                int checkY = startY + y;
                
                // 그리드 범위 체크
                if (checkX >= gridWidth || checkY >= gridHeight)
                    return false;
                
                int checkIndex = checkY * gridWidth + checkX;
                
                // 슬롯이 비어있는지 체크
                if (slots[checkIndex].isOccupied)
                    return false;
            }
        }
        
        return true;
    }
    
    public int GetItemCount(ItemType type)
    {
        return items.FindAll(item => item.itemType == type).Count;
    }
    
    public bool HasItem(string itemName)
    {
        return items.Exists(item => item.itemName == itemName);
    }
}