using UnityEngine;

/// <summary>
/// 소모품 종류
/// - 확정 3종: HealthPotion / MentalPotion / ResetScroll
/// - 추후 추가 시 열거형 항목만 늘리면 됨
/// </summary>
public enum ConsumableType
{
    HealthPotion,   // 체력 물약  — 체력 즉시 회복
    MentalPotion,   // 정신력 물약 — 정신력 즉시 회복
    ResetScroll,    // 초기화 주문서 — 각인 초기화 (RuneInscriptionSystem 연동)
}

/// <summary>
/// 소모품 데이터 ScriptableObject — ItemData 상속
///
/// [기획 반영]
/// - 즉시형만 지원 (버프형 없음)
/// - 체력 물약: 최대 체력의 % 회복
/// - 정신력 물약: 고정 포인트 회복
/// - 초기화 주문서: 각인술사(RuneInscriptionSystem)에서 처리
/// - 종류와 효과량은 Tables/Consumable.xlsx 에서 관리 (ID 는 에셋 이름)
/// </summary>
[CreateAssetMenu(fileName = "NewConsumable", menuName = "Yggdrasil/Items/ConsumableData")]
public class ConsumableData : ItemData
{
    private ConsumableRow Row => GameTables.GetConsumable(name);

    protected override ItemRow GetTableRow()
    {
        return Row;
    }

    protected override ItemType GetFixedItemType()
    {
        return ItemType.Consumable;
    }

    // ─────────────────────── 프로퍼티 ───────────────────────

    public ConsumableType ConsumableType => Row.ConsumableType;
    public float          EffectAmount   => Row.fEffectAmount;

    // ─────────────────────── 핵심 메서드 ───────────────────────

    /// <summary>
    /// 소모품 사용 시도.
    /// true  = 사용 성공 (인벤토리에서 1개 차감 처리는 호출부에서)
    /// false = 사용 불가 (이미 최대치 등)
    /// </summary>
    public bool TryUse(PlayerStats stats)
    {
        if (stats == null)
        {
            Debug.LogWarning("[ConsumableData] PlayerStats 가 null 입니다.");
            return false;
        }

        switch (ConsumableType)
        {
            case ConsumableType.HealthPotion:
                return TryUseHealthPotion(stats);

            case ConsumableType.MentalPotion:
                return TryUseMentalPotion(stats);

            case ConsumableType.ResetScroll:
                // 각인 초기화는 무기 인스턴스가 필요하므로
                // RuneInscriptionSystem 에서 직접 처리.
                // 여기서는 '사용 가능 여부'만 true 반환 (실제 초기화는 별도 처리)
                return true;

            default:
                Debug.LogWarning($"[ConsumableData] 처리되지 않은 ConsumableType: {ConsumableType}");
                return false;
        }
    }

    // ─────────────────────── 내부 처리 ───────────────────────

    private bool TryUseHealthPotion(PlayerStats stats)
    {
        if (stats.Health >= stats.MaxHealth) return false;
        // 최대 체력 대비 % 회복 (장비로 최대치가 늘어도 비율 유지)
        stats.ModifyHealth(stats.MaxHealth * (EffectAmount / 100f));
        return true;
    }

    private bool TryUseMentalPotion(PlayerStats stats)
    {
        if (stats.Mental >= stats.MaxMental) return false;
        stats.ModifyMental(EffectAmount);
        return true;
    }
}
