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

    // ── 추후 추가 예시 ──────────────────────────────
    // EnhanceStone,   // 강화석 — 강화 성공 확률 보조
    // AntiCurse,      // 저주 해제 등
}

/// <summary>
/// 소모품 데이터 ScriptableObject — ItemData 상속
///
/// [기획 반영]
/// - 즉시형만 지원 (버프형 없음)
/// - 체력 물약: PlayerStats.ModifyHealth(+amount)
/// - 정신력 물약: PlayerStats.ModifyMental(+amount)
/// - 초기화 주문서: WeaponData.ClearRunes() (RuneInscriptionSystem 에서 처리)
/// - 나중에 소모품 추가 시 ConsumableType 열거형 + switch 케이스만 추가
/// </summary>
[CreateAssetMenu(fileName = "NewConsumable", menuName = "Yggdrasil/Items/ConsumableData")]
public class ConsumableData : ItemData
{
    // ─────────────────────── 소모품 기본 스펙 ───────────────────────

    [Header("소모품 종류")]
    [SerializeField] private ConsumableType consumableType = ConsumableType.HealthPotion;

    [Header("즉시 효과량")]
    [Tooltip("체력/정신력 물약: 회복량. 초기화 주문서: 사용 안 함 (0으로 두면 됨)")]
    [SerializeField] [Range(0f, 100f)] private float effectAmount = 30f;

    [Header("사용 조건 메시지 (UI 표시용)")]
    [Tooltip("이미 최대치일 때 등 사용 불가 상황에서 보여줄 메시지")]
    [SerializeField] private string cannotUseMessage = "이미 최대치입니다.";

    // ─────────────────────── 프로퍼티 ───────────────────────

    public ConsumableType ConsumableType => consumableType;
    public float          EffectAmount   => effectAmount;
    public string         CannotUseMessage => cannotUseMessage;

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

        switch (consumableType)
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

            // ── 추후 추가 시 케이스 추가 ──────────────────────
            // case ConsumableType.EnhanceStone:
            //     return TryUseEnhanceStone(stats);

            default:
                Debug.LogWarning($"[ConsumableData] 처리되지 않은 ConsumableType: {consumableType}");
                return false;
        }
    }

    // ─────────────────────── 내부 처리 ───────────────────────

    private bool TryUseHealthPotion(PlayerStats stats)
    {
        if (stats.Health >= 100f) return false;
        stats.ModifyHealth(effectAmount);
        return true;
    }

    private bool TryUseMentalPotion(PlayerStats stats)
    {
        if (stats.Mental >= 100f) return false;
        stats.ModifyMental(effectAmount);
        return true;
    }

    // ─────────────────────── 에디터 기본값 자동 설정 ───────────────────────
#if UNITY_EDITOR
    private void OnValidate()
    {
        // 소모품 종류별 권장 기본값 안내
        switch (consumableType)
        {
            case ConsumableType.HealthPotion:
                if (effectAmount == 0f) effectAmount = 30f;
                cannotUseMessage = "체력이 이미 최대치입니다.";
                break;
            case ConsumableType.MentalPotion:
                if (effectAmount == 0f) effectAmount = 25f;
                cannotUseMessage = "정신력이 이미 최대치입니다.";
                break;
            case ConsumableType.ResetScroll:
                effectAmount     = 0f; // 주문서는 수치 효과 없음
                cannotUseMessage = "각인이 없어 사용할 수 없습니다.";
                break;
        }
    }
#endif
}