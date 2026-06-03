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

    // ─────────────────────── 공개 메서드 ───────────────────────

    /// <summary>
    /// ItemData 기반 툴팁 표시 (상점, 드롭 아이템 등 인스턴스 없는 경우)
    /// </summary>
    public void ShowTooltip(ItemData item, Vector3 position)
    {
        if (item == null)
        {
            return;
        }

        _currentItem = item;

        RefreshCommonFields(item);

        if (itemStatsText != null)
        {
            if (item is EquipmentData eq)
            {
                // ItemData 기반이므로 런타임 강화/각인 정보 없음
                itemStatsText.text = GetEquipmentStatsFromData(eq);
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

        ShowPanel();
    }

    /// <summary>
    /// ItemInstance 기반 툴팁 표시 (인벤토리, 장비 슬롯 등 런타임 아이템)
    /// 강화 단계(WeaponInstance)와 각인 슬롯(ArmorInstance)을 실제 값으로 표시
    /// </summary>
    public void ShowTooltip(ItemInstance instance, Vector3 position)
    {
        if (instance == null || instance.Data == null)
        {
            return;
        }

        _currentItem = instance.Data;

        RefreshCommonFields(instance.Data);

        if (itemStatsText != null)
        {
            WeaponInstance weaponInstance = instance as WeaponInstance;
            ArmorInstance armorInstance = instance as ArmorInstance;

            if (weaponInstance != null)
            {
                itemStatsText.text = GetWeaponStatsFromInstance(weaponInstance);
            }
            else if (armorInstance != null)
            {
                itemStatsText.text = GetArmorStatsFromInstance(armorInstance);
            }
            else if (instance.Data is RelicData rel)
            {
                itemStatsText.text = GetRelicStats(rel);
            }
            else
            {
                itemStatsText.text = "";
            }
        }

        ShowPanel();
    }

    public void HideTooltip()
    {
        _currentItem = null;
        tooltipPanel.SetActive(false);
    }

    // ─────────────────────── 내부 공통 처리 ───────────────────────

    /// <summary>이름/타입/설명/가격/아이콘/등급 테두리 갱신 (두 ShowTooltip 공통)</summary>
    private void RefreshCommonFields(ItemData item)
    {
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
    }

    /// <summary>위치 설정 후 패널 활성화 (활성화 순서 바꾸면 한 프레임 깜빡임 발생)</summary>
    private void ShowPanel()
    {
        Vector2 mousePosForPanel = Mouse.current.position.ReadValue();
        _tooltipRect.position = ClampToScreen(mousePosForPanel + offset);
        tooltipPanel.SetActive(true);
    }

    // ─────────────────────── 스탯 텍스트 생성 ───────────────────────

    /// <summary>
    /// ItemData 기반 장비 스탯 (강화/각인 정보 없음)
    /// 상점 아이템처럼 인스턴스가 없는 경우에 사용
    /// </summary>
    private string GetEquipmentStatsFromData(EquipmentData e)
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

        result += "내구도: " + e.durability.ToString("F0") + "/" + e.maxDurability.ToString("F0");
        return result;
    }

    /// <summary>
    /// WeaponInstance 기반 무기 스탯
    /// 강화 단계 실제 값 표시 (EnhancementLevel)
    /// </summary>
    private string GetWeaponStatsFromInstance(WeaponInstance weapon)
    {
        string result = "";

        // 강화 단계가 있으면 이름 옆에 +N 표시
        if (weapon.EnhancementLevel > 0)
        {
            result += "강화: +" + weapon.EnhancementLevel.ToString() + "\n";
        }

        // 강화가 반영된 최종 공격력 표시
        result += "공격력: " + weapon.FinalDamage.ToString("F0") + "\n";

        // 다음 강화 성공률 표시 (5강이면 최대강 표시)
        if (weapon.EnhancementLevel >= 5)
        {
            result += "[ 최대 강화 완료 ]";
        }
        else
        {
            result += "다음 강화 성공률: " + weapon.CurrentSuccessRate.ToString("F0") + "%";

            // 4강에서 실패하면 0강으로 초기화되므로 경고 표시
            if (weapon.IsLastEnhance == true)
            {
                result += "\n※ 실패 시 강화 단계 초기화";
            }
        }

        return result;
    }

    /// <summary>
    /// ArmorInstance 기반 방어구 스탯
    /// 각인 슬롯 1, 2 실제 값 표시 (RuneSlot1, RuneSlot2)
    /// </summary>
    private string GetArmorStatsFromInstance(ArmorInstance armor)
    {
        string result = "";

        if (armor.ArmorData != null && armor.DefenseBonus > 0)
        {
            result += "방어력: " + armor.DefenseBonus.ToString() + "\n";
        }

        // 각인 슬롯 1
        if (armor.RuneSlot1 != RuneElement.None)
        {
            result += "각인 1: " + GetRuneElementName(armor.RuneSlot1) + "\n";
        }
        else
        {
            result += "각인 1: 없음\n";
        }

        // 각인 슬롯 2
        if (armor.RuneSlot2 != RuneElement.None)
        {
            result += "각인 2: " + GetRuneElementName(armor.RuneSlot2) + "\n";
        }
        else
        {
            result += "각인 2: 없음\n";
        }

        // 세트 발동 여부 안내
        if (armor.HasRune == true)
        {
            result += "[ 세트 효과 진행 중 ]";
        }

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

    // ─────────────────────── 이름 변환 유틸 ───────────────────────

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

    /// <summary>RuneElement 열거형을 한글로 변환</summary>
    private static string GetRuneElementName(RuneElement element)
    {
        if (element == RuneElement.Fire)
        {
            return "불";
        }
        if (element == RuneElement.Water)
        {
            return "물";
        }
        if (element == RuneElement.Wind)
        {
            return "바람";
        }
        if (element == RuneElement.Earth)
        {
            return "땅";
        }
        if (element == RuneElement.Darkness)
        {
            return "어둠";
        }
        return "없음";
    }

    /// <summary>
    /// InscriptionType 열거형을 한글로 변환
    /// ItemData 기반 경로에서만 사용 (현재 미사용, 추후 호환용으로 보존)
    /// </summary>
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
