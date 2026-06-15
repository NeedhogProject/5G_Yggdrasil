/*
 * BlacksmithSystem.cs
 * 대장장이 NPC 패널 — 무기 선택 후 EnhancementSystem 패널로 넘겨 코인 플립 강화 진행
 * 담당: 김보민
 */

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

    [Header("강화 UI")]
    public TMP_Text weaponNameText;  // 추가
    public TMP_Text currentLevelText;
    public TMP_Text successRateText;

    [Header("시스템 연동")]
    [SerializeField] private EnhancementSystem enhancementSystem;

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

    // WeaponData 대신 WeaponInstance 사용 (강화 단계는 런타임에 있음)
    private WeaponInstance _selectedWeapon = null;
    private bool _isOpen = false;

    // 대장간이 열려 있는지 (인벤토리 우클릭으로 무기 받을 수 있는지 판단용)
    public bool IsOpen => _isOpen;

    // 무기 선택 화면이 떠 있는지 (강화 패널 표시 상태)
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

        // 메뉴 화면이면 완전히 닫기 (EnhancementSystem 패널로 넘어간 상태는 건드리지 않음)
        if (menuPanel != null && menuPanel.activeSelf == true)
        {
            CloseBlacksmith();
        }
    }

    // ─────────────────────── 무기 선택 ───────────────────────

    /// <summary>
    /// 인벤토리 슬롯에서 무기 클릭 시 외부에서 호출
    /// WeaponInstance 를 받아야 강화 단계 정보가 유지됨
    /// </summary>
    public void SelectWeapon(WeaponInstance weapon)
    {
        if (weapon == null)
        {
            dialogueText.text = "무기를 선택해주게.";
            return;
        }

        _selectedWeapon = weapon;
        UpdateUI();
    }

    // ─────────────────────── UI 갱신 ───────────────────────

    private void UpdateUI()
    {
        if (_selectedWeapon == null)
        {
            return;
        }

        // 추가
        weaponNameText.text = _selectedWeapon.Data.itemName;

        int level = _selectedWeapon.EnhancementLevel;
        currentLevelText.text = "현재 강화: +" + level.ToString();
        successRateText.text = "성공 확률: " + _selectedWeapon.CurrentSuccessRate.ToString("F0") + "%";
        enhanceButton.interactable = true;
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
}