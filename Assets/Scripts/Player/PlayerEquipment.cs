using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어 장비 착탈 시스템
///
/// [기획 반영]
/// - 슬롯: 무기 1개 + 방어구 4개 (투구/갑옷/각반/장화)
/// - 장착 방식: 인벤토리 UI 에서 드래그앤드롭 → EquipItem() 호출
/// - 이미 장착 중인 슬롯에 새 장비 장착 시 기존 장비 자동으로 인벤토리로 반환
/// - 장착/해제 시 PlayerStats, PlayerCombat, ArmorSetManager 자동 연동
///
/// [연동 시스템]
/// - PlayerStats  : 방어구 방어력 AddEquipmentDefense / RemoveEquipmentDefense
/// - PlayerCombat : 무기 EquipWeapon / UnequipWeapon
/// - ArmorSetManager : OnArmorEquipped / OnArmorUnequipped
/// - InventorySystem : 교체된 장비 반환 (InventorySystem 완성 후 연동)
/// </summary>
public class PlayerEquipment : MonoBehaviour
{
    // ─────────────────────── 참조 ───────────────────────

    [Header("참조")]
    [SerializeField] private PlayerStats      playerStats;
    [SerializeField] private PlayerCombat     playerCombat;
    [SerializeField] private ArmorSetManager  armorSetManager;
    [SerializeField] private InventorySystem  inventorySystem;
    [SerializeField] private InscriptionColorHelper inscriptionColorHelper;

    // ─────────────────────── 장비 슬롯 ───────────────────────

    /// <summary>현재 장착 무기</summary>
    public WeaponInstance EquippedWeapon { get; private set; }

    /// <summary>방어구 슬롯 (부위 → 인스턴스)</summary>
    private readonly Dictionary<ArmorSlot, ArmorInstance> _armorSlots
        = new Dictionary<ArmorSlot, ArmorInstance>
        {
            { ArmorSlot.Helmet, null },
            { ArmorSlot.Chest,  null },
            { ArmorSlot.Legs,   null },
            { ArmorSlot.Boots,  null }
        };

    // ─────────────────────── 이벤트 ───────────────────────

    /// <summary>장비 변경 시 발생 (UI 갱신용)</summary>
    public event System.Action OnEquipmentChanged;

    // ─────────────────────── 초기화 ───────────────────────

    private void Awake()
    {
        if (playerStats      == null) playerStats      = GetComponent<PlayerStats>();
        if (playerCombat     == null) playerCombat     = GetComponent<PlayerCombat>();
        if (armorSetManager  == null) armorSetManager  = GetComponent<ArmorSetManager>();
        if (inventorySystem  == null) inventorySystem  = InventorySystem.Instance;
    }

    // ─────────────────────── 장착 ───────────────────────

    /// <summary>
    /// 아이템 장착 시도 — 인벤토리 UI 드래그앤드롭에서 호출
    /// 교체된 기존 장비는 자동으로 인벤토리로 반환
    /// </summary>
    public ItemInstance EquipItem(ItemInstance item)
    {
        if (item == null) return null;

        ItemInstance replaced = null;

        switch (item)
        {
            case WeaponInstance weapon:
                replaced = EquipWeapon(weapon);
                break;
            case ArmorInstance armor:
                replaced = EquipArmor(armor);
                break;
            default:
                Debug.LogWarning($"[PlayerEquipment] 장착 불가 아이템: {item.Data?.ItemName}");
                return null;
        }

        // 교체된 기존 장비 → 인벤토리로 자동 반환
        if (replaced != null)
        {
            if (inventorySystem == null) inventorySystem = InventorySystem.Instance;
            inventorySystem?.AddItem(replaced);
        }

        return replaced;
    }

    // ─────────────────────── 무기 장착 ───────────────────────

    private ItemInstance EquipWeapon(WeaponInstance newWeapon)
    {
        WeaponInstance previous = EquippedWeapon;

        // 기존 무기 해제
        if (previous != null)
            DetachWeapon(previous);

        // 새 무기 장착
        EquippedWeapon = newWeapon;
        playerCombat?.EquipWeapon(newWeapon);

        Debug.Log($"[PlayerEquipment] 무기 장착: {newWeapon.WeaponData?.ItemName}");
        OnEquipmentChanged?.Invoke();

        // 교체된 기존 무기 반환 (인벤토리로 돌아감)
        return previous;
    }

    private void DetachWeapon(WeaponInstance weapon)
    {
        playerCombat?.UnequipWeapon();
        EquippedWeapon = null;
    }

    // ─────────────────────── 방어구 장착 ───────────────────────

    private ItemInstance EquipArmor(ArmorInstance newArmor)
    {
        ArmorSlot slot = newArmor.Slot;
        ArmorInstance previous = _armorSlots[slot];

        // 기존 방어구 해제
        if (previous != null)
            DetachArmor(previous);

        // 새 방어구 장착
        _armorSlots[slot] = newArmor;
        newArmor.Equip();
        playerStats?.AddEquipmentDefense(newArmor.DefenseBonus);
        playerStats?.AddEquipmentMaxHealth(newArmor.MaxHealthBonus);
        armorSetManager?.OnArmorEquipped(newArmor); // 추가

        // 각인 색상 UI 갱신
        ArmorInstance.InscriptionInfo info = newArmor.GetInscription( );

       
        //inscriptionColorHelper?.UpdateInscriptionColor(slot.ToString(), info.Element.ToString());
    
        OnEquipmentChanged?.Invoke();

        // 교체된 기존 방어구 반환 (인벤토리로 돌아감)
        return previous;
    }

    private void DetachArmor(ArmorInstance armor)
    {
        armor.Unequip();
        playerStats?.RemoveEquipmentDefense(armor.DefenseBonus);
        playerStats?.RemoveEquipmentMaxHealth(armor.MaxHealthBonus);
        armorSetManager?.OnArmorUnequipped(armor);
        _armorSlots[armor.Slot] = null;
        inscriptionColorHelper?.UpdateInscriptionColor(armor.Slot.ToString(), "");
    }

    // ─────────────────────── 해제 ───────────────────────

    /// <summary>
    /// 장비 해제 — 인벤토리 UI 에서 슬롯에서 드래그로 꺼낼 때 호출
    /// 반환값: 해제된 아이템 인스턴스
    /// </summary>
    public ItemInstance UnequipItem(ItemInstance item)
    {
        if (item == null) return null;

        switch (item)
        {
            case WeaponInstance weapon when weapon == EquippedWeapon:
                DetachWeapon(weapon);
                OnEquipmentChanged?.Invoke();
                return weapon;

            case ArmorInstance armor when _armorSlots[armor.Slot] == armor:
                DetachArmor(armor);
                OnEquipmentChanged?.Invoke();
                return armor;

            default:
                Debug.LogWarning($"[PlayerEquipment] 해제 실패: 장착 중이 아닌 아이템");
                return null;
        }
    }

    /// <summary>슬롯 지정 해제</summary>
    public ArmorInstance UnequipArmorSlot(ArmorSlot slot)
    {
        ArmorInstance armor = _armorSlots[slot];
        if (armor == null) return null;
        DetachArmor(armor);
        OnEquipmentChanged?.Invoke();
        return armor;
    }

    /// <summary>무기 해제</summary>
    public WeaponInstance UnequipWeapon()
    {
        WeaponInstance weapon = EquippedWeapon;
        if (weapon == null) return null;
        DetachWeapon(weapon);
        OnEquipmentChanged?.Invoke();
        return weapon;
    }

    // ─────────────────────── 사망 처리 ───────────────────────

    /// <summary>
    /// 플레이어 사망 시 PlayerDeath 에서 호출
    /// 모든 장비 해제 (인벤토리 드롭은 PlayerDeath 에서 처리)
    /// </summary>
    public List<ItemInstance> UnequipAll()
    {
        List<ItemInstance> dropped = new List<ItemInstance>();

        if (EquippedWeapon != null)
        {
            dropped.Add(EquippedWeapon);
            DetachWeapon(EquippedWeapon);
        }

        foreach (object slot in System.Enum.GetValues(typeof(ArmorSlot)))
        {
            ArmorSlot armorSlot = (ArmorSlot)slot;
            ArmorInstance armor = _armorSlots[armorSlot];
            if (armor == null) continue;
            dropped.Add(armor);
            DetachArmor(armor);
        }

        OnEquipmentChanged?.Invoke();
        return dropped;
    }

    // ─────────────────────── 조회 ───────────────────────

    /// <summary>특정 부위 방어구 반환</summary>
    public ArmorInstance GetArmor(ArmorSlot slot) => _armorSlots[slot];

    /// <summary>해당 부위 슬롯이 비어있는지</summary>
    public bool IsSlotEmpty(ArmorSlot slot) => _armorSlots[slot] == null;

    /// <summary>무기 슬롯이 비어있는지</summary>
    public bool IsWeaponSlotEmpty => EquippedWeapon == null;

    /// <summary>현재 장착 중인 모든 방어구 목록</summary>
    public IEnumerable<ArmorInstance> GetAllArmors()
    {
        foreach (ArmorInstance armor in _armorSlots.Values)
            if (armor != null) yield return armor;
    }

    /// <summary>총 장착 방어력 합산</summary>
    public float TotalArmorDefense
    {
        get
        {
            float total = 0f;
            foreach (ArmorInstance armor in GetAllArmors())
                total += armor.DefenseBonus;
            return total;
        }
    }
}
