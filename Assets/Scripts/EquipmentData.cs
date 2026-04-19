using UnityEngine;

/// <summary>
/// NPC 시스템(BlacksmithSystem, EnhancementSystem, InscriptionMasterSystem,
/// ShopSystem, UIItemTooltip, EquipmentSlotUI)에서 공통으로 참조하는
/// 장비 데이터 클래스 — ItemData 상속
/// </summary>
[CreateAssetMenu(fileName = "NewEquipment", menuName = "Yggdrasil/Items/EquipmentData")]
public class EquipmentData : ItemData
{
    [Header("장비 슬롯")]
    [SerializeField] private EquipmentType _equipmentType = EquipmentType.Weapon;

    [Header("기본 전투 스탯")]
    [SerializeField] private int _attack  = 0;
    [SerializeField] private int _defense = 0;

    [Header("강화")]
    [SerializeField] private int _upgradeLevel = 0;

    [Header("내구도")]
    [SerializeField] private float _durability    = 100f;
    [SerializeField] private float _maxDurability = 100f;

    [Header("각인")]
    [SerializeField] private bool            _isInscribed     = false;
    [SerializeField] private InscriptionType _inscriptionType = InscriptionType.None;

    // ─────────────────────── 프로퍼티 ───────────────────────

    /// <summary>PascalCase — 코어 시스템용</summary>
    public EquipmentType EquipmentSlot   => _equipmentType;

    /// <summary>camelCase — InscriptionMasterSystem 호환용</summary>
    public EquipmentType equipmentType   => _equipmentType;

    public int attack
    {
        get => _attack;
        set => _attack = Mathf.Max(0, value);
    }

    public int defense
    {
        get => _defense;
        set => _defense = Mathf.Max(0, value);
    }

    public int upgradeLevel
    {
        get => _upgradeLevel;
        set => _upgradeLevel = Mathf.Clamp(value, 0, 5);
    }

    public float durability
    {
        get => _durability;
        set => _durability = Mathf.Clamp(value, 0f, _maxDurability);
    }

    public float maxDurability => _maxDurability;

    public bool isInscribed
    {
        get => _isInscribed;
        set => _isInscribed = value;
    }

    public InscriptionType inscriptionType
    {
        get => _inscriptionType;
        set => _inscriptionType = value;
    }

    // ─────────────────────── 메서드 ───────────────────────

    /// <summary>강화 시 스탯 10% 증가 — BlacksmithSystem, EnhancementSystem 호출</summary>
    public void UpgradeStats()
    {
        _attack  = Mathf.RoundToInt(_attack  * 1.1f);
        _defense = Mathf.RoundToInt(_defense * 1.1f);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _durability   = Mathf.Clamp(_durability, 0f, _maxDurability);
        _upgradeLevel = Mathf.Clamp(_upgradeLevel, 0, 5);
        _attack       = Mathf.Max(0, _attack);
        _defense      = Mathf.Max(0, _defense);
    }
#endif
}
