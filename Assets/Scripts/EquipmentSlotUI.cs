using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class EquipmentSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Slot Settings")]
    public EquipmentType slotType;

    [Header("UI References")]
    public Image    equipmentIcon;
    public Image    highlightImage;
    public Image    inscriptionIcon;
    public TMP_Text equipmentLevelText;

    [Header("Slot Data")]
    public EquipmentData currentEquipment;
    public bool isEquipped = false;

    // EquipmentSystem 대신 PlayerEquipment 직접 참조
    [SerializeField] private PlayerEquipment playerEquipment;

    private void Start()
    {
        if (playerEquipment == null)
            playerEquipment = FindFirstObjectByType<PlayerEquipment>();
        UpdateSlotUI();
    }

    public void SetEquipment(EquipmentData equipment)
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
        if (equipmentIcon != null)
        {
            equipmentIcon.gameObject.SetActive(isEquipped);

            if (isEquipped && currentEquipment != null)
            {
                equipmentIcon.sprite = currentEquipment.itemIcon;
                equipmentIcon.color  = Color.white;

                if (equipmentLevelText != null)
                {
                    bool show = currentEquipment.upgradeLevel > 0;
                    equipmentLevelText.gameObject.SetActive(show);
                    if (show) equipmentLevelText.text = $"+{currentEquipment.upgradeLevel}";
                }

                if (inscriptionIcon != null)
                {
                    bool show = currentEquipment.isInscribed;
                    inscriptionIcon.gameObject.SetActive(show);
                    if (show) inscriptionIcon.color = GetInscriptionColor(currentEquipment.inscriptionType);
                }
            }
        }

        if (highlightImage != null)
            highlightImage.gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlightImage != null) highlightImage.gameObject.SetActive(true);

        if (isEquipped && currentEquipment != null && UIItemTooltip.Instance != null)
            UIItemTooltip.Instance.ShowTooltip(currentEquipment, transform.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlightImage != null) highlightImage.gameObject.SetActive(false);
        UIItemTooltip.Instance?.HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isEquipped || currentEquipment == null || playerEquipment == null) return;

        if (eventData.button == PointerEventData.InputButton.Left ||
            eventData.button == PointerEventData.InputButton.Right)
        {
            TryUnequip();
        }
    }

    // EquipmentType → PlayerEquipment 해제 메서드 매핑
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

    private static ArmorSlot SlotTypeToArmorSlot(EquipmentType type) => type switch
    {
        EquipmentType.Helmet => ArmorSlot.Helmet,
        EquipmentType.Chest  => ArmorSlot.Chest,
        EquipmentType.Legs   => ArmorSlot.Legs,
        EquipmentType.Boots  => ArmorSlot.Boots,
        _                    => ArmorSlot.Helmet
    };

    private static Color GetInscriptionColor(InscriptionType type) => type switch
    {
        InscriptionType.Fire     => new Color(1f, 0.3f, 0.3f),
        InscriptionType.Water    => new Color(0.3f, 0.5f, 1f),
        InscriptionType.Wind     => new Color(0.3f, 1f, 0.3f),
        InscriptionType.Earth    => new Color(0.6f, 0.4f, 0.2f),
        InscriptionType.Darkness => new Color(0.3f, 0.3f, 0.3f),
        _                        => Color.white
    };
}
