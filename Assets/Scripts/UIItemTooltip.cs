/*
 * UIItemTooltip.cs
 * 아이템 호버 시 툴팁 패널 표시 (마우스 추적 / 화면 밖 클램프)
 * 담당: 김보민
 */

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

    private RectTransform _tooltipRect;
    private ItemData _currentItem;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        tooltipPanel.SetActive(false);
    }

    private void Update()
    {
        if (tooltipPanel.activeSelf == false)
        {
            return;
        }

        // 마우스 위치 즉시 추적 (Lerp 없이 깜빡임 방지)
        Vector2 mousePos = Mouse.current.position.ReadValue();
        _tooltipRect.position = ClampToScreen(mousePos + offset);
    }

    public void ShowTooltip(ItemData item, Vector3 position)
    {
        if (item == null)
        {
            return;
        }

        _currentItem = item;

        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
        }
        if (itemTypeText != null)
        {
            itemTypeText.text = GetItemTypeName(item.itemType);
        }
        if (itemDescriptionText != null)
        {
            bool hasDescription = string.IsNullOrEmpty(item.itemDescription) == false;
            itemDescriptionText.text = hasDescription == true ? item.itemDescription : "";
        }
        if (itemPriceText != null)
        {
            itemPriceText.text = item.basePrice.ToString() + " G";
        }
        if (itemIconImage != null)
        {
            itemIconImage.sprite = item.itemIcon;
        }
        if (rarityBorder != null)
        {
            rarityBorder.color = GetRarityColor(item.rarity);
        }
        if (itemStatsText != null)
        {
            if (item is EquipmentData eq)
            {
                itemStatsText.text = GetEquipmentStats(eq);
            }
            else if (item is RelicData rel)
            {
                itemStatsText.text = GetRelicStats(rel);
            }
            else
            {
                itemStatsText.text = "";
            }
        }

        // 위치 먼저 설정 후 활성화 (활성화 순서 바꾸면 한 프레임 깜빡임 발생)
        Vector2 mousePosForPanel = Mouse.current.position.ReadValue();
        _tooltipRect.position = ClampToScreen(mousePosForPanel + offset);
        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        _currentItem = null;
        tooltipPanel.SetActive(false);
    }

    private string GetEquipmentStats(EquipmentData e)
    {
        string result = "";

        if (e.attack > 0)
        {
            result += "공격력: " + e.attack.ToString() + "\n";
        }
        if (e.defense > 0)
        {
            result += "방어력: " + e.defense.ToString() + "\n";
        }
        if (e.upgradeLevel > 0)
        {
            result += "강화: +" + e.upgradeLevel.ToString() + "\n";
        }
        if (e.isInscribed == true)
        {
            result += "각인: " + GetInscriptionName(e.inscriptionType) + "\n";
        }

        result += "내구도: " + e.durability.ToString("F0") + "/" + e.maxDurability.ToString("F0");
        return result;
    }

    private static string GetRelicStats(RelicData r)
    {
        if (r.isIdentified == true)
        {
            return r.relicEffect;
        }
        return "미확인 유물\n감정이 필요합니다.";
    }

    private static string GetItemTypeName(ItemType type)
    {
        if (type == ItemType.Equipment)
        {
            return "장비";
        }
        if (type == ItemType.Consumable)
        {
            return "소비 아이템";
        }
        if (type == ItemType.Relic)
        {
            return "유물";
        }
        if (type == ItemType.Resource)
        {
            return "자원";
        }
        if (type == ItemType.QuestItem)
        {
            return "퀘스트 아이템";
        }
        return "기타";
    }

    private static string GetInscriptionName(InscriptionType type)
    {
        if (type == InscriptionType.Fire)
        {
            return "불";
        }
        if (type == InscriptionType.Water)
        {
            return "물";
        }
        if (type == InscriptionType.Wind)
        {
            return "바람";
        }
        if (type == InscriptionType.Earth)
        {
            return "땅";
        }
        if (type == InscriptionType.Darkness)
        {
            return "어둠";
        }
        return "없음";
    }

    private static Color GetRarityColor(ItemRarity rarity)
    {
        if (rarity == ItemRarity.Common)
        {
            return new Color(0.8f, 0.8f, 0.8f);
        }
        if (rarity == ItemRarity.Uncommon) // 기획서 등급과 다름: 정건희 enum 확인 필요
        {
            return new Color(0.3f, 1f, 0.3f);
        }
        if (rarity == ItemRarity.Rare)
        {
            return new Color(0.3f, 0.5f, 1f);
        }
        if (rarity == ItemRarity.Epic)
        {
            return new Color(0.8f, 0.3f, 1f);
        }
        if (rarity == ItemRarity.Legendary)
        {
            return new Color(1f, 0.6f, 0f);
        }
        return Color.white;
    }

    private Vector2 ClampToScreen(Vector2 pos)
    {
        Vector2 screen = new Vector2(Screen.width, Screen.height);
        Vector2 tipSize = _tooltipRect.sizeDelta;
        pos.x = Mathf.Clamp(pos.x, 0, screen.x - tipSize.x);
        pos.y = Mathf.Clamp(pos.y, tipSize.y, screen.y);
        return pos;
    }
}