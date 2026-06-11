// ShopSystem.cs
// 상인 NPC(벨라) 패널
// 시작 메뉴(구매/판매/대화) → 구매 화면(우클릭 확인 팝업) / 판매 화면(판매창 스테이징)
// ESC: 팝업 닫기 -> 메뉴로 돌아가기 -> 상점 완전히 닫기 순서
// ★버튼 연결을 EnsureSetup 으로 분리 — ShopUI 가 꺼진 채 시작해도 OpenShop 때 보장됨

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class ShopSystem : MonoBehaviour
{
    [Header("UI 참조")]
    public GameObject shopPanel;
    public TMP_Text dialogueText;
    public TMP_Text goldText;
    public Button buyTabButton;
    public Button sellTabButton;
    public Button closeButton;

    [Header("상품 목록")]
    public Transform itemListContainer;
    public GameObject shopItemPrefab;

    [Header("판매 목록")]
    public List<ShopItemData> shopItems = new List<ShopItemData>();

    [Header("구매 확인 팝업")]
    public GameObject buyConfirmPopup;
    public TMP_Text confirmItemNameText;
    public TMP_Text quantityText;
    public TMP_Text totalPriceText;
    public TMP_Text balanceAfterText;
    public Button quantityUpButton;
    public Button quantityDownButton;
    public Button confirmBuyButton;
    public Button cancelBuyButton;

    [Header("시작 메뉴")]
    public GameObject menuPanel;
    public Button menuBuyButton;
    public Button menuSellButton;
    public Button menuTalkButton;

    [Header("판매 (판매창)")]
    public GameObject sellControls;
    public TMP_Text sellTotalText;
    public TMP_Text sellBalanceText;
    public Button confirmSellButton;
    public Button cancelSellButton;

    [Header("판매 설정")]
    [SerializeField] private float sellPriceRatio = 0.3f;

    [Header("대화하기 대사")]
    [TextArea(2, 4)]
    public string[] talkLines = new string[]
    {
        "세계수 아래 마을은 늘 분주하답니다.",
        "필요한 게 있으면 언제든 말해요.",
        "요즘 던전이 험하다던데... 부디 조심해요.",
        "단골손님껜 가끔 덤도 챙겨드린답니다. 후훗."
    };

    public static ShopSystem Instance { get; private set; }

    private InventoryUI _inventoryUI = null;
    private ShopItemData _pendingShopItem = null;
    private int _pendingQuantity = 1;
    private int _talkIndex = 0;
    private List<StagedSellItem> _stagedItems = new List<StagedSellItem>();
    private bool isOpen = false;
    private ShopTab currentTab = ShopTab.Buy;

    // 버튼 연결이 끝났는지 (중복 연결 방지)
    private bool _isSetup = false;

    private enum ShopTab
    {
        Buy,
        Sell
    }

    // 판매창에 담긴 아이템 한 칸
    private class StagedSellItem
    {
        public ItemData data;
        public ItemInstance instance; // null 이면 데이터 전용
    }

    private void Awake()
    {
        Instance = this;

        // 버튼 연결을 먼저 해두고 (Awake 는 활성 상태에서 불림)
        EnsureSetup();

        // 내부 패널들 초기 상태로 꺼두기
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        if (buyConfirmPopup != null)
        {
            buyConfirmPopup.SetActive(false);
        }

        // 시작 시 ShopUI(자기 자신) 꺼두기
        // 벨라에게 E 누르면 OpenShop 에서 다시 켜짐
        gameObject.SetActive(false);
    }

    // 버튼 이벤트 연결 (Start 또는 OpenShop 에서 1회 실행)
    // ShopUI 가 꺼진 채 시작하면 Start 가 안 불리므로 OpenShop 에서도 보장
    private void EnsureSetup()
    {
        if (_isSetup == true)
        {
            return;
        }

        if (buyTabButton != null)
        {
            buyTabButton.onClick.AddListener(OnBuyTabClicked);
        }
        if (sellTabButton != null)
        {
            sellTabButton.onClick.AddListener(OnSellTabClicked);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseShop);
        }

        if (quantityUpButton != null)
        {
            quantityUpButton.onClick.AddListener(OnQuantityUpClicked);
        }
        if (quantityDownButton != null)
        {
            quantityDownButton.onClick.AddListener(OnQuantityDownClicked);
        }
        if (confirmBuyButton != null)
        {
            confirmBuyButton.onClick.AddListener(ConfirmBuy);
        }
        if (cancelBuyButton != null)
        {
            cancelBuyButton.onClick.AddListener(CancelBuy);
        }

        if (menuBuyButton != null)
        {
            menuBuyButton.onClick.AddListener(OnMenuBuyClicked);
        }
        if (menuSellButton != null)
        {
            menuSellButton.onClick.AddListener(OnMenuSellClicked);
        }
        if (menuTalkButton != null)
        {
            menuTalkButton.onClick.AddListener(OnMenuTalkClicked);
        }

        if (confirmSellButton != null)
        {
            confirmSellButton.onClick.AddListener(ConfirmSell);
        }
        if (cancelSellButton != null)
        {
            cancelSellButton.onClick.AddListener(CancelSell);
        }

        _isSetup = true;
    }

    // 탭 버튼 콜백 (람다 대신 명시 메서드로 — 코드 스타일)
    private void OnBuyTabClicked()
    {
        SwitchTab(ShopTab.Buy);
    }

    private void OnSellTabClicked()
    {
        SwitchTab(ShopTab.Sell);
    }

    private void OnQuantityUpClicked()
    {
        ChangeBuyQuantity(1);
    }

    private void OnQuantityDownClicked()
    {
        ChangeBuyQuantity(-1);
    }

    public void OpenShop()
    {
        // ShopUI(자기 자신) 가 꺼져있으면 먼저 켜기 (안의 ShopPanel 이 보이도록)
        if (gameObject.activeSelf == false)
        {
            gameObject.SetActive(true);
        }

        // 꺼진 채 시작했을 수 있으니 버튼 연결 보장
        EnsureSetup();

        if (isOpen == true)
        {
            return;
        }

        isOpen = true;

        UpdateGoldDisplay();
        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);

        ShowMenu();
    }

    // 상점이 열려있는지 외부 확인용
    public bool IsOpen
    {
        get { return isOpen; }
    }

    // 지금 판매 탭인지 (인벤 우클릭 담기 판단용)
    public bool IsSellMode
    {
        get { return currentTab == ShopTab.Sell; }
    }

    public void CloseShop()
    {
        // 판매창에 담아둔 것 인벤으로 되돌림 (손실 방지)
        for (int i = 0; i < _stagedItems.Count; i++)
        {
            ReturnStagedToInventory(_stagedItems[i]);
        }
        _stagedItems.Clear();

        shopPanel.SetActive(false);

        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        isOpen = false;

        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);

        InventoryUI inventoryUI = ResolveInventoryUI();
        if (inventoryUI != null)
        {
            inventoryUI.CloseInventory();
        }

        // ShopUI(자기 자신) 끄기 — 다음에 OpenShop 때 다시 켜짐
        gameObject.SetActive(false);
    }
    private void ShowMenu()
    {
        // 판매창에 담아둔 것 인벤으로 되돌림 (메뉴로 가면 판매 취소)
        for (int i = 0; i < _stagedItems.Count; i++)
        {
            ReturnStagedToInventory(_stagedItems[i]);
        }
        _stagedItems.Clear();

        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        InventoryUI inventoryUI = ResolveInventoryUI();
        if (inventoryUI != null)
        {
            inventoryUI.CloseInventory();
        }
    }

    // 구매하기 선택
    public void OnMenuBuyClicked()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }

        SetTabsVisible(true);
        SwitchTab(ShopTab.Buy);
        OpenInventoryBeside();
    }

    // 판매하기 선택
    public void OnMenuSellClicked()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }

        SetTabsVisible(true);
        SwitchTab(ShopTab.Sell);
        OpenInventoryBeside();
    }

    // 대화하기 선택 (누를 때마다 대사가 바뀜)
    public void OnMenuTalkClicked()
    {
        if (talkLines == null || talkLines.Length == 0)
        {
            dialogueText.text = "벨라: ...";
            return;
        }

        dialogueText.text = "벨라: " + talkLines[_talkIndex];

        _talkIndex = _talkIndex + 1;
        if (_talkIndex >= talkLines.Length)
        {
            _talkIndex = 0;
        }
    }

    // 구매/판매 화면에서 인벤토리 같이 열기
    private void OpenInventoryBeside()
    {
        InventoryUI inventoryUI = ResolveInventoryUI();
        if (inventoryUI != null)
        {
            inventoryUI.OpenInventoryForShop();
        }
    }

    // 구매/판매 탭 버튼 표시 (메뉴 화면에선 숨김)
    private void SetTabsVisible(bool visible)
    {
        if (buyTabButton != null)
        {
            buyTabButton.gameObject.SetActive(visible);
        }
        if (sellTabButton != null)
        {
            sellTabButton.gameObject.SetActive(visible);
        }
    }

    private void Update()
    {
        if (isOpen == false)
        {
            return;
        }
        if (Keyboard.current == null)
        {
            return;
        }
        if (Keyboard.current.escapeKey.wasPressedThisFrame == false)
        {
            return;
        }

        // 구매 확인 팝업이 떠 있으면 팝업만 닫기
        if (buyConfirmPopup != null && buyConfirmPopup.activeSelf == true)
        {
            CloseBuyConfirm();
            return;
        }

        // 구매/판매 창이 떠 있으면 메뉴로 돌아가기
        if (shopPanel != null && shopPanel.activeSelf == true)
        {
            ShowMenu();
            return;
        }

        // 메뉴 화면이면 상점 완전히 닫기
        CloseShop();
    }

    private void SwitchTab(ShopTab tab)
    {
        currentTab = tab;

        if (tab == ShopTab.Buy)
        {
            ShowBuyTab();
        }
        else
        {
            ShowSellTab();
        }
    }

    private void ShowBuyTab()
    {
        ClearList();

        foreach (ShopItemData shopItem in shopItems)
        {
            GameObject itemObj = Instantiate(shopItemPrefab, itemListContainer);
            ShopItemUI itemUI = itemObj.GetComponent<ShopItemUI>();
            itemUI.SetupBuyItem(shopItem, this);
        }

        if (sellControls != null)
        {
            sellControls.SetActive(false);
        }

        dialogueText.text = "무엇을 구매하시겠어요?";
    }

    private void ShowSellTab()
    {
        if (sellControls != null)
        {
            sellControls.SetActive(true);
        }

        RefreshSellStaging();
        dialogueText.text = "팔 물건을 판매창에 담아주세요.";
    }

    // 단일 구매 (현재는 팝업 흐름이 대체, 호환용)
    public void BuyItem(ShopItemData shopItem)
    {
        if (PlayerStats.Instance.gold < shopItem.buyPrice)
        {
            dialogueText.text = "골드가 부족합니다!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        if (InventorySystem.Instance.AddItem(shopItem.itemData) == false)
        {
            dialogueText.text = "인벤토리가 가득 찼습니다!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        PlayerStats.Instance.gold -= shopItem.buyPrice;

        dialogueText.text = shopItem.itemData.itemName + "을(를) 구매했습니다!";

        UpdateGoldDisplay();
        AudioManager.Instance?.PlaySFX(SFXClip.ItemPickup);
    }

    private void UpdateGoldDisplay()
    {
        goldText.text = "달란: " + PlayerStats.Instance.gold;
    }

    private void ClearList()
    {
        foreach (Transform child in itemListContainer)
        {
            Destroy(child.gameObject);
        }
    }

    // 인벤토리 UI 참조 확보
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

    // 구매 확인 팝업 열기 (우클릭 시 ShopItemUI 가 호출)
    public void OpenBuyConfirm(ShopItemData shopItem)
    {
        if (shopItem == null || shopItem.itemData == null)
        {
            return;
        }

        _pendingShopItem = shopItem;
        _pendingQuantity = 1;

        if (buyConfirmPopup != null)
        {
            buyConfirmPopup.SetActive(true);
        }
        if (confirmItemNameText != null)
        {
            confirmItemNameText.text = shopItem.itemData.itemName;
        }

        RefreshBuyConfirm();
    }

    // 수량 증감
    private void ChangeBuyQuantity(int delta)
    {
        if (_pendingShopItem == null)
        {
            return;
        }

        int maxQ = GetMaxBuyQuantity(_pendingShopItem);
        _pendingQuantity = _pendingQuantity + delta;

        if (_pendingQuantity < 1)
        {
            _pendingQuantity = 1;
        }
        if (_pendingQuantity > maxQ)
        {
            _pendingQuantity = maxQ;
        }

        RefreshBuyConfirm();
    }

    // 구매 가능 최대 수량 (골드/재고 기준)
    private int GetMaxBuyQuantity(ShopItemData shopItem)
    {
        int byGold = 9999;
        if (shopItem.buyPrice > 0)
        {
            byGold = PlayerStats.Instance.gold / shopItem.buyPrice;
        }

        int maxQ = byGold;

        if (shopItem.stock >= 0 && shopItem.stock < maxQ)
        {
            maxQ = shopItem.stock;
        }

        if (maxQ < 1)
        {
            maxQ = 1;
        }

        return maxQ;
    }

    // 팝업 텍스트 갱신
    private void RefreshBuyConfirm()
    {
        if (_pendingShopItem == null)
        {
            return;
        }

        int total = _pendingShopItem.buyPrice * _pendingQuantity;
        int balanceAfter = PlayerStats.Instance.gold - total;

        if (quantityText != null)
        {
            quantityText.text = _pendingQuantity.ToString();
        }
        if (totalPriceText != null)
        {
            totalPriceText.text = "구매 금액: " + total + " 달란";
        }
        if (balanceAfterText != null)
        {
            balanceAfterText.text = "구매 후 잔액: " + balanceAfter + " 달란";
        }
    }

    // 확인: 수량만큼 구매
    private void ConfirmBuy()
    {
        if (_pendingShopItem != null)
        {
            BuyItem(_pendingShopItem, _pendingQuantity);
        }
        CloseBuyConfirm();
    }

    // 취소
    private void CancelBuy()
    {
        CloseBuyConfirm();
    }

    // 팝업 닫기
    private void CloseBuyConfirm()
    {
        _pendingShopItem = null;
        _pendingQuantity = 1;

        if (buyConfirmPopup != null)
        {
            buyConfirmPopup.SetActive(false);
        }
    }

    // 수량 구매 처리
    public void BuyItem(ShopItemData shopItem, int quantity)
    {
        if (shopItem == null || shopItem.itemData == null)
        {
            return;
        }

        int totalPrice = shopItem.buyPrice * quantity;

        if (PlayerStats.Instance.gold < totalPrice)
        {
            dialogueText.text = "골드가 부족합니다!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        int boughtCount = 0;
        for (int i = 0; i < quantity; i++)
        {
            bool added = InventorySystem.Instance.AddItem(shopItem.itemData);
            if (added == false)
            {
                break;
            }
            boughtCount = boughtCount + 1;
        }

        if (boughtCount == 0)
        {
            dialogueText.text = "인벤토리가 가득 찼습니다!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        int spent = shopItem.buyPrice * boughtCount;
        PlayerStats.Instance.gold -= spent;

        if (shopItem.stock > 0)
        {
            shopItem.stock -= boughtCount;
        }

        dialogueText.text = shopItem.itemData.itemName + " " + boughtCount + "개 구매!";

        UpdateGoldDisplay();
        AudioManager.Instance?.PlaySFX(SFXClip.ItemPickup);
    }

    // 아이템 판매가 (구매가 × 비율)
    public int GetSellPrice(ItemData item)
    {
        if (item == null)
        {
            return 0;
        }

        return Mathf.RoundToInt(item.basePrice * sellPriceRatio);
    }

    // 인벤 아이템을 판매창에 담기 (인벤 슬롯 우클릭 시 호출됨)
    public void StageForSale(InventorySlot slot)
    {
        if (slot == null || slot.currentItem == null)
        {
            return;
        }

        if (currentTab != ShopTab.Sell)
        {
            return;
        }

        StagedSellItem entry = new StagedSellItem();
        entry.data = slot.currentItem;
        entry.instance = slot.CurrentInstance;

        InventorySystem.Instance.RemoveItemAtSlot(slot);
        _stagedItems.Add(entry);

        RefreshSellStaging();
    }

    // 판매창에서 빼서 인벤으로 되돌리기 (담긴 카드 우클릭 시)
    public void UnstageItem(ItemData data)
    {
        if (data == null)
        {
            return;
        }

        int index = -1;
        for (int i = 0; i < _stagedItems.Count; i++)
        {
            if (_stagedItems[i].data == data)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return;
        }

        ReturnStagedToInventory(_stagedItems[index]);
        _stagedItems.RemoveAt(index);

        RefreshSellStaging();
    }

    // 담긴 아이템 하나를 인벤으로 되돌리기 (내부용)
    private void ReturnStagedToInventory(StagedSellItem entry)
    {
        if (entry.instance != null)
        {
            InventorySystem.Instance.AddItem(entry.instance);
        }
        else if (entry.data != null)
        {
            InventorySystem.Instance.AddItem(entry.data);
        }
    }

    // 판매 확정: 담긴 것 모두 판매
    public void ConfirmSell()
    {
        if (_stagedItems.Count == 0)
        {
            dialogueText.text = "판매할 물건이 없어요.";
            return;
        }

        int total = 0;
        for (int i = 0; i < _stagedItems.Count; i++)
        {
            total = total + GetSellPrice(_stagedItems[i].data);
        }

        PlayerStats.Instance.gold += total;
        _stagedItems.Clear();

        dialogueText.text = total + " 달란에 판매했어요!";

        UpdateGoldDisplay();
        RefreshSellStaging();
        AudioManager.Instance?.PlaySFX(SFXClip.ItemPickup);
    }

    // 취소: 담긴 것 모두 인벤으로 되돌리기
    public void CancelSell()
    {
        for (int i = 0; i < _stagedItems.Count; i++)
        {
            ReturnStagedToInventory(_stagedItems[i]);
        }
        _stagedItems.Clear();

        RefreshSellStaging();
    }

    // 판매창 표시 갱신 (담긴 카드 + 금액/잔액)
    private void RefreshSellStaging()
    {
        ClearList();

        for (int i = 0; i < _stagedItems.Count; i++)
        {
            GameObject itemObj = Instantiate(shopItemPrefab, itemListContainer);
            ShopItemUI itemUI = itemObj.GetComponent<ShopItemUI>();
            itemUI.SetupSellItem(_stagedItems[i].data, this);
        }

        int total = 0;
        for (int i = 0; i < _stagedItems.Count; i++)
        {
            total = total + GetSellPrice(_stagedItems[i].data);
        }

        int balanceAfter = PlayerStats.Instance.gold + total;

        if (sellTotalText != null)
        {
            sellTotalText.text = "판매 금액: " + total + " 달란";
        }
        if (sellBalanceText != null)
        {
            sellBalanceText.text = "판매 후 잔액: " + balanceAfter + " 달란";
        }
    }
}

[System.Serializable]
public class ShopItemData
{
    public ItemData itemData;
    public int buyPrice;
    public int stock = -1; // -1 은 무제한
}