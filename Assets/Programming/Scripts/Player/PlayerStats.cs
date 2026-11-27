using UnityEngine;
using System;

// HUDManager 보다 먼저 Awake 가 호출되도록 강제 (HUDManager.Start 에서 Instance 참조 안전)
[DefaultExecutionOrder(-100)]
public class PlayerStats : MonoBehaviour
{
    // ── 싱글톤 ────────────────────────────────────
    public static PlayerStats Instance { get; private set; }

    // ── Inspector 필드 ────────────────────────────
    [Header("기본 스탯")]
    [SerializeField][Range(0, 100)] private float health = 100f;
    [SerializeField] private float baseDefense = 100f;
    [SerializeField][Range(0, 100)] private float mental = 100f;

    [Header("장비 방어력 (착용 시 추가)")]
    [SerializeField] private float equipmentDefense = 0f;

    [Header("장비 최대 체력 보너스 (착용 시 추가)")]
    [SerializeField] private float equipmentMaxHealth = 0f;

    [Header("세트 효과 보너스 (ArmorSetManager 가 설정)")]
    [SerializeField] private float setMaxHealthPercent = 0f;
    [SerializeField] private float setDefensePercent = 0f;
    [SerializeField] private float setMentalBonus = 0f;       // 최대 정신력 포인트 가산
    [SerializeField] private float setDamageReductionPercent = 0f; // 받는 피해 감소 %

    [Header("유물 보너스 (PlayerEquipment 가 설정)")]
    [SerializeField] private float relicMentalBonus = 0f;     // 최대 정신력 포인트 가산
    [SerializeField] private float relicGoldBonus = 0f;       // 달란 획득 비율 (0.05 = 5%)
    [SerializeField] private float relicAttackBonus = 0f;     // 공격력 고정값 가산

    [Header("정신력 패널티 (정신력 낮을수록 능력치 감소)")]
    // 정신력 0 일 때 공격력 감소율. 0.5 = 최대 50% 감소
    [SerializeField] [Range(0f, 1f)] private float mentalAttackPenalty = 0.5f;
    // 정신력 0 일 때 방어력 감소율
    [SerializeField] [Range(0f, 1f)] private float mentalDefensePenalty = 0.5f;
    // 정신력 0 일 때 이동속도 감소율
    [SerializeField] [Range(0f, 1f)] private float mentalMoveSpeedPenalty = 0.3f;

    // 보호막 런타임 상태 (땅 3세트 보호막, SetEffectController 가 설정)
    private float _shield = 0f;
    private float _lastCombatTime = -999f;

    [Header("골드")]
    [SerializeField] private int _gold = 0;
    // 달란 최대 보유량 (기획: 초과분은 버림)
    public const int MAX_GOLD = 999999;

    // ── 상수 ──────────────────────────────────────
    private const float MIN_STAT = 0f;
    public const float MAX_STAT = 100f;
    // 밸런싱 여지를 위한 정신력 최대 상한
    public const float MAX_MENTAL_CAP = 150f;

    // ── 계산 프로퍼티 ─────────────────────────────
    /// <summary>최대 정신력 = 기본 100 + 세트 보너스 (상한 150)</summary>
    public float MaxMental => Mathf.Min(MAX_STAT + setMentalBonus + relicMentalBonus, MAX_MENTAL_CAP);

    /// <summary>유물 달란 획득 비율 — EnemyBase 참조</summary>
    public float RelicGoldBonus => relicGoldBonus;

    /// <summary>유물 공격력 고정값 — PlayerCombat 참조</summary>
    public float RelicAttackBonus => relicAttackBonus;

    // 정신력 100 초과분은 버퍼(소모 여유분) — 배율은 1.0 상한 고정
    public float MentalMultiplier => Mathf.Clamp01(mental / MAX_STAT);

    // 정신력 부족분 (0 = 만피, 1 = 정신력 0)
    private float MentalDeficit => 1f - MentalMultiplier;

    /// <summary>공격력 배율 (정신력 패널티 반영). 1.0 = 패널티 없음</summary>
    public float MentalAttackMultiplier => 1f - MentalDeficit * mentalAttackPenalty;

    /// <summary>이동속도 배율 (정신력 패널티 반영). 1.0 = 패널티 없음</summary>
    public float MentalMoveSpeedMultiplier => 1f - MentalDeficit * mentalMoveSpeedPenalty;

    // 계산 순서: 기본 + 장비 합산 후 세트 % 배율 적용 (기획: 기본 -> 장비 -> 세트)
    public float TotalDefense => (baseDefense + equipmentDefense) * (1f + setDefensePercent / 100f);
    // 정신력 패널티는 방어력 감소 계수로 적용 (강도는 mentalDefensePenalty)
    public float EffectiveDefense => TotalDefense * (1f - MentalDeficit * mentalDefensePenalty);

    /// <summary>최대 체력 = (기본 100 + 장비 보너스) x 세트 체력 % 배율</summary>
    public float MaxHealth => (MAX_STAT + equipmentMaxHealth) * (1f + setMaxHealthPercent / 100f);

    /// <summary>보호막 최대치 = 최대 체력 x 세트 보호막 %</summary>

    /// <summary>현재 보호막 수치 (피해 선흡수)</summary>
    public float Shield => _shield;

    /// <summary>마지막 공격/피격 시각 (SetEffectController 전투 판정용)</summary>
    public float LastCombatTime => _lastCombatTime;

    // ── 이벤트 ────────────────────────────────────
    public event Action<float> OnHealthChanged;
    public event Action<float> OnDefenseChanged;
    public event Action<float> OnMentalChanged;
    public event Action<int> OnGoldChanged;
    public event Action OnStatsChanged;

    // ── 공개 프로퍼티 ─────────────────────────────
    public float Health => health;
    public float BaseDefense => baseDefense;
    public float EquipmentDefense => equipmentDefense;
    public float Defense => TotalDefense;
    public float Mental => mental;

    public int gold
    {
        get => _gold;
        set
        {
            // 0 ~ 최대 보유량으로 제한 (초과분 버림)
            _gold = Mathf.Clamp(value, 0, MAX_GOLD);
            if (OnGoldChanged != null)
            {
                OnGoldChanged.Invoke(_gold);
            }
        }
    }

    // ── 초기화 ────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ClampAllStats();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>전투 행동 알림 — 공격 시 PlayerCombat 에서 호출 (보호막 재충전 타이머 리셋)</summary>
    public void NotifyCombatAction()
    {
        _lastCombatTime = Time.time;
    }

    // HUD 갱신 구독은 HUDManager 측에서 담당 (PlayerStats 는 UI 를 모름)

    // ── 스탯 변경 ─────────────────────────────────

    public void ModifyHealth(float amount)
    {
        health = Mathf.Clamp(health + amount, MIN_STAT, MaxHealth);
        if (OnHealthChanged != null)
        {
            OnHealthChanged.Invoke(health);
        }
        if (OnStatsChanged != null)
        {
            OnStatsChanged.Invoke();
        }
    }

    public void ModifyBaseDefense(float amount)
    {
        baseDefense = Mathf.Max(MIN_STAT, baseDefense + amount);
        if (OnDefenseChanged != null)
        {
            OnDefenseChanged.Invoke(TotalDefense);
        }
        if (OnStatsChanged != null)
        {
            OnStatsChanged.Invoke();
        }
    }

    // 유물 옵션 가감 (장착 시 양수, 해제 시 음수)
    public void AddRelicBonus(RelicOptionType _type, float _fValue)
    {
        if (_type == RelicOptionType.MaxHealth)
        {
            // HP 는 장비 최대 체력과 같은 규칙 (마을이면 회복, 해제 시 상한 보정)
            if (_fValue > 0f)
            {
                AddEquipmentMaxHealth(_fValue);
            }
            else
            {
                RemoveEquipmentMaxHealth(-_fValue);
            }
        }
        else if (_type == RelicOptionType.MaxMental)
        {
            relicMentalBonus += _fValue;
            // 최대치가 줄면 현재 정신력도 상한으로 보정
            ModifyMental(0f);
        }
        else if (_type == RelicOptionType.DropGold)
        {
            // 테이블은 % 단위
            relicGoldBonus += _fValue / 100f;
        }
        else if (_type == RelicOptionType.Attack)
        {
            relicAttackBonus += _fValue;
        }
    }

    public void ModifyMental(float amount)
    {
        mental = Mathf.Clamp(mental + amount, MIN_STAT, MaxMental);
        if (OnMentalChanged != null)
        {
            OnMentalChanged.Invoke(mental);
        }
        if (OnStatsChanged != null)
        {
            OnStatsChanged.Invoke();
        }
    }

    public void AddEquipmentDefense(float amount)
    {
        equipmentDefense += amount;
        if (equipmentDefense < 0f)
        {
            equipmentDefense = 0f;
        }
        if (OnDefenseChanged != null)
        {
            OnDefenseChanged.Invoke(TotalDefense);
        }
        if (OnStatsChanged != null)
        {
            OnStatsChanged.Invoke();
        }
    }

    public void RemoveEquipmentDefense(float amount)
    {
        equipmentDefense -= amount;
        if (equipmentDefense < 0f)
        {
            equipmentDefense = 0f;
        }
        if (OnDefenseChanged != null)
        {
            OnDefenseChanged.Invoke(TotalDefense);
        }
        if (OnStatsChanged != null)
        {
            OnStatsChanged.Invoke();
        }
    }

    /// <summary>
    /// 장비 최대 체력 추가 (방어구 착용 시 호출)
    /// 마을: 늘어난 최대치만큼 현재 체력도 회복
    /// 던전: 최대치만 늘고 현재 체력 유지 (단, 만피였으면 새 최대치로 만피 유지)
    /// </summary>
    public void AddEquipmentMaxHealth(float amount)
    {
        // 증가 전 만피 여부 판정
        bool bWasFull = health >= MaxHealth;

        equipmentMaxHealth += amount;
        if (equipmentMaxHealth < 0f)
        {
            equipmentMaxHealth = 0f;
        }

        bool bInTown = GameManager.Instance == null || GameManager.Instance.IsInTown;

        if (amount > 0f)
        {
            if (bInTown)
            {
                // 마을: 늘어난 만큼 현재 체력도 회복
                health += amount;
            }
            else if (bWasFull)
            {
                // 던전이지만 만피였으면 새 최대치로 만피 유지
                health = MaxHealth;
            }
            // 던전 + 만피 아니면 현재 체력 그대로
        }

        // 현재 체력이 최대치를 넘지 않도록 정리
        health = Mathf.Clamp(health, MIN_STAT, MaxHealth);

        if (OnHealthChanged != null)
        {
            OnHealthChanged.Invoke(health);
        }
        if (OnStatsChanged != null)
        {
            OnStatsChanged.Invoke();
        }
    }

    /// <summary>
    /// 장비 최대 체력 제거 (방어구 해제 시 호출)
    /// 최대치가 줄면 현재 체력도 그 상한으로 깎임
    /// </summary>
    public void RemoveEquipmentMaxHealth(float amount)
    {
        equipmentMaxHealth -= amount;
        if (equipmentMaxHealth < 0f)
        {
            equipmentMaxHealth = 0f;
        }

        // 최대치 감소로 현재 체력이 넘치면 상한으로 보정
        health = Mathf.Clamp(health, MIN_STAT, MaxHealth);

        if (OnHealthChanged != null)
        {
            OnHealthChanged.Invoke(health);
        }
        if (OnStatsChanged != null)
        {
            OnStatsChanged.Invoke();
        }
    }

    public void SetEquipmentDefense(float value)
    {
        equipmentDefense = Mathf.Max(0f, value);
        if (OnDefenseChanged != null)
        {
            OnDefenseChanged.Invoke(TotalDefense);
        }
        if (OnStatsChanged != null)
        {
            OnStatsChanged.Invoke();
        }
    }

    /// <summary>
    /// 세트 효과 보너스 일괄 설정 (ArmorSetManager 가 재계산 시 호출)
    /// 체력/방어는 % 배율, 정신력은 최대치 포인트
    /// </summary>
    public void SetSetBonusPercents(float healthPercent, float defensePercent, float mentalBonus)
    {
        setMaxHealthPercent = healthPercent;
        setDefensePercent = defensePercent;
        setMentalBonus = mentalBonus;

        // 최대치 변동으로 현재 수치가 상한을 넘으면 보정
        health = Mathf.Clamp(health, MIN_STAT, MaxHealth);
        mental = Mathf.Clamp(mental, MIN_STAT, MaxMental);

        if (OnHealthChanged != null)
        {
            OnHealthChanged.Invoke(health);
        }
        if (OnDefenseChanged != null)
        {
            OnDefenseChanged.Invoke(TotalDefense);
        }
        if (OnMentalChanged != null)
        {
            OnMentalChanged.Invoke(mental);
        }
        if (OnStatsChanged != null)
        {
            OnStatsChanged.Invoke();
        }
    }

    /// <summary>받는 피해 감소 % 설정 (ArmorSetManager 가 호출)</summary>
    public void SetDamageReductionPercent(float _fPercent)
    {
        setDamageReductionPercent = _fPercent;
    }

    /// <summary>보호막 수치 직접 설정 (땅 3세트, SetEffectController 가 호출)</summary>
    public void SetShieldValue(float _fValue)
    {
        _shield = Mathf.Max(0f, _fValue);
        if (OnStatsChanged != null)
        {
            OnStatsChanged.Invoke();
        }
    }

    // ── 데미지 계산 ───────────────────────────────

    public float TakeDamage(float rawDamage)
    {
        float def = EffectiveDefense;

        // 방어력 1당 데미지 0.05% 감소 (방어력 100 = 5% 감소)
        float dd = def * 0.05f;

        // 감소율 상한 95% (최소 5% 는 항상 피해)
        dd = Mathf.Clamp(dd, 0f, 95f);
        float damageRatio = 1f - (dd / 100f);
        float actualDamage = rawDamage * Mathf.Max(damageRatio, 0.05f);

        // 세트 받는 피해 감소 (땅 2세트)
        actualDamage *= Mathf.Max(0f, 1f - setDamageReductionPercent / 100f);

        // 피격 — 보호막 재충전 타이머 리셋
        _lastCombatTime = Time.time;

        // 보호막 선흡수 (남은 피해만 체력으로)
        float healthDamage = actualDamage;
        if (_shield > 0f)
        {
            float absorbed = Mathf.Min(_shield, healthDamage);
            _shield -= absorbed;
            healthDamage -= absorbed;
        }

        ModifyHealth(-healthDamage);
        return actualDamage;
    }

    public void ConsumeMental(float amount)
    {
        ModifyMental(-amount);
    }

    // ── 내부 유틸 ─────────────────────────────────

    private void ClampAllStats()
    {
        health = Mathf.Clamp(health, MIN_STAT, MaxHealth);
        baseDefense = Mathf.Max(MIN_STAT, baseDefense);
        if (equipmentDefense < 0f)
        {
            equipmentDefense = 0f;
        }
        mental = Mathf.Clamp(mental, MIN_STAT, MaxMental);
    }
}
