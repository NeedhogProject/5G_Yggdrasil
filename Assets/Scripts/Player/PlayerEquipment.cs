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
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private ArmorSetManager armorSetManager;
    [SerializeField] private InventorySystem inventorySystem;
    [SerializeField] private InscriptionColorHelper inscriptionColorHelper;

    [Header("무기 모델")]
    [SerializeField] private Transform weaponHolder;   // 오른손 본 아래 빈 오브젝트(WeaponHolder)

    private GameObject _objWeaponModel;   // 현재 손에 부착된 무기 모델 인스턴스

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

    // 유물 슬롯 2개 (반지처럼 장착). 유물은 RelicData 기반 — 전용 인스턴스가 없어 ItemInstance 로 보관
    private readonly ItemInstance[] _relicSlots = new ItemInstance[2];

    // ─────────────────────── 이벤트 ───────────────────────

    /// <summary>장비 변경 시 발생 (UI 갱신용)</summary>
    public event System.Action OnEquipmentChanged;

    // ─────────────────────── 초기화 ───────────────────────

    private void Awake()
    {
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (playerCombat == null) playerCombat = GetComponent<PlayerCombat>();
        if (armorSetManager == null) armorSetManager = GetComponent<ArmorSetManager>();
        if (inventorySystem == null) inventorySystem = InventorySystem.Instance;
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

        // 유물은 전용 유물 슬롯으로 장착 (RelicData 기반)
        if (item.Data is RelicData)
        {
            replaced = EquipRelicAuto(item);
        }
        else
        {
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
        AttachWeaponModel(newWeapon);   // 손에 무기 모델 부착

        Debug.Log($"[PlayerEquipment] 무기 장착: {newWeapon.WeaponData?.ItemName}");
        OnEquipmentChanged?.Invoke();

        // 교체된 기존 무기 반환 (인벤토리로 돌아감)
        return previous;
    }

    private void DetachWeapon(WeaponInstance weapon)
    {
        playerCombat?.UnequipWeapon();
        RemoveWeaponModel();   // 부착된 무기 모델 제거
        EquippedWeapon = null;
    }

    // 무기 모델 부착/제거

    /// <summary>무기 모델을 weaponHolder 자식으로 부착</summary>
    private void AttachWeaponModel(WeaponInstance weapon)
    {
        RemoveWeaponModel();

        if (weaponHolder == null) return;
        if (weapon == null || weapon.WeaponData == null) return;

        GameObject prefab = weapon.WeaponData.WeaponModelPrefab;
        if (prefab == null) return;

        _objWeaponModel = Instantiate(prefab, weaponHolder);
        _objWeaponModel.transform.localPosition = Vector3.zero;
        _objWeaponModel.transform.localRotation = Quaternion.identity;
    }

    /// <summary>부착된 무기 모델 제거</summary>
    private void RemoveWeaponModel()
    {
        if (_objWeaponModel != null)
        {
            Destroy(_objWeaponModel);
            _objWeaponModel = null;
        }
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
        ArmorInstance.InscriptionInfo info = newArmor.GetInscription();


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

    // ─────────────────────── 유물 장착 ───────────────────────

    // 빈 유물 슬롯을 찾아 장착한다. 두 칸 다 차 있으면 0번 칸을 교체한다.
    // 교체된 기존 유물을 반환 (없으면 null)
    private ItemInstance EquipRelicAuto(ItemInstance relic)
    {
        if (relic == null)
        {
            return null;
        }

        int targetIndex = -1;

        // 빈 칸 먼저 찾기
        for (int i = 0; i < _relicSlots.Length; i++)
        {
            if (_relicSlots[i] == null)
            {
                targetIndex = i;
                break;
            }
        }

        // 빈 칸이 없으면 0번 칸을 교체
        if (targetIndex == -1)
        {
            targetIndex = 0;
        }

        return EquipRelicAt(targetIndex, relic);
    }

    // 지정한 유물 슬롯(0 또는 1)에 장착. 교체된 기존 유물 반환
    public ItemInstance EquipRelicAt(int index, ItemInstance relic)
    {
        if (index < 0 || index >= _relicSlots.Length)
        {
            return null;
        }
        if (relic == null)
        {
            return null;
        }

        ItemInstance previous = _relicSlots[index];

        // 기존 유물 효과 해제
        if (previous != null)
        {
            RemoveRelicEffect(previous);
        }

        // 새 유물 장착
        _relicSlots[index] = relic;
        ApplyRelicEffect(relic);

        Debug.Log("[PlayerEquipment] 유물 장착 (슬롯 " + index.ToString() + "): " + relic.Data?.ItemName);
        OnEquipmentChanged?.Invoke();

        return previous;
    }

    // 지정한 유물 슬롯 해제. 해제된 유물 반환
    public ItemInstance UnequipRelicSlot(int index)
    {
        if (index < 0 || index >= _relicSlots.Length)
        {
            return null;
        }

        ItemInstance relic = _relicSlots[index];
        if (relic == null)
        {
            return null;
        }

        RemoveRelicEffect(relic);
        _relicSlots[index] = null;
        OnEquipmentChanged?.Invoke();

        return relic;
    }

    // 지정한 유물 슬롯의 유물 반환 (없으면 null) — EquipmentSlotUI 표시용
    public ItemInstance GetRelic(int index)
    {
        if (index < 0 || index >= _relicSlots.Length)
        {
            return null;
        }
        return _relicSlots[index];
    }

    // 유물 효과 적용 — 아직 유물 스탯이 정의되지 않아 비워둠 (효과 확정 시 여기에 작성)
    private void ApplyRelicEffect(ItemInstance relic)
    {
        // TODO: 유물 효과 적용. RelicData 에 스탯이 생기면 PlayerStats 등에 반영
    }

    // 유물 효과 해제 — ApplyRelicEffect 와 짝. 효과 확정 시 반대 처리 작성
    private void RemoveRelicEffect(ItemInstance relic)
    {
        // TODO: 유물 효과 해제
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

        // 유물 슬롯도 전부 해제
        for (int i = 0; i < _relicSlots.Length; i++)
        {
            ItemInstance relic = _relicSlots[i];
            if (relic == null)
            {
                continue;
            }
            dropped.Add(relic);
            RemoveRelicEffect(relic);
            _relicSlots[i] = null;
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