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
    DamageReduction,// 받는 피해 감소 %
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
/// 각 Tier 값은 그 단계에서 새로 추가되는 수치 (누적 — N세트 착용 시 1~N 단계 합산 적용)
/// 확률/지속/범위형 특수 효과는 SetEffectController 인스펙터에서 조정
///
/// [기획 확정 세트 효과 — 수치 보너스만]
/// ■ 불   Tier2: 공격력 +5%
/// ■ 바람 Tier2: 이동속도 +5% / Tier3: 이동속도 +10%, 공격속도 +10%
/// ■ 땅   Tier2: 받는 피해 -5%
/// ■ 어둠 Tier2: 체력 -5%, 이동속도 +4%
/// </summary>
[CreateAssetMenu(fileName = "NewSetEffect", menuName = "Yggdrasil/Items/SetEffectData")]
public class SetEffectData : ScriptableObject
{
    // ─────────────────────── 세트 식별 ───────────────────────

    [Header("세트 원소")]
    [Tooltip("이 데이터가 담당하는 원소. ArmorInstance 의 각인 원소와 매칭됨.")]
    [SerializeField] private RuneElement element = RuneElement.Fire;

    // ─────────────────────── Tier 별 수치 보너스 ───────────────────────

    [Header("Tier 1 — 추가 수치 보너스 (어둠 전용, 1개 착용)")]
    [SerializeField] private StatBonus[] tier1Bonuses = new StatBonus[0];

    [Header("Tier 2 — 추가 수치 보너스 (2개 착용)")]
    [SerializeField] private StatBonus[] tier2Bonuses = new StatBonus[0];

    [Header("Tier 3 — 추가 수치 보너스 (3개 착용, 하위 단계에 더해짐)")]
    [SerializeField] private StatBonus[] tier3Bonuses = new StatBonus[0];

    [Header("Tier 4 — 추가 수치 보너스 (4개 착용, 하위 단계에 더해짐)")]
    [SerializeField] private StatBonus[] tier4Bonuses = new StatBonus[0];

    // ─────────────────────── 프로퍼티 ───────────────────────

    public RuneElement Element => element;

    // ─────────────────────── 조회 메서드 ───────────────────────

    /// <summary>해당 Tier 에서 추가되는 수치 보너스 배열 반환</summary>
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
