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

    // ItemData 리스트는 하위 호환용으로 유지
    public List<ItemData> items = new List<ItemData>();

    // 인스턴스 리스트: 강화/각인 등 런타임 상태 보존용
    private List<ItemInstance> _itemInstances = new List<ItemInstance>();

    private InventorySlot _selectedSlot = null;
    private ItemInstance _selectedInstance = null;
    private bool _isDragging = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (playerEquipment == null)
        {
            playerEquipment = FindFirstObjectByType<PlayerEquipment>();
        }
    }

    private void Start()
    {
        InitializeInventory();
        Debug.Log("[InventorySystem] 슬롯 " + slots.Count.ToString() + "개 생성 완료");
    }

    private void Update()
    {
        if (_isDragging == true && _selectedInstance != null)
        {
            // 드래그 중 마우스 추적 (InventorySlot의 OnDrag에서 처리)
        }
    }

    // ─────────────────────── 초기화 ───────────────────────

    private void InitializeInventory()
    {
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                GameObject slotObj = Instantiate(slotPrefab, slotContainer);
                InventorySlot slot = slotObj.GetComponent<InventorySlot>();

                slot.slotIndex = y * gridWidth + x;
                slot.gridPosition = new Vector2Int(x, y);

                slots.Add(slot);
            }
        }
    }

    // ─────────────────────── 아이템 추가 ───────────────────────

    /// <summary>
    /// ItemInstance 기반 추가 (인게임 드롭, 구매 등 실제 아이템)
    /// 강화/각인 정보가 툴팁에 정상 표시됨
    /// </summary>
    public bool AddItem(ItemInstance instance)
    {
        if (instance == null || instance.Data == null)
        {
            return false;
        }

        InventorySlot emptySlot = FindEmptySlot(instance.Data);

        if (emptySlot == null)
        {
            Debug.LogWarning("[InventorySystem] 인벤토리가 가득 찼습니다!");
            return false;
        }

        emptySlot.SetItem(instance);
        _itemInstances.Add(instance);
        items.Add(instance.Data);

        Debug.Log("[InventorySystem] 아이템 추가: " + instance.Data.itemName);
        return true;
    }

    /// <summary>
    /// ItemData 기반 추가 (하위 호환용, 상점 미리보기 등)
    /// 런타임 정보 없이 기본 데이터만 표시
    /// </summary>
    public bool AddItem(ItemData item)
    {
        if (item == null)
        {
            return false;
        }

        InventorySlot emptySlot = FindEmptySlot(item);

        if (emptySlot == null)
        {
            Debug.LogWarning("[InventorySystem] 인벤토리가 가득 찼습니다!");
            return false;
        }

        emptySlot.SetItem(item);
        items.Add(item);

        Debug.Log("[InventorySystem] 아이템 추가(데이터 전용): " + item.itemName);
        return true;
    }

    // ─────────────────────── 아이템 제거 ───────────────────────

    /// <summary>인스턴스 기반 제거</summary>
    public void RemoveItem(ItemInstance instance)
    {
        if (instance == null)
        {
            return;
        }

        InventorySlot slot = slots.Find(s => s.currentItem == instance.Data);

        if (slot != null)
        {
            slot.ClearSlot();
        }

        _itemInstances.Remove(instance);
        items.Remove(instance.Data);

        Debug.Log("[InventorySystem] 아이템 제거: " + instance.Data.itemName);
    }

    /// <summary>ItemData 기반 제거 (하위 호환용)</summary>
    public void RemoveItem(ItemData item)
    {
        if (item == null)
        {
            return;
        }

        InventorySlot slot = slots.Find(s => s.currentItem == item);

        if (slot != null)
        {
            slot.ClearSlot();
        }

        items.Remove(item);

        Debug.Log("[InventorySystem] 아이템 제거: " + item.itemName);
    }
    // 슬롯은 건드리지 않고 데이터 리스트에서만 제거 (컨테이너 간 이동용)
    public void RemoveInstanceData(ItemInstance instance)
    {
        if (instance == null)
        {
            return;
        }
        _itemInstances.Remove(instance);
        items.Remove(instance.Data);
    }

    // 데이터 리스트에만 추가 (슬롯 배치는 호출측에서) (컨테이너 간 이동용)
    public void AddInstanceData(ItemInstance instance)
    {
        if (instance == null)
        {
            return;
        }
        _itemInstances.Add(instance);
        items.Add(instance.Data);
    }

    // ─────────────────────── 슬롯 클릭 ───────────────────────

    public void OnSlotClicked(InventorySlot slot)
    {
        if (_selectedSlot == null)
        {
            // 첫 번째 클릭: 아이템 선택
            if (slot.isOccupied == false)
            {
                return;
            }

            _selectedSlot = slot;
            _isDragging = true;

            Debug.Log("[InventorySystem] 아이템 선택: " + slot.currentItem?.itemName);
        }
        else
        {
            // 두 번째 클릭: 아이템 이동
            if (slot.CanPlaceItem(_selectedSlot.currentItem) == true)
            {
                // InventorySlot.OnDrop에서 스왑 처리하므로 여기선 선택 해제만
                Debug.Log("[InventorySystem] 아이템 이동");
            }

            _selectedSlot = null;
            _selectedInstance = null;
            _isDragging = false;
        }
    }

    // ─────────────────────── 아이템 사용/버리기 ───────────────────────

    public void UseItem(ItemData item)
    {
        if (item == null)
        {
            return;
        }

        if (item.itemType == ItemType.Equipment)
        {
            // 인스턴스 목록에서 해당 데이터의 인스턴스 찾기
            ItemInstance instance = _itemInstances.Find(i => i.Data == item);

            if (instance is WeaponInstance weaponInstance)
            {
                playerEquipment?.EquipItem(weaponInstance);
            }
            else if (instance is ArmorInstance armorInstance)
            {
                playerEquipment?.EquipItem(armorInstance);
            }
            else
            {
                // 인스턴스 없으면 WeaponData로 폴백
                WeaponData weaponData = item as WeaponData;
                if (weaponData != null)
                {
                    playerEquipment?.EquipItem(new WeaponInstance(weaponData));
                }
            }
        }
        else if (item.itemType == ItemType.Consumable)
        {
            item.UseItem();
            RemoveItem(item);
        }
    }

    public void DropItem(ItemData item)
    {
        RemoveItem(item);

        Vector3 dropPosition = PlayerController.Instance != null
            ? PlayerController.Instance.transform.position + Vector3.forward * 2f
            : Vector3.zero;

        Instantiate(item.itemPrefab, dropPosition, Quaternion.identity);

        Debug.Log("[InventorySystem] 아이템 버림: " + item.itemName);
    }

    // ─────────────────────── 유틸 ───────────────────────

    private InventorySlot FindEmptySlot(ItemData item)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].CanPlaceItem(item) == false)
            {
                continue;
            }

            if (CheckAreaAvailable(i, item.itemSize) == true)
            {
                return slots[i];
            }
        }

        return null;
    }

    private bool CheckAreaAvailable(int startIndex, Vector2Int itemSize)
    {
        int startX = startIndex % gridWidth;
        int startY = startIndex / gridWidth;

        for (int y = 0; y < itemSize.y; y++)
        {
            for (int x = 0; x < itemSize.x; x++)
            {
                int checkX = startX + x;
                int checkY = startY + y;

                if (checkX >= gridWidth || checkY >= gridHeight)
                {
                    return false;
                }

                int checkIndex = checkY * gridWidth + checkX;

                if (slots[checkIndex].isOccupied == true)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>특정 타입 아이템 개수 반환</summary>
    public int GetItemCount(ItemType type)
    {
        return items.FindAll(item => item.itemType == type).Count;
    }

    /// <summary>이름으로 아이템 보유 여부 확인</summary>
    public bool HasItem(string itemName)
    {
        return items.Exists(item => item.itemName == itemName);
    }

    /// <summary>인스턴스 목록 반환 (SaveSystem 저장 시 사용)</summary>
    public List<ItemInstance> GetAllInstances()
    {
        return _itemInstances;
    }
}
