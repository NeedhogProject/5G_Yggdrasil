// ShopSystem.cs
// 상인 NPC(벨라) 패널
// 시작 메뉴(구매/판매/대화) → 구매 화면(우클릭 확인 팝업) / 판매 화면(판매창 스테이징)
// ESC: 팝업 닫기 -> 메뉴로 돌아가기 -> 상점 완전히 닫기 순서
// 버튼 연결을 EnsureSetup 으로 분리 — ShopUI 가 꺼진 채 시작해도 OpenShop 때 보장됨
// 대사 출력은 SetDialogue 를 통해 한 글자씩 타이핑 (대장장이/각인술사와 동일 방식)

using System.Collections;
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

    [Header("대사 타이핑 효과 (초당 간격, 0이면 즉시)")]
    [SerializeField] private float dialogueTextSpeed = 0.03f;

    [Header("상품 목록")]
    public Transform itemListContainer;
    public GameObject shopItemPrefab;

    [Header("판매 목록")]
    public List<ShopItemData> shopItems = new List<ShopItemData>();

    [Header("구매 확인 팝업")]
    public GameObject buyConfirmPopup;
    [Tooltip("구매 확인 팝업에 선택한 아이템 이미지를 표시할 Image")]
    public Image buyConfirmItemIcon;
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

    [Header("판매 확인 팝업 (구매 팝업과 동일 구조 — 수량 선택)")]
    public GameObject sellConfirmPopup;
    [Tooltip("판매 확인 팝업에 선택한 아이템 이미지를 표시할 Image")]
    public Image sellConfirmItemIcon;
    public TMP_Text sellConfirmItemNameText;
    public TMP_Text sellConfirmQuantityText;
    public TMP_Text sellConfirmPriceText;
    public TMP_Text sellConfirmBalanceText;
    public Button sellQuantityUpButton;
    public Button sellQuantityDownButton;
    public Button confirmSellSingleButton;
    public Button cancelSellSingleButton;

    [Header("판매 설정")]
    [SerializeField] private float sellPriceRatio = 0.3f;

    [Header("벨라 대사 — 일반 (대화하기 버튼)")]
    [TextArea(2, 4)]
    public string[] talkLines = new string[]
    {
        "정직한 가격으로 모십니다~! 어서오세요!",
        "필요한 게 있으신가요? 천천히 둘러보세요~",
        "헤헤, 찾아와 주셔서 감사합니다!"
    };

    [Header("벨라 대사 — 구매 성공")]
    [TextArea(2, 4)]
    public string[] buyLines = new string[]
    {
        "일단 한 번 보고 가세요~ 후회 안 한다니까?",
        "정말 좋은 선택이에요!",
        "좋은 물건은 좋은 주인을 만나야 하니까요!"
    };

    [Header("벨라 대사 — 판매 성공")]
    [TextArea(2, 4)]
    public string[] sellLines = new string[]
    {
        "오~ 그 물건 좋아보이는데요?",
        "상태가 좋네요! 이 정도면 충분히 값어치를 하겠어요!"
    };

    [Header("벨라 대사 — 돈 부족")]
    [TextArea(2, 4)]
    public string[] noMoneyLines = new string[]
    {
        "앗, 조금만 더 모아 오시면 될 것 같아요!",
        "괜찮아요! 다음에 다시 오시면 되니까요!"
    };

    [Header("벨라 대사 — 뒤로 가기")]
    [TextArea(2, 4)]
    public string[] backLines = new string[]
    {
        "오늘도 찾아와 주셔서 감사해요!",
        "좋은 하루 보내세요! 또 만나요!",
        "필요한 게 생기면 벨라를 찾아주세요~"
    };

    public static ShopSystem Instance { get; private set; }

    // 유물 전용 판매 모드 (학자 NPC 가 상점 판매창을 재활용할 때 켬)
    private bool _relicOnlyMode = false;

    // 학자 모드에서 X/ESC 로 닫을 때 돌아갈 NPC 대화 메뉴 콜백 (NPCInteractable 가 설정)
    public System.Action onBackToMenu = null;

    private InventoryUI _inventoryUI = null;
    private ShopItemData _pendingShopItem = null;
    private int _pendingQuantity = 1;
    private InventorySlot _pendingSellSlot = null;
    private int _pendingSellQuantity = 1;
    private int _talkIndex = 0;
    private List<StagedSellItem> _stagedItems = new List<StagedSellItem>();
    private bool isOpen = false;
    private ShopTab currentTab = ShopTab.Buy;

    // 진행 중인 타이핑 코루틴 (새 대사 나오면 멈추고 새로 시작)
    private Coroutine _typingCoroutine = null;

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
        public InventorySlot slot;    // 인벤에서 어둡게 마킹한 원본 슬롯
        public int quantity = 1;      // 판매창에 담은 수량 (묶음 아이템은 N개까지)
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

        if (sellConfirmPopup != null)
        {
            sellConfirmPopup.SetActive(false);
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
            closeButton.onClick.AddListener(OnCloseButtonClicked);
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

        if (sellQuantityUpButton != null)
        {
            sellQuantityUpButton.onClick.AddListener(OnSellQuantityUpClicked);
        }
        if (sellQuantityDownButton != null)
        {
            sellQuantityDownButton.onClick.AddListener(OnSellQuantityDownClicked);
        }
        if (confirmSellSingleButton != null)
        {
            confirmSellSingleButton.onClick.AddListener(ConfirmSellSingle);
        }
        if (cancelSellSingleButton != null)
        {
            cancelSellSingleButton.onClick.AddListener(CloseSellConfirm);
        }

        // 표시용 텍스트가 수량 버튼 위를 덮어 클릭을 가로채지 않도록 레이캐스트를 끈다
        // (특히 수량 숫자 텍스트 상자가 넓어 +/- 버튼을 덮는 문제 방지)
        DisableTextRaycast(confirmItemNameText);
        DisableTextRaycast(quantityText);
        DisableTextRaycast(totalPriceText);
        DisableTextRaycast(balanceAfterText);
        DisableTextRaycast(sellConfirmItemNameText);
        DisableTextRaycast(sellConfirmQuantityText);
        DisableTextRaycast(sellConfirmPriceText);
        DisableTextRaycast(sellConfirmBalanceText);

        _isSetup = true;
    }

    // 표시용 텍스트의 레이캐스트 타겟을 꺼서 아래 버튼 클릭을 막지 않게 한다
    private void DisableTextRaycast(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        Graphic graphic = text.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.raycastTarget = false;
        }
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
        // 설정창이 열려있으면 상점 열지 않음
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsSettingOpen == true)
        {
            return;
        }

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
        _relicOnlyMode = false;
        onBackToMenu = null;

        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);

        InventoryUI inventoryUI = ResolveInventoryUI();
        if (inventoryUI != null)
        {
            inventoryUI.CloseInventory();
        }

        // ShopUI(자기 자신) 끄기 — 다음에 OpenShop 때 다시 켜짐
        gameObject.SetActive(false);
    }

    // 학자 전용 — 상점 판매창을 유물 전용 모드로 바로 연다 (내부 선택 메뉴 없이 판매 화면으로)
    // 유물이 아닌 아이템은 StageForSale 에서 담기 거부되어 판매 불가 칸처럼 동작한다
    public void OpenRelicSell()
    {
        // ShopUI(자기 자신) 가 꺼져있으면 켜기
        if (gameObject.activeSelf == false)
        {
            gameObject.SetActive(true);
        }

        // 꺼진 채 시작했을 수 있으니 버튼 연결 보장
        EnsureSetup();

        _relicOnlyMode = true;
        isOpen = true;

        UpdateGoldDisplay();
        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);

        // 내부 선택 메뉴를 건너뛰고 바로 판매 화면으로
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }

        // 구매/판매 탭 버튼 숨김 (유물 판매 전용이라 탭 전환 불필요)
        SetTabsVisible(false);
        SwitchTab(ShopTab.Sell);
        OpenInventoryBeside();

        SetDialogue("유물을 판매창에 담아주세요! 유물만 받고 있어요.");
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
            SetDialogue("벨라: ...");
            return;
        }

        SetDialogue("벨라: " + talkLines[_talkIndex]);

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

        // ESC 는 X(닫기) 버튼과 동일하게 처리 (한 단계 뒤로)
        OnCloseButtonClicked();
    }

    // X(닫기) 버튼 처리 — 바로 종료하지 않고 한 단계씩 뒤로 간다
    // 구매 확인 팝업 열림: 팝업만 닫기
    // 구매/판매 창: 선택 메뉴로 복귀 (대화 종료 아님)
    // 선택 메뉴: 상점 완전 종료
    public void OnCloseButtonClicked()
    {
        // 구매 확인 팝업이 떠 있으면 팝업만 닫기
        if (buyConfirmPopup != null && buyConfirmPopup.activeSelf == true)
        {
            CloseBuyConfirm();
            return;
        }

        // 판매 확인 팝업이 떠 있으면 팝업만 닫기
        if (sellConfirmPopup != null && sellConfirmPopup.activeSelf == true)
        {
            CloseSellConfirm();
            return;
        }

        // 학자(유물 전용) 모드는 내부 선택 메뉴가 없으므로 닫고 학자 대화 메뉴로 복귀한다
        if (_relicOnlyMode == true)
        {
            GoBackToNpcMenu();
            return;
        }

        // 구매/판매 창이 떠 있으면 상점 전용 선택 메뉴로 돌아가기 (대화 종료 아님)
        if (shopPanel != null && shopPanel.activeSelf == true)
        {
            // 뒤로 가기 대사 출력 (메뉴 패널에 dialogueText 가 있으면 표시)
            ShowBackLine();
            ShowMenu();
            return;
        }

        // 선택 메뉴 화면이면 상점 완전히 닫기
        CloseShop();
    }

    // 학자 모드 전용 — 상점을 닫고 NPC 대화 선택 메뉴로 복귀한다
    // CloseShop 이 ShopUI 를 꺼버리므로 항상 켜져 있는 GameManager 에서 재오픈을 실행한다
    private void GoBackToNpcMenu()
    {
        // 콜백을 먼저 보관 (CloseShop 에서 비우므로)
        System.Action back = onBackToMenu;

        CloseShop();

        if (back == null)
        {
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartCoroutine(ReopenNpcMenuNextFrame(back));
        }
        else
        {
            back();
        }
    }

    // 한 프레임 뒤에 NPC 대화 메뉴를 다시 연다
    private IEnumerator ReopenNpcMenuNextFrame(System.Action back)
    {
        yield return null;

        if (back != null)
        {
            back();
        }
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

        SetDialogue("무엇을 구매하시겠어요?");
    }

    private void ShowSellTab()
    {
        if (sellControls != null)
        {
            sellControls.SetActive(true);
        }

        RefreshSellStaging();
        SetDialogue("팔 물건을 판매창에 담아주세요.");
    }

    // 단일 구매 (현재는 팝업 흐름이 대체, 호환용)
    public void BuyItem(ShopItemData shopItem)
    {
        if (PlayerStats.Instance.gold < shopItem.buyPrice)
        {
            SetDialogue(GetRandomLine(noMoneyLines));
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        if (InventorySystem.Instance.AddItem(shopItem.itemData) == false)
        {
            SetDialogue("인벤토리가 가득 찼습니다!");
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        PlayerStats.Instance.gold -= shopItem.buyPrice;

        SetDialogue(GetRandomLine(buyLines));

        UpdateGoldDisplay();
        AudioManager.Instance?.PlaySFX(SFXClip.ItemPickup);
    }

    private void UpdateGoldDisplay()
    {
        if (goldText == null)
        {
            return;
        }
        goldText.text = PlayerStats.Instance.gold.ToString();
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

        SetDialogue(GetRandomLine(buyLines));
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

        // 선택한 아이템 이미지 표시
        if (buyConfirmItemIcon != null)
        {
            Sprite icon = _pendingShopItem.itemData != null ? _pendingShopItem.itemData.itemIcon : null;
            if (icon != null)
            {
                buyConfirmItemIcon.sprite = icon;
                buyConfirmItemIcon.preserveAspect = true;
                buyConfirmItemIcon.enabled = true;
            }
            else
            {
                buyConfirmItemIcon.enabled = false;
            }
        }

        if (quantityText != null)
        {
            quantityText.text = _pendingQuantity.ToString();
        }
        if (totalPriceText != null)
        {
            totalPriceText.text = total.ToString();
        }
        if (balanceAfterText != null)
        {
            balanceAfterText.text = balanceAfter.ToString();
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

        if (buyConfirmItemIcon != null)
        {
            buyConfirmItemIcon.enabled = false;
        }

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
            SetDialogue(GetRandomLine(noMoneyLines));
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
            SetDialogue("인벤토리가 가득 찼습니다!");
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        int spent = shopItem.buyPrice * boughtCount;
        PlayerStats.Instance.gold -= spent;

        if (shopItem.stock > 0)
        {
            shopItem.stock -= boughtCount;
        }

        SetDialogue(GetRandomLine(buyLines));

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
    // 인벤에서 빼지 않고 슬롯을 어둡게 마킹만 한다 (실제 제거는 판매 확정 시)
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

        // 유물 전용 모드(학자)면 유물이 아닌 아이템은 담지 못한다 (판매 불가 칸 처리)
        if (_relicOnlyMode == true && slot.currentItem.itemType != ItemType.Relic)
        {
            SetDialogue("유물만 판매할 수 있어요!");
            return;
        }

        // 일반 상점에서는 유물을 판매할 수 없다 (유물은 유물 전용 판매창에서만 거래)
        if (_relicOnlyMode == false && slot.currentItem.itemType == ItemType.Relic)
        {
            SetDialogue("유물은 여기서 받지 않아요. 유물 판매에서 팔아주세요!");
            return;
        }

        // 미감정 유물은 판매 불가 (감정 후 판매)
        if (slot.CurrentInstance is RelicInstance relic && relic.IsIdentified == false)
        {
            SetDialogue("감정하지 않은 유물은 받을 수 없어요. 먼저 감정을 받아주세요!");
            return;
        }

        int maxQuantity = GetMaxSellQuantity(slot);

        // 이미 담긴 슬롯이면 수량을 1 늘린다 (보유 수량 한도까지)
        for (int i = 0; i < _stagedItems.Count; i++)
        {
            if (_stagedItems[i].slot == slot)
            {
                if (_stagedItems[i].quantity < maxQuantity)
                {
                    _stagedItems[i].quantity = _stagedItems[i].quantity + 1;
                    RefreshSellStaging();
                }
                else
                {
                    // 보유한 만큼 다 담아서 더 담을 수 없음
                    AudioManager.Instance?.PlaySFX(SFXClip.UIError);
                }
                return;
            }
        }

        StagedSellItem entry = new StagedSellItem();
        entry.data = slot.currentItem;
        entry.instance = slot.CurrentInstance;
        entry.slot = slot;
        entry.quantity = 1;

        // 인벤에선 그대로 두고 어둡게 표시만
        slot.SetSaleMarked(true);

        _stagedItems.Add(entry);

        RefreshSellStaging();
    }

    // 인벤 슬롯 우클릭으로 담기 취소 (어둡게 마킹된 슬롯을 다시 누름)
    public void UnstageSlot(InventorySlot slot)
    {
        if (slot == null)
        {
            return;
        }

        int index = -1;
        for (int i = 0; i < _stagedItems.Count; i++)
        {
            if (_stagedItems[i].slot == slot)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return;
        }

        // 마킹 해제 (인벤 그대로 복원)
        slot.SetSaleMarked(false);
        _stagedItems.RemoveAt(index);

        RefreshSellStaging();
    }

    // 판매창 카드 우클릭으로 빼기 (마킹 해제, 인벤은 원래 그대로라 복원 불필요)
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

        // 해당 슬롯 마킹 해제 (인벤에 아이템은 그대로 있음)
        if (_stagedItems[index].slot != null)
        {
            _stagedItems[index].slot.SetSaleMarked(false);
        }
        _stagedItems.RemoveAt(index);

        RefreshSellStaging();
    }

    // 담긴 표시 해제 (내부용) — 인벤에선 빼지 않았으므로 마킹만 푼다
    private void ReturnStagedToInventory(StagedSellItem entry)
    {
        if (entry.slot != null)
        {
            entry.slot.SetSaleMarked(false);
        }
    }

    // 판매 확정: 담긴 것 모두 판매 (이때 인벤에서 실제 제거)
    public void ConfirmSell()
    {
        if (_stagedItems.Count == 0)
        {
            SetDialogue("판매할 물건이 없어요.");
            return;
        }

        int total = 0;

        // 담긴 목록을 복사해 처리한다 (유물 판매가 원본 목록을 수정할 수 있으므로)
        List<StagedSellItem> pending = new List<StagedSellItem>(_stagedItems);

        for (int i = 0; i < pending.Count; i++)
        {
            StagedSellItem entry = pending[i];
            InventorySlot slot = entry.slot;
            if (slot == null)
            {
                continue;
            }

            int unitPrice = GetSellPrice(entry.data);
            int quantity = entry.quantity;
            if (quantity < 1)
            {
                quantity = 1;
            }

            // 제거 전에 마킹 해제 (색 원복)
            slot.SetSaleMarked(false);

            // 유물은 한 칸에 쌓이지 않으므로 같은 종류 유물 칸을 수량만큼 찾아 판매한다
            // (골드 지급과 담긴 목록 정리는 SellRelicsByCount 내부에서 처리)
            if (slot.CurrentInstance is RelicInstance)
            {
                _pendingSellSlot = slot;
                SellRelicsByCount(entry.data, quantity, unitPrice);
                _pendingSellSlot = null;
                continue;
            }

            // 일반 아이템: 묶음에서 판매 수량만큼 차감, 0이 되면 슬롯 비우기
            ItemInstance instance = slot.CurrentInstance;
            if (instance != null)
            {
                instance.RemoveStack(quantity);

                if (instance.IsEmpty == true)
                {
                    InventorySystem.Instance.RemoveItemAtSlot(slot);
                }
                else
                {
                    slot.SetItem(instance);
                }

                total = total + unitPrice * quantity;
            }
        }

        // 남은 담긴 목록 정리 (유물 처리로 일부는 이미 빠졌을 수 있음)
        for (int i = 0; i < _stagedItems.Count; i++)
        {
            if (_stagedItems[i].slot != null)
            {
                _stagedItems[i].slot.SetSaleMarked(false);
            }
        }
        _stagedItems.Clear();

        // 유물 판매 골드는 SellRelicsByCount 가 이미 더했고, 여기서는 일반 아이템 골드만 지급
        PlayerStats.Instance.gold += total;

        SetDialogue(GetRandomLine(sellLines));

        UpdateGoldDisplay();
        RefreshSellStaging();
        AudioManager.Instance?.PlaySFX(SFXClip.ItemPickup);
    }

    // 판매창에 담긴 아이템을 우클릭하면 판매 확인 팝업을 연다 (InventorySlot 에서 호출)
    public void OpenSellConfirm(InventorySlot slot)
    {
        if (slot == null || slot.currentItem == null)
        {
            return;
        }

        // 아직 담기지 않은 아이템이면 먼저 1개 담는다 (수량 선택창을 바로 열 수 있도록)
        StagedSellItem existing = FindStaged(slot);
        if (existing == null)
        {
            StageForSale(slot);
            existing = FindStaged(slot);
        }
        if (existing == null)
        {
            // 담기지 못한 아이템 (판매 불가 등) 이면 팝업을 열지 않는다
            return;
        }

        _pendingSellSlot = slot;
        _pendingSellQuantity = existing.quantity;

        if (sellConfirmPopup != null)
        {
            sellConfirmPopup.SetActive(true);
        }

        RefreshSellConfirm();
    }

    // 해당 슬롯이 판매창에 담겨 있으면 그 항목을 돌려준다 (없으면 null)
    private StagedSellItem FindStaged(InventorySlot slot)
    {
        for (int i = 0; i < _stagedItems.Count; i++)
        {
            if (_stagedItems[i].slot == slot)
            {
                return _stagedItems[i];
            }
        }
        return null;
    }

    // 판매 가능한 최대 수량 (슬롯의 현재 묶음 수)
    private int GetMaxSellQuantity(InventorySlot slot)
    {
        if (slot == null || slot.CurrentInstance == null)
        {
            return 1;
        }

        // 유물은 한 칸에 쌓이지 않으므로, 같은 종류(감정된) 유물 칸 수를 최대 수량으로 쓴다
        if (slot.CurrentInstance is RelicInstance)
        {
            return Mathf.Max(1, CountSameRelics(slot.CurrentInstance.Data));
        }

        return Mathf.Max(1, slot.CurrentInstance.StackCount);
    }

    // 인벤토리에서 같은 종류의 감정된 유물이 들어있는 칸 수를 센다 (유물 묶음 판매용)
    private int CountSameRelics(ItemData relicData)
    {
        if (relicData == null || InventorySystem.Instance == null)
        {
            return 0;
        }

        int count = 0;
        List<InventorySlot> allSlots = InventorySystem.Instance.slots;
        for (int i = 0; i < allSlots.Count; i++)
        {
            InventorySlot current = allSlots[i];
            if (current == null || current.IsOwner == false)
            {
                continue;
            }

            if (current.CurrentInstance is RelicInstance relic && relic.IsIdentified == true && relic.Data == relicData)
            {
                count = count + 1;
            }
        }
        return count;
    }

    // 판매 수량 변경 (± 버튼, 구매와 동일 방식)
    private void ChangeSellQuantity(int delta)
    {
        if (_pendingSellSlot == null)
        {
            return;
        }

        int maxQ = GetMaxSellQuantity(_pendingSellSlot);
        _pendingSellQuantity = _pendingSellQuantity + delta;

        if (_pendingSellQuantity < 1)
        {
            _pendingSellQuantity = 1;
        }
        if (_pendingSellQuantity > maxQ)
        {
            _pendingSellQuantity = maxQ;
        }

        RefreshSellConfirm();
    }

    private void OnSellQuantityUpClicked()
    {
        ChangeSellQuantity(1);
    }

    private void OnSellQuantityDownClicked()
    {
        ChangeSellQuantity(-1);
    }

    // 판매 확인 팝업 내용 갱신 (이름, 수량, 판매 금액, 판매 후 잔액)
    private void RefreshSellConfirm()
    {
        if (_pendingSellSlot == null || _pendingSellSlot.currentItem == null)
        {
            return;
        }

        int total = GetSellPrice(_pendingSellSlot.currentItem) * _pendingSellQuantity;
        int balanceAfter = PlayerStats.Instance.gold + total;

        // 선택한 아이템 이미지 표시
        if (sellConfirmItemIcon != null)
        {
            Sprite icon = _pendingSellSlot.currentItem.itemIcon;
            if (icon != null)
            {
                sellConfirmItemIcon.sprite = icon;
                sellConfirmItemIcon.preserveAspect = true;
                sellConfirmItemIcon.enabled = true;
            }
            else
            {
                sellConfirmItemIcon.enabled = false;
            }
        }

        if (sellConfirmItemNameText != null)
        {
            sellConfirmItemNameText.text = _pendingSellSlot.currentItem.itemName;
        }
        if (sellConfirmQuantityText != null)
        {
            sellConfirmQuantityText.text = _pendingSellQuantity.ToString();
        }
        if (sellConfirmPriceText != null)
        {
            sellConfirmPriceText.text = total.ToString();
        }
        if (sellConfirmBalanceText != null)
        {
            sellConfirmBalanceText.text = balanceAfter.ToString();
        }
    }

    // 확인 버튼: 선택한 수량만큼 판매창에 담는다 (실제 판매는 판매 버튼에서 한꺼번에)
    public void ConfirmSellSingle()
    {
        if (_pendingSellSlot == null || _pendingSellSlot.currentItem == null)
        {
            CloseSellConfirm();
            return;
        }

        int maxQuantity = GetMaxSellQuantity(_pendingSellSlot);
        int quantity = Mathf.Clamp(_pendingSellQuantity, 1, maxQuantity);

        // 이미 담긴 항목이면 수량만 바꾸고, 없으면 새로 담는다
        StagedSellItem entry = FindStaged(_pendingSellSlot);
        if (entry == null)
        {
            entry = new StagedSellItem();
            entry.data = _pendingSellSlot.currentItem;
            entry.instance = _pendingSellSlot.CurrentInstance;
            entry.slot = _pendingSellSlot;

            _pendingSellSlot.SetSaleMarked(true);
            _stagedItems.Add(entry);
        }
        entry.quantity = quantity;

        RefreshSellStaging();
        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);

        CloseSellConfirm();
    }

    // 같은 종류의 감정된 유물을 지정 수량만큼 인벤에서 찾아 판매 처리한다 (유물 전용)
    // 담긴(마킹된) 팝업 대상 칸을 먼저 처리하고, 모자라면 같은 종류의 다른 칸을 추가로 판다
    private void SellRelicsByCount(ItemData relicData, int count, int unitPrice)
    {
        if (relicData == null || InventorySystem.Instance == null)
        {
            return;
        }

        // 판매 대상 칸 모으기 (1순위: 현재 팝업 대상 칸, 2순위: 같은 종류의 다른 감정 유물 칸)
        List<InventorySlot> targets = new List<InventorySlot>();

        if (_pendingSellSlot != null)
        {
            targets.Add(_pendingSellSlot);
        }

        List<InventorySlot> allSlots = InventorySystem.Instance.slots;
        for (int i = 0; i < allSlots.Count; i++)
        {
            InventorySlot current = allSlots[i];
            if (current == null || current.IsOwner == false)
            {
                continue;
            }
            if (current == _pendingSellSlot)
            {
                continue;
            }

            if (current.CurrentInstance is RelicInstance relic && relic.IsIdentified == true && relic.Data == relicData)
            {
                targets.Add(current);
            }
        }

        // 모은 칸에서 count 개 만큼만 실제 판매
        int sold = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            if (sold >= count)
            {
                break;
            }

            InventorySlot current = targets[i];

            // 담긴 목록에서 제거
            for (int j = _stagedItems.Count - 1; j >= 0; j--)
            {
                if (_stagedItems[j].slot == current)
                {
                    _stagedItems.RemoveAt(j);
                }
            }

            current.SetSaleMarked(false);
            InventorySystem.Instance.RemoveItemAtSlot(current);
            sold = sold + 1;
        }

        PlayerStats.Instance.gold += unitPrice * sold;
    }

    // 취소 버튼: 팝업만 닫는다
    public void CloseSellConfirm()
    {
        _pendingSellSlot = null;
        _pendingSellQuantity = 1;

        if (sellConfirmItemIcon != null)
        {
            sellConfirmItemIcon.enabled = false;
        }

        if (sellConfirmPopup != null)
        {
            sellConfirmPopup.SetActive(false);
        }
    }

    // 취소: 담긴 것 모두 마킹 해제 (인벤은 그대로라 추가 복원 불필요)
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
            itemUI.SetupSellItem(_stagedItems[i].data, _stagedItems[i].slot, this, _stagedItems[i].quantity);
        }

        int total = 0;
        for (int i = 0; i < _stagedItems.Count; i++)
        {
            total = total + GetSellPrice(_stagedItems[i].data) * _stagedItems[i].quantity;
        }

        int balanceAfter = PlayerStats.Instance.gold + total;

        if (sellTotalText != null)
        {
            sellTotalText.text = total.ToString();
        }
        if (sellBalanceText != null)
        {
            sellBalanceText.text = balanceAfter.ToString();
        }
    }

    // 대사 출력 (타이핑 효과, null 체크 포함)
    // 기존 호출부는 그대로 두고 이 메서드만 바꿔서 모든 대사에 타이핑 적용
    private void SetDialogue(string text)
    {
        if (dialogueText == null)
        {
            return;
        }

        // 이전 타이핑이 진행 중이면 멈춘다
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        // 속도가 0 이하면 즉시 표시
        if (dialogueTextSpeed <= 0f)
        {
            dialogueText.text = text;
            return;
        }

        // 오브젝트가 비활성이면 코루틴을 못 돌리므로 즉시 표시
        if (gameObject.activeInHierarchy == false)
        {
            dialogueText.text = text;
            return;
        }

        _typingCoroutine = StartCoroutine(TypeDialogue(text));
    }

    // 한 글자씩 출력하는 코루틴 (WaitForSecondsRealtime 로 일시정지 영향 없음)
    private IEnumerator TypeDialogue(string text)
    {
        dialogueText.text = "";

        int i = 0;
        int length = text.Length;

        while (i < length)
        {
            dialogueText.text = dialogueText.text + text[i];
            i = i + 1;
            yield return new WaitForSecondsRealtime(dialogueTextSpeed);
        }

        _typingCoroutine = null;
    }

    // 배열에서 랜덤 대사 한 줄 반환 (비어있으면 빈 문자열)
    private string GetRandomLine(string[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            return "";
        }
        int index = Random.Range(0, lines.Length);
        return lines[index];
    }

    // 뒤로 가기 대사 출력
    private void ShowBackLine()
    {
        string line = GetRandomLine(backLines);
        if (line.Length > 0)
        {
            SetDialogue(line);
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