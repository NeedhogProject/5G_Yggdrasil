using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class EquipmentSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("슬롯 설정")]
    public EquipmentType slotType;

    [Header("UI 참조")]
    public Image equipmentIcon;
    public Image highlightImage;
    public Image inscriptionIcon;
    public TMP_Text equipmentLevelText;

    [Header("빈 슬롯 표시")]
    [Tooltip("이 부위가 비었을 때 흐리게 표시할 실루엣 스프라이트 (부위별로 지정)")]
    public Sprite emptySlotIcon;
    [Tooltip("빈 슬롯 실루엣의 흐림 정도 (0=투명, 1=불투명)")]
    [Range(0f, 1f)]
    public float emptySlotAlpha = 0.3f;

    [Header("슬롯 데이터")]
    public ItemData currentEquipment;
    public bool isEquipped = false;

    [SerializeField] private PlayerEquipment playerEquipment;

    private void Start()
    {
        if (playerEquipment == null)
        {
            playerEquipment = FindFirstObjectByType<PlayerEquipment>();
        }
        UpdateSlotUI();
    }

    private void OnEnable()
    {
        if (playerEquipment == null)
        {
            playerEquipment = FindFirstObjectByType<PlayerEquipment>();
        }
        if (playerEquipment != null)
        {
            // 장착/해제 시 이 슬롯을 자동 갱신
            playerEquipment.OnEquipmentChanged += RefreshFromEquipment;
            RefreshFromEquipment();
        }
    }

    private void OnDisable()
    {
        if (playerEquipment != null)
        {
            playerEquipment.OnEquipmentChanged -= RefreshFromEquipment;
        }
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

    // PlayerEquipment 의 현재 장착 상태를 읽어 이 슬롯에 반영
    private void RefreshFromEquipment()
    {
        if (playerEquipment == null)
        {
            return;
        }

        ItemData equipped = GetEquippedDataForThisSlot();
        if (equipped != null)
        {
            SetEquipment(equipped);
        }
        else
        {
            ClearSlot();
        }
    }

    // 이 슬롯 부위에 장착된 아이템 데이터 반환 (없으면 null)
    private ItemData GetEquippedDataForThisSlot()
    {
        if (slotType == EquipmentType.Weapon)
        {
            WeaponInstance weapon = playerEquipment.EquippedWeapon;
            return weapon?.Data;
        }

        ArmorSlot armorSlot = SlotTypeToArmorSlot(slotType);
        ArmorInstance armor = playerEquipment.GetArmor(armorSlot);
        return armor?.Data;
    }

    private void UpdateSlotUI()
    {
        if (equipmentIcon == null)
        {
            return;
        }

        if (isEquipped == false || currentEquipment == null)
        {
            // 빈 슬롯: 부위 실루엣을 흐리게 표시 (실루엣이 없으면 아이콘 숨김)
            if (emptySlotIcon != null)
            {
                equipmentIcon.gameObject.SetActive(true);
                equipmentIcon.sprite = emptySlotIcon;
                equipmentIcon.color = new Color(1f, 1f, 1f, emptySlotAlpha);
            }
            else
            {
                equipmentIcon.gameObject.SetActive(false);
            }

            if (equipmentLevelText != null)
            {
                equipmentLevelText.gameObject.SetActive(false);
            }
            if (inscriptionIcon != null)
            {
                inscriptionIcon.gameObject.SetActive(false);
            }
            if (highlightImage != null)
            {
                highlightImage.gameObject.SetActive(false);
            }
            return;
        }

        // 장착됨: 풀컬러 아이템 아이콘
        equipmentIcon.gameObject.SetActive(true);
        equipmentIcon.sprite = currentEquipment.itemIcon;
        equipmentIcon.color = Color.white;

        // 강화 단계: 무기(WeaponData)만 표시
        if (equipmentLevelText != null)
        {
            int level = GetEnhancementLevel(currentEquipment);
            bool showLevel = level > 0;
            equipmentLevelText.gameObject.SetActive(showLevel);
            if (showLevel == true)
            {
                equipmentLevelText.text = "+" + level.ToString();
            }
        }

        // 각인 아이콘: 방어구(ArmorData)만 표시
        if (inscriptionIcon != null)
        {
            RuneElement rune = GetFirstRune(currentEquipment);
            bool showRune = rune != RuneElement.None;
            inscriptionIcon.gameObject.SetActive(showRune);
            if (showRune == true)
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

        if (isEquipped == false || currentEquipment == null)
        {
            return;
        }

        if (UIItemTooltip.Instance != null)
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

        if (UIItemTooltip.Instance != null)
        {
            UIItemTooltip.Instance.HideTooltip();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isEquipped == false || currentEquipment == null || playerEquipment == null)
        {
            return;
        }

        bool isLeftClick = eventData.button == PointerEventData.InputButton.Left;
        bool isRightClick = eventData.button == PointerEventData.InputButton.Right;

        if (isLeftClick == true || isRightClick == true)
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

    // 각인 원소 조회: ArmorData면 RuneSlot1, 그 외 None
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
        if (type == EquipmentType.Helmet)
        {
            return ArmorSlot.Helmet;
        }
        if (type == EquipmentType.Chest)
        {
            return ArmorSlot.Chest;
        }
        if (type == EquipmentType.Legs)
        {
            return ArmorSlot.Legs;
        }
        if (type == EquipmentType.Boots)
        {
            return ArmorSlot.Boots;
        }
        return ArmorSlot.Helmet; // 도달하면 안 되는 분기: slotType 설정 오류 의심
    }

    private static Color GetRuneColor(RuneElement rune)
    {
        if (rune == RuneElement.Fire)
        {
            return new Color(1f, 0.3f, 0.3f);
        }
        if (rune == RuneElement.Water)
        {
            return new Color(0.3f, 0.5f, 1f);
        }
        if (rune == RuneElement.Wind)
        {
            return new Color(0.3f, 1f, 0.3f);
        }
        if (rune == RuneElement.Earth)
        {
            return new Color(0.6f, 0.4f, 0.2f);
        }
        if (rune == RuneElement.Darkness) // 정건희 enum 이름 확인 후 Dark / Darkness 통일
        {
            return new Color(0.4f, 0.2f, 0.6f);
        }
        return Color.white;
    }
}