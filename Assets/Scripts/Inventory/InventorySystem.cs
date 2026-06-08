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
        OccupyAreaOnly(emptySlot, instance.Data);
        _itemInstances.Add(instance);
        items.Add(instance.Data);

        Debug.Log("[InventorySystem] 아이템 추가: " + instance.Data.itemName);
        return true;
    }

    // SetItem은 호출측에서 이미 한 경우, 영역 점유와 아이콘 크기만 처리
    private void OccupyAreaOnly(InventorySlot ownerSlot, ItemData item)
    {
        ownerSlot.ownerSlot = ownerSlot;

        Vector2Int size = item.itemSize;
        int startX = ownerSlot.slotIndex % gridWidth;
        int startY = ownerSlot.slotIndex / gridWidth;

        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                int idx = (startY + y) * gridWidth + (startX + x);
                if (idx < 0 || idx >= slots.Count)
                {
                    continue;
                }

                slots[idx].isOccupied = true;
                slots[idx].ownerSlot = ownerSlot;
            }
        }

        ResizeItemIcon(ownerSlot, size);
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

        // 주인 슬롯에 아이템 배치 + 차지하는 모든 칸 점유
        OccupyArea(emptySlot, item);
        items.Add(item);

        Debug.Log("[InventorySystem] 아이템 추가(데이터 전용): " + item.itemName);
        return true;
    }

    // 아이템 크기만큼 칸을 점유 (주인 슬롯 + 보조 칸)
    private void OccupyArea(InventorySlot ownerSlot, ItemData item)
    {
        ownerSlot.SetItem(item);
        OccupyAreaOnly(ownerSlot, item);
    }

    // 주인 슬롯 아이콘을 아이템 크기에 맞게 확장
    private void ResizeItemIcon(InventorySlot ownerSlot, Vector2Int size)
    {
        if (ownerSlot.itemIcon == null)
        {
            return;
        }

        GridLayoutGroup grid = slotContainer.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            return;
        }

        float cellW = grid.cellSize.x;
        float cellH = grid.cellSize.y;
        float spaceX = grid.spacing.x;
        float spaceY = grid.spacing.y;

        // 차지하는 전체 픽셀 크기 = 셀 크기 × 칸수 + 사이 간격
        float width  = cellW * size.x + spaceX * (size.x - 1);
        float height = cellH * size.y + spaceY * (size.y - 1);

        RectTransform iconRect = ownerSlot.itemIcon.rectTransform;
        // 좌상단 고정으로 우하단으로 확장
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot     = new Vector2(0f, 1f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(width, height);
    }

    // 슬롯 인덱스로 슬롯 가져오기
    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Count)
        {
            return null;
        }
        return slots[index];
    }

    // 외부(컨테이너 이동 등)에서 특정 슬롯의 점유 영역을 해제할 때 사용
    public void ReleaseSlotArea(InventorySlot ownerSlot)
    {
        if (ownerSlot == null)
        {
            return;
        }

        // 보조 칸이면 주인 슬롯으로 보정
        if (ownerSlot.ownerSlot != null && ownerSlot.ownerSlot != ownerSlot)
        {
            ownerSlot = ownerSlot.ownerSlot;
        }
        ReleaseArea(ownerSlot);
    }

    // 드래그한 아이템을 목표 슬롯으로 이동 (다중 칸 충돌 검사 포함)
    // 성공하면 true, 공간 부족이면 false (원위치 유지)
    public bool MoveItem(InventorySlot fromOwner, InventorySlot toSlot)
    {
        if (fromOwner == null || toSlot == null)
        {
            return false;
        }
        if (fromOwner == toSlot)
        {
            return false;
        }

        ItemData item = fromOwner.currentItem;
        if (item == null)
        {
            return false;
        }

        // 목표 위치가 자기 자신이 차지한 칸이면 무시
        Vector2Int size = item.itemSize;
        int toX = toSlot.slotIndex % gridWidth;
        int toY = toSlot.slotIndex / gridWidth;

        // 먼저 출발지 점유를 임시 해제해야 자기 영역과 겹쳐도 배치 가능
        ItemInstance movingInstance = FindInstanceBySlot(fromOwner);
        ReleaseAreaSilent(fromOwner);

        // 목표 영역이 비었는지 검사
        if (CanPlaceAt(toX, toY, size) == false)
        {
            // 실패 — 원위치 복원
            OccupyAreaWithInstance(fromOwner, item, movingInstance);
            return false;
        }

        // 출발지 데이터 비우고 목표지에 배치
        ItemData itemData = fromOwner.currentItem;
        fromOwner.ClearSlot();
        RestoreItemIcon(fromOwner);

        if (movingInstance != null)
        {
            toSlot.SetItem(movingInstance);
        }
        else
        {
            toSlot.SetItem(itemData);
        }
        OccupyAreaOnly(toSlot, item);

        return true;
    }

    // 인스턴스 찾기 (주인 슬롯 기준)
    private ItemInstance FindInstanceBySlot(InventorySlot ownerSlot)
    {
        if (ownerSlot.currentItem == null)
        {
            return null;
        }
        return _itemInstances.Find(i => i.Data == ownerSlot.currentItem);
    }

    // 특정 좌표에 크기만큼 배치 가능한지 (점유/경계 검사)
    private bool CanPlaceAt(int startX, int startY, Vector2Int size)
    {
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                int checkX = startX + x;
                int checkY = startY + y;

                if (checkX >= gridWidth || checkY >= gridHeight)
                {
                    return false;
                }

                int idx = checkY * gridWidth + checkX;
                if (idx < 0 || idx >= slots.Count)
                {
                    return false;
                }

                if (slots[idx].isOccupied == true)
                {
                    return false;
                }
            }
        }
        return true;
    }

    // 점유만 해제 (아이콘/데이터는 호출측에서 처리)
    private void ReleaseAreaSilent(InventorySlot ownerSlot)
    {
        ItemData item = ownerSlot.currentItem;
        if (item == null)
        {
            return;
        }

        Vector2Int size = item.itemSize;
        int startX = ownerSlot.slotIndex % gridWidth;
        int startY = ownerSlot.slotIndex / gridWidth;

        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                int idx = (startY + y) * gridWidth + (startX + x);
                if (idx < 0 || idx >= slots.Count)
                {
                    continue;
                }
                slots[idx].isOccupied = false;
                slots[idx].ownerSlot = null;
            }
        }
    }

    // 인스턴스 유무에 따라 영역 점유 (이동 실패 복원용)
    private void OccupyAreaWithInstance(InventorySlot ownerSlot, ItemData item, ItemInstance instance)
    {
        if (instance != null)
        {
            ownerSlot.SetItem(instance);
        }
        else
        {
            ownerSlot.SetItem(item);
        }
        OccupyAreaOnly(ownerSlot, item);
    }

    // ─────────────────────── 아이템 제거 ───────────────────────

    /// <summary>인스턴스 기반 제거</summary>
    public void RemoveItem(ItemInstance instance)
    {
        if (instance == null)
        {
            return;
        }

        InventorySlot slot = slots.Find(s => s.currentItem == instance.Data && s.IsOwner);
        if (slot == null)
        {
            slot = slots.Find(s => s.currentItem == instance.Data);
        }

        if (slot != null)
        {
            ReleaseArea(slot);
        }

        _itemInstances.Remove(instance);
        items.Remove(instance.Data);

        Debug.Log("[InventorySystem] 아이템 제거: " + instance.Data.itemName);
    }

    // 주인 슬롯이 차지한 모든 칸을 해제하고 아이콘 크기 복원
    private void ReleaseArea(InventorySlot ownerSlot)
    {
        ItemData item = ownerSlot.currentItem;
        if (item == null)
        {
            ownerSlot.ClearSlot();
            return;
        }

        Vector2Int size = item.itemSize;
        int startX = ownerSlot.slotIndex % gridWidth;
        int startY = ownerSlot.slotIndex / gridWidth;

        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                int idx = (startY + y) * gridWidth + (startX + x);
                if (idx < 0 || idx >= slots.Count)
                {
                    continue;
                }

                // 보조 칸 점유 해제
                slots[idx].isOccupied = false;
                slots[idx].ownerSlot = null;
            }
        }

        // 아이콘 크기 원래대로 복원 후 주인 슬롯 비우기
        RestoreItemIcon(ownerSlot);
        ownerSlot.ClearSlot();
    }

    // 아이콘을 1칸 크기로 복원
    private void RestoreItemIcon(InventorySlot ownerSlot)
    {
        if (ownerSlot.itemIcon == null)
        {
            return;
        }

        GridLayoutGroup grid = slotContainer.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            return;
        }

        RectTransform iconRect = ownerSlot.itemIcon.rectTransform;
        // 슬롯을 꽉 채우는 기본 형태로 복원 (stretch)
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.pivot     = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = Vector2.zero;
    }

    /// <summary>ItemData 기반 제거 (하위 호환용)</summary>
    public void RemoveItem(ItemData item)
    {
        if (item == null)
        {
            return;
        }

        InventorySlot slot = slots.Find(s => s.currentItem == item && s.IsOwner);
        if (slot == null)
        {
            slot = slots.Find(s => s.currentItem == item);
        }

        if (slot != null)
        {
            ReleaseArea(slot);
        }

        items.Remove(item);

        Debug.Log("[InventorySystem] 아이템 제거: " + item.itemName);
    }
    // 특정 슬롯의 아이템만 정확히 제거 (데이터 리스트 + 점유 영역 정리)
// 같은 종류 아이템이 여러 개일 때 우클릭한 슬롯만 제거하기 위함 (상점 판매 등)
public void RemoveItemAtSlot(InventorySlot slot)
{
    if (slot == null)
    {
        return;
    }

    // 보조 칸이면 주인 슬롯으로 보정
    InventorySlot ownerSlot = slot;
    if (slot.ownerSlot != null && slot.ownerSlot != slot)
    {
        ownerSlot = slot.ownerSlot;
    }

    ItemData item = ownerSlot.currentItem;
    if (item == null)
    {
        return;
    }

    // 데이터 리스트에서 제거 (인스턴스 우선)
    ItemInstance instance = FindInstanceBySlot(ownerSlot);
    if (instance != null)
    {
        _itemInstances.Remove(instance);
    }
    items.Remove(item);

    // 점유 영역 해제 + 슬롯 비우기
    ReleaseArea(ownerSlot);

    Debug.Log("[InventorySystem] 슬롯 아이템 제거: " + item.itemName);
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

            // 착용 전에 인벤토리에서 먼저 제거 (복사 방지)
            // 교체된 기존 장비는 PlayerEquipment.EquipItem 내부에서 인벤토리로 반환됨
            if (instance is WeaponInstance weaponInstance)
            {
                RemoveItem(instance);
                playerEquipment?.EquipItem(weaponInstance);
            }
            else if (instance is ArmorInstance armorInstance)
            {
                RemoveItem(instance);
                playerEquipment?.EquipItem(armorInstance);
            }
            else
            {
                // 인스턴스 없으면 WeaponData로 폴백
                WeaponData weaponData = item as WeaponData;
                if (weaponData != null)
                {
                    RemoveItem(item);
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
