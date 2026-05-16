using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 학자 NPC 패널.
/// 미감정 유물을 골드를 소모해 감정한 뒤 능력치를 공개한다.
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

    private RelicData selectedRelic;
    private bool      isOpen = false;

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

        dialogueText.text = "유물 감정이 필요하신가요? 어디 한번 살펴보겠습니다.";

        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
    }

    public void CloseScholar()
    {
        scholarPanel.SetActive(false);
        isOpen = false;

        selectedRelic = null;

        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }

    // 인벤토리 슬롯 클릭 시 외부에서 호출
    public void SelectRelic(ItemData relic)
    {
        if (relic.itemType != ItemType.Relic)
        {
            dialogueText.text = "유물만 감정할 수 있습니다.";
            return;
        }

        RelicData relicData = relic as RelicData;
        if (relicData == null)
        {
            dialogueText.text = "유물 데이터를 읽을 수 없습니다.";
            return;
        }

        selectedRelic = relicData;
        UpdateIdentificationUI();
    }

    private void UpdateIdentificationUI()
    {
        if (selectedRelic == null)
        {
            return;
        }

        if (selectedRelic.isIdentified)
        {
            relicInfoText.text             = $"{selectedRelic.itemName}\n{selectedRelic.relicEffect}";
            identifyCostText.text          = "이미 감정된 유물입니다.";
            identifyButton.interactable    = false;
        }
        else
        {
            relicInfoText.text             = "미감정 유물\n감정이 필요합니다.";
            identifyCostText.text          = $"감정 비용: {identifyCost} 골드";
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

        if (selectedRelic.isIdentified)
        {
            dialogueText.text = "이미 감정된 유물입니다.";
            return;
        }

        if (PlayerStats.Instance.gold < identifyCost)
        {
            dialogueText.text = "골드가 부족합니다!";
            AudioManager.Instance?.PlaySFX(SFXClip.UIError);
            return;
        }

        PlayerStats.Instance.gold -= identifyCost;

        selectedRelic.RevealStats();

        dialogueText.text = $"감정 완료! 이건 {selectedRelic.itemName}이군요!\n{selectedRelic.relicEffect}";

        UpdateIdentificationUI();

        AudioManager.Instance?.PlaySFX(SFXClip.SetEffectActivate);
    }
}
