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

    [Header("멀티셀 아이콘 오버레이")]
    [Tooltip("그리드 위에 겹쳐 둔 빈 RectTransform (그리드 다음 형제, GridLayoutGroup 없음). 지정 시 아이템 아이콘을 이 위에 그려 격자선 위로 깨끗하게 표시. 비우면 기존 칸 자식 방식")]
    [SerializeField] private RectTransform iconOverlay;

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
            // 진단: 어떤 인스턴스 때문에 자기 파괴되는지 기록
            Debug.LogWarning("[InventorySystem] 중복 감지 - 새 인스턴스 자기 파괴 (씬: " + gameObject.scene.name
                + ", 기존: " + Instance.gameObject.name + ")");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("[InventorySystem] 싱글턴 등록 (씬: " + gameObject.scene.name + ")");

        // DontDestroyOnLoad 는 루트 오브젝트에만 동작하므로 자식이면 루트로 분리
        if (transform.parent != null)
        {
            Debug.LogWarning("[InventorySystem] 부모('" + transform.parent.name + "') 아래에 있어 루트로 분리함 - 씬에서 루트로 배치 권장");
            transform.SetParent(null);
        }

        // 모든 씬에서 인벤토리(데이터 + UI)를 유지
        DontDestroyOnLoad(gameObject);

        if (playerEquipment == null)
        {
            playerEquipment = FindFirstObjectByType<PlayerEquipment>();
        }
    }

    private void OnDestroy()
    {
        // 어떤 경로로 파괴되어도 static 참조가 파괴된 객체를 가리키지 않도록 정리
        if (Instance == this)
        {
            Debug.Log("[InventorySystem] 현재 인스턴스 파괴됨 - static 정리");
            Instance = null;
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

        // 중첩 가능 아이템은 기존 묶음에 전부 들어가면 새 칸을 쓰지 않음
        if (CanMergeAll(instance.Data, instance.StackCount) == true)
        {
            MergeIntoStacks(instance.Data, instance.StackCount);
            Debug.Log("[InventorySystem] 아이템 합침: " + instance.Data.itemName + " x" + instance.StackCount);
            return true;
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

    // 중첩 아이템을 수량째로 추가 (기존 묶음 먼저, 남으면 새 칸 1개), 못 넣은 수량 반환
    public int AddItemStack(ItemData _item, int _nCount)
    {
        if (_item == null || _nCount <= 0)
        {
            return 0;
        }

        int nRemain = MergeIntoStacks(_item, _nCount);
        if (nRemain <= 0)
        {
            return 0;
        }

        ItemInstance instance = CreateInstanceFromData(_item);
        int nAmount = Mathf.Min(nRemain, _item.MaxStack);
        instance.AddStack(nAmount - 1);

        if (AddItem(instance) == false)
        {
            return nRemain;
        }
        return nRemain - nAmount;
    }

    // 해당 아이템이 놓인 주인 칸 목록 (상점 판매창에 담긴 칸은 제외)
    private List<InventorySlot> GetItemSlots(ItemData _item)
    {
        return slots.FindAll(s => s.IsOwner == true && s.IsSaleMarked == false && s.CurrentInstance != null && s.CurrentInstance.Data == _item);
    }

    // 기존 묶음의 남은 칸으로 수량 전부를 받을 수 있는지
    private bool CanMergeAll(ItemData _item, int _nCount)
    {
        if (_item.IsStackable == false)
        {
            return false;
        }

        int nSpace = 0;
        foreach (InventorySlot slot in GetItemSlots(_item))
        {
            nSpace += _item.MaxStack - slot.CurrentInstance.StackCount;
        }
        return nSpace >= _nCount;
    }

    // 같은 아이템 기존 묶음에 수량을 채우고 남은 수량 반환
    private int MergeIntoStacks(ItemData _item, int _nCount)
    {
        int nRemain = _nCount;

        foreach (InventorySlot slot in GetItemSlots(_item))
        {
            if (nRemain <= 0)
            {
                break;
            }

            int nAdded = slot.CurrentInstance.AddStack(nRemain);
            if (nAdded > 0)
            {
                nRemain -= nAdded;
                slot.SetItem(slot.CurrentInstance); // 수량 표시 갱신
            }
        }
        return nRemain;
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
    /// ItemData 기반 추가 (시작 장비 등)
    /// 타입에 맞는 런타임 인스턴스를 생성해 추가한다.
    /// 인스턴스를 가지므로 창고 이동/강화/각인/판매가 정상 동작한다.
    /// </summary>
    public bool AddItem(ItemData item)
    {
        if (item == null)
        {
            return false;
        }

        // 데이터 전용으로 넣지 않고 타입에 맞는 인스턴스를 만들어 추가한다.
        ItemInstance instance = CreateInstanceFromData(item);
        if (instance != null)
        {
            return AddItem(instance);
        }

        // 인스턴스 생성 실패 시에만 기존 데이터 전용 방식으로 추가 (안전장치)
        InventorySlot emptySlot = FindEmptySlot(item);

        if (emptySlot == null)
        {
            Debug.LogWarning("[InventorySystem] 인벤토리가 가득 찼습니다!");
            return false;
        }

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

        // 아이콘이 칸 전체를 채우도록 비율 유지 해제 (여백/격자처럼 보이는 현상 제거)
        ownerSlot.itemIcon.preserveAspect = false;

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
        float width = cellW * size.x + spaceX * (size.x - 1);
        float height = cellH * size.y + spaceY * (size.y - 1);

        RectTransform iconRect = ownerSlot.itemIcon.rectTransform;

        if (iconOverlay != null)
        {
            // 방식 A: 아이콘을 오버레이 레이어로 옮겨 모든 칸 위에 렌더 (격자선이 아이콘 위로 비치는 문제 해결)
            iconRect.SetParent(iconOverlay, false);
            iconRect.SetAsLastSibling();

            // 아이콘이 슬롯의 호버/클릭/드래그를 가리지 않도록 레이캐스트 차단 (슬롯 배경이 입력을 받음)
            ownerSlot.itemIcon.raycastTarget = false;

            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.sizeDelta = new Vector2(width, height);

            // 주인 칸의 월드 좌상단 모서리에 아이콘 좌상단을 맞춤 (레이아웃 패딩/정렬과 무관하게 정확)
            Vector3[] corners = new Vector3[4];
            ownerSlot.GetComponent<RectTransform>().GetWorldCorners(corners);
            iconRect.position = corners[1]; // 1 = 좌상단(TL)
            return;
        }

        // 오버레이 미지정 시: 기존 방식 (주인 칸 자식으로 확장 — 멀티셀은 격자선 비칠 수 있음)
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(width, height);
    }

    // 오버레이 아이콘 위치 재계산 (인벤토리 열기/탭 전환 등 그리드가 보이는 시점에 호출)
    // 패널이 비활성이거나 위치가 바뀐 상태에서 배치된 아이콘을 올바른 칸으로 다시 맞춤
    public void RefreshIconPositions()
    {
        if (iconOverlay == null)
        {
            return;
        }

        // 칸의 월드 좌표가 확정되도록 그리드 레이아웃을 즉시 갱신
        RectTransform gridRect = slotContainer as RectTransform;
        if (gridRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
        }

        foreach (InventorySlot slot in slots)
        {
            if (slot == null)
            {
                continue;
            }
            if (slot.IsOwner == false)
            {
                continue;
            }
            if (slot.currentItem == null)
            {
                continue;
            }
            ResizeItemIcon(slot, slot.currentItem.itemSize);
        }
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
        // 위험: 같은 아이템이 여러 칸(묶음, 강화 단계 차이)일 수 있어 아이템 종류로 찾지 않고 칸의 인스턴스를 사용
        ItemInstance movingInstance = fromOwner.CurrentInstance;
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

    // 그리드 좌표로 슬롯 반환 (드롭 시 잡은 위치 보정용). 범위 밖이면 null
    public InventorySlot GetSlotAt(Vector2Int grid)
    {
        if (grid.x < 0 || grid.y < 0 || grid.x >= gridWidth || grid.y >= gridHeight)
        {
            return null;
        }
        int idx = grid.y * gridWidth + grid.x;
        if (idx < 0 || idx >= slots.Count)
        {
            return null;
        }
        return slots[idx];
    }

    // 지정한 슬롯 위치에 인스턴스를 배치한다 (창고 -> 인벤토리 드롭 위치 지정용)
    // 그 위치에 아이템 크기만큼 공간이 있으면 배치하고 true, 없으면 false 를 반환한다.
    // false 면 호출측에서 AddItem 으로 폴백하면 된다.
    public bool AddItemAtSlot(InventorySlot targetSlot, ItemInstance instance)
    {
        if (targetSlot == null || instance == null || instance.Data == null)
        {
            return false;
        }

        int startX = targetSlot.slotIndex % gridWidth;
        int startY = targetSlot.slotIndex / gridWidth;

        // 그 자리에 아이템 크기만큼 들어갈 공간이 있는지 검사
        if (CanPlaceAt(startX, startY, instance.Data.itemSize) == false)
        {
            return false;
        }

        targetSlot.SetItem(instance);
        OccupyAreaOnly(targetSlot, instance.Data);
        _itemInstances.Add(instance);
        items.Add(instance.Data);

        Debug.Log("[InventorySystem] 지정 위치 배치: " + instance.Data.itemName);
        return true;
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

        // 위험: 같은 아이템 묶음이 여러 칸일 수 있어 아이템 종류가 아닌 인스턴스로 칸을 찾음 (못 찾아도 다른 칸은 건드리지 않음)
        InventorySlot slot = slots.Find(s => s.CurrentInstance == instance && s.IsOwner);

        if (slot != null)
        {
            ReleaseArea(slot);
        }

        _itemInstances.Remove(instance);
        items.Remove(instance.Data);

        Debug.Log("[InventorySystem] 아이템 제거: " + instance.Data.itemName);
    }

    /// <summary>인벤토리에 있는 해당 아이템 총 수량 (여러 묶음 합산)</summary>
    public int GetItemCount(ItemData _item)
    {
        int nTotal = 0;
        foreach (InventorySlot slot in GetItemSlots(_item))
        {
            nTotal += slot.CurrentInstance.StackCount;
        }
        return nTotal;
    }

    // 지정 수량만 제거 (재료 소모 등), 부족하면 아무것도 빼지 않고 false, 0개가 된 묶음은 칸까지 비움
    public bool RemoveItemCount(ItemData _item, int _nCount)
    {
        if (_item == null || _nCount <= 0)
        {
            return false;
        }

        if (GetItemCount(_item) < _nCount)
        {
            Debug.LogWarning("[InventorySystem] 수량 부족: " + _item.itemName + " (필요 " + _nCount + ")");
            return false;
        }

        // 뒤쪽 칸의 묶음부터 빼서 앞쪽 묶음을 최대한 유지
        List<InventorySlot> itemSlots = GetItemSlots(_item);
        int nRemain = _nCount;

        for (int i = itemSlots.Count - 1; i >= 0 && nRemain > 0; i--)
        {
            InventorySlot slot = itemSlots[i];
            nRemain -= slot.CurrentInstance.RemoveStack(nRemain);

            if (slot.CurrentInstance.IsEmpty == true)
            {
                RemoveItemAtSlot(slot);
            }
            else
            {
                slot.SetItem(slot.CurrentInstance); // 수량 표시 갱신
            }
        }

        Debug.Log("[InventorySystem] 수량 제거: " + _item.itemName + " x" + _nCount);
        return true;
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

        // 오버레이에 올려둔 아이콘이면 원래 슬롯의 자식으로 되돌림
        if (iconOverlay != null && ownerSlot.itemIcon.transform.parent == iconOverlay)
        {
            ownerSlot.itemIcon.rectTransform.SetParent(ownerSlot.transform, false);
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
        iconRect.pivot = new Vector2(0.5f, 0.5f);
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
            // 칸의 인스턴스도 목록에서 제거 (남으면 대장간 목록 등에 보이지 않는 아이템으로 남음)
            if (slot.CurrentInstance != null)
            {
                _itemInstances.Remove(slot.CurrentInstance);
            }
            ReleaseArea(slot);
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

    // 지정한 슬롯의 아이템을 제거 (멀티셀 영역 해제 + 데이터 제거)
    // 상점 판매창 담기처럼 슬롯 단위로 정확히 제거할 때 사용
    public void RemoveItemAtSlot(InventorySlot slot)
    {
        if (slot == null)
        {
            return;
        }

        // 보조 칸이면 주인 슬롯으로 보정
        InventorySlot ownerSlot = slot;
        if (ownerSlot.ownerSlot != null && ownerSlot.ownerSlot != ownerSlot)
        {
            ownerSlot = ownerSlot.ownerSlot;
        }

        if (ownerSlot.currentItem == null)
        {
            return;
        }

        // 슬롯을 비우기 전에 데이터 참조를 먼저 확보
        ItemData removedItem = ownerSlot.currentItem;
        ItemInstance removedInstance = ownerSlot.CurrentInstance;

        // 멀티셀 점유 해제 + 아이콘 복원 + 슬롯 비우기
        ReleaseArea(ownerSlot);

        // 데이터 리스트에서 제거 (인스턴스 우선, 없으면 데이터 기준)
        if (removedInstance != null)
        {
            _itemInstances.Remove(removedInstance);
        }
        items.Remove(removedItem);

        Debug.Log("[InventorySystem] 슬롯 아이템 제거: " + removedItem.itemName);
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

    // _clickedInstance: 우클릭한 칸의 인스턴스 (같은 아이템이 여러 칸이면 클릭한 것을 사용)
    public void UseItem(ItemData item, ItemInstance _clickedInstance = null)
    {
        if (item == null)
        {
            return;
        }

        if (item.itemType == ItemType.Equipment)
        {
            // 클릭한 칸의 인스턴스 우선, 없으면 해당 데이터의 인스턴스 찾기
            ItemInstance instance = _clickedInstance != null ? _clickedInstance : _itemInstances.Find(i => i.Data == item);

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
            ConsumableData consumable = item as ConsumableData;
            if (consumable == null)
            {
                return;
            }

            // 초기화 주문서는 각인술사 NPC 에서 사용 (우클릭 소모 방지)
            if (consumable.ConsumableType == ConsumableType.ResetScroll)
            {
                return;
            }

            // 사용 실패(이미 최대치 등) 시 물약을 소모하지 않음
            if (consumable.TryUse(PlayerStats.Instance) == false)
            {
                return;
            }

            // 클릭한 칸의 물약을 소모 (같은 물약이 여러 칸이면 다른 칸이 비워지지 않게)
            if (_clickedInstance != null)
            {
                RemoveItem(_clickedInstance);
            }
            else
            {
                RemoveItem(item);
            }
        }
        else if (item.itemType == ItemType.Relic)
        {
            // 유물 우클릭 → 유물 슬롯에 장착 (교체된 기존 유물은 EquipItem 내부에서 인벤토리로 반환)
            ItemInstance relicInstance = _clickedInstance != null ? _clickedInstance : _itemInstances.Find(i => i.Data == item);
            if (relicInstance == null)
            {
                return;
            }

            // 위험: 판매용, 미감정 유물은 장착이 거부되므로 인벤토리에서 빼기 전에 막아야 사라지지 않음
            RelicInstance relic = relicInstance as RelicInstance;
            if (relic == null || relic.CanEquip == false)
            {
                Debug.Log("[InventorySystem] 장착할 수 없는 유물 (판매용 또는 미감정): " + item.itemName);
                return;
            }

            // 장착 전에 인벤토리에서 먼저 제거 (복사 방지)
            RemoveItem(relicInstance);
            playerEquipment?.EquipItem(relicInstance);
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

    // ─────────────────────── 소모품 단축키 연동 ───────────────────────

    // 지정한 소모품 종류의 보유 총 수량 (단축키 슬롯 수량 표시용)
    public int GetConsumableCount(ConsumableType type)
    {
        int total = 0;

        for (int i = 0; i < _itemInstances.Count; i++)
        {
            ConsumableInstance consumable = _itemInstances[i] as ConsumableInstance;
            if (consumable == null || consumable.ConsumableData == null)
            {
                continue;
            }
            if (consumable.ConsumableData.ConsumableType == type)
            {
                total = total + consumable.StackCount;
            }
        }

        return total;
    }

    // 지정한 소모품 종류를 1개 사용한다 (단축키 슬롯에서 호출)
    // 효과 적용 + 1개 차감. 수량 0이 되면 인벤토리에서 제거, 아니면 슬롯 수량 표시 갱신.
    // 사용 성공 시 true, 보유 없음/최대치 등 사용 불가면 false.
    public bool UseConsumableByType(ConsumableType type)
    {
        for (int i = 0; i < _itemInstances.Count; i++)
        {
            ConsumableInstance consumable = _itemInstances[i] as ConsumableInstance;
            if (consumable == null || consumable.ConsumableData == null)
            {
                continue;
            }
            if (consumable.ConsumableData.ConsumableType != type)
            {
                continue;
            }

            // 효과 적용 + 1개 차감 (이미 최대치 등이면 false)
            bool success = consumable.TryUse(PlayerStats.Instance);
            if (success == false)
            {
                return false;
            }

            if (consumable.IsEmpty == true)
            {
                // 다 썼으면 인벤토리에서 제거 (슬롯 영역도 해제됨)
                RemoveItem(consumable);
            }
            else
            {
                // 남았으면 해당 슬롯 수량 표시만 갱신
                InventorySlot owner = slots.Find(s => s.CurrentInstance == consumable);
                if (owner != null)
                {
                    owner.SetItem(consumable);
                }
            }

            return true;
        }

        // 해당 소모품이 인벤토리에 없음
        return false;
    }

    /// <summary>
    /// 슬롯에 실제 배치된 모든 아이템을 인스턴스로 반환 (저장용)
    /// 데이터 전용으로 추가된 아이템(_itemInstances 에 없는 것)도
    /// 임시 인스턴스로 변환해서 포함시킴
    /// </summary>
    public List<ItemInstance> GetAllItemsForSave()
    {
        List<ItemInstance> result = new List<ItemInstance>();

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            // 주인 슬롯만 (보조 칸 제외)
            if (slot == null)
            {
                continue;
            }
            if (slot.IsOwner == false)
            {
                continue;
            }
            if (slot.currentItem == null)
            {
                continue;
            }

            // 슬롯에 인스턴스가 이미 있으면 그걸 사용
            ItemInstance instance = slot.CurrentInstance;

            // 인스턴스가 없으면(데이터 전용) 임시 인스턴스 생성
            if (instance == null)
            {
                instance = CreateInstanceFromData(slot.currentItem);
            }

            if (instance != null)
            {
                // 불러올 때 같은 칸에 놓기 위해 위치 기록
                instance.SlotPosition = slot.gridPosition;
                result.Add(instance);
            }
        }

        return result;
    }

    // ItemData 로부터 적절한 ItemInstance 생성 (추가/저장/바닥 드랍 공용)
    public static ItemInstance CreateInstanceFromData(ItemData data)
    {
        if (data == null)
        {
            return null;
        }

        if (data is WeaponData weaponData)
        {
            return new WeaponInstance(weaponData);
        }

        if (data is ArmorData armorData)
        {
            return new ArmorInstance(armorData);
        }

        if (data is ConsumableData consumableData)
        {
            return new ConsumableInstance(consumableData, 1);
        }

        if (data is ResourceData resourceData)
        {
            return new ResourceInstance(resourceData, 1);
        }

        // 유물은 감정 여부를 하나씩 따로 저장해야 해서 전용 인스턴스로 생성
        if (data is RelicData relicData)
        {
            return new RelicInstance(relicData);
        }

        return new ItemInstance(data);
    }
}