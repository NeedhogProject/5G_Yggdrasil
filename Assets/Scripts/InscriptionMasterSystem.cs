using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 각인술사 NPC 패널.
/// 방어구에 원소 각인을 부여하거나 초기화권으로 제거한다.
/// 슬롯별 자원 비용: 투구 5개 / 갑옷 7개 / 각반 6개 / 장화 5개
/// </summary>
public class InscriptionMasterSystem : MonoBehaviour
{
    [Header("UI 참조")]
    public GameObject inscriptionPanel;
    public TMP_Text   dialogueText;
    public Button     inscribeButton;
    public Button     resetButton;
    public Button     closeButton;

    [Header("각인 UI")]
    public TMP_Dropdown   inscriptionTypeDropdown;
    public TMP_Text       resourceCostText;
    public TMP_Text       currentInscriptionText;

    [Header("자원 표시")]
    public TMP_Text fireResourceText;
    public TMP_Text waterResourceText;
    public TMP_Text windResourceText;
    public TMP_Text earthResourceText;
    public TMP_Text darknessResourceText;

    // 초기화권 아이템 이름 상수 (문자열 직접 비교 대신 한 곳에서 관리)
    private const string RESET_SCROLL_NAME = "각인 초기화권";

    private ArmorData       selectedArmor;
    private InscriptionType selectedInscriptionType;
    private bool            isOpen = false;

    private void Start()
    {
        inscriptionPanel.SetActive(false);

        inscribeButton.onClick.AddListener(OnInscribeClicked);
        resetButton.onClick.AddListener(OnResetClicked);
        closeButton.onClick.AddListener(CloseInscriptionMaster);

        if (inscriptionTypeDropdown == null == false)
        {
            inscriptionTypeDropdown.onValueChanged.AddListener(OnInscriptionTypeChanged);
            SetupDropdown();
        }
    }

    private void SetupDropdown()
    {
        inscriptionTypeDropdown.ClearOptions();

        List<string> options = new List<string>
        {
            "불 (Fire)",
            "물 (Water)",
            "바람 (Wind)",
            "땅 (Earth)",
            "어둠 (Darkness)"
        };

        inscriptionTypeDropdown.AddOptions(options);
        selectedInscriptionType = InscriptionType.Fire;
    }

    public void OpenInscriptionMaster()
    {
        if (isOpen)
        {
            return;
        }
        inscriptionPanel.SetActive(true);
        isOpen = true;

        dialogueText.text = "각인을 원하시오? 자원만 있다면 문제없지.";

        UpdateResourceDisplay();
        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
    }

    public void CloseInscriptionMaster()
    {
        inscriptionPanel.SetActive(false);
        isOpen = false;

        selectedArmor = null;

        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }

    // 인벤토리 슬롯 클릭 시 외부에서 호출
    public void SelectArmor(ArmorData armor)
    {
        if (armor == null)
        {
            dialogueText.text = "방어구를 선택해주게.";
            return;
        }

        selectedArmor = armor;
        UpdateInscriptionUI();
    }

    private void UpdateInscriptionUI()
    {
        if (selectedArmor == null)
        {
            return;
        }

        if (selectedArmor.HasRune)
        {
            currentInscriptionText.text    = $"현재 각인: {GetElementName(selectedArmor.RuneSlot1)}";
            inscribeButton.interactable    = false;
            resetButton.interactable       = true;
        }
        else
        {
            currentInscriptionText.text    = "현재 각인: 없음";
            inscribeButton.interactable    = true;
            resetButton.interactable       = false;
        }

        UpdateResourceCost();
    }

    private void UpdateResourceDisplay()
    {
        ResourceInventory resourceInv = ResourceInventory.Instance;
        if (resourceInv == null)
        {
            return;
        }

        fireResourceText.text     = $"불: {resourceInv.GetResourceCount(InscriptionType.Fire)}";
        waterResourceText.text    = $"물: {resourceInv.GetResourceCount(InscriptionType.Water)}";
        windResourceText.text     = $"바람: {resourceInv.GetResourceCount(InscriptionType.Wind)}";
        earthResourceText.text    = $"땅: {resourceInv.GetResourceCount(InscriptionType.Earth)}";
        darknessResourceText.text = $"어둠: {resourceInv.GetResourceCount(InscriptionType.Darkness)}";
    }

    private void UpdateResourceCost()
    {
        int cost = GetInscriptionCost();
        resourceCostText.text = $"필요 자원: {GetInscriptionTypeName(selectedInscriptionType)} x {cost}";
    }

    private void OnInscriptionTypeChanged(int index)
    {
        // 드롭다운 인덱스 0부터 Fire(1)에 매핑 (None = 0 제외)
        selectedInscriptionType = (InscriptionType)(index + 1);
        UpdateResourceCost();
    }

    private void OnInscribeClicked()
    {
        if (selectedArmor == null)
        {
            dialogueText.text = "먼저 각인할 방어구를 선택해주게.";
            return;
        }

        if (selectedArmor.HasRune)
        {
            dialogueText.text = "이미 각인된 방어구일세. 초기화권이 필요하네.";
            return;
        }

        ResourceInventory resourceInv = ResourceInventory.Instance;
        int cost = GetInscriptionCost();

        if (resourceInv.GetResourceCount(selectedInscriptionType) < cost)
        {
            dialogueText.text = "자원이 부족하군!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        resourceInv.RemoveResource(selectedInscriptionType, cost);

        // ArmorData의 RuneSlot1에 원소 기록 (RuntimeInstance에서 관리해야 하나 SO 직접 수정)
        // 추후 ArmorInstance로 이전 예정
        ApplyRuneToArmor(selectedArmor, selectedInscriptionType);

        dialogueText.text = $"{selectedArmor.itemName}에 {GetInscriptionTypeName(selectedInscriptionType)} 각인을 새겼네.";

        UpdateInscriptionUI();
        UpdateResourceDisplay();

        AudioManager.Instance?.PlaySFX(SFXClip.InscribeApply);
    }

    private void OnResetClicked()
    {
        if (selectedArmor == null)
        {
            dialogueText.text = "먼저 초기화할 방어구를 선택해주게.";
            return;
        }

        if (selectedArmor.HasRune == false)
        {
            dialogueText.text = "각인되지 않은 방어구일세.";
            return;
        }

        if (InventorySystem.Instance.HasItem(RESET_SCROLL_NAME) == false)
        {
            dialogueText.text = "각인 초기화권이 필요하네!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        ItemData resetScroll = InventorySystem.Instance.items.Find(
            item => item.itemName == RESET_SCROLL_NAME);
        InventorySystem.Instance.RemoveItem(resetScroll);

        ClearRuneFromArmor(selectedArmor);

        dialogueText.text = $"{selectedArmor.itemName}의 각인을 초기화했네.";

        UpdateInscriptionUI();
        AudioManager.Instance?.PlaySFX(SFXClip.InscribeReset);
    }

    // 슬롯별 자원 비용 (기획서: 투구 5, 갑옷 7, 각반 6, 장화 5)
    private int GetInscriptionCost()
    {
        if (selectedArmor == null)
        {
            return 0;
        }

        switch (selectedArmor.ArmorSlot)
        {
            case ArmorSlot.Helmet: return 5;
            case ArmorSlot.Chest:  return 7;
            case ArmorSlot.Legs:   return 6;
            case ArmorSlot.Boots:  return 5;
            default:               return 5;
        }
    }

    // InscriptionType을 RuneElement로 변환해 ArmorData에 기록
    // ArmorData가 SerializeField private이라 직접 쓰기 불가 → 리플렉션 대신 임시 public setter 요청
    // 현재는 Debug.Log로 대체하고 추후 ArmorInstance 방식으로 전환
    private static void ApplyRuneToArmor(ArmorData armor, InscriptionType type)
    {
        // TODO: ArmorData에 SetRune(RuneElement) 메서드 추가 후 교체
        Debug.Log($"[InscriptionMasterSystem] {armor.itemName}에 {type} 각인 적용 (ArmorData.SetRune 필요)");
    }

    private static void ClearRuneFromArmor(ArmorData armor)
    {
        // TODO: ArmorData에 ClearRune() 메서드 추가 후 교체
        Debug.Log($"[InscriptionMasterSystem] {armor.itemName} 각인 초기화 (ArmorData.ClearRune 필요)");
    }

    private static string GetInscriptionTypeName(InscriptionType type)
    {
        switch (type)
        {
            case InscriptionType.Fire:     return "불";
            case InscriptionType.Water:    return "물";
            case InscriptionType.Wind:     return "바람";
            case InscriptionType.Earth:    return "땅";
            case InscriptionType.Darkness: return "어둠";
            default:                       return "없음";
        }
    }

    private static string GetElementName(RuneElement rune)
    {
        switch (rune)
        {
            case RuneElement.Fire:     return "불";
            case RuneElement.Water:    return "물";
            case RuneElement.Wind:     return "바람";
            case RuneElement.Earth:    return "땅";
            case RuneElement.Darkness: return "어둠";
            default:                   return "없음";
        }
    }
}
