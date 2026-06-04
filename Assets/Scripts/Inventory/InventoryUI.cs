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
    }

    void Update()
    {
        if (InputReader.Instance != null && InputReader.Instance.InventoryPressed)
        {
            ToggleInventory();
        }

        if (InputReader.Instance != null && InputReader.Instance.CancelPressed && isOpen)
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
        inventoryPanel.SetActive(true);
        isOpen = true;

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