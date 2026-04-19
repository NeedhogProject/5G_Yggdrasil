using UnityEngine;
using System;

/// <summary>
/// 플레이어 스탯 관리 - 체력, 방어력(기본 0~100, 장비로 추가 증가), 정신력 (0~100)
/// 정신력이 낮을수록 방어력 효과 감소·받는 피해 증가. 물약으로 회복 가능.
///
/// [싱글턴]
/// ShopSystem, BlacksmithSystem, ScholarSystem, EnhancementSystem 에서
/// PlayerStats.Instance.gold 형태로 참조
/// </summary>
public class PlayerStats : MonoBehaviour
{
    // ─────────────────────── 싱글턴 ───────────────────────

    public static PlayerStats Instance { get; private set; }

    // ─────────────────────── Inspector 필드 ───────────────────────

    [Header("기본 스탯 (0~100)")]
    [SerializeField] [Range(0, 100)] private float health      = 100f;
    [SerializeField] [Range(0, 100)] private float baseDefense = 0f;
    [SerializeField] [Range(0, 100)] private float mental      = 100f;

    [Header("장비 방어력 (착용 시 추가)")]
    [SerializeField] private float equipmentDefense = 0f;

    [Header("골드")]
    [SerializeField] private int _gold = 0;

    [Header("물약 회복량")]
    [SerializeField] private float healthPotionAmount = 30f;
    [SerializeField] private float mentalPotionAmount = 25f;

    // ─────────────────────── 상수 ───────────────────────

    private const float MIN_STAT = 0f;
    private const float MAX_STAT = 100f;

    // ─────────────────────── 계산 프로퍼티 ───────────────────────

    /// <summary>정신력에 따른 능력치 배율 (0~1)</summary>
    public float MentalMultiplier => mental / MAX_STAT;

    /// <summary>총 방어력 = 기본 + 장비</summary>
    public float TotalDefense => baseDefense + equipmentDefense;

    /// <summary>정신력이 반영된 실제 방어력</summary>
    public float EffectiveDefense => TotalDefense * MentalMultiplier;

    // ─────────────────────── 이벤트 ───────────────────────

    public event Action<float> OnHealthChanged;
    public event Action<float> OnDefenseChanged;
    public event Action<float> OnMentalChanged;
    public event Action<int>   OnGoldChanged;
    public event Action        OnStatsChanged;

    // ─────────────────────── 공개 프로퍼티 ───────────────────────

    public float Health           => health;
    public float BaseDefense      => baseDefense;
    public float EquipmentDefense => equipmentDefense;
    public float Defense          => TotalDefense;
    public float Mental           => mental;

    /// <summary>
    /// 골드 — ShopSystem, BlacksmithSystem, ScholarSystem, EnhancementSystem 에서
    /// PlayerStats.Instance.gold 로 참조하므로 camelCase 프로퍼티 제공
    /// </summary>
    public int gold
    {
        get => _gold;
        set
        {
            _gold = Mathf.Max(0, value);
            OnGoldChanged?.Invoke(_gold);
        }
    }

    // ─────────────────────── 초기화 ───────────────────────

    private void Awake()
    {
        // 싱글턴 — 플레이어는 씬에 1개만 존재
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
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 체력 변경
    /// </summary>
    public void ModifyHealth(float amount)
    {
        health = Mathf.Clamp(health + amount, MIN_STAT, MAX_STAT);
        OnHealthChanged?.Invoke(health);
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 기본 방어력 변경 (0~100)
    /// </summary>
    public void ModifyBaseDefense(float amount)
    {
        baseDefense = Mathf.Clamp(baseDefense + amount, MIN_STAT, MAX_STAT);
        OnDefenseChanged?.Invoke(TotalDefense);
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 장비 방어력 추가 (착용 시 호출)
    /// </summary>
    public void AddEquipmentDefense(float amount)
    {
        equipmentDefense += amount;
        if (equipmentDefense < 0f) equipmentDefense = 0f;
        OnDefenseChanged?.Invoke(TotalDefense);
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 장비 방어력 제거 (해제 시 호출)
    /// </summary>
    public void RemoveEquipmentDefense(float amount)
    {
        equipmentDefense -= amount;
        if (equipmentDefense < 0f) equipmentDefense = 0f;
        OnDefenseChanged?.Invoke(TotalDefense);
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 장비 방어력 직접 설정 (장비 시스템에서 총량 관리 시)
    /// </summary>
    public void SetEquipmentDefense(float value)
    {
        equipmentDefense = Mathf.Max(0f, value);
        OnDefenseChanged?.Invoke(TotalDefense);
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 정신력 변경 - 하락 시 능력치 배율 감소
    /// </summary>
    public void ModifyMental(float amount)
    {
        mental = Mathf.Clamp(mental + amount, MIN_STAT, MAX_STAT);
        OnMentalChanged?.Invoke(mental);
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 체력 물약 사용
    /// </summary>
    public bool UseHealthPotion()
    {
        if (health >= MAX_STAT) return false;

        ModifyHealth(healthPotionAmount);
        return true;
    }

    /// <summary>
    /// 정신력 물약 사용 - 정신력 회복으로 능력치 상승
    /// </summary>
    public bool UseMentalPotion()
    {
        if (mental >= MAX_STAT) return false;

        ModifyMental(mentalPotionAmount);
        return true;
    }

    /// <summary>
    /// 맞은 데미지 계산 - AD(받는 피해), DD(데미지 감소). 정신력이 낮으면 불리 적용
    /// 0~100: 1당 DD 0.25% 증가 (100→DD 25%, AD 75%)
    /// 100~200: 1당 DD 0.125% 증가 (200→DD 37.5%, AD 62.5%)
    /// 200~300: 1당 DD 0.0625% 증가 (300→DD 43.75%, AD 56.25%)
    /// 300~400: 1당 DD 0.03125% 증가 (400→DD 46.875%, AD 53.125%)
    /// 400~500: 1당 DD 0.015625% 증가 (500→DD 48.4375%, AD 51.5625%)
    /// </summary>
    public float TakeDamage(float rawDamage)
    {
        float def = EffectiveDefense;
        float dd; // 데미지 감소 %
        if (def <= 100f)
            dd = def * 0.25f; // 0~100: 1당 DD 0.25% 증가
        else if (def <= 200f)
            dd = 25f + (def - 100f) * 0.125f; // 100~200: 1당 DD 0.125% 증가
        else if (def <= 300f)
            dd = 37.5f + (def - 200f) * 0.0625f; // 200~300: 1당 DD 0.0625% 증가
        else if (def <= 400f)
            dd = 43.75f + (def - 300f) * 0.03125f; // 300~400: 1당 DD 0.03125% 증가
        else
            dd = 46.875f + (def - 400f) * 0.015625f; // 400~500: 1당 DD 0.015625% 증가
        dd = Mathf.Clamp(dd, 0f, 95f); // 0~95% (최소 5% 데미지)
        float damageRatio = 1f - (dd / 100f); // AD = 100% - DD
        float actualDamage = rawDamage * Mathf.Max(damageRatio, 0.05f);
        actualDamage *= (2f - MentalMultiplier);
        ModifyHealth(-actualDamage);
        return actualDamage;
    }

    /// <summary>
    /// 정신력 소모 (스킬 사용 등)
    /// </summary>
    public void ConsumeMental(float amount)
    {
        ModifyMental(-amount);
    }

    private void ClampAllStats()
    {
        health = Mathf.Clamp(health, MIN_STAT, MAX_STAT);
        baseDefense = Mathf.Clamp(baseDefense, MIN_STAT, MAX_STAT);
        if (equipmentDefense < 0f) equipmentDefense = 0f;
        mental = Mathf.Clamp(mental, MIN_STAT, MAX_STAT);
    }

#if UNITY_EDITOR
    [ContextMenu("테스트: 체력 물약")]
    private void TestHealthPotion() => UseHealthPotion();

    [ContextMenu("테스트: 정신력 물약")]
    private void TestMentalPotion() => UseMentalPotion();
#endif
}
