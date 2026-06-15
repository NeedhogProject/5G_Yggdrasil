// StorageUI.cs
// 창고 UI — 격자 슬롯 생성, 열기/닫기, 창고 데이터 표시
// 인벤토리 슬롯 프리팹을 재활용하며, 슬롯은 Storage 컨테이너로 표시한다
// 위치 유지 방식: 슬롯에 놓인 아이템은 그 자리에 유지된다 (인벤토리와 동일)

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

    [Header("참조")]
    [SerializeField] private HouseSystem houseSystem;

    // ─────────────────────── 상태 ───────────────────────
    // 창고와 함께 여닫을 인벤토리 UI (지연 탐색)
    private InventoryUI _inventoryUI = null;
    private List<InventorySlot> _slots = new List<InventorySlot>();
    private bool _initialized = false;
    private bool _isOpen = false;

    // 창고를 처음 열 때 한 번만 HouseSystem 데이터를 슬롯에 펼쳤는지 여부
    // 이후로는 슬롯 위치가 진실이 되어 RefreshSlots 로 재배치하지 않는다.
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

    // HouseSystem 데이터를 슬롯에 펼친다 (최초 1회 또는 강제 갱신 시)
    // 0번 슬롯부터 순서대로 채운다 (저장 데이터 로드용 초기 배치)
    private void PopulateFromData()
    {
        if (houseSystem == null)
        {
            return;
        }

        // 전부 비우기
        foreach (InventorySlot slot in _slots)
        {
            slot.ClearSlot();
        }

        // 창고 아이템 채우기
        IReadOnlyList<ItemInstance> items = houseSystem.GetStorageItems();
        for (int i = 0; i < items.Count; i++)
        {
            if (i >= _slots.Count)
            {
                break;
            }
            _slots[i].SetItem(items[i]);
        }
    }

    // 빈 슬롯 하나를 찾아 반환 (없으면 null)
    private InventorySlot FindFirstEmptySlot()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].isOccupied == false)
            {
                return _slots[i];
            }
        }
        return null;
    }

    // 창고 데이터에 추가 + 빈 칸에 배치 (인벤 -> 창고 드래그용), 성공 여부 반환
    // 위치 유지를 위해 RefreshSlots 로 전체 재배치하지 않고, 빈 칸 하나에만 놓는다.
    public bool AddToStorageData(ItemInstance instance)
    {
        if (houseSystem == null)
        {
            return false;
        }
        if (instance == null)
        {
            return false;
        }

        // 빈 슬롯을 먼저 확보 (없으면 가득 찬 것)
        InventorySlot emptySlot = FindFirstEmptySlot();
        if (emptySlot == null)
        {
            Debug.Log("[StorageUI] 창고가 가득 찼습니다.");
            return false;
        }

        bool added = houseSystem.AddToStorage(instance);
        if (added == true)
        {
            // 전체 재배치 대신 빈 칸 한 곳에만 배치 (기존 아이템 위치 유지)
            emptySlot.SetItem(instance);
        }
        return added;
    }

    // 창고 데이터에서 제거 + 해당 슬롯 비우기 (창고 -> 인벤 드래그용)
    // 위치 유지를 위해 전체 재배치하지 않고, 해당 인스턴스가 든 슬롯만 비운다.
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

        // 해당 인스턴스를 들고 있는 슬롯을 찾아 비운다
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].CurrentInstance == instance)
            {
                _slots[i].ClearSlot();
                break;
            }
        }
    }

    // 창고 내부에서 슬롯 간 위치 이동 (창고 슬롯끼리 드래그)
    // 단일 칸 기준: 목표가 비었으면 이동, 차있으면 두 슬롯의 아이템을 맞바꾼다.
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

        ItemInstance fromInstance = fromSlot.CurrentInstance;
        if (fromInstance == null)
        {
            return;
        }

        // 목표 칸이 비어 있으면 그냥 이동
        if (toSlot.isOccupied == false)
        {
            fromSlot.ClearSlot();
            toSlot.SetItem(fromInstance);
            return;
        }

        // 목표 칸이 차 있으면 두 아이템을 맞바꾼다
        ItemInstance toInstance = toSlot.CurrentInstance;
        if (toInstance == null)
        {
            // 인스턴스 없는 칸이면 스왑 불가, 그냥 무시
            return;
        }

        fromSlot.SetItem(toInstance);
        toSlot.SetItem(fromInstance);
    }
}