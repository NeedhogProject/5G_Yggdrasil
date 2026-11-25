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
/// - 각인 슬롯은 부위당 최대 1개 (방어구 전용)
/// - 세트는 단일 원소 기반
///   · 불/물/바람/땅 : 같은 원소 방어구 2개부터 세트 효과 발동
///   · 어둠           : 1개부터 효과 발동 (특수 케이스)
///   · 불2+물2 같은 크로스 세트도 각각 독립 발동
/// </summary>
[CreateAssetMenu(fileName = "NewArmor", menuName = "Yggdrasil/Items/ArmorData")]
public class ArmorData : ItemData
{
    // ─────────────────────── 방어구 기본 스펙 ───────────────────────
    // 부위, 방어력, 최대 체력, 기본 각인은 Tables/Armor.xlsx 에서 관리 (ID 는 에셋 이름)

    private ArmorRow Row => GameTables.GetArmor(name);

    protected override ItemRow GetTableRow()
    {
        return Row;
    }

    // ─────────────────────── 프로퍼티 ───────────────────────

    public ArmorSlot   ArmorSlot      => Row.ArmorSlot;
    public float       DefenseBonus   => Row.fDefenseBonus;
    public float       MaxHealthBonus => Row.fMaxHealthBonus;
    public RuneElement RuneSlot1      => Row.RuneSlot1;
    public bool HasRune    => RuneSlot1 != RuneElement.None;
    public bool IsRuneFull => RuneSlot1 != RuneElement.None;
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
