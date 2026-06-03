

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

    [Header("골드")]
    [SerializeField] private int _gold = 0;

    [Header("물약 회복량")]
    [SerializeField] private float healthPotionAmount = 30f;
    [SerializeField] private float mentalPotionAmount = 25f;

    // ── 상수 ──────────────────────────────────────
    private const float MIN_STAT = 0f;
    public const float MAX_STAT = 100f;

    // ── 계산 프로퍼티 ─────────────────────────────
    public float MentalMultiplier => mental / MAX_STAT;
    public float TotalDefense => baseDefense + equipmentDefense;
    public float EffectiveDefense => TotalDefense * MentalMultiplier;

    /// <summary>최대 체력 = 기본(100) + 장비 보너스</summary>
    public float MaxHealth => MAX_STAT + equipmentMaxHealth;

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
            _gold = Mathf.Max(0, value);
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

    public void ModifyMental(float amount)
    {
        mental = Mathf.Clamp(mental + amount, MIN_STAT, MAX_STAT);
        if (OnMentalChanged != null)
        {
            OnMentalChanged.Invoke(mental);
        }
        if (OnStatsChanged != null)
        {
            OnStatsChanged.Invoke();
        }
    }

    // ── 물약 ──────────────────────────────────────

    public bool UseHealthPotion()
    {
        if (health >= MaxHealth)
        {
            return false;
        }
        ModifyHealth(healthPotionAmount);
        return true;
    }

    public bool UseMentalPotion()
    {
        if (mental >= MAX_STAT)
        {
            return false;
        }
        ModifyMental(mentalPotionAmount);
        return true;
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

        // 정신력 패널티 — 정신력 낮을수록 받는 피해 증가
        actualDamage *= (2f - MentalMultiplier);

        ModifyHealth(-actualDamage);
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
        mental = Mathf.Clamp(mental, MIN_STAT, MAX_STAT);
    }

#if UNITY_EDITOR
    [ContextMenu("테스트: 체력 물약")]
    private void TestHealthPotion()
    {
        UseHealthPotion();
    }

    [ContextMenu("테스트: 정신력 물약")]
    private void TestMentalPotion()
    {
        UseMentalPotion();
    }
#endif
}