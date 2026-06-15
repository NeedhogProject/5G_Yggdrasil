// StorageUI.cs
// 창고 UI — 격자 슬롯 생성, 열기/닫기, 창고 데이터 표시
// 인벤토리 슬롯 프리팹을 재활용하며, 슬롯은 Storage 컨테이너로 표시한다
// 위치 유지 + 멀티셀(여러 칸) 점유 방식: 인벤토리와 동일하게 동작한다

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class StorageUI : MonoBehaviour
{
    // ─────────────────────── 싱글턴 ───────────────────────

    public static StorageUI Instance { get; private set; }

    // ─────────────────────── 설정 ───────────────────────

    [Header("창고 격자 크기")]
    public int gridWidth = 8;
    public int gridHeight = 8;

    [Header("UI 참조")]
    public GameObject storagePanel;
    public Transform slotContainer;
    public GameObject slotPrefab;  // 인벤토리와 같은 슬롯 프리팹
    public Button closeButton;

    [Tooltip("그리드 위에 겹쳐 둔 빈 RectTransform. 지정 시 멀티셀 아이템 아이콘을 이 위에 그려 여러 칸에 걸쳐 표시한다. 비우면 단일 칸 방식")]
    [SerializeField] private RectTransform iconOverlay;

    [Header("참조")]
    [SerializeField] private HouseSystem houseSystem;

    // ─────────────────────── 상태 ───────────────────────
    // 창고와 함께 여닫을 인벤토리 UI (지연 탐색)
    private InventoryUI _inventoryUI = null;
    private List<InventorySlot> _slots = new List<InventorySlot>();
    private bool _initialized = false;
    private bool _isOpen = false;

    // 창고를 처음 열 때 한 번만 HouseSystem 데이터를 슬롯에 펼쳤는지 여부
    // 이후로는 슬롯 위치가 진실이 되어 PopulateFromData 로 재배치하지 않는다.
    private bool _populatedFromData = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (houseSystem == null)
        {
            houseSystem = FindFirstObjectByType<HouseSystem>();
        }
    }

    private void Start()
    {
        if (storagePanel != null)
        {
            storagePanel.SetActive(false);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseStorage);
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        // 여는 건 상자 앞 StorageInteractable 이 담당
        // 여기선 Esc 로 닫기만 처리
        if (_isOpen == true && Keyboard.current.escapeKey.wasPressedThisFrame == true)
        {
            CloseStorage();
        }
    }

    // 슬롯 격자 생성 (최초 1회만)
    private void InitializeSlots()
    {
        if (_initialized == true)
        {
            return;
        }

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                GameObject slotObj = Instantiate(slotPrefab, slotContainer);
                InventorySlot slot = slotObj.GetComponent<InventorySlot>();

                slot.slotIndex = y * gridWidth + x;
                slot.gridPosition = new Vector2Int(x, y);
                slot.container = InventorySlot.SlotContainer.Storage;

                _slots.Add(slot);
            }
        }

        _initialized = true;
    }

    // 인벤토리 UI 참조 확보 (InventorySystem 우선, 없으면 씬 탐색)
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

    // 창고 열기
    public void OpenStorage()
    {
        // 설정창이 열려있으면 창고 열지 않음
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsSettingOpen == true)
        {
            return;
        }

        InitializeSlots();

        // 최초 1회만 HouseSystem 데이터를 슬롯에 펼친다.
        // 이후에는 슬롯 위치가 유지되므로 다시 재배치하지 않는다.
        if (_populatedFromData == false)
        {
            PopulateFromData();
            _populatedFromData = true;
        }

        if (storagePanel != null)
        {
            storagePanel.SetActive(true);
        }
        _isOpen = true;

        Time.timeScale = 0f;
        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);

        // 창고를 열면 인벤토리도 같이 열기 (드래그로 아이템 옮기기 위함)
        InventoryUI inventoryUI = ResolveInventoryUI();
        if (inventoryUI != null)
        {
            inventoryUI.OpenInventory(false);
        }
    }

    // 창고 닫기
    public void CloseStorage()
    {
        if (storagePanel != null)
        {
            storagePanel.SetActive(false);
        }
        _isOpen = false;

        Time.timeScale = 1f;
        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);

        // 같이 열었던 인벤토리도 닫기
        InventoryUI inventoryUI = ResolveInventoryUI();
        if (inventoryUI != null)
        {
            inventoryUI.CloseInventory();
        }
    }

    public bool IsOpen
    {
        get { return _isOpen; }
    }

    // ─────────────────────── 좌표/슬롯 헬퍼 ───────────────────────

    // 그리드 좌표로 슬롯 찾기 (범위 밖이면 null)
    private InventorySlot GetSlotAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= gridWidth || y >= gridHeight)
        {
            return null;
        }
        int idx = y * gridWidth + x;
        if (idx < 0 || idx >= _slots.Count)
        {
            return null;
        }
        return _slots[idx];
    }

    // 특정 좌표에 아이템 크기만큼 배치 가능한지 검사 (경계 + 점유)
    // ignoreOwner: 이동 시 자기 자신이 점유한 칸은 무시하고 검사하기 위한 주인 슬롯
    private bool CanPlaceAt(int startX, int startY, Vector2Int size, InventorySlot ignoreOwner)
    {
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                int cx = startX + x;
                int cy = startY + y;

                InventorySlot slot = GetSlotAt(cx, cy);
                if (slot == null)
                {
                    // 경계를 벗어남
                    return false;
                }

                if (slot.isOccupied == true)
                {
                    // 무시 대상(자기 자신)의 칸이면 통과
                    if (ignoreOwner != null && slot.ownerSlot == ignoreOwner)
                    {
                        continue;
                    }
                    return false;
                }
            }
        }
        return true;
    }

    // 주인 슬롯에 아이템을 배치하고 크기만큼 영역을 점유 (보조 칸 포함)
    private void OccupyArea(InventorySlot ownerSlot, ItemInstance instance)
    {
        ownerSlot.SetItem(instance);
        ownerSlot.ownerSlot = ownerSlot;

        Vector2Int size = instance.Data.itemSize;
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

                InventorySlot slot = GetSlotAt(startX + x, startY + y);
                if (slot == null)
                {
                    continue;
                }

                slot.isOccupied = true;
                slot.ownerSlot = ownerSlot;
            }
        }

        ResizeItemIcon(ownerSlot, size);
    }

    // 주인 슬롯이 점유한 영역을 모두 해제 (보조 칸 + 주인 칸)
    private void ReleaseArea(InventorySlot ownerSlot)
    {
        if (ownerSlot == null)
        {
            return;
        }

        // 이 주인을 가리키는 모든 칸 해제
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].ownerSlot == ownerSlot)
            {
                _slots[i].ownerSlot = null;
                _slots[i].isOccupied = false;
            }
        }

        RestoreIconSize(ownerSlot);
        ownerSlot.ClearSlot();
    }

    // 주인 슬롯 아이콘을 아이템 크기에 맞게 확장 (멀티셀이면 여러 칸에 걸쳐 표시)
    private void ResizeItemIcon(InventorySlot ownerSlot, Vector2Int size)
    {
        if (ownerSlot.itemIcon == null)
        {
            return;
        }

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
            // 아이콘을 오버레이 레이어로 옮겨 모든 칸 위에 렌더 (격자선 위로 깨끗하게 표시)
            iconRect.SetParent(iconOverlay, false);
            iconRect.SetAsLastSibling();

            // 아이콘이 슬롯 입력을 가리지 않도록 레이캐스트 차단
            ownerSlot.itemIcon.raycastTarget = false;

            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.sizeDelta = new Vector2(width, height);

            // 주인 칸의 월드 좌상단 모서리에 아이콘 좌상단을 맞춤
            Vector3[] corners = new Vector3[4];
            ownerSlot.GetComponent<RectTransform>().GetWorldCorners(corners);
            iconRect.position = corners[1]; // 1 = 좌상단(TL)
            return;
        }

        // 오버레이 미지정 시: 주인 칸 자식으로 확장 (멀티셀은 격자선 비칠 수 있음)
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.sizeDelta = new Vector2(width, height);
    }

    // 아이콘을 원래 한 칸 크기/부모로 되돌림 (영역 해제 시)
    private void RestoreIconSize(InventorySlot ownerSlot)
    {
        if (ownerSlot.itemIcon == null)
        {
            return;
        }

        RectTransform iconRect = ownerSlot.itemIcon.rectTransform;

        if (iconOverlay != null)
        {
            // 오버레이로 옮겼던 아이콘을 다시 주인 슬롯의 자식으로 복귀
            iconRect.SetParent(ownerSlot.transform, false);
        }

        // 한 칸 가득 채우도록 복귀
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        iconRect.sizeDelta = Vector2.zero;
    }

    // ─────────────────────── 데이터 표시/이동 ───────────────────────

    // HouseSystem 데이터를 슬롯에 펼친다 (최초 1회 또는 강제 갱신 시)
    // 빈 칸을 찾아 멀티셀 점유로 배치한다 (저장 데이터 로드용 초기 배치)
    private void PopulateFromData()
    {
        if (houseSystem == null)
        {
            return;
        }

        // 전부 비우기
        foreach (InventorySlot slot in _slots)
        {
            slot.ownerSlot = null;
            slot.ClearSlot();
        }

        // 창고 아이템 채우기 (각 아이템 크기만큼 점유)
        IReadOnlyList<ItemInstance> items = houseSystem.GetStorageItems();
        for (int i = 0; i < items.Count; i++)
        {
            ItemInstance instance = items[i];
            if (instance == null || instance.Data == null)
            {
                continue;
            }

            InventorySlot target = FindAreaForSize(instance.Data.itemSize);
            if (target == null)
            {
                // 더 둘 자리가 없으면 중단
                break;
            }
            OccupyArea(target, instance);
        }
    }

    // 아이템 크기가 들어갈 빈 영역의 주인 슬롯을 찾는다 (없으면 null)
    private InventorySlot FindAreaForSize(Vector2Int size)
    {
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (CanPlaceAt(x, y, size, null) == true)
                {
                    return GetSlotAt(x, y);
                }
            }
        }
        return null;
    }

    // 창고 데이터에 추가 + 빈 영역에 배치 (인벤 -> 창고 드래그용), 성공 여부 반환
    // 멀티셀 크기만큼 들어갈 빈 영역을 찾아 배치한다. (기존 아이템 위치 유지)
    public bool AddToStorageData(ItemInstance instance)
    {
        if (houseSystem == null)
        {
            return false;
        }
        if (instance == null || instance.Data == null)
        {
            return false;
        }

        // 크기만큼 들어갈 빈 영역 확보
        InventorySlot target = FindAreaForSize(instance.Data.itemSize);
        if (target == null)
        {
            Debug.Log("[StorageUI] 창고에 둘 공간이 없습니다.");
            return false;
        }

        bool added = houseSystem.AddToStorage(instance);
        if (added == true)
        {
            OccupyArea(target, instance);
        }
        return added;
    }

    // 창고 데이터에서 제거 + 해당 영역 비우기 (창고 -> 인벤 드래그용)
    public void RemoveFromStorageData(ItemInstance instance)
    {
        if (houseSystem == null)
        {
            return;
        }
        if (instance == null)
        {
            return;
        }

        houseSystem.TakeFromStorage(instance);

        // 해당 인스턴스를 들고 있는 주인 슬롯을 찾아 영역 해제
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].CurrentInstance == instance)
            {
                ReleaseArea(_slots[i]);
                break;
            }
        }
    }

    // 창고 내부에서 슬롯 간 위치 이동 (창고 슬롯끼리 드래그)
    // toSlot 위치에 아이템 크기만큼 들어갈 공간이 있으면 이동한다.
    // 잡은 칸 보정(grabOffset)은 InventorySlot 쪽에서 전달된 toSlot 이 이미 주인 칸 기준이라고 가정한다.
    public void MoveWithinStorage(InventorySlot fromSlot, InventorySlot toSlot)
    {
        if (fromSlot == null || toSlot == null)
        {
            return;
        }
        if (fromSlot == toSlot)
        {
            return;
        }

        // 보조 칸을 드롭 대상으로 잡았으면 주인 칸으로 환원
        InventorySlot fromOwner = fromSlot;
        if (fromSlot.ownerSlot != null && fromSlot.ownerSlot != fromSlot)
        {
            fromOwner = fromSlot.ownerSlot;
        }

        InventorySlot toOwner = toSlot;
        if (toSlot.ownerSlot != null && toSlot.ownerSlot != toSlot)
        {
            toOwner = toSlot.ownerSlot;
        }

        ItemInstance fromInstance = fromOwner.CurrentInstance;
        if (fromInstance == null || fromInstance.Data == null)
        {
            return;
        }

        int toX = toOwner.slotIndex % gridWidth;
        int toY = toOwner.slotIndex / gridWidth;

        // 목표 영역이 (자기 자신 제외) 비어 있는지 검사
        bool canPlace = CanPlaceAt(toX, toY, fromInstance.Data.itemSize, fromOwner);
        if (canPlace == false)
        {
            // 들어갈 공간 없음 — 이동 취소 (원위치 유지)
            Debug.Log("[StorageUI] 그 위치에는 둘 공간이 없습니다.");
            return;
        }

        // 기존 영역 해제 후 새 위치에 점유
        ReleaseArea(fromOwner);
        OccupyArea(toOwner, fromInstance);
    }
}