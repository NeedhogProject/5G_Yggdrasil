using UnityEngine;

/// <summary>
/// 무기 런타임 인스턴스 — ItemInstance 상속
///
/// ScriptableObject(WeaponData)는 원본 — 절대 수정 금지
/// 강화 단계 등 게임 중 변하는 무기 상태를 여기서 관리
/// 각인은 방어구 전용 — 무기에는 강화만 가능
/// </summary>
[System.Serializable]
public class WeaponInstance : ItemInstance
{
    // ─────────────────────── 원본 무기 데이터 ───────────────────────

    /// <summary>WeaponData 로 캐스팅된 원본 데이터</summary>
    public WeaponData WeaponData => Data as WeaponData;

    // ─────────────────────── 강화 상태 ───────────────────────

    /// <summary>현재 강화 단계 (0~5)</summary>
    public int EnhancementLevel { get; private set; } = 0;

    // ─────────────────────── 생성자 ───────────────────────

    public WeaponInstance(WeaponData data) : base(data)
    {
        EnhancementLevel = 0;
    }

    /// <summary>
    /// 테스트 전용 생성자 — WeaponType 강제 지정
    /// HitboxSystem ContextMenu 테스트에서 사용
    /// </summary>
    public WeaponInstance(WeaponData data, WeaponType overrideType) : base(data)
    {
        EnhancementLevel = 0;
        _overrideWeaponType = overrideType;
        _hasOverride        = true;
    }

    private WeaponType _overrideWeaponType;
    private bool       _hasOverride;

    /// <summary>WeaponData 타입 (오버라이드 있으면 우선 사용)</summary>
    public WeaponType ResolvedWeaponType =>
        _hasOverride ? _overrideWeaponType : (WeaponData?.WeaponType ?? WeaponType.Sword);

    // ─────────────────────── 강화 메서드 ───────────────────────

    /// <summary>
    /// 강화 시도 (대장장이 시스템에서 호출)
    /// coinResult: true = 앞면(성공), false = 뒷면(실패)
    /// </summary>
    public EnhanceResult TryEnhance(bool coinResult)
    {
        if (EnhancementLevel >= 5) return EnhanceResult.AlreadyMax;

        if (coinResult)
        {
            EnhancementLevel++;
            return EnhancementLevel == 5 ? EnhanceResult.MaxReached : EnhanceResult.Success;
        }
        else
        {
            if (EnhancementLevel == 4) // 4→5 실패: 태초마을 (완전 초기화)
            {
                EnhancementLevel = 0;
                return EnhanceResult.ResetToBase;
            }
            else // 1~4 실패: 등급 하락
            {
                EnhancementLevel = Mathf.Max(0, EnhancementLevel - 1);
                return EnhanceResult.Downgrade;
            }
        }
    }

    // ─────────────────────── 계산된 스탯 ───────────────────────

    /// <summary>
    /// 강화 단계가 반영된 최종 공격력
    /// 1강마다 기본 공격력의 10% 증가
    /// </summary>
    public float FinalDamage =>
        WeaponData != null ? WeaponData.BaseDamage * (1f + EnhancementLevel * 0.1f) : 0f;

    /// <summary>현재 강화 단계의 성공 확률 (%)</summary>
    private static readonly float[] SuccessRates = { 100f, 80f, 60f, 40f, 20f };
    public float CurrentSuccessRate =>
        EnhancementLevel < SuccessRates.Length ? SuccessRates[EnhancementLevel] : 0f;

    /// <summary>4→5강 시도인지 (실패 시 태초마을)</summary>
    public bool IsLastEnhance => EnhancementLevel == 4;

    public override string ToString() =>
        $"[{WeaponData?.ItemName ?? "null"}] +{EnhancementLevel} | 공격력: {FinalDamage:F1}";
}