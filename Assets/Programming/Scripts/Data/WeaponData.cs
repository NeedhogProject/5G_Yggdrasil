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
    // 무기 종류, 등급, 전투 수치는 Tables/Weapon.xlsx 에서 관리 (ID 는 에셋 이름)

    private WeaponRow Row => GameTables.GetWeapon(name);

    protected override ItemRow GetTableRow()
    {
        return Row;
    }

    [Header("무기 모델")]
    [Tooltip("손에 장착될 무기 3D 모델 프리팹")]
    [SerializeField] private GameObject weaponModelPrefab = null;

    // ─────────────────────── 강화 시스템 ───────────────────────
    // 기획: 최대 5강, 1→4는 실패 시 등급 하락, 4→5는 성공 아니면 태초마을(초기화)
    // 각인을 넣으면 강화 단계 1로 초기화

    [Header("강화 단계 (0 ~ 5)")]
    [SerializeField] [Range(0, 5)] private int enhancementLevel = 0;

    // 강화 성공 확률, 공격력 배율, 공격속도 배율은 Tables/Enhance.xlsx 에서 관리
    private static float[] enhanceSuccessRates => GameTables.EnhanceSuccessRates;
    private static float[] attackMultipliers => GameTables.AttackMultipliers;
    private static float[] speedMultipliers => GameTables.SpeedMultipliers;

    // ─────────────────────── 프로퍼티 ───────────────────────

    public WeaponType   WeaponType   => Row.WeaponType;
    public WeaponRarity WeaponRarity => Row.WeaponRarity;
    public float        BaseDamage       => Row.fBaseDamage;
    public float        AttackSpeed      => Row.fAttackSpeed;

    /// <summary>공격 리치 (Unity 단위). 단검 ≈ 1.2, 장검 ≈ 2.0, 창 ≈ 3.5</summary>
    public float Reach       => Row.fReach;
    public float AttackWidth => Row.fAttackWidth;

    /// <summary>공격 경직 시간 (초)</summary>
    public float AttackLockTime => Row.fAttackLockTime;

    public int EnhancementLevel => enhancementLevel;

    /// <summary>손에 장착될 무기 모델 프리팹</summary>
    public GameObject WeaponModelPrefab => weaponModelPrefab;

    /// <summary>현재 강화 단계의 성공 확률 (%)</summary>
    public float CurrentSuccessRate =>
        enhancementLevel < enhanceSuccessRates.Length
            ? enhanceSuccessRates[enhancementLevel]
            : 0f;

    /// <summary>4→5강 여부 (성공 or 태초마을)</summary>
    public bool IsLastEnhance => enhancementLevel == 4;

    // ─────────────────────── 강화 보정 공격력 ───────────────────────

    /// <summary>강화 단계가 반영된 실제 공격력 (기획서 배율 적용)</summary>
    public float FinalDamage =>
        BaseDamage * attackMultipliers[Mathf.Clamp(enhancementLevel, 0, attackMultipliers.Length - 1)];

    /// <summary>강화 단계가 반영된 실제 공격속도 (기획서 배율 적용)</summary>
    public float FinalAttackSpeed =>
        AttackSpeed * speedMultipliers[Mathf.Clamp(enhancementLevel, 0, speedMultipliers.Length - 1)];

    /// <summary>강화 단계별 공격력 배율 표 (대장간 미리보기에서 참조)</summary>
    public float[] AttackMultipliers => attackMultipliers;

    /// <summary>강화 단계별 공격속도 배율 표 (대장간 미리보기에서 참조)</summary>
    public float[] SpeedMultipliers => speedMultipliers;

    /// <summary>강화 단계별 성공 확률 표 (WeaponInstance 에서 참조)</summary>
    public float[] EnhanceSuccessRates => enhanceSuccessRates;

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
    Fail,         // 실패, 등급 변동 없음 (0~3강)
    Downgrade,    // 실패, 1강 하락 (4강에서만)
    ResetToBase,  // 4→5 실패 → 태초마을(완전 초기화)
    AlreadyMax    // 이미 5강
}