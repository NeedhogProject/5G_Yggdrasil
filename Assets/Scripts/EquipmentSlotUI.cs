using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class EquipmentSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("슬롯 설정")]
    public EquipmentType slotType;

    [Header("UI 참조")]
    public Image    equipmentIcon;
    public Image    highlightImage;
    public Image    inscriptionIcon;
    public TMP_Text equipmentLevelText;

    [Header("슬롯 데이터")]
    public ItemData currentEquipment;
    public bool isEquipped = false;

    [SerializeField] private PlayerEquipment playerEquipment;

    private void Start()
    {
        if (playerEquipment == null)
            playerEquipment = FindFirstObjectByType<PlayerEquipment>();
        UpdateSlotUI();
    }

    // ItemData 베이스 타입으로 받아 WeaponData / ArmorData 모두 수용
    public void SetEquipment(ItemData equipment)
    {
        currentEquipment = equipment;
        isEquipped = equipment != null;
        UpdateSlotUI();
    }

    public void ClearSlot()
    {
        currentEquipment = null;
        isEquipped = false;
        UpdateSlotUI();
    }

    private void UpdateSlotUI()
    {
        if (equipmentIcon == null)
        {
            return;
        }

        equipmentIcon.gameObject.SetActive(isEquipped);

        if (isEquipped == false || currentEquipment == null)
        {
            if (equipmentLevelText != null) equipmentLevelText.gameObject.SetActive(false);
            if (inscriptionIcon   != null) inscriptionIcon.gameObject.SetActive(false);
            if (highlightImage    != null) highlightImage.gameObject.SetActive(false);
            return;
        }

        equipmentIcon.sprite = currentEquipment.itemIcon;
        equipmentIcon.color  = Color.white;

        // 강화 단계: 무기(WeaponData)만 표시
        if (equipmentLevelText != null)
        {
            int level = GetEnhancementLevel(currentEquipment);
            bool showLevel = level > 0;
            equipmentLevelText.gameObject.SetActive(showLevel);
            if (showLevel)
            {
                equipmentLevelText.text = $"+{level}";
            }
        }

        // 각인 아이콘: 방어구(ArmorData)만 표시
        if (inscriptionIcon != null)
        {
            RuneElement rune = GetFirstRune(currentEquipment);
            bool showRune = rune != RuneElement.None;
            inscriptionIcon.gameObject.SetActive(showRune);
            if (showRune)
            {
                inscriptionIcon.color = GetRuneColor(rune);
            }
        }

        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(true);
        }

        if (isEquipped && currentEquipment != null && UIItemTooltip.Instance != null)
        {
            UIItemTooltip.Instance.ShowTooltip(currentEquipment, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(false);
        }
        UIItemTooltip.Instance?.HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isEquipped == false || currentEquipment == null || playerEquipment == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left ||
            eventData.button == PointerEventData.InputButton.Right)
        {
            TryUnequip();
        }
    }

    private void TryUnequip()
    {
        if (slotType == EquipmentType.Weapon)
        {
            playerEquipment.UnequipWeapon();
        }
        else
        {
            ArmorSlot armorSlot = SlotTypeToArmorSlot(slotType);
            playerEquipment.UnequipArmorSlot(armorSlot);
        }
        ClearSlot();
    }

    // 강화 단계 조회: WeaponData면 EnhancementLevel, 그 외 0
    private static int GetEnhancementLevel(ItemData data)
    {
        if (data is WeaponData weaponData)
        {
            return weaponData.EnhancementLevel;
        }
        return 0;
    }

    // 첫 번째 룬 조회: ArmorData면 RuneSlot1, 그 외 None
    private static RuneElement GetFirstRune(ItemData data)
    {
        if (data is ArmorData armorData)
        {
            return armorData.RuneSlot1;
        }
        return RuneElement.None;
    }

    private static ArmorSlot SlotTypeToArmorSlot(EquipmentType type)
    {
        switch (type)
        {
            case EquipmentType.Helmet: return ArmorSlot.Helmet;
            case EquipmentType.Chest:  return ArmorSlot.Chest;
            case EquipmentType.Legs:   return ArmorSlot.Legs;
            case EquipmentType.Boots:  return ArmorSlot.Boots;
            default:                   return ArmorSlot.Helmet;
        }
    }

    private static Color GetRuneColor(RuneElement rune)
    {
        switch (rune)
        {
            case RuneElement.Fire:     return new Color(1f, 0.3f, 0.3f);
            case RuneElement.Water:    return new Color(0.3f, 0.5f, 1f);
            case RuneElement.Wind:     return new Color(0.3f, 1f, 0.3f);
            case RuneElement.Earth:    return new Color(0.6f, 0.4f, 0.2f);
            case RuneElement.Darkness: return new Color(0.4f, 0.2f, 0.6f);
            default:                   return Color.white;
        }
    }
}
