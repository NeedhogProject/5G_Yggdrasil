using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public GameObject itemInventoryPanel;
    public GameObject resourceInventoryPanel;

    [Header("Tab Buttons")]
    public Button itemTabButton;
    public Button resourceTabButton;
    public Button closeButton;
    public Button dropButton;

    [Header("바깥 클릭 닫기")]
    [Tooltip("InventoryPanel 뒤에 배치할 전체화면 투명 버튼")]
    public Button outsideCloseButton;

    [Header("Info Display")]
    public TMP_Text inventoryInfoText;
    public TMP_Text weightText;

    [Header("창고와 같이 열릴 때 인벤토리 위치")]
    [SerializeField] private Vector2 storageModePosition = new Vector2(-370f, 0f);

    [Header("상점과 같이 열릴 때 인벤토리 위치")]
    [SerializeField] private Vector2 shopModePosition = new Vector2(370f, 0f);

    // 다른 패널(창고/상점)과 같이 열린 상태인지
    private bool _openedBeside = false;


    private RectTransform _panelRect = null;
    private Vector2 _defaultPanelPosition = Vector2.zero;

    private bool isOpen = false;
    private InventoryTab currentTab = InventoryTab.Item;

    private enum InventoryTab
    {
        Item,
        Resource
    }

    void Start()
    {
        inventoryPanel.SetActive(false);

        if (itemTabButton == null == false)
        {
            itemTabButton.onClick.AddListener(() => SwitchTab(InventoryTab.Item));
        }

        if (resourceTabButton == null == false)
        {
            resourceTabButton.onClick.AddListener(() => SwitchTab(InventoryTab.Resource));
        }

        if (closeButton == null == false)
        {
            closeButton.onClick.AddListener(CloseInventory);
        }

        if (dropButton == null == false)
        {
            dropButton.onClick.AddListener(OnDropButtonClicked);
        }

        if (outsideCloseButton == null == false)
        {
            outsideCloseButton.onClick.AddListener(CloseInventory);
            outsideCloseButton.gameObject.SetActive(false);
        }

        _panelRect = inventoryPanel.GetComponent<RectTransform>();
        if (_panelRect == null == false)
        {
            _defaultPanelPosition = _panelRect.anchoredPosition;
        }
    }

    void Update()
    {
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            // 창고/상점과 같이 열린 상태면 I 로 닫지 않음
            if (isOpen == true && _openedBeside == true)
            {
                // 아무것도 안 함
            }
            else
            {
                ToggleInventory();
            }
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame && isOpen)
        {
            CloseInventory();
        }
    }

    public void ToggleInventory()
    {
        if (isOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    public void OpenInventory()
    {
        _openedBeside = false;
        OpenInventoryAt(_defaultPanelPosition, true);
    }

    // 창고와 같이 열기 (왼쪽). showOutsideClose=true 면 단독과 동일
    public void OpenInventory(bool showOutsideClose)
    {
        if (showOutsideClose == true)
        {
            _openedBeside = false;
            OpenInventoryAt(_defaultPanelPosition, true);
        }
        else
        {
            _openedBeside = true;
            OpenInventoryAt(storageModePosition, false);
        }
    }

    // 상점과 같이 열기 (오른쪽)
    public void OpenInventoryForShop()
    {
        _openedBeside = true;
        OpenInventoryAt(shopModePosition, false);
    }

    // 실제 열기 처리 (위치 + 바깥닫기 버튼)
    private void OpenInventoryAt(Vector2 position, bool showOutsideClose)
    {
        inventoryPanel.SetActive(true);
        isOpen = true;

        if (_panelRect == null == false)
        {
            _panelRect.anchoredPosition = position;
        }

        if (outsideCloseButton == null == false)
        {
            outsideCloseButton.gameObject.SetActive(showOutsideClose);
        }

        SwitchTab(InventoryTab.Item);
        UpdateInventoryInfo();

        Time.timeScale = 0f;
        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
    }

    public void CloseInventory()
    {
        _openedBeside = false;
        inventoryPanel.SetActive(false);
        isOpen = false;

        if (outsideCloseButton == null == false)
        {
            outsideCloseButton.gameObject.SetActive(false);
        }

        if (InputReader.Instance != null) InputReader.Instance.UIBlocking = false;
        Time.timeScale = 1f;
        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }

    // 버리기 버튼 클릭 시 호출
    private void OnDropButtonClicked()
    {
        InventorySystem inventory = InventorySystem.Instance;

        if (inventory == null)
        {
            return;
        }

        Debug.Log("[InventoryUI] 버리기 버튼 클릭");
    }

    private void SwitchTab(InventoryTab tab)
    {
        currentTab = tab;

        if (tab == InventoryTab.Item)
        {
            itemInventoryPanel.SetActive(true);
            resourceInventoryPanel.SetActive(false);
        }
        else
        {
            itemInventoryPanel.SetActive(false);
            resourceInventoryPanel.SetActive(true);
        }

        UpdateInventoryInfo();
    }

    private void UpdateInventoryInfo()
    {
        InventorySystem inventory = InventorySystem.Instance;

        if (inventory == null)
        {
            return;
        }

        int usedSlots = inventory.items.Count;
        int maxSlots = inventory.maxSlots;

        inventoryInfoText.text = "아이템: " + usedSlots + "/" + maxSlots;
    }
}