using UnityEngine;

/// <summary>
/// 소모품 런타임 인스턴스 — ItemInstance 상속
/// 체력 물약 / 정신력 물약 / 초기화 주문서
/// </summary>
[System.Serializable]
public class ConsumableInstance : ItemInstance
{
    // ─────────────────────── 원본 데이터 ───────────────────────

    public ConsumableData ConsumableData => Data as ConsumableData;

    // ─────────────────────── 생성자 ───────────────────────

    public ConsumableInstance(ConsumableData data, int count = 1) : base(data)
    {
        StackCount = Mathf.Clamp(count, 1, data.MaxStack);
    }

    // ─────────────────────── 사용 ───────────────────────

    /// <summary>
    /// 소모품 사용 시도
    /// 성공 시 수량 1 차감. 수량 0이 되면 IsEmpty = true → 인벤토리에서 제거
    /// </summary>
    public bool TryUse(PlayerStats stats)
    {
        if (IsEmpty) return false;

        bool success = ConsumableData.TryUse(stats);
        if (success) RemoveStack(1);
        return success;
    }

    /// <summary>
    /// 초기화 주문서: 방어구 각인 초기화 전용 사용
    /// ArmorInstance 를 직접 받아 ClearRunes() 호출
    /// </summary>
    public bool TryUseResetScroll(ArmorInstance armor)
    {
        if (IsEmpty) return false;
        if (ConsumableData.ConsumableType != ConsumableType.ResetScroll) return false;
        if (armor == null || !armor.HasRune) return false;

        armor.ClearRunes();
        RemoveStack(1);
        return true;
    }

    public override string ToString() =>
        $"[{ConsumableData?.ItemName ?? "null"}] x{StackCount}";
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 자원(원석) 런타임 인스턴스 — ItemInstance 상속
/// 불/물/바람/땅/어둠 원석. 중첩 가능.
/// </summary>
[System.Serializable]
public class ResourceInstance : ItemInstance
{
    // ─────────────────────── 원본 데이터 ───────────────────────

    public ResourceData ResourceData => Data as ResourceData;

    // ─────────────────────── 생성자 ───────────────────────

    public ResourceInstance(ResourceData data, int count = 1) : base(data)
    {
        StackCount = Mathf.Clamp(count, 1, data.MaxStack);
    }

    // ─────────────────────── 유틸 ───────────────────────

    /// <summary>각인 시스템에 넘길 RuneElement 반환</summary>
    public RuneElement ToRuneElement() =>
        ResourceData != null ? ResourceData.ToRuneElement() : RuneElement.None;

    /// <summary>원소 종류</summary>
    public ResourceType ResourceType =>
        ResourceData?.ResourceType ?? ResourceType.Fire;

    /// <summary>
    /// 자원 소비 (각인술사에게 맡길 때)
    /// 반환값: 실제 소비된 수량
    /// </summary>
    public int Consume(int amount)
    {
        if (amount <= 0) return 0;
        return RemoveStack(amount);
    }

    public override string ToString() =>
        $"[{ResourceData?.ItemName ?? "null"}({ResourceType})] x{StackCount}";
}