using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 학자 NPC 패널.
/// 미감정 유물을 달란을 소모해 감정한 뒤 능력치를 공개한다.
/// 인벤토리를 같이 열고, 인벤토리의 유물을 우클릭해 하나씩 골라 감정한다.
/// </summary>
public class ScholarSystem : MonoBehaviour
{
    [Header("UI 참조")]
    public GameObject scholarPanel;
    public TMP_Text   dialogueText;
    public Button     identifyButton;
    public Button     closeButton;

    [Header("감정 UI")]
    public TMP_Text identifyCostText;
    public TMP_Text relicInfoText;

    [Header("감정 설정")]
    public int identifyCost = 50;

    public static ScholarSystem Instance { get; private set; }

    private RelicInstance selectedRelic;
    private bool      isOpen = false;

    /// <summary>감정창이 열려 있는지 — InventorySlot 우클릭 분기에서 참조</summary>
    public bool IsOpen => isOpen;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        scholarPanel.SetActive(false);

        identifyButton.onClick.AddListener(OnIdentifyClicked);
        closeButton.onClick.AddListener(CloseScholar);
    }

    public void OpenScholar()
    {
        if (isOpen)
        {
            return;
        }
        scholarPanel.SetActive(true);
        isOpen = true;

        dialogueText.text = "유물 감정이 필요하신가요? 인벤토리에서 유물을 우클릭해 보여주세요.";
        relicInfoText.text = "";
        identifyCostText.text = $"감정 비용: {identifyCost} 달란";
        identifyButton.interactable = false;

        // 감정할 유물을 고를 수 있게 인벤토리를 옆에 같이 연다
        FindFirstObjectByType<InventoryUI>()?.OpenInventoryForShop();

        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
    }

    public void CloseScholar()
    {
        scholarPanel.SetActive(false);
        isOpen = false;

        selectedRelic = null;

        FindFirstObjectByType<InventoryUI>()?.CloseInventory();

        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }

    // 인벤토리 슬롯 우클릭 시 InventorySlot 에서 호출
    public void SelectRelic(ItemInstance _instance)
    {
        RelicInstance relic = _instance as RelicInstance;
        if (relic == null)
        {
            dialogueText.text = "유물만 감정할 수 있습니다.";
            return;
        }

        selectedRelic = relic;
        UpdateIdentificationUI();
    }

    private void UpdateIdentificationUI()
    {
        if (selectedRelic == null)
        {
            return;
        }

        if (selectedRelic.IsIdentified == true)
        {
            relicInfoText.text             = $"{selectedRelic.RelicData.itemName}\n{selectedRelic.RelicData.relicEffect}";
            identifyCostText.text          = "이미 감정된 유물입니다.";
            identifyButton.interactable    = false;
        }
        else
        {
            relicInfoText.text             = "미감정 유물\n감정이 필요합니다.";
            identifyCostText.text          = $"감정 비용: {identifyCost} 달란";
            identifyButton.interactable    = PlayerStats.Instance.gold >= identifyCost;
        }
    }

    private void OnIdentifyClicked()
    {
        if (selectedRelic == null)
        {
            dialogueText.text = "먼저 감정할 유물을 선택해주세요.";
            return;
        }

        if (selectedRelic.IsIdentified == true)
        {
            dialogueText.text = "이미 감정된 유물입니다.";
            return;
        }

        if (PlayerStats.Instance.gold < identifyCost)
        {
            dialogueText.text = "달란이 부족합니다!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        PlayerStats.Instance.gold -= identifyCost;

        selectedRelic.Identify();

        dialogueText.text = $"감정 완료! 이건 {selectedRelic.RelicData.itemName}이군요!\n{selectedRelic.RelicData.relicEffect}";

        UpdateIdentificationUI();

        AudioManager.Instance?.PlaySFX(SFXClip.SetEffectActivate);
    }
}
