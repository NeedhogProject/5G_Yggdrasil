// StorageUI.cs
// 창고 UI — 격자 슬롯 생성, 열기/닫기, 창고 데이터 표시
// 인벤토리 슬롯 프리팹을 재활용하며, 슬롯은 Storage 컨테이너로 표시한다
// 위치 유지 + 멀티셀(여러 칸) 점유 방식: 인벤토리와 동일하게 동작한다
// R키로 아이템을 90도 회전할 수 있다 (인벤토리와 동일)

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

    // 마우스로 가리키는 창고 칸 (R키 회전 대상)
    private InventorySlot _hoveredSlot = null;

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

        // RotateItem 키: 마우스로 가리키는 창고 칸의 아이템을 90도 회전
        if (_isOpen == true && WasRotateKeyPressed() == true)
        {
            RotateHoveredItem();
        }
    }

    // 아이템 회전 키 입력 확인 (RotateItem 액션, 없으면 기본 R 폴백)
    private bool WasRotateKeyPressed()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        if (KeyBindingManager.Instance != null)
        {
            InputAction rotateAction = KeyBindingManager.Instance.FindAction("RotateItem");
            if (rotateAction != null)
            {
                return rotateAction.WasPressedThisFrame();
            }
        }

        return Keyboard.current.rKey.wasPressedThisFrame;
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

    // ─────────────────────── 호버 기록 (R키 회전 대상) ───────────────────────

    // 마우스가 올라간 창고 칸을 기록 (InventorySlot.OnPointerEnter 에서 호출)
    public void SetHoveredSlot(InventorySlot _slot)
    {
        _hoveredSlot = _slot;
    }

    // 창고 칸에서 마우스가 벗어나면 기록 해제 (InventorySlot.OnPointerExit/OnDisable 에서 호출)
    public void ClearHoveredSlot(InventorySlot _slot)
    {
        if (_hoveredSlot == _slot)
        {
            _hoveredSlot = null;
        }
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

    // 칸 좌표로 슬롯 조회 (인벤토리와 창고 사이 위치 교환용)
    public InventorySlot GetSlotAt(Vector2Int _vGrid)
    {
        return GetSlotAt(_vGrid.x, _vGrid.y);
    }

    // 창고 총 용량 기준으로 _incoming 을 넣고 _outgoing 을 뺐을 때 들어갈 수 있는지 (위치 교환용)
    public bool HasCapacity(ItemData _incoming, ItemData _outgoing)
    {
        if (houseSystem == null)
        {
            return false;
        }
        return houseSystem.UsedSlots - _outgoing.InventorySize + _incoming.InventorySize <= houseSystem.StorageCapacity;
    }

    // 창고 데이터에 추가 + 지정 칸에 배치 (드롭한 위치 그대로), 성공 여부 반환
    public bool PlaceAtSlot(InventorySlot _slot, ItemInstance _instance)
    {
        if (houseSystem == null)
        {
            return false;
        }

        Vector2Int vGrid = _slot.gridPosition;
        // 회전 반영 크기로 배치 가능 여부 검사
        if (CanPlaceAt(vGrid.x, vGrid.y, _instance.EffectiveSize, null) == false)
        {
            return false;
        }

        if (houseSystem.AddToStorage(_instance) == false)
        {
            return false;
        }

        OccupyArea(_slot, _instance);
        return true;
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

        // 회전 반영 점유 크기
        Vector2Int size = instance.EffectiveSize;
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

    // 주인 슬롯이 점유한 영역을 모두 해제 (보조 칸 + 주인 칸) — 아이템/아이콘도 비움
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

    // 점유 칸만 비운다 (아이템/아이콘은 유지 — 제자리 회전용)
    private void FreeCells(InventorySlot ownerSlot)
    {
        if (ownerSlot == null)
        {
            return;
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].ownerSlot == ownerSlot)
            {
                _slots[i].ownerSlot = null;
                _slots[i].isOccupied = false;
            }
        }
    }

    // 주인 칸 기준으로 현재 인스턴스 크기(회전 반영)만큼 다시 점유 + 아이콘 갱신
    // SetItem 은 호출하지 않는다 (아이템은 이미 주인 칸에 있음 — 회전용)
    private void OccupyCellsOnly(InventorySlot ownerSlot, ItemInstance instance)
    {
        ownerSlot.ownerSlot = ownerSlot;
        ownerSlot.isOccupied = true;

        Vector2Int size = instance.EffectiveSize;
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

    // ─────────────────────── R키 회전 ───────────────────────

    // 마우스로 가리키는 창고 칸의 아이템을 90도 회전한다 (R키)
    public bool RotateHoveredItem()
    {
        if (_hoveredSlot == null)
        {
            return false;
        }

        // 보조 칸이면 주인 슬롯 기준
        InventorySlot owner = _hoveredSlot;
        if (owner.ownerSlot != null && owner.ownerSlot != owner)
        {
            owner = owner.ownerSlot;
        }

        return RotateItemAt(owner);
    }

    // 지정한 주인 칸의 아이템을 회전한다. 회전 후 공간이 부족하면 원래대로 되돌린다.
    public bool RotateItemAt(InventorySlot ownerSlot)
    {
        if (ownerSlot == null)
        {
            return false;
        }

        ItemInstance instance = ownerSlot.CurrentInstance;
        if (instance == null || instance.Data == null)
        {
            return false;
        }

        // 정사각형 아이템은 회전해도 똑같으므로 무시
        if (instance.Data.itemSize.x == instance.Data.itemSize.y)
        {
            return false;
        }

        int startX = ownerSlot.slotIndex % gridWidth;
        int startY = ownerSlot.slotIndex / gridWidth;

        // 점유를 임시로 풀고(아이템은 유지) 회전
        FreeCells(ownerSlot);
        instance.ToggleRotation();

        // 1) 제자리에 회전해서 들어가면 그대로
        if (CanPlaceAt(startX, startY, instance.EffectiveSize, null) == true)
        {
            OccupyCellsOnly(ownerSlot, instance);
            AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
            return true;
        }

        // 2) 제자리 안 되면, 회전한 상태로 들어갈 빈 자리를 찾아 그곳으로 이동
        InventorySlot target = FindAreaForSize(instance.EffectiveSize);
        if (target != null && target != ownerSlot)
        {
            // 기존 주인 칸 비우고(아이콘 복원) 새 자리에 회전 상태로 배치
            RestoreIconSize(ownerSlot);
            ownerSlot.ClearSlot();

            OccupyArea(target, instance);
            AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
            return true;
        }

        // 3) 어디에도 안 들어가면 회전 취소하고 원래대로 재점유
        instance.ToggleRotation();
        OccupyCellsOnly(ownerSlot, instance);
        AudioManager.Instance?.PlaySFX(SFXClip.UIError);
        return false;
    }

    // 주인 슬롯 아이콘을 아이템 크기에 맞게 확장 (멀티셀이면 여러 칸에 걸쳐 표시, 회전 시 90도 회전)
    private void ResizeItemIcon(InventorySlot ownerSlot, Vector2Int size)
    {
        if (ownerSlot.itemIcon == null)
        {
            return;
        }

        // 아이콘 원본 비율 유지 (칸에 맞춰 늘어나 찌그러지지 않게)
        ownerSlot.itemIcon.preserveAspect = true;

        GridLayoutGroup grid = slotContainer.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            return;
        }

        float cellW = grid.cellSize.x;
        float cellH = grid.cellSize.y;
        float spaceX = grid.spacing.x;
        float spaceY = grid.spacing.y;

        // 차지하는 전체 픽셀 크기 = 셀 크기 × 칸수 + 사이 간격 (size 는 회전 반영된 점유 크기)
        float width = cellW * size.x + spaceX * (size.x - 1);
        float height = cellH * size.y + spaceY * (size.y - 1);

        // 이 아이템이 회전된 상태인지
        bool rotated = ownerSlot.CurrentInstance != null && ownerSlot.CurrentInstance.IsRotated == true;

        RectTransform iconRect = ownerSlot.itemIcon.rectTransform;

        if (iconOverlay != null)
        {
            // 점유 영역 등급색 하이라이트 (아이콘 뒤에 깔아 차지하는 칸을 명확히 표시)
            EnsureFootprintHighlight(ownerSlot, width, height);

            // 아이콘을 오버레이 레이어로 옮겨 모든 칸 위에 렌더 (격자선 위로 깨끗하게 표시)
            iconRect.SetParent(iconOverlay, false);
            iconRect.SetAsLastSibling();

            // 아이콘이 슬롯 입력을 가리지 않도록 레이캐스트 차단
            ownerSlot.itemIcon.raycastTarget = false;

            if (rotated == false)
            {
                // 회전 안 됨 — 점유 영역 정중앙 정렬 (비율 유지로 가운데에 들어감)
                iconRect.localEulerAngles = Vector3.zero;
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(width, height);
                iconRect.position = GetFootprintCenterWorld(ownerSlot, size);
                return;
            }

            // 회전됨 — 점유 영역 중심에 두고 90도 회전 (스프라이트는 원본 방향이라 가로·세로 뒤집어 돌림)
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(height, width);
            iconRect.localEulerAngles = new Vector3(0f, 0f, 90f);
            iconRect.position = GetFootprintCenterWorld(ownerSlot, size);
            return;
        }

        // 오버레이 미지정 시: 주인 칸 자식으로 확장 (멀티셀은 격자선 비칠 수 있음)
        if (rotated == false)
        {
            iconRect.localEulerAngles = Vector3.zero;
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.sizeDelta = new Vector2(width, height);
            return;
        }

        // 회전됨 (비오버레이) — 주인 칸 기준 중심 근사 배치 후 90도 회전
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(height, width);
        iconRect.localEulerAngles = new Vector3(0f, 0f, 90f);
        iconRect.anchoredPosition = new Vector2(width * 0.5f, -height * 0.5f);
    }

    // 점유 영역(회전 반영)의 월드 중심 좌표 — 주인칸 좌상단과 우하단칸 우하단의 중점
    private Vector3 GetFootprintCenterWorld(InventorySlot ownerSlot, Vector2Int size)
    {
        int startX = ownerSlot.slotIndex % gridWidth;
        int startY = ownerSlot.slotIndex / gridWidth;

        Vector3[] tl = new Vector3[4];
        ownerSlot.GetComponent<RectTransform>().GetWorldCorners(tl);

        InventorySlot brSlot = GetSlotAt(startX + size.x - 1, startY + size.y - 1);
        if (brSlot == null)
        {
            return tl[1];
        }

        Vector3[] br = new Vector3[4];
        brSlot.GetComponent<RectTransform>().GetWorldCorners(br);

        // tl[1] = 주인칸 좌상단(TL), br[3] = 우하단칸 우하단(BR)
        return (tl[1] + br[3]) * 0.5f;
    }

    // 점유 영역에 등급 색 하이라이트를 깐다 (멀티셀이 차지하는 칸을 명확히). 아이콘보다 뒤에 렌더
    private void EnsureFootprintHighlight(InventorySlot ownerSlot, float width, float height)
    {
        if (iconOverlay == null)
        {
            return;
        }

        // 등급 색 (툴팁과 동일), 은은하게
        ItemData data = ownerSlot.currentItem;
        Color col = data != null ? UIItemTooltip.GetRarityColor(data.rarity) : Color.white;
        col.a = 0.13f;

        Image hl = ownerSlot.footprintHighlight;
        if (hl == null)
        {
            GameObject hlObj = new GameObject("FootprintHighlight", typeof(RectTransform));
            hl = hlObj.AddComponent<Image>();
            hl.raycastTarget = false;
            ownerSlot.footprintHighlight = hl;
        }

        RectTransform hr = hl.rectTransform;
        hr.SetParent(iconOverlay, false);
        hr.SetAsLastSibling();
        hr.localEulerAngles = Vector3.zero;
        hr.anchorMin = new Vector2(0f, 1f);
        hr.anchorMax = new Vector2(0f, 1f);
        hr.pivot = new Vector2(0f, 1f);
        hr.sizeDelta = new Vector2(width, height);

        Vector3[] corners = new Vector3[4];
        ownerSlot.GetComponent<RectTransform>().GetWorldCorners(corners);
        hr.position = corners[1]; // 좌상단(TL)

        hl.color = col;
        hl.enabled = true;
    }

    // 점유 하이라이트 제거 (아이템이 칸을 떠날 때)
    private void RemoveFootprintHighlight(InventorySlot ownerSlot)
    {
        if (ownerSlot != null && ownerSlot.footprintHighlight != null)
        {
            Destroy(ownerSlot.footprintHighlight.gameObject);
            ownerSlot.footprintHighlight = null;
        }
    }

    // 아이콘을 원래 한 칸 크기/부모로 되돌림 (영역 해제 시)
    private void RestoreIconSize(InventorySlot ownerSlot)
    {
        // 점유 하이라이트도 같이 제거
        RemoveFootprintHighlight(ownerSlot);

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

        // 한 칸 가득 채우도록 복귀 (회전도 초기화)
        iconRect.localEulerAngles = Vector3.zero;
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

            InventorySlot target = FindAreaForSize(instance.EffectiveSize);
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

        // 크기만큼(회전 반영) 들어갈 빈 영역 확보
        InventorySlot target = FindAreaForSize(instance.EffectiveSize);
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

        // 목표 영역이 (자기 자신 제외) 비어 있는지 검사 (회전 반영 크기)
        bool canPlace = CanPlaceAt(toX, toY, fromInstance.EffectiveSize, fromOwner);
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
