using UnityEngine;

/// <summary>
/// 줄기 열쇠 종류 — 방향과 1:1 대응
/// </summary>
public enum KeyDirection
{
    North,  // 북쪽 줄기 열쇠
    South,  // 남쪽 줄기 열쇠
    East,   // 동쪽 줄기 열쇠
    West    // 서쪽 줄기 열쇠
}

/// <summary>
/// 줄기 열쇠 아이템 데이터 ScriptableObject — ItemData 상속
///
/// [기획 반영]
/// - 각 층 생명체가 열쇠 1개씩 랜덤 소유 (층 내 중복 없음)
/// - 해당 방향 줄기 앞에서 E키로 삽입
/// - 삽입 시 구멍 연출 후 다음 층 입장
/// - 3→4층은 줄기 1개 고정이지만 동일하게 열쇠 삽입 연출 있음
/// - 방향과 아이템 값은 Tables/FloorKey.xlsx 에서 관리 (ID 는 에셋 이름)
/// </summary>
[CreateAssetMenu(fileName = "NewFloorKey", menuName = "Yggdrasil/Items/FloorKeyData")]
public class FloorKeyData : ItemData
{
    private FloorKeyRow Row => GameTables.GetFloorKey(name);

    protected override ItemRow GetTableRow()
    {
        return Row;
    }

    protected override ItemType GetFixedItemType()
    {
        return ItemType.QuestItem;
    }

    // 이 열쇠가 열 수 있는 줄기 방향
    public KeyDirection KeyDirection => Row.KeyDirection;
}
