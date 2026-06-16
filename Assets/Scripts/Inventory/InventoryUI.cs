using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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

        if (itemTabButton != null)
        {
            itemTabButton.onClick.AddListener(() => SwitchTab(InventoryTab.Item));
        }

        if (resourceTabButton != null)
        {
            resourceTabButton.onClick.AddListener(() => SwitchTab(InventoryTab.Resource));
        }

        // 닫기 버튼은 단순 CloseInventory 가 아니라 분기 처리 메서드로 연결
        // (상점과 같이 열린 상태면 상점 전체를 닫아야 함)
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        if (dropButton != null)
        {
            dropButton.onClick.AddListener(OnDropButtonClicked);
        }

        if (outsideCloseButton != null)
        {
            outsideCloseButton.onClick.AddListener(OnCloseButtonClicked);
            outsideCloseButton.gameObject.SetActive(false);
        }

        _panelRect = inventoryPanel.GetComponent<RectTransform>();
        if (_panelRect != null)
        {
            _defaultPanelPosition = _panelRect.anchoredPosition;
        }
    }

    void Update()
    {
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            // 설정창이 열려있으면 인벤토리 토글 막음
            if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsSettingOpen == true)
            {
                // 아무것도 안 함
            }
            // 창고/상점과 같이 열린 상태면 I 로 닫지 않음
            else if (isOpen == true && _openedBeside == true)
            {
                // 아무것도 안 함
            }
            else
            {
                ToggleInventory();
            }
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame == true && isOpen == true)
        {
            // 상점과 같이 열린 상태면 ESC 는 ShopSystem 이 전담한다.
            // (여기서 닫으면 인벤만 닫히고 상점 패널이 남는 문제 발생)
            if (IsShopOpen() == true)
            {
                return;
            }

            CloseInventory();
        }
    }

    // 상점이 같이 열려있는지 확인
    private bool IsShopOpen()
    {
        if (_openedBeside == false)
        {
            return false;
        }
        if (ShopSystem.Instance == null)
        {
            return false;
        }
        return ShopSystem.Instance.IsOpen;
    }

    // 닫기 버튼(X / 바깥 클릭) 처리
    // 상점과 같이 열린 상태면 상점 전체를 닫고, 아니면 인벤토리만 닫는다.
    private void OnCloseButtonClicked()
    {
        if (IsShopOpen() == true)
        {
            // 상점 전체 닫기 (내부에서 인벤토리도 같이 닫음)
            ShopSystem.Instance.CloseShop();
            return;
        }

        CloseInventory();
    }

    public void ToggleInventory()
    {
        if (isOpen == true)
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

        if (_panelRect != null)
        {
            _panelRect.anchoredPosition = position;
        }

        if (outsideCloseButton != null)
        {
            outsideCloseButton.gameObject.SetActive(showOutsideClose);
        }

        SwitchTab(InventoryTab.Item);

        Time.timeScale = 0f;
        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
    }

    public void CloseInventory()
    {
        _openedBeside = false;
        inventoryPanel.SetActive(false);
        isOpen = false;

        if (outsideCloseButton != null)
        {
            outsideCloseButton.gameObject.SetActive(false);
        }

        if (InputReader.Instance != null)
        {
            InputReader.Instance.UIBlocking = false;
        }
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

            // 그리드가 보이게 된 시점에 오버레이 아이콘 위치를 다시 잡음
            InventorySystem.Instance?.RefreshIconPositions();
        }
        else
        {
            itemInventoryPanel.SetActive(false);
            resourceInventoryPanel.SetActive(true);
        }
    }
}