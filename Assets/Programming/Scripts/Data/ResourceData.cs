using UnityEngine;

/// <summary>
/// 자원(원석) 종류 - 각인/세트 효과에 사용되는 5종 원소
/// WeaponData 의 RuneElement 와 1:1 대응
/// </summary>
public enum ResourceType
{
    Fire    = 0,  // 불
    Water   = 1,  // 물
    Wind    = 2,  // 바람
    Earth   = 3,  // 땅
    Darkness= 4,  // 어둠
}

/// <summary>
/// 자원 데이터 ScriptableObject — ItemData 상속
///
/// [기획 반영]
/// - 자원 5종 (불/물/바람/땅/어둠) — 각인·세트 효과 재료
/// - 어둠은 몬스터 드롭 전용 (자원 노드 배치 불가, ResourceNode 가 검사)
/// - 각인술사 NPC 에게 맡겨 각인 조합에 사용
/// - 원소와 아이템 값은 Tables/Resource.xlsx 에서 관리 (ID 는 에셋 이름)
/// </summary>
[CreateAssetMenu(fileName = "NewResource", menuName = "Yggdrasil/Items/ResourceData")]
public class ResourceData : ItemData
{
    private ResourceRow Row => GameTables.GetResource(name);

    protected override ItemRow GetTableRow()
    {
        return Row;
    }

    protected override ItemType GetFixedItemType()
    {
        return ItemType.Resource;
    }

    /// <summary>원소 종류</summary>
    public ResourceType ResourceType => Row.Element;

    /// <summary>
    /// ResourceType 에서 InscriptionType 으로 변환
    /// 자원 인벤토리(ResourceInventory) 추가 시 사용
    /// </summary>
    public InscriptionType ToInscriptionType()
    {
        return ResourceType switch
        {
            ResourceType.Fire     => InscriptionType.Fire,
            ResourceType.Water    => InscriptionType.Water,
            ResourceType.Wind     => InscriptionType.Wind,
            ResourceType.Earth    => InscriptionType.Earth,
            ResourceType.Darkness => InscriptionType.Darkness,
            _                     => InscriptionType.None
        };
    }
}
