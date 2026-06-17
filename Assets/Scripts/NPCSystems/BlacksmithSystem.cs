/*
 * BlacksmithSystem.cs
 * 대장장이 NPC 패널 — 좌측 무기 목록에서 선택 후 CoinFlipUI 로 코인 플립 강화 진행
 * 담당: 김보민
 */

using System.Collections;
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

    [Header("재료 칸 (3종 — 재료 미정, 뼈대만)")]
    // 재료 종류가 확정되면 아이콘/필요량 데이터를 연결한다.
    // 현재는 보유/필요 표시만 가능하도록 배열로 받아둔다.
    public Image[] materialIcons;       // 재료 아이콘 3개
    public TMP_Text[] materialCountTexts; // 보유/필요 텍스트 3개 (예: 12/50)

    [Header("시스템 연동")]
    [SerializeField] private EnhancementSystem enhancementSystem;

    [Header("강화 비용")]
    public int baseCost = 100;

    [Header("시작 메뉴")]
    public GameObject menuPanel;
    public Button menuEnhanceButton;
    public Button menuTalkButton;

    [Header("대사 타이핑 효과")]
    // 글자 하나당 대기 시간(초). 0 이면 즉시 표시.
    [SerializeField] private float dialogueTextSpeed = 0.03f;

    [Header("대화하기 대사")]
    [TextArea(2, 4)]
    public string[] talkLines = new string[]
    {
        "쇠는 거짓말을 안 하지. 두드린 만큼 강해지는 법이야.",
        "강화는 동전 한 닢에 운을 거는 일일세. 각오는 됐나?",
        "위로 올라갈수록 쇠도 단단해지더군. 좋은 재료를 가져오게.",
        "무리한 강화는 무기를 잃게 만들지. 욕심은 금물이야."
    };

    // 무기 강화 배율 (WeaponData 의 값과 동일하게 유지한다)
    // WeaponData 의 배열이 private 이라 미리보기 계산용으로 여기에 복사해 둔다.
    // 정건희가 WeaponData 의 배율을 바꾸면 이 두 줄도 같이 맞춰야 한다.
    private static readonly float[] AttackMultipliers = { 1.00f, 1.02f, 1.04f, 1.07f, 1.09f, 1.15f };
    private static readonly float[] SpeedMultipliers = { 1.00f, 1.00f, 1.00f, 1.02f, 1.03f, 1.07f };

    private int _talkIndex = 0;

    // 현재 선택된 무기와 카드
    private WeaponInstance _selectedWeapon = null;
    private BlacksmithItemUI _selectedCard = null;

    // 생성된 무기 카드 목록 (갱신 시 정리용)
    private List<BlacksmithItemUI> _spawnedCards = new List<BlacksmithItemUI>();

    private bool _isOpen = false;
    private bool _isEnhancing = false;

    // 현재 진행 중인 타이핑 코루틴
    private Coroutine _typingCoroutine = null;

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

        if (enhanceButton != null)
        {
            enhanceButton.onClick.AddListener(OnEnhanceClicked);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseBlacksmith);
        }

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

        // NPCDialogue 가 메뉴(맡기기/대화하기)를 담당하므로
        // 여기서는 바로 무기 목록 강화 화면을 띄운다.
        ShowEnhanceView();
    }

    // 무기 목록 강화 화면 표시
    private void ShowEnhanceView()
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

        SetDialogue("강화할 무기를 골라보게.");
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

        SetDialogue("어서오게! 무기를 벼릴 준비가 됐나?");
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

        SetDialogue("강화할 무기를 골라보게.");
    }

    // 대화하기 선택 (누를 때마다 대사가 바뀜)
    private void OnMenuTalkClicked()
    {
        if (talkLines == null || talkLines.Length == 0)
        {
            SetDialogue("대장장이: ...");
            return;
        }

        SetDialogue("대장장이: " + talkLines[_talkIndex]);

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

        // 강화 패널이 떠 있으면 완전히 닫기 (메뉴는 NPCDialogue 가 담당하므로 복귀 단계 없음)
        if (blacksmithPanel.activeSelf == true)
        {
            CloseBlacksmith();
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
            SetDialogue("무기를 선택해주게.");
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

        // 현재 스탯 (공격력 / 공격속도) — 자체 배율 배열로 계산
        float currentDamage = data.BaseDamage * GetMultiplier(AttackMultipliers, level);
        float currentSpeed = data.AttackSpeed * GetMultiplier(SpeedMultipliers, level);
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

            // 다음 강 스탯 미리보기 — 자체 배율 배열로 계산 (WeaponData 원본 변경 없이)
            float nextDamage = data.BaseDamage * GetMultiplier(AttackMultipliers, level + 1);
            float nextSpeed = data.AttackSpeed * GetMultiplier(SpeedMultipliers, level + 1);

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

        // 재료 칸 갱신 (재료 미정 — 현재는 0/0 표시)
        UpdateMaterialSlots();

        // 버튼 활성화 (골드 충분 + 최대강 미만)
        bool canAfford = PlayerStats.Instance != null && PlayerStats.Instance.gold >= cost;
        if (enhanceButton != null)
        {
            enhanceButton.interactable = canAfford == true && level < 5;
        }
    }

    // 재료 칸 갱신
    // 재료 시스템이 미정이라 현재는 0/0 으로 표시만 한다.
    // 재료가 정해지면 여기서 보유량/필요량을 채우고 부족 시 빨간색 처리한다.
    private void UpdateMaterialSlots()
    {
        if (materialCountTexts == null)
        {
            return;
        }

        for (int i = 0; i < materialCountTexts.Length; i++)
        {
            if (materialCountTexts[i] != null)
            {
                materialCountTexts[i].text = "0/0";
                materialCountTexts[i].color = Color.white;
            }
        }
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
        if (enhanceButton != null)
        {
            enhanceButton.interactable = false;
        }
    }

    // ─────────────────────── 강화 버튼 ───────────────────────

    private void OnEnhanceClicked()
    {
        if (_selectedWeapon == null)
        {
            SetDialogue("먼저 강화할 무기를 선택해주게.");
            return;
        }

        if (_selectedWeapon.EnhancementLevel >= 5)
        {
            SetDialogue("이미 최대 강화 단계일세.");
            return;
        }

        if (_isEnhancing == true)
        {
            return;
        }

        int cost = CalculateEnhancementCost(_selectedWeapon.EnhancementLevel);

        // 골드 확인
        if (PlayerStats.Instance == null || PlayerStats.Instance.gold < cost)
        {
            SetDialogue("골드가 부족하군!");
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        // 재료 확인 (재료 미정 — 추후 여기서 보유량 검사)
        // if (HasEnoughMaterials() == false) { ... return; }

        if (CoinFlipUI.Instance == null)
        {
            Debug.LogWarning("[BlacksmithSystem] CoinFlipUI 참조가 없습니다.");
            return;
        }

        // 골드 차감
        PlayerStats.Instance.gold = PlayerStats.Instance.gold - cost;

        // 재료 차감 (재료 미정 — 추후 구현)
        // ConsumeMaterials();

        StartCoinFlip();
    }

    // 코인 플립 연출 시작
    private void StartCoinFlip()
    {
        _isEnhancing = true;

        if (enhanceButton != null)
        {
            enhanceButton.interactable = false;
        }

        SetDialogue("코인을 던지는 중...");

        float rateNormalized = _selectedWeapon.CurrentSuccessRate / 100f;

        // 코인 애니메이션 시작 — 완료 후 OnCoinFlipComplete 콜백 호출
        bool started = CoinFlipUI.Instance.PlayCoinFlip(rateNormalized, OnCoinFlipComplete);

        if (started == false)
        {
            // 코인이 이미 돌고 있으면 골드 환불 후 취소
            int cost = CalculateEnhancementCost(_selectedWeapon.EnhancementLevel);
            PlayerStats.Instance.gold = PlayerStats.Instance.gold + cost;

            SetDialogue("잠시 후 다시 시도해주게.");

            _isEnhancing = false;
            if (enhanceButton != null)
            {
                enhanceButton.interactable = true;
            }
        }
    }

    // 코인 애니메이션 완료 후 호출되는 콜백
    // result: true = 앞면(성공), false = 뒷면(실패)
    private void OnCoinFlipComplete(bool result)
    {
        if (_selectedWeapon == null)
        {
            _isEnhancing = false;
            return;
        }

        EnhanceResult enhanceResult = _selectedWeapon.TryEnhance(result);

        switch (enhanceResult)
        {
            case EnhanceResult.Success:
                SetDialogue(_selectedWeapon.Data.itemName + "이(가) +" + _selectedWeapon.EnhancementLevel.ToString() + "강이 되었네!");
                break;

            case EnhanceResult.MaxReached:
                SetDialogue(_selectedWeapon.Data.itemName + "이(가) +5 최대 강화 달성!");
                break;

            case EnhanceResult.Downgrade:
                SetDialogue(_selectedWeapon.Data.itemName + "이(가) +" + _selectedWeapon.EnhancementLevel.ToString() + "강으로 낮아졌네.");
                break;

            case EnhanceResult.ResetToBase:
                SetDialogue(_selectedWeapon.Data.itemName + "의 강화가 초기화되었네...");
                break;

            case EnhanceResult.Fail:
                SetDialogue("아쉽군. 이번엔 실패했네.");
                break;

            case EnhanceResult.AlreadyMax:
                SetDialogue("이미 최대 강화 단계일세.");
                break;
        }

        _isEnhancing = false;

        // 선택된 카드의 강화 단계 표시 갱신
        if (_selectedCard != null)
        {
            _selectedCard.Setup(_selectedWeapon, this);
            _selectedCard.SetSelected(true);
        }

        // 미리보기 갱신
        UpdatePreview();
    }

    // ─────────────────────── 대사 타이핑 ───────────────────────

    // 대사를 타이핑 효과로 출력 (대장장이 대사 공통 경로)
    private void SetDialogue(string text)
    {
        if (dialogueText == null)
        {
            return;
        }

        // 이전 타이핑이 진행 중이면 멈추고 새로 시작
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        // 비활성 상태이거나 속도 0 이면 즉시 표시
        if (isActiveAndEnabled == false || dialogueTextSpeed <= 0f)
        {
            dialogueText.text = text;
            return;
        }

        _typingCoroutine = StartCoroutine(TypeDialogue(text));
    }

    // 대사를 글자 하나씩 출력하는 타이핑 효과
    private IEnumerator TypeDialogue(string text)
    {
        dialogueText.text = "";

        if (text == null)
        {
            yield break;
        }

        int i = 0;
        while (i < text.Length)
        {
            dialogueText.text = dialogueText.text + text[i];
            i = i + 1;
            yield return new WaitForSeconds(dialogueTextSpeed);
        }

        _typingCoroutine = null;
    }

    // ─────────────────────── 유틸 ───────────────────────

    private int CalculateEnhancementCost(int currentLevel)
    {
        return baseCost * (currentLevel + 1);
    }
}