using UnityEngine;

/// <summary>
/// 유물 데이터 — ScholarSystem(감정), UIItemTooltip 에서 참조
/// 감정 여부는 유물마다 달라 RelicInstance 에 저장
/// 옵션과 값은 Tables/Relic.xlsx 에서 관리 (ID 는 에셋 이름)
/// </summary>
[CreateAssetMenu(fileName = "NewRelic", menuName = "Yggdrasil/Items/RelicData")]
public class RelicData : ItemData
{
    private RelicRow Row => GameTables.GetRelic(name);

    protected override ItemRow GetTableRow()
    {
        return Row;
    }

    protected override ItemType GetFixedItemType()
    {
        return ItemType.Relic;
    }

    public RelicOptionType OptionType => Row.OptionType;

    // 테이블 값 그대로 (달란 획득량은 %, 나머지는 고정값)
    public float OptionValue => Row.fOptionValue;

    // 장착용 유물인지 (옵션 None 은 판매용)
    public bool IsEquippable => OptionType != RelicOptionType.None;

    /// <summary>유물 효과 문구 (옵션 값으로 자동 생성) — UIItemTooltip, ScholarSystem 참조</summary>
    public string relicEffect
    {
        get
        {
            string value = OptionValue.ToString("0.##");
            switch (OptionType)
            {
                case RelicOptionType.MaxHealth:
                    return "최대 HP +" + value;
                case RelicOptionType.MaxMental:
                    return "최대 정신력 +" + value;
                case RelicOptionType.DropGold:
                    return "달란 획득량 +" + value + "%";
                case RelicOptionType.Attack:
                    return "공격력 +" + value;
                default:
                    return "판매 전용";
            }
        }
    }
}
