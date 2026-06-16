/*
 * BlacksmithSystem.cs
 * 대장장이 NPC 패널 — 좌측 무기 목록에서 선택 후 EnhancementSystem 패널로 넘겨 코인 플립 강화 진행
 * 담당: 김보민
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class BlacksmithSystem : MonoBehaviour
{
    public static BlacksmithSystem Instance { get; private set; }

    [Header("UI 참조")]
    public GameObject blacksmithPanel;
    public TMP_Text dialogueText;
    public Button enhanceButton;
    public Button closeButton;

    [Header("무기 목록")]
    public Transform weaponListContainer; // 좌측 무기 카드들이 담길 곳
    public GameObject weaponCardPrefab;    // BlacksmithItemCard 프리팹

    [Header("강화 미리보기 UI")]
    public TMP_Text weaponNameText;
    public TMP_Text currentLevelText;   // 현재 강 (예: 1강)
    public TMP_Text nextLevelText;      // 다음 강 (예: 2강)
    public TMP_Text currentStatsText;   // 현재 스탯
    public TMP_Text nextStatsText;      // 다음 스탯
    public TMP_Text successRateText;
    public TMP_Text costText;           // 강화 비용
    public TMP_Text balanceAfterText;   // 강화 후 잔액

    [Header("시스템 연동")]
    [SerializeField] private EnhancementSystem enhancementSystem;

    [Header("강화 비용")]
    public int baseCost = 100;

    [Header("시작 메뉴")]
    public GameObject menuPanel;
    public Button menuEnhanceButton;
    public Button menuTalkButton;

    [Header("대화하기 대사")]
    [TextArea(2, 4)]
    public string[] talkLines = new string[]
    {
        "쇠는 거짓말을 안 하지. 두드린 만큼 강해지는 법이야.",
        "강화는 동전 한 닢에 운을 거는 일일세. 각오는 됐나?",
        "위로 올라갈수록 쇠도 단단해지더군. 좋은 재료를 가져오게.",
        "무리한 강화는 무기를 잃게 만들지. 욕심은 금물이야."
    };

    private int _talkIndex = 0;

    // 현재 선택된 무기와 카드
    private WeaponInstance _selectedWeapon = null;
    private BlacksmithItemUI _selectedCard = null;

    // 생성된 무기 카드 목록 (갱신 시 정리용)
    private List<BlacksmithItemUI> _spawnedCards = new List<BlacksmithItemUI>();

    private bool _isOpen = false;

    // 대장간이 열려 있는지
    public bool IsOpen => _isOpen;

    // 무기 선택 화면(강화 패널)이 떠 있는지
    public bool IsWeaponSelectMode => _isOpen == true && blacksmithPanel != null && blacksmithPanel.activeSelf == true;

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
        blacksmithPanel.SetActive(false);

        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        enhanceButton.onClick.AddListener(OnEnhanceClicked);
        closeButton.onClick.AddListener(CloseBlacksmith);

        if (menuEnhanceButton != null)
        {
            menuEnhanceButton.onClick.AddListener(OnMenuEnhanceClicked);
        }
        if (menuTalkButton != null)
        {
            menuTalkButton.onClick.AddListener(OnMenuTalkClicked);
        }
    }

    // ─────────────────────── 열기 / 닫기 ───────────────────────

    public void OpenBlacksmith()
    {
        if (_isOpen == true)
        {
            return;
        }

        _isOpen = true;

        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);

        ShowMenu();
    }

    public void CloseBlacksmith()
    {
        blacksmithPanel.SetActive(false);

        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        _isOpen = false;
        _selectedWeapon = null;
        _selectedCard = null;

        ClearWeaponList();

        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }

    // ─────────────────────── 시작 메뉴 ───────────────────────

    // 시작 메뉴 보여주기 (강화/대화 선택 화면)
    private void ShowMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }

        blacksmithPanel.SetActive(false);

        dialogueText.text = "어서오게! 무기를 벼릴 준비가 됐나?";
    }

    // 강화하기 선택
    private void OnMenuEnhanceClicked()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        blacksmithPanel.SetActive(true);

        // 인벤토리의 무기들을 목록으로 채움
        RefreshWeaponList();

        // 선택 초기화
        _selectedWeapon = null;
        _selectedCard = null;
        ClearPreview();

        dialogueText.text = "강화할 무기를 골라보게.";
    }

    // 대화하기 선택 (누를 때마다 대사가 바뀜)
    private void OnMenuTalkClicked()
    {
        if (talkLines == null || talkLines.Length == 0)
        {
            dialogueText.text = "대장장이: ...";
            return;
        }

        dialogueText.text = "대장장이: " + talkLines[_talkIndex];

        _talkIndex = _talkIndex + 1;
        if (_talkIndex >= talkLines.Length)
        {
            _talkIndex = 0;
        }
    }

    // ESC: 강화 화면이면 메뉴로 복귀, 메뉴 화면이면 완전히 닫기
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

        // 강화 패널이 떠 있으면 메뉴로 복귀
        if (blacksmithPanel.activeSelf == true)
        {
            ShowMenu();
            return;
        }

        // 메뉴 화면이면 완전히 닫기
        if (menuPanel != null && menuPanel.activeSelf == true)
        {
            CloseBlacksmith();
        }
    }

    // ─────────────────────── 무기 목록 ───────────────────────

    // 인벤토리에서 무기들을 모아 좌측 목록에 표시
    // 정렬: 무기 종류(단검 > 장검 > 창) → 같은 종류는 강화 단계 높은 순
    private void RefreshWeaponList()
    {
        ClearWeaponList();

        if (InventorySystem.Instance == null)
        {
            return;
        }
        if (weaponCardPrefab == null || weaponListContainer == null)
        {
            return;
        }

        // 인벤토리의 모든 인스턴스에서 무기만 추림
        List<WeaponInstance> weapons = new List<WeaponInstance>();
        List<ItemInstance> all = InventorySystem.Instance.GetAllInstances();

        for (int i = 0; i < all.Count; i++)
        {
            WeaponInstance weapon = all[i] as WeaponInstance;
            if (weapon != null)
            {
                weapons.Add(weapon);
            }
        }

        // 정렬: 종류 우선, 같은 종류는 강화 단계 내림차순
        weapons.Sort(CompareWeapon);

        // 카드 생성
        for (int i = 0; i < weapons.Count; i++)
        {
            GameObject cardObj = Instantiate(weaponCardPrefab, weaponListContainer);
            BlacksmithItemUI card = cardObj.GetComponent<BlacksmithItemUI>();
            if (card != null)
            {
                card.Setup(weapons[i], this);
                _spawnedCards.Add(card);
            }
        }
    }

    // 무기 정렬 비교: 종류(단검0 > 장검1 > 창2) 오름차순, 같은 종류는 강화 단계 내림차순
    private int CompareWeapon(WeaponInstance a, WeaponInstance b)
    {
        WeaponData dataA = a.Data as WeaponData;
        WeaponData dataB = b.Data as WeaponData;

        if (dataA == null || dataB == null)
        {
            return 0;
        }

        int typeA = (int)dataA.WeaponType;
        int typeB = (int)dataB.WeaponType;

        if (typeA != typeB)
        {
            return typeA.CompareTo(typeB);
        }

        // 같은 종류면 강화 단계 높은 순 (내림차순)
        return b.EnhancementLevel.CompareTo(a.EnhancementLevel);
    }

    // 생성된 카드 모두 제거
    private void ClearWeaponList()
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

    // ─────────────────────── 무기 선택 ───────────────────────

    // 무기 카드 클릭 시 BlacksmithItemUI 가 호출
    public void SelectWeaponFromList(BlacksmithItemUI card)
    {
        if (card == null || card.Weapon == null)
        {
            return;
        }

        // 이전 선택 카드 강조 해제
        if (_selectedCard != null)
        {
            _selectedCard.SetSelected(false);
        }

        _selectedCard = card;
        _selectedCard.SetSelected(true);
        _selectedWeapon = card.Weapon;

        UpdatePreview();
    }

    // (구) 인벤토리 우클릭 선택 호환용 — 외부에서 WeaponInstance 직접 넘기는 경우
    public void SelectWeapon(WeaponInstance weapon)
    {
        if (weapon == null)
        {
            dialogueText.text = "무기를 선택해주게.";
            return;
        }

        _selectedWeapon = weapon;
        UpdatePreview();
    }

    // ─────────────────────── 미리보기 갱신 ───────────────────────

    // 선택된 무기의 현재강 > 다음강 스탯, 비용, 잔액 표시
    private void UpdatePreview()
    {
        if (_selectedWeapon == null)
        {
            ClearPreview();
            return;
        }

        WeaponData data = _selectedWeapon.Data as WeaponData;
        if (data == null)
        {
            ClearPreview();
            return;
        }

        int level = _selectedWeapon.EnhancementLevel;

        if (weaponNameText != null)
        {
            weaponNameText.text = _selectedWeapon.Data.itemName;
        }

        if (currentLevelText != null)
        {
            currentLevelText.text = level.ToString() + "강";
        }

        // 현재 스탯 (공격력 / 공격속도) — WeaponData 배율 배열로 계산
        float currentDamage = data.BaseDamage * GetMultiplier(data.AttackMultipliers, level);
        float currentSpeed = data.AttackSpeed * GetMultiplier(data.SpeedMultipliers, level);
        if (currentStatsText != null)
        {
            currentStatsText.text = "공격력 " + currentDamage.ToString("F0")
                + "\n속도 " + currentSpeed.ToString("F1");
        }

        if (level < 5)
        {
            if (nextLevelText != null)
            {
                nextLevelText.text = (level + 1).ToString() + "강";
            }

            // 다음 강 스탯 미리보기 — 배율 직접 계산 (원본 변경 없이)
            float nextDamage = data.BaseDamage * GetMultiplier(data.AttackMultipliers, level + 1);
            float nextSpeed = data.AttackSpeed * GetMultiplier(data.SpeedMultipliers, level + 1);

            if (nextStatsText != null)
            {
                nextStatsText.text = "공격력 " + nextDamage.ToString("F0")
                    + "\n속도 " + nextSpeed.ToString("F1");
            }

            if (successRateText != null)
            {
                successRateText.text = "성공 확률 " + _selectedWeapon.CurrentSuccessRate.ToString("F0") + "%";
            }
        }
        else
        {
            if (nextLevelText != null)
            {
                nextLevelText.text = "MAX";
            }
            if (nextStatsText != null)
            {
                nextStatsText.text = "최대 강화";
            }
            if (successRateText != null)
            {
                successRateText.text = "";
            }
        }

        // 비용 / 잔액
        int cost = CalculateEnhancementCost(level);
        int balanceAfter = 0;
        if (PlayerStats.Instance != null)
        {
            balanceAfter = PlayerStats.Instance.gold - cost;
        }

        if (costText != null)
        {
            costText.text = cost.ToString() + " 달란";
        }
        if (balanceAfterText != null)
        {
            balanceAfterText.text = balanceAfter.ToString() + " 달란";
        }

        // 버튼 활성화 (골드 충분 + 최대강 미만)
        bool canAfford = PlayerStats.Instance != null && PlayerStats.Instance.gold >= cost;
        enhanceButton.interactable = canAfford == true && level < 5;
    }

    // 배율 배열에서 안전하게 값 가져오기
    private float GetMultiplier(float[] multipliers, int index)
    {
        if (multipliers == null || multipliers.Length == 0)
        {
            return 1f;
        }
        int clamped = Mathf.Clamp(index, 0, multipliers.Length - 1);
        return multipliers[clamped];
    }

    // 미리보기 비우기
    private void ClearPreview()
    {
        if (weaponNameText != null)
        {
            weaponNameText.text = "";
        }
        if (currentLevelText != null)
        {
            currentLevelText.text = "";
        }
        if (nextLevelText != null)
        {
            nextLevelText.text = "";
        }
        if (currentStatsText != null)
        {
            currentStatsText.text = "";
        }
        if (nextStatsText != null)
        {
            nextStatsText.text = "";
        }
        if (successRateText != null)
        {
            successRateText.text = "";
        }
        if (costText != null)
        {
            costText.text = "";
        }
        if (balanceAfterText != null)
        {
            balanceAfterText.text = "";
        }
        enhanceButton.interactable = false;
    }

    // ─────────────────────── 강화 버튼 ───────────────────────

    private void OnEnhanceClicked()
    {
        if (_selectedWeapon == null)
        {
            dialogueText.text = "먼저 강화할 무기를 선택해주게.";
            return;
        }

        if (_selectedWeapon.EnhancementLevel >= 5)
        {
            dialogueText.text = "이미 최대 강화 단계일세.";
            return;
        }

        if (enhancementSystem == null)
        {
            Debug.LogWarning("[BlacksmithSystem] EnhancementSystem 참조가 없습니다.");
            return;
        }

        // 대장간 패널 닫고 코인 플립 강화 패널 열기
        blacksmithPanel.SetActive(false);
        enhancementSystem.SelectWeapon(_selectedWeapon);
        enhancementSystem.OpenEnhancement();
    }

    // ─────────────────────── 유틸 ───────────────────────

    private int CalculateEnhancementCost(int currentLevel)
    {
        return baseCost * (currentLevel + 1);
    }
}