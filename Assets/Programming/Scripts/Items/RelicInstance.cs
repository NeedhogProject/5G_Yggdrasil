using UnityEngine;

/// <summary>
/// 유물 런타임 인스턴스 — ItemInstance 상속
///
/// [기획 반영]
/// - 감정 여부는 유물 하나마다 따로 저장 (같은 종류라도 각각 감정 필요)
/// - 미감정 유물은 효과와 희귀도가 숨겨지고 장착, 판매 불가
/// - 판매용 유물(옵션 None)도 감정 대상
/// </summary>
[System.Serializable]
public class RelicInstance : ItemInstance
{
    /// <summary>RelicData 로 캐스팅된 원본 데이터</summary>
    public RelicData RelicData => Data as RelicData;

    /// <summary>감정 완료 여부 — ScholarSystem, UIItemTooltip, ShopSystem 참조</summary>
    public bool IsIdentified { get; private set; } = false;

    // 장착 가능 여부 (감정된 장착용 유물만)
    public bool CanEquip => IsIdentified == true && RelicData != null && RelicData.IsEquippable == true;

    public RelicInstance(RelicData _data) : base(_data)
    {
    }

    // 감정 처리 (ScholarSystem 에서 호출)
    public void Identify()
    {
        IsIdentified = true;
        Debug.Log($"[RelicInstance] {Data.ItemName} 감정 완료");
    }
}
