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
            // 창고가 열려있는 동안에는 I 로 인벤토리를 닫지 않음
            bool storageOpen = StorageUI.Instance != null && StorageUI.Instance.IsOpen == true;
            if (storageOpen == false)
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
        OpenInventory(true);
    }

    // showOutsideClose: 전체화면 바깥클릭-닫기 버튼 사용 여부
    // 창고와 같이 열 때는 false (창고 클릭을 가로채지 않도록)
    public void OpenInventory(bool showOutsideClose)
    {
        Debug.Log("[InventoryUI] storageModePosition = " + storageModePosition);   // 임시
        inventoryPanel.SetActive(true);
        isOpen = true;

        // 위치 조정: 창고와 같이 열 때는 옆으로, 단독으로 열 때는 기본 위치
        if (_panelRect == null == false)
        {
            if (showOutsideClose == false)
            {
                _panelRect.anchoredPosition = storageModePosition;
            }
            else
            {
                _panelRect.anchoredPosition = _defaultPanelPosition;
            }
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