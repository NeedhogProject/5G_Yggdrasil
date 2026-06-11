using UnityEngine;

// ─────────────────────── 보조 열거형 ───────────────────────

/// <summary>수치 증가 대상</summary>
public enum StatBonusType
{
    None,
    AttackDamage,   // 공격력 %
    Defense,        // 방어력 %
    Health,         // 체력 % (최대 체력 기준)
    Mental,         // 정신력 (포인트 가산, 0~100 스케일. % 아님)
    AttackSpeed,    // 공격 속도 %
    MoveSpeed,      // 이동 속도 %
    LifeSteal,      // 흡혈 % (공격 시 입힌 데미지 비례 회복)
    Shield,         // 보호막 % (최대 체력 비례, 피해 선흡수)
}

/// <summary>수치 증가/감소 1개 항목</summary>
[System.Serializable]
public struct StatBonus
{
    [Tooltip("대상 능력치")]
    public StatBonusType bonusType;

    [Tooltip("변화량. 양수=증가, 음수=감소\n" +
             "Mental 은 포인트(ex: 20 → 정신력 +20), 나머지는 % (ex: 5 → 5%)")]
    [Range(-100f, 100f)]
    public float percent;
}

// ─────────────────────── SetEffectData ───────────────────────

/// <summary>
/// 단일 원소 세트 효과 정의 ScriptableObject
/// 각 Tier 값은 최종값 (중첩 합산 아님 — 현재 Tier 의 보너스만 적용)
///
/// [기획 확정 세트 효과]
///
/// ■ 불 (Fire)
///   Tier2: 공격력 +5%
///   Tier3: 공격력 +10%, 공격속도 +5%
///   Tier4: 공격력 +12%, 공격속도 +7%, 흡혈 +3%
///
/// ■ 물 (Water)
///   Tier2: 공격력 +2%, 체력 +2%
///   Tier3: 공격력 +5%, 체력 +5%, 정신력 +20
///   Tier4: 공격력 +7%, 체력 +7%, 정신력 +35
///
/// ■ 땅 (Earth)
///   Tier2: 체력 +5%, 방어력 +5%
///   Tier3: 체력 +10%, 방어력 +10%, 보호막 +10%
///   Tier4: 체력 +15%, 방어력 +15%, 보호막 +20%
///
/// ■ 바람 (Wind)
///   Tier2: 공격속도 +5%, 이동속도 +5%
///   Tier3: 공격속도 +10%, 이동속도 +10%, 정신력 +25
///   Tier4: 공격속도 +17%, 이동속도 +17%, 정신력 +40
///
/// ■ 어둠 (Darkness)
///   Tier1: 공격력 +5%, 체력 -5%
///   Tier2: 공격력 +7%, 공격속도 +7%, 체력 -8%, 방어력 -8%
///   Tier3: 공격력 +12%, 공격속도 +12%, 체력 -14%, 방어력 -14%
///   Tier4: 공격력 +17%, 공격속도 +17%, 체력 -20%, 방어력 -20%, 정신력 -20
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

    [Header("Tier 3 — 수치 보너스 (3개 착용)")]
    [SerializeField] private StatBonus[] tier3Bonuses = new StatBonus[0];

    [Header("Tier 4 — 수치 보너스 (4개 착용)")]
    [SerializeField] private StatBonus[] tier4Bonuses = new StatBonus[0];

    // ─────────────────────── 프로퍼티 ───────────────────────

    public RuneElement Element => element;

    // ─────────────────────── 조회 메서드 ───────────────────────

    /// <summary>해당 Tier 의 수치 보너스 배열 반환 (최종값)</summary>
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
