using UnityEngine;

/// <summary>
/// 런타임 아이템 인스턴스 베이스 클래스
///
/// ScriptableObject(ItemData)는 원본 데이터 — 절대 직접 수정 금지
/// ItemInstance 는 게임 중 변화하는 상태(수량, 인벤토리 위치 등)를 담당
///
/// [상속 구조]
/// ItemInstance
/// ├── WeaponInstance   (강화 단계, 각인 상태)
/// ├── ArmorInstance    (세트 효과 캐싱)
/// ├── ConsumableInstance (현재 수량)
/// └── ResourceInstance   (현재 수량)
/// </summary>
[System.Serializable]
public class ItemInstance
{
    // ─────────────────────── 원본 데이터 참조 ───────────────────────

    /// <summary>원본 ScriptableObject — 읽기 전용</summary>
    public ItemData Data { get; private set; }

    // ─────────────────────── 인벤토리 상태 ───────────────────────

    /// <summary>인벤토리 격자 내 좌상단 위치 (InventorySystem 에서 관리)</summary>
    public Vector2Int SlotPosition { get; set; } = new Vector2Int(-1, -1);

    /// <summary>인벤토리에 배치돼 있는지</summary>
    public bool IsPlaced => SlotPosition.x >= 0 && SlotPosition.y >= 0;

    /// <summary>현재 수량 (stackable 아이템용, 기본 1)</summary>
    public int StackCount { get; protected set; } = 1;

    // ─────────────────────── 고유 ID ───────────────────────

    /// <summary>런타임 고유 ID — 인벤토리/저장 시 식별용</summary>
    public string InstanceId { get; private set; }

    // ─────────────────────── 생성자 ───────────────────────

    public ItemInstance(ItemData data)
    {
        Data       = data;
        InstanceId = System.Guid.NewGuid().ToString();
        StackCount = 1;
    }

    // ─────────────────────── 수량 관리 ───────────────────────

    /// <summary>
    /// 수량 추가. isStackable 이 false면 무시.
    /// 반환값: 실제로 추가된 수량
    /// </summary>
    public int AddStack(int amount)
    {
        if (!Data.IsStackable) return 0;
        int before = StackCount;
        StackCount = Mathf.Clamp(StackCount + amount, 1, Data.MaxStack);
        return StackCount - before;
    }

    /// <summary>
    /// 수량 감소.
    /// 반환값: 실제로 감소된 수량
    /// </summary>
    public int RemoveStack(int amount)
    {
        int before = StackCount;
        StackCount = Mathf.Max(0, StackCount - amount);
        return before - StackCount;
    }

    /// <summary>수량이 0이 되어 인벤토리에서 제거해야 하는지</summary>
    public bool IsEmpty => StackCount <= 0;

    // ─────────────────────── 유틸 ───────────────────────

    /// <summary>인스턴스 위치 초기화 (인벤토리에서 제거 시)</summary>
    public void ClearSlotPosition() => SlotPosition = new Vector2Int(-1, -1);

    public override string ToString() =>
        $"[{Data?.ItemName ?? "null"}] x{StackCount} @ {SlotPosition} (ID: {InstanceId[..8]}...)";
}