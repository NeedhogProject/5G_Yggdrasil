using UnityEngine;

/// <summary>
/// 판매 전용 아이템 데이터 ScriptableObject — ItemData 상속
/// 아이템 값은 Tables/Sellable.xlsx 에서 관리 (ID 는 에셋 이름)
/// </summary>
[CreateAssetMenu(fileName = "NewSellable", menuName = "Yggdrasil/Items/SellableData")]
public class SellableData : ItemData
{
    private SellableRow Row => GameTables.GetSellable(name);

    protected override ItemRow GetTableRow()
    {
        return Row;
    }

    protected override ItemType GetFixedItemType()
    {
        return ItemType.Sellable;
    }
}
