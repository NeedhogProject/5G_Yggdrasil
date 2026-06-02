// StorageUI.cs
// 창고 UI — 격자 슬롯 생성, 열기/닫기, 창고 데이터 표시
// 인벤토리 슬롯 프리팹을 재활용하며, 슬롯은 Storage 컨테이너로 표시한다

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

    private List<InventorySlot> _slots = new List<InventorySlot>();
    private bool _initialized = false;
    private bool _isOpen = false;

    private void Awake()
    {
        Debug.Log("[StorageUI] Awake 실행됨!");   // 임시

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

        // G 키로 창고 열기/닫기
        if (Keyboard.current.gKey.wasPressedThisFrame == true)
        {
            if (_isOpen == true)
            {
                CloseStorage();
            }
            else
            {
                OpenStorage();
            }
        }

        // Esc 로 닫기
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

    // 창고 열기
    public void OpenStorage()
    {
        InitializeSlots();
        RefreshSlots();

        if (storagePanel != null)
        {
            storagePanel.SetActive(true);
        }
        _isOpen = true;

        Time.timeScale = 0f;
        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
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
    }

    public bool IsOpen
    {
        get { return _isOpen; }
    }

    // 창고 데이터(HouseSystem)를 슬롯에 표시
    public void RefreshSlots()
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
}