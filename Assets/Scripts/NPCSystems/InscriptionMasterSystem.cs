/*
 * InscriptionMasterSystem.cs
 * 각인술사 NPC 패널 — 각인하기 / 각인 초기화하기 두 화면.
 *   · 공통: 좌측 방어구 목록 + 하단 에르델 마르 대사바
 *   · 각인하기: 방어구 선택 + 우측 자원 목록 클릭으로 원소 선택 + 자원·골드 소모
 *   · 초기화하기: 방어구 선택 + 초기화권으로 각인 제거 (확인 버튼)
 * 실제 자원/골드 소모와 각인 처리는 RuneInscriptionSystem 이 담당한다.
 * 각인 슬롯은 부위당 1개.
 * 담당: 김보민
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class InscriptionMasterSystem : MonoBehaviour
{
    public static InscriptionMasterSystem Instance { get; private set; }

    [Header("공통 UI 참조")]
    public GameObject inscriptionPanel;
    public GameObject inscribeView; // 각인하기 화면
    public GameObject resetView;    // 각인 초기화하기 화면
    public TMP_Text dialogueText;
    public TMP_Text npcNameText;
    public Button closeButton;

    [Header("방어구 목록 (공통)")]
    public Transform armorListContainer;
    public GameObject armorCardPrefab; // InscriptionItemCard 프리팹

    [Header("각인하기 화면 — 선택 정보")]
    public Image inscribeArmorIcon;
    public TMP_Text inscribeArmorNameText;
    public TMP_Text inscribeArmorSlotText;     // 슬롯 상태 (예: "0/1", "1/1")
    public Image selectedElementIcon;
    public TMP_Text selectedElementNameText;   // 예: "불의 각인"
    public TMP_Text elementResourceText;       // 보유/필요 (예: "12/5"), 부족 시 빨강

    [Header("각인하기 화면 — 비용 / 버튼")]
    public TMP_Text goldCostText;     // 각인 비용
    public TMP_Text goldBalanceText;  // 각인 후 잔액
    public Button inscribeButton;     // 장비 각인

    [Header("각인하기 화면 — 자원 목록 (불/물/바람/땅/어둠 5개)")]
    public ResourceRowUI[] resourceRows;

    [Header("초기화하기 화면")]
    public Image resetArmorIcon;
    public TMP_Text resetArmorNameText;
    public TMP_Text resetArmorSlotText;
    public Image resetScrollIcon;
    public TMP_Text resetScrollCountText; // 예: "1/1"
    public Button confirmButton;          // 확인 (각인 제거)

    [Header("대화하기 대사")]
    [TextArea(2, 4)]
    public string[] talkLines = new string[]
    {
        "각인은 장비에 영혼을 새기는 일이라네. 신중히 고르게.",
        "같은 원소를 모아 입으면 그 힘이 깨어나지.",
        "어둠은 하나만으로도 깨어난다네. 다루기 까다롭지만.",
        "한 번 새긴 각인은 초기화권 없이는 지울 수 없으이."
    };

    // 화면 모드
    private enum ViewMode
    {
        Inscribe,
        Reset
    }

    private ViewMode _mode = ViewMode.Inscribe;

    private int _talkIndex = 0;

    // 현재 선택된 방어구와 카드
    private ArmorInstance _selectedArmor = null;
    private InscriptionItemUI _selectedCard = null;

    // 현재 선택된 각인 원소 (기본값 불)
    private RuneElement _selectedElement = RuneElement.Fire;

    // 생성된 방어구 카드 목록 (갱신 시 정리용)
    private List<InscriptionItemUI> _spawnedCards = new List<InscriptionItemUI>();

    private bool _isOpen = false;

    // 각인술사 패널이 열려 있는지
    public bool IsOpen => _isOpen;

    // ESC 로 패널을 닫을 때 돌아갈 동작 (NPC 대화 메뉴 재오픈). NPCInteractable 가 설정한다.
    public System.Action onBackToMenu = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (inscriptionPanel != null)
        {
            inscriptionPanel.SetActive(false);
        }

        if (inscribeButton != null)
        {
            inscribeButton.onClick.AddListener(OnInscribeClicked);
        }
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnResetConfirmClicked);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseInscriptionMaster);
        }
    }

    // ─────────────────────── 화면 열기 (외부/NPCDialogue 에서 호출) ───────────────────────

    // 각인하기 화면 열기
    public void OpenInscribeScreen()
    {
        _isOpen = true;
        _mode = ViewMode.Inscribe;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SFXClip.UIOpen);
        }

        if (inscriptionPanel != null)
        {
            inscriptionPanel.SetActive(true);
        }
        if (inscribeView != null)
        {
            inscribeView.SetActive(true);
        }
        if (resetView != null)
        {
            resetView.SetActive(false);
        }

        RefreshArmorList();

        _selectedArmor = null;
        _selectedCard = null;
        _selectedElement = RuneElement.Fire;

        UpdateResourceRows();
        ClearInscribePreview();

        SetDialogue("각인할 방어구와 자원을 고르게.");
    }

    // 각인 초기화하기 화면 열기
    public void OpenResetScreen()
    {
        _isOpen = true;
        _mode = ViewMode.Reset;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SFXClip.UIOpen);
        }

        if (inscriptionPanel != null)
        {
            inscriptionPanel.SetActive(true);
        }
        if (inscribeView != null)
        {
            inscribeView.SetActive(false);
        }
        if (resetView != null)
        {
            resetView.SetActive(true);
        }

        RefreshArmorList();

        _selectedArmor = null;
        _selectedCard = null;

        ClearResetPreview();

        SetDialogue("각인을 지울 장비를 고르게.");
    }

    // 구버전 호환 진입점 — NPCInteractable 등 기존 호출부가 사용한다.
    // 기본은 각인하기 화면을 연다. (초기화 화면은 OpenResetScreen 로 별도 진입)
    public void OpenInscriptionMaster()
    {
        OpenInscribeScreen();
    }

    public void CloseInscriptionMaster()
    {
        if (inscriptionPanel != null)
        {
            inscriptionPanel.SetActive(false);
        }

        _isOpen = false;
        _selectedArmor = null;
        _selectedCard = null;
        onBackToMenu = null;

        ClearArmorList();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SFXClip.UIClose);
        }
    }

    // ESC: 패널이 떠 있으면 닫는다.
    private void Update()
    {
        if (_isOpen == false)
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

        GoBackToMenu();
    }

    // ESC: 패널을 닫고 이전 화면(NPC 대화 메뉴)으로 돌아간다.
    private void GoBackToMenu()
    {
        // 콜백을 먼저 보관 (CloseInscriptionMaster 에서 비우므로)
        System.Action back = onBackToMenu;

        CloseInscriptionMaster();

        if (back != null)
        {
            // 같은 프레임에 대화창이 ESC 로 다시 닫히는 것을 막기 위해 한 프레임 뒤에 연다.
            StartCoroutine(ReopenMenuNextFrame(back));
        }
    }

    // 한 프레임 뒤에 NPC 대화 메뉴를 다시 연다.
    private IEnumerator ReopenMenuNextFrame(System.Action back)
    {
        yield return null;

        if (back != null)
        {
            back();
        }
    }

    // ─────────────────────── 방어구 목록 ───────────────────────

    // 인벤토리에서 방어구들을 모아 좌측 목록에 표시
    // 정렬: 부위(투구 > 갑옷 > 각반 > 장화), 같은 부위는 이름 순
    private void RefreshArmorList()
    {
        ClearArmorList();

        if (InventorySystem.Instance == null)
        {
            return;
        }
        if (armorCardPrefab == null || armorListContainer == null)
        {
            return;
        }

        List<ArmorInstance> armors = new List<ArmorInstance>();
        List<ItemInstance> all = InventorySystem.Instance.GetAllInstances();

        for (int i = 0; i < all.Count; i++)
        {
            ArmorInstance armor = all[i] as ArmorInstance;
            if (armor != null)
            {
                armors.Add(armor);
            }
        }

        armors.Sort(CompareArmor);

        for (int i = 0; i < armors.Count; i++)
        {
            GameObject cardObj = Instantiate(armorCardPrefab, armorListContainer);
            InscriptionItemUI card = cardObj.GetComponent<InscriptionItemUI>();
            if (card != null)
            {
                card.Setup(armors[i], this);
                _spawnedCards.Add(card);
            }
        }
    }

    // 방어구 정렬 비교: 부위(투구0 > 갑옷1 > 각반2 > 장화3) 오름차순, 같은 부위는 이름 순
    private int CompareArmor(ArmorInstance a, ArmorInstance b)
    {
        ArmorData dataA = a.ArmorData;
        ArmorData dataB = b.ArmorData;

        if (dataA == null || dataB == null)
        {
            return 0;
        }

        int slotA = (int)dataA.ArmorSlot;
        int slotB = (int)dataB.ArmorSlot;

        if (slotA != slotB)
        {
            return slotA.CompareTo(slotB);
        }

        return string.Compare(dataA.itemName, dataB.itemName);
    }

    private void ClearArmorList()
    {
        for (int i = 0; i < _spawnedCards.Count; i++)
        {
            if (_spawnedCards[i] != null)
            {
                Destroy(_spawnedCards[i].gameObject);
            }
        }
        _spawnedCards.Clear();
    }

    // ─────────────────────── 방어구 선택 ───────────────────────

    // 방어구 카드 클릭 시 InscriptionItemUI 가 호출
    public void SelectArmorFromList(InscriptionItemUI card)
    {
        if (card == null || card.Armor == null)
        {
            return;
        }

        if (_selectedCard != null)
        {
            _selectedCard.SetSelected(false);
        }

        _selectedCard = card;
        _selectedCard.SetSelected(true);
        _selectedArmor = card.Armor;

        if (_mode == ViewMode.Inscribe)
        {
            UpdateInscribePreview();
        }
        else
        {
            UpdateResetPreview();
        }
    }

    // ─────────────────────── 원소 선택 (자원 목록 클릭) ───────────────────────

    // ResourceRowUI 가 호출
    public void SelectElement(RuneElement element)
    {
        if (element == RuneElement.None)
        {
            return;
        }

        _selectedElement = element;
        UpdateResourceRows();
        UpdateInscribePreview();
    }

    // 자원 목록 줄들의 보유 수량 + 선택 강조 갱신
    private void UpdateResourceRows()
    {
        if (resourceRows == null)
        {
            return;
        }

        ResourceInventory inv = ResourceInventory.Instance;

        for (int i = 0; i < resourceRows.Length; i++)
        {
            if (resourceRows[i] == null)
            {
                continue;
            }

            int owned = 0;
            if (inv != null)
            {
                InscriptionType type = RuneInscriptionSystem.ToInscriptionType(resourceRows[i].Element);
                owned = inv.GetResourceCount(type);
            }

            resourceRows[i].SetCount(owned);
            resourceRows[i].SetSelected(resourceRows[i].Element == _selectedElement);
        }
    }

    // ─────────────────────── 각인하기 미리보기 ───────────────────────

    private void UpdateInscribePreview()
    {
        if (_selectedArmor == null)
        {
            ClearInscribePreview();
            return;
        }

        ArmorData data = _selectedArmor.ArmorData;
        if (data == null)
        {
            ClearInscribePreview();
            return;
        }

        // 선택 방어구
        if (inscribeArmorIcon != null)
        {
            inscribeArmorIcon.sprite = data.itemIcon;
            inscribeArmorIcon.enabled = true;
        }
        if (inscribeArmorNameText != null)
        {
            inscribeArmorNameText.text = data.itemName;
        }
        if (inscribeArmorSlotText != null)
        {
            inscribeArmorSlotText.text = GetSlotStateText(_selectedArmor);
        }

        // 선택 원소
        if (selectedElementIcon != null)
        {
            selectedElementIcon.color = RuneInscriptionSystem.GetElementColor(_selectedElement);
            selectedElementIcon.enabled = true;
        }
        if (selectedElementNameText != null)
        {
            selectedElementNameText.text = RuneInscriptionSystem.GetElementName(_selectedElement) + "의 각인";
            selectedElementNameText.color = RuneInscriptionSystem.GetElementColor(_selectedElement);
        }

        // 자원 보유/필요
        int resourceCost = 0;
        int goldCost = 0;
        if (RuneInscriptionSystem.Instance != null)
        {
            resourceCost = RuneInscriptionSystem.Instance.GetResourceCost(data.ArmorSlot);
            goldCost = RuneInscriptionSystem.Instance.GetGoldCost(data.ArmorSlot);
        }

        int ownedResource = 0;
        if (ResourceInventory.Instance != null)
        {
            InscriptionType type = RuneInscriptionSystem.ToInscriptionType(_selectedElement);
            ownedResource = ResourceInventory.Instance.GetResourceCount(type);
        }

        if (elementResourceText != null)
        {
            elementResourceText.text = ownedResource.ToString() + "/" + resourceCost.ToString();
            if (ownedResource < resourceCost)
            {
                elementResourceText.color = Color.red;
            }
            else
            {
                elementResourceText.color = Color.white;
            }
        }

        // 골드 비용 / 잔액
        int gold = 0;
        if (PlayerStats.Instance != null)
        {
            gold = PlayerStats.Instance.gold;
        }

        if (goldCostText != null)
        {
            goldCostText.text = goldCost.ToString() + " 달란";
        }
        if (goldBalanceText != null)
        {
            goldBalanceText.text = (gold - goldCost).ToString() + " 달란";
        }

        // 버튼 활성화: 각인 안 됨 + 자원 충분 + 골드 충분
        bool notInscribed = _selectedArmor.HasRune == false;
        bool enoughResource = ownedResource >= resourceCost;
        bool enoughGold = gold >= goldCost;

        if (inscribeButton != null)
        {
            inscribeButton.interactable = notInscribed == true && enoughResource == true && enoughGold == true;
        }
    }

    private void ClearInscribePreview()
    {
        if (inscribeArmorIcon != null)
        {
            inscribeArmorIcon.sprite = null;
            inscribeArmorIcon.enabled = false;
        }
        if (inscribeArmorNameText != null)
        {
            inscribeArmorNameText.text = "";
        }
        if (inscribeArmorSlotText != null)
        {
            inscribeArmorSlotText.text = "";
        }
        if (selectedElementNameText != null)
        {
            selectedElementNameText.text = RuneInscriptionSystem.GetElementName(_selectedElement) + "의 각인";
            selectedElementNameText.color = RuneInscriptionSystem.GetElementColor(_selectedElement);
        }
        if (selectedElementIcon != null)
        {
            selectedElementIcon.color = RuneInscriptionSystem.GetElementColor(_selectedElement);
        }
        if (elementResourceText != null)
        {
            elementResourceText.text = "";
            elementResourceText.color = Color.white;
        }
        if (goldCostText != null)
        {
            goldCostText.text = "";
        }
        if (goldBalanceText != null)
        {
            goldBalanceText.text = "";
        }
        if (inscribeButton != null)
        {
            inscribeButton.interactable = false;
        }
    }

    // ─────────────────────── 초기화하기 미리보기 ───────────────────────

    private void UpdateResetPreview()
    {
        if (_selectedArmor == null)
        {
            ClearResetPreview();
            return;
        }

        ArmorData data = _selectedArmor.ArmorData;
        if (data == null)
        {
            ClearResetPreview();
            return;
        }

        if (resetArmorIcon != null)
        {
            resetArmorIcon.sprite = data.itemIcon;
            resetArmorIcon.enabled = true;
        }
        if (resetArmorNameText != null)
        {
            resetArmorNameText.text = data.itemName;
        }
        if (resetArmorSlotText != null)
        {
            resetArmorSlotText.text = GetSlotStateText(_selectedArmor);
        }

        int scrolls = 0;
        if (RuneInscriptionSystem.Instance != null)
        {
            scrolls = RuneInscriptionSystem.Instance.GetResetScrollCount();
        }

        if (resetScrollCountText != null)
        {
            resetScrollCountText.text = scrolls.ToString() + "/1";
        }

        // 버튼 활성화: 각인 있음 + 초기화권 보유
        bool hasRune = _selectedArmor.HasRune == true;
        bool hasScroll = scrolls >= 1;

        if (confirmButton != null)
        {
            confirmButton.interactable = hasRune == true && hasScroll == true;
        }
    }

    private void ClearResetPreview()
    {
        if (resetArmorIcon != null)
        {
            resetArmorIcon.sprite = null;
            resetArmorIcon.enabled = false;
        }
        if (resetArmorNameText != null)
        {
            resetArmorNameText.text = "";
        }
        if (resetArmorSlotText != null)
        {
            resetArmorSlotText.text = "";
        }

        int scrolls = 0;
        if (RuneInscriptionSystem.Instance != null)
        {
            scrolls = RuneInscriptionSystem.Instance.GetResetScrollCount();
        }
        if (resetScrollCountText != null)
        {
            resetScrollCountText.text = scrolls.ToString() + "/1";
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }
    }

    // ─────────────────────── 각인 부여 버튼 ───────────────────────

    private void OnInscribeClicked()
    {
        if (_selectedArmor == null)
        {
            SetDialogue("먼저 각인할 방어구를 선택해주게.");
            return;
        }

        if (RuneInscriptionSystem.Instance == null)
        {
            return;
        }

        InscribeResult result = RuneInscriptionSystem.Instance.TryInscribe(_selectedArmor, _selectedElement);
        string elementName = RuneInscriptionSystem.GetElementName(_selectedElement);
        string armorName = _selectedArmor.ArmorData != null ? _selectedArmor.ArmorData.itemName : "방어구";

        switch (result)
        {
            case InscribeResult.Success:
                {
                    SetDialogue(armorName + "에 " + elementName + " 각인을 새겼네.");
                    PlaySFX(SFXClip.InscribeApply);
                    break;
                }
            case InscribeResult.AlreadyInscribed:
                {
                    SetDialogue("이미 각인된 방어구일세. 초기화가 필요하이.");
                    PlaySFX(SFXClip.UIError);
                    break;
                }
            case InscribeResult.NotEnoughResource:
                {
                    SetDialogue(elementName + " 자원이 부족하군!");
                    PlaySFX(SFXClip.UIError);
                    break;
                }
            case InscribeResult.NotEnoughGold:
                {
                    SetDialogue("골드가 부족하군!");
                    PlaySFX(SFXClip.UIError);
                    break;
                }
            case InscribeResult.NoArmor:
                {
                    SetDialogue("먼저 각인할 방어구를 선택해주게.");
                    break;
                }
            default:
                {
                    SetDialogue("지금은 각인할 수 없네.");
                    PlaySFX(SFXClip.UIError);
                    break;
                }
        }

        if (result == InscribeResult.Success)
        {
            if (_selectedCard != null)
            {
                _selectedCard.Setup(_selectedArmor, this);
                _selectedCard.SetSelected(true);
            }
            UpdateResourceRows();
        }

        UpdateInscribePreview();
    }

    // ─────────────────────── 각인 초기화 확인 버튼 ───────────────────────

    private void OnResetConfirmClicked()
    {
        if (_selectedArmor == null)
        {
            SetDialogue("먼저 초기화할 방어구를 선택해주게.");
            return;
        }

        if (RuneInscriptionSystem.Instance == null)
        {
            return;
        }

        ResetResult result = RuneInscriptionSystem.Instance.TryReset(_selectedArmor);
        string armorName = _selectedArmor.ArmorData != null ? _selectedArmor.ArmorData.itemName : "방어구";

        switch (result)
        {
            case ResetResult.Success:
                {
                    SetDialogue(armorName + "의 각인을 지웠네.");
                    PlaySFX(SFXClip.InscribeReset);
                    break;
                }
            case ResetResult.NoRune:
                {
                    SetDialogue("각인되지 않은 방어구일세.");
                    break;
                }
            case ResetResult.NoScroll:
                {
                    SetDialogue("각인 초기화권이 필요하네!");
                    PlaySFX(SFXClip.UIError);
                    break;
                }
            case ResetResult.NoArmor:
                {
                    SetDialogue("먼저 초기화할 방어구를 선택해주게.");
                    break;
                }
        }

        if (result == ResetResult.Success)
        {
            if (_selectedCard != null)
            {
                _selectedCard.Setup(_selectedArmor, this);
                _selectedCard.SetSelected(true);
            }
        }

        UpdateResetPreview();
    }

    // ─────────────────────── 대화하기 ───────────────────────

    // 외부(NPCDialogue 등)에서 호출 — 누를 때마다 대사가 바뀐다.
    public void TalkOnce()
    {
        if (talkLines == null || talkLines.Length == 0)
        {
            SetDialogue("각인술사: ...");
            return;
        }

        SetDialogue("각인술사: " + talkLines[_talkIndex]);

        _talkIndex = _talkIndex + 1;
        if (_talkIndex >= talkLines.Length)
        {
            _talkIndex = 0;
        }
    }

    // ─────────────────────── 유틸 ───────────────────────

    // 슬롯 상태 문자열 (각인 있으면 1/1, 없으면 0/1)
    private string GetSlotStateText(ArmorInstance armor)
    {
        if (armor == null)
        {
            return "0/1";
        }
        if (armor.HasRune == true)
        {
            return "1/1";
        }
        return "0/1";
    }

    private void SetDialogue(string text)
    {
        if (dialogueText != null)
        {
            dialogueText.text = text;
        }
    }

    private void PlaySFX(SFXClip clip)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }
}