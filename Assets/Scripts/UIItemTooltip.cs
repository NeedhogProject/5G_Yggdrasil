using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class UIItemTooltip : MonoBehaviour
{
    public static UIItemTooltip Instance { get; private set; }

    [Header("UI References")]
    public GameObject tooltipPanel;
    public TMP_Text itemNameText;
    public TMP_Text itemTypeText;
    public TMP_Text itemDescriptionText;
    public TMP_Text itemStatsText;
    public TMP_Text itemPriceText;
    public Image itemIconImage;
    public Image rarityBorder;

    [Header("Tooltip Settings")]
    public Vector2 offset = new Vector2(15, -15);

    private RectTransform tooltipRect;
    private ItemData _currentItem;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        tooltipPanel.SetActive(false);
    }

    void Update()
    {
        if (tooltipPanel.activeSelf == false) return;

        // 마우스 위치 즉시 따라가기 (Lerp 없이 — 깜빡임 원인 제거)
        Vector2 mousePos = Mouse.current.position.ReadValue();
        tooltipRect.position = ClampToScreen(mousePos + offset);
    }

    public void ShowTooltip(ItemData item, Vector3 position)
    {
        if (item == null) return;

        // 이미 같은 아이템 표시 중이면 위치만 갱신
        _currentItem = item;

        // 텍스트 설정
        if (itemNameText        != null) itemNameText.text        = item.itemName;
        if (itemTypeText        != null) itemTypeText.text        = GetItemTypeName(item.itemType);
        if (itemDescriptionText != null) itemDescriptionText.text = string.IsNullOrEmpty(item.itemDescription)
                                                                    ? "" : item.itemDescription;
        if (itemPriceText       != null) itemPriceText.text       = $"{item.basePrice} G";
        if (itemIconImage       != null) itemIconImage.sprite     = item.itemIcon;

        if (rarityBorder != null)
            rarityBorder.color = GetRarityColor(item.rarity);

        if (itemStatsText != null)
        {
            if      (item is EquipmentData eq) itemStatsText.text = GetEquipmentStats(eq);
            else if (item is RelicData rel)    itemStatsText.text = GetRelicStats(rel);
            else                               itemStatsText.text = "";
        }

        // 위치 먼저 설정 후 활성화 (깜빡임 방지)
        Vector2 mousePos = Mouse.current.position.ReadValue();
        tooltipRect.position = ClampToScreen(mousePos + offset);
        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        _currentItem = null;
        tooltipPanel.SetActive(false);
    }

    private string GetEquipmentStats(EquipmentData e)
    {
        string s = "";
        if (e.attack       > 0) s += $"공격력: {e.attack}\n";
        if (e.defense      > 0) s += $"방어력: {e.defense}\n";
        if (e.upgradeLevel > 0) s += $"강화: +{e.upgradeLevel}\n";
        if (e.isInscribed)      s += $"각인: {GetInscriptionName(e.inscriptionType)}\n";
        s += $"내구도: {e.durability:F0}/{e.maxDurability:F0}";
        return s;
    }

    private string GetRelicStats(RelicData r) =>
        r.isIdentified ? r.relicEffect : "미확인 유물\n감정이 필요합니다.";

    private string GetItemTypeName(ItemType type) => type switch
    {
        ItemType.Equipment  => "장비",
        ItemType.Consumable => "소비 아이템",
        ItemType.Relic      => "유물",
        ItemType.Resource   => "자원",
        ItemType.QuestItem  => "퀘스트 아이템",
        _                   => "기타"
    };

    private string GetInscriptionName(InscriptionType type) => type switch
    {
        InscriptionType.Fire     => "불",
        InscriptionType.Water    => "물",
        InscriptionType.Wind     => "바람",
        InscriptionType.Earth    => "땅",
        InscriptionType.Darkness => "어둠",
        _                        => "없음"
    };

    private Color GetRarityColor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common    => new Color(0.8f, 0.8f, 0.8f),
        ItemRarity.Uncommon  => new Color(0.3f, 1f,   0.3f),
        ItemRarity.Rare      => new Color(0.3f, 0.5f, 1f),
        ItemRarity.Epic      => new Color(0.8f, 0.3f, 1f),
        ItemRarity.Legendary => new Color(1f,   0.6f, 0f),
        _                    => Color.white
    };

    private Vector2 ClampToScreen(Vector2 pos)
    {
        Vector2 screen  = new Vector2(Screen.width, Screen.height);
        Vector2 tipSize = tooltipRect.sizeDelta;
        pos.x = Mathf.Clamp(pos.x, 0, screen.x - tipSize.x);
        pos.y = Mathf.Clamp(pos.y, tipSize.y, screen.y);
        return pos;
    }
}
