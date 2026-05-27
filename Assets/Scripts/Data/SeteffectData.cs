using UnityEngine;

// ─────────────────────── 보조 열거형 ───────────────────────

/// <summary>수치 증가 대상</summary>
public enum StatBonusType
{
    None,
    AttackDamage,   // 공격력 %
    Defense,        // 방어력 %
    Health,         // 체력 % (최대 체력 기준)
    Mental,         // 정신력 % (최대 정신력 기준)
    AttackSpeed,    // 공격 속도 %
    MoveSpeed,      // 이동 속도 %
}

/// <summary>수치 증가/감소 1개 항목</summary>
[System.Serializable]
public struct StatBonus
{
    [Tooltip("대상 능력치")]
    public StatBonusType bonusType;

    [Tooltip("변화량 (%). 양수=증가, 음수=감소\n" +
             "ex) 5 → 5% 증가 / -10 → 10% 감소")]
    [Range(-100f, 100f)]
    public float percent;
}

/// <summary>특수 효과 종류 (쿨다운 있는 발동형)</summary>
public enum SpecialEffectType
{
    None,
    ElementalDamageOnHit,       // 공격 시 원소 데미지 추가 (무기 공격력 % 기반)
    LifeStealOnHit,             // 공격 시 최대 체력 비례 회복 (불 4세트)
    ShieldOnHit,                // 피격 시 최대 체력 비례 방어막 (물 4세트)
    ElementalDamageFromDefense, // 방어력 기반 원소 데미지 (땅 3/4세트)
}

/// <summary>특수 효과 1개 항목</summary>
[System.Serializable]
public struct SpecialEffect
{
    [Tooltip("효과 종류")]
    public SpecialEffectType effectType;

    [Tooltip("수치 (%). 의미는 effectType 에 따라 다름\n" +
             "ElementalDamageOnHit       → 무기 공격력의 N%\n" +
             "LifeStealOnHit             → 최대 체력의 N%\n" +
             "ShieldOnHit                → 최대 체력의 N%\n" +
             "ElementalDamageFromDefense → 방어력의 N%")]
    [Range(0f, 200f)]
    public float valuePercent;

    [Tooltip("원소 종류 (데미지 계열 효과에 사용)")]
    public RuneElement element;

    [Tooltip("발동 쿨다운 (초)")]
    [Range(0.1f, 60f)]
    public float cooldown;
}

// ─────────────────────── SetEffectData ───────────────────────

/// <summary>
/// 단일 원소 세트 효과 정의 ScriptableObject
///
/// [기획 확정 세트 효과]
///
/// ■ 불 (Fire)
///   Tier2: 공격력 +5%
///   Tier3: 공격 시 무기 공격력 12% 불 데미지 추가
///   Tier4: 공격 시 무기 공격력 25% 불 데미지 추가
///          + 공격 시 최대 체력 2% 회복 (쿨다운 1초)
///
/// ■ 물 (Water)
///   Tier2: 공격력 +2%, 체력 +2%
///   Tier3: 공격 시 무기 공격력 10% 물 데미지 추가
///   Tier4: 공격 시 무기 공격력 20% 물 데미지 추가
///          + 피격 시 최대 체력 5% 방어막 (쿨다운 7.5초)
///
/// ■ 바람 (Wind)
///   Tier2: 공격 속도 +7%, 이동 속도 +5%
///   Tier3: 공격 시 무기 공격력 15% 바람 데미지 추가
///   Tier4: 공격 속도 +20%, 이동 속도 +7%
///
/// ■ 땅 (Earth)
///   Tier2: 방어력 +7%, 최대 체력 +7%
///   Tier3: 방어력의 20% 땅 데미지 추가
///   Tier4: 방어력 +15%, 이동 속도 -7%
///          + 방어력의 5% 땅 데미지 (쿨다운형)
///
/// ■ 어둠 (Darkness)
///   Tier1: 공격력 +9%, 정신력 -15%
///   Tier2: 이동 속도 +9%, 공격 속도 +9%
///   Tier3: 무기 공격력 25% 어둠 데미지 추가, 최대 체력 -10%
///   Tier4: 무기 공격력 35% 어둠 데미지 추가, 최대 체력 -15%
/// </summary>
[CreateAssetMenu(fileName = "NewSetEffect", menuName = "Yggdrasil/Items/SetEffectData")]
public class SetEffectData : ScriptableObject
{
    // ─────────────────────── 세트 식별 ───────────────────────

    [Header("세트 원소")]
    [Tooltip("이 데이터가 담당하는 원소. ArmorInstance 의 각인 원소와 매칭됨.")]
    [SerializeField] private RuneElement element = RuneElement.Fire;

    // ─────────────────────── Tier 별 수치 보너스 ───────────────────────

    [Header("Tier 1 — 수치 보너스 (어둠 전용, 1개 착용)")]
    [SerializeField] private StatBonus[] tier1Bonuses = new StatBonus[0];

    [Header("Tier 2 — 수치 보너스 (2개 착용)")]
    [SerializeField] private StatBonus[] tier2Bonuses = new StatBonus[0];

    [Header("Tier 3 — 수치 보너스 (3개 착용, 없으면 비워두기)")]
    [SerializeField] private StatBonus[] tier3Bonuses = new StatBonus[0];

    [Header("Tier 4 — 수치 보너스 (4개 착용)")]
    [SerializeField] private StatBonus[] tier4Bonuses = new StatBonus[0];

    // ─────────────────────── Tier 별 특수 효과 ───────────────────────

    [Header("Tier 3 — 특수 효과 (원소 데미지 등)")]
    [SerializeField] private SpecialEffect[] tier3Effects = new SpecialEffect[0];

    [Header("Tier 4 — 특수 효과 (Tier3 효과 포함 + 추가)")]
    [SerializeField] private SpecialEffect[] tier4Effects = new SpecialEffect[0];

    // ─────────────────────── 프로퍼티 ───────────────────────

    public RuneElement Element => element;

    // ─────────────────────── 조회 메서드 ───────────────────────

    /// <summary>해당 Tier 의 수치 보너스 배열 반환</summary>
    public StatBonus[] GetStatBonuses(SetTier tier)
    {
        return tier switch
        {
            SetTier.Tier1 => tier1Bonuses,
            SetTier.Tier2 => tier2Bonuses,
            SetTier.Tier3 => tier3Bonuses,
            SetTier.Tier4 => tier4Bonuses,
            _             => new StatBonus[0]
        };
    }

    /// <summary>해당 Tier 의 특수 효과 배열 반환</summary>
    public SpecialEffect[] GetSpecialEffects(SetTier tier)
    {
        return tier switch
        {
            SetTier.Tier3 => tier3Effects,
            SetTier.Tier4 => tier4Effects,
            _             => new SpecialEffect[0]
        };
    }

    // ─────────────────────── 에디터 유효성 검사 ───────────────────────
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (element == RuneElement.None)
            Debug.LogWarning($"[SetEffectData: {name}] 원소가 None 입니다.");

        // 어둠이 아닌데 Tier1 보너스가 있으면 경고
        if (element != RuneElement.Darkness && tier1Bonuses.Length > 0)
            Debug.LogWarning($"[SetEffectData: {name}] 어둠 외 원소는 Tier1 효과가 없습니다.");
    }
#endif
}