using UnityEngine;

/// <summary>
/// 방어구 부위 - 기획안 기준 4종
/// </summary>
public enum ArmorSlot
{
    Helmet,  // 투구  (머리)
    Chest,   // 갑옷  (몸)
    Legs,    // 각반  (다리)
    Boots    // 장화  (신발)
}

/// <summary>
/// 방어구 데이터 ScriptableObject — ItemData 상속
///
/// [기획 반영]
/// - 방어구 부위 4종 (투구/갑옷/각반/장화)
/// - 추가 방어력 : PlayerStats.AddEquipmentDefense() 에 전달할 값
/// - 각인 슬롯 2개 (방어구 전용) : 각인술사에서 원소 2개 조합
/// - 세트는 단일 원소 기반
///   · 불/물/바람/땅 : 같은 원소 방어구 2개부터 세트 효과 발동
///   · 어둠           : 1개부터 효과 발동 (특수 케이스)
///   · 불2+물2 같은 크로스 세트도 각각 독립 발동
/// </summary>
[CreateAssetMenu(fileName = "NewArmor", menuName = "Yggdrasil/Items/ArmorData")]
public class ArmorData : ItemData
{
    // ─────────────────────── 방어구 기본 스펙 ───────────────────────

    [Header("방어구 부위")]
    [SerializeField] private ArmorSlot armorSlot = ArmorSlot.Chest;

    [Header("방어력")]
    [Tooltip("착용 시 PlayerStats.AddEquipmentDefense() 에 전달되는 값")]
    [SerializeField] [Range(0f, 200f)] private float defenseBonus = 10f;

    // ─────────────────────── 각인 슬롯 ───────────────────────

    [Header("각인 슬롯 (최대 2개) — 방어구 전용")]
    [Tooltip("각인 첫 번째 원소 (각인술사에서 설정)")]
    [SerializeField] private RuneElement runeSlot1 = RuneElement.None;

    [Tooltip("각인 두 번째 원소 (각인술사에서 설정)")]
    [SerializeField] private RuneElement runeSlot2 = RuneElement.None;

    // ─────────────────────── 프로퍼티 ───────────────────────

    public ArmorSlot   ArmorSlot   => armorSlot;
    public float       DefenseBonus => defenseBonus;
    public RuneElement RuneSlot1   => runeSlot1;
    public RuneElement RuneSlot2   => runeSlot2;
    public bool HasRune    => runeSlot1 != RuneElement.None || runeSlot2 != RuneElement.None;
    public bool IsRuneFull => runeSlot1 != RuneElement.None && runeSlot2 != RuneElement.None;

    /// <summary>
    /// 이 방어구가 기여하는 원소 목록 반환 (중복 제거)
    /// ex) 불+불 → [Fire],  불+물 → [Fire, Water]
    /// ArmorSetManager 에서 원소별 세트 카운트 집계에 사용
    /// </summary>
    public System.Collections.Generic.List<RuneElement> GetContributingElements()
    {
        System.Collections.Generic.List<RuneElement> result = new System.Collections.Generic.List<RuneElement>();
        if (runeSlot1 != RuneElement.None && !result.Contains(runeSlot1))
            result.Add(runeSlot1);
        if (runeSlot2 != RuneElement.None && !result.Contains(runeSlot2))
            result.Add(runeSlot2);
        return result;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (defenseBonus < 0f) defenseBonus = 0f;
    }
#endif
}

// ─────────────────────── 세트 효과 단계 ───────────────────────

/// <summary>
/// 단일 원소 착용 수에 따른 세트 효과 단계
/// 어둠은 1개부터 Tier1, 나머지는 2개부터 Tier2
/// </summary>
public enum SetTier
{
    None,   // 효과 없음
    Tier1,  // 1개 (어둠 전용)
    Tier2,  // 2개
    Tier3,  // 3개
    Tier4   // 4개
}

public static class SetTierExtensions
{
    public static SetTier FromCount(RuneElement element, int count)
    {
        if (element == RuneElement.Darkness)
        {
            return count switch
            {
                1    => SetTier.Tier1,
                2    => SetTier.Tier2,
                3    => SetTier.Tier3,
                >= 4 => SetTier.Tier4,
                _    => SetTier.None
            };
        }
        return count switch
        {
            2    => SetTier.Tier2,
            3    => SetTier.Tier3,
            >= 4 => SetTier.Tier4,
            _    => SetTier.None
        };
    }
}