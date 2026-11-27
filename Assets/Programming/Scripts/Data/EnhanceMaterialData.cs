using UnityEngine;

/// <summary>
/// 강화 재료 데이터 ScriptableObject — ItemData 상속
/// 아이템 값은 Tables/EnhanceMaterial.xlsx 에서 관리 (ID 는 에셋 이름)
/// </summary>
[CreateAssetMenu(fileName = "NewEnhanceMaterial", menuName = "Yggdrasil/Items/EnhanceMaterialData")]
public class EnhanceMaterialData : ItemData
{
    private EnhanceMaterialRow Row => GameTables.GetEnhanceMaterial(name);

    protected override ItemRow GetTableRow()
    {
        return Row;
    }

    protected override ItemType GetFixedItemType()
    {
        return ItemType.EnhanceMaterial;
    }
}
