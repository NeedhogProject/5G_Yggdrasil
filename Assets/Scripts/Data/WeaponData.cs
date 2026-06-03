using UnityEngine;

/// <summary>
/// 무기 종류 - 기획안 기준 3종
/// </summary>
public enum WeaponType
{
    Dagger,   // 단검: 완전 근접, 짧은 리치
    Sword,    // 장검: 중간 밸런스
    Spear     // 창  : 가장 긴 리치
}

/// <summary>
/// 무기 등급 (강화·각인 시 참조)
/// </summary>
public enum WeaponRarity
{
    Common,     // 일반
    Rare,       // 희귀
    Epic,       // 영웅
    Legendary   // 전설
}

/// <summary>
/// 무기 데이터 ScriptableObject — ItemData 상속
/// 무기 종류(단검/장검/창), 리치, 공격력, 공격 속도, 강화 단계(0~5) 포함
/// 각인은 방어구 전용 — 무기에는 강화만 가능
/// </summary>
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Yggdrasil/Items/WeaponData")]
public class WeaponData : ItemData
{
    // ─────────────────────── 무기 기본 스펙 ───────────────────────

    [Header("무기 종류")]
    [SerializeField] private WeaponType   weaponType = WeaponType.Sword;
    [SerializeField] private WeaponRarity _weaponRarity = WeaponRarity.Common;

    [Header("전투 스펙")]
    [Tooltip("기본 공격력 (정신력·강화 보정 전)")]
    [SerializeField] [Range(1f, 200f)] private float baseDamage = 10f;

    [Tooltip("초당 공격 횟수 (공격 속도)")]
    [SerializeField] [Range(0.1f, 5f)]  private float attackSpeed = 1f;

    [Tooltip("공격이 닿는 거리 (Unity 단위). 단검<장검<창 권장값: 1.2 / 2.0 / 3.5")]
    [SerializeField] [Range(0.5f, 6f)]  private float reach = 2f;

    [Tooltip("공격 판정 너비(호/원 반경). 찌르기 계열(창/단검)은 좁게, 장검은 넓게")]
    [SerializeField] [Range(0.1f, 2f)]  private float attackWidth = 0.5f;

    // ─────────────────────── 강화 시스템 ───────────────────────
    // 기획: 최대 5강, 1→4는 실패 시 등급 하락, 4→5는 성공 아니면 태초마을(초기화)
    // 각인을 넣으면 강화 단계 1로 초기화

    [Header("강화 단계 (0 ~ 5)")]
    [SerializeField] [Range(0, 5)] private int enhancementLevel = 0;

    // 0강부터 4강 시도 시 성공 확률 (기획서 수치: 90/75/45/25/10%)
    private static readonly float[] EnhanceSuccessRates = { 90f, 75f, 45f, 25f, 10f };

    // 강화 단계별 공격력 배율 (기획서 기준: 0/2/4/7/9/15%)
    private static readonly float[] AttackMultipliers = { 1.00f, 1.02f, 1.04f, 1.07f, 1.09f, 1.15f };

    // 강화 단계별 공격속도 배율 (기획서 기준: 3강부터 2/3/7%)
    private static readonly float[] SpeedMultipliers = { 1.00f, 1.00f, 1.00f, 1.02f, 1.03f, 1.07f };

    // ─────────────────────── 프로퍼티 ───────────────────────

    public WeaponType   WeaponType   { get => weaponType; internal set => weaponType = value; }
    public WeaponRarity WeaponRarity => _weaponRarity;
    public float        BaseDamage       => baseDamage;
    public float        AttackSpeed      => attackSpeed;

    /// <summary>공격 리치 (Unity 단위). 단검 ≈ 1.2, 장검 ≈ 2.0, 창 ≈ 3.5</summary>
    public float Reach       => reach;
    public float AttackWidth => attackWidth;

    public int EnhancementLevel => enhancementLevel;

    /// <summary>현재 강화 단계의 성공 확률 (%)</summary>
    public float CurrentSuccessRate =>
        enhancementLevel < EnhanceSuccessRates.Length
            ? EnhanceSuccessRates[enhancementLevel]
            : 0f;

    /// <summary>4→5강 여부 (성공 or 태초마을)</summary>
    public bool IsLastEnhance => enhancementLevel == 4;

    // ─────────────────────── 강화 보정 공격력 ───────────────────────

    /// <summary>강화 단계가 반영된 실제 공격력 (기획서 배율 적용)</summary>
    public float FinalDamage =>
        baseDamage * AttackMultipliers[Mathf.Clamp(enhancementLevel, 0, AttackMultipliers.Length - 1)];

    /// <summary>강화 단계가 반영된 실제 공격속도 (기획서 배율 적용)</summary>
    public float FinalAttackSpeed =>
        attackSpeed * SpeedMultipliers[Mathf.Clamp(enhancementLevel, 0, SpeedMultipliers.Length - 1)];

    // ─────────────────────── 런타임 메서드 (무기 복사본에서 사용) ───────────────────────

    /// <summary>
    /// 강화 시도. 성공 여부는 코인 결과(앞=true/뒤=false)로 전달.
    /// true  → 강화 성공, enhancementLevel++
    /// false → 1~4강은 등급 하락(enhancementLevel--), 4→5 실패 시 초기화
    /// 반환값: EnhanceResult
    /// ※ ScriptableObject 원본을 직접 수정하지 말고 런타임 복사본(WeaponInstance)에서 호출할 것
    /// </summary>
    public EnhanceResult TryEnhance(bool coinResult)
    {
        if (enhancementLevel >= 5) return EnhanceResult.AlreadyMax;

        if (coinResult)
        {
            enhancementLevel++;
            return enhancementLevel == 5 ? EnhanceResult.MaxReached : EnhanceResult.Success;
        }
        else
        {
            if (IsLastEnhance) // 4강 실패: 2단계 하락 (4강에서 2강으로)
            {
                enhancementLevel = Mathf.Max(0, enhancementLevel - 2);
                return EnhanceResult.Downgrade;
            }
            else // 0~3강 실패: 1단계 하락 (0강은 변동 없음)
            {
                enhancementLevel = Mathf.Max(0, enhancementLevel - 1);
                return EnhanceResult.Downgrade;
            }
        }
    }

    // ─────────────────────── 에디터 기본값 자동 설정 ───────────────────────
#if UNITY_EDITOR
    private void OnValidate()
    {
        // 무기 종류별 권장 리치·너비 자동 세팅 (처음 생성 시 참고용)
        switch (weaponType)
        {
            case WeaponType.Dagger:
                if (reach      > 1.5f) reach       = 1.2f;
                if (attackWidth > 0.5f) attackWidth = 0.3f;
                break;
            case WeaponType.Sword:
                // 기본값 유지
                break;
            case WeaponType.Spear:
                if (reach < 3f) reach = 3.5f;
                if (attackWidth > 0.6f) attackWidth = 0.4f;
                break;
        }
    }
#endif
}

// ─────────────────────── 보조 열거형 / 구조체 ───────────────────────

/// <summary>
/// 원소 자원 종류 (각인·세트 효과에 사용)
/// ResourceData.ResourceType 과 1:1 대응
/// </summary>
public enum RuneElement
{
    None     = -1,
    Fire     = 0,  // 불
    Water    = 1,  // 물
    Wind     = 2,  // 바람
    Earth    = 3,  // 땅
    Darkness = 4,  // 어둠
}

/// <summary>강화 결과 코드</summary>
public enum EnhanceResult
{
    Success,      // 성공 (최대강 미만)
    MaxReached,   // 5강 달성
    Downgrade,    // 실패 → 등급 하락
    ResetToBase,  // 4→5 실패 → 태초마을(완전 초기화)
    AlreadyMax    // 이미 5강
}