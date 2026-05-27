using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 단일 원소 세트 효과 관리 시스템
///
/// [기획 반영]
/// - 원소별로 착용 수를 집계해 SetTier 결정
/// - 불/물/바람/땅 : 2개부터 Tier2 발동
/// - 어둠           : 1개부터 Tier1 발동
/// - 크로스 세트    : 불2+물2 → 불 Tier2 + 물 Tier2 각각 독립 발동
/// - Tier2 이상 수치 보너스 → 착용 즉시 PlayerStats 적용
/// - Tier3/4 특수 효과 → 쿨다운 있는 발동형
///
/// [사용법]
/// - PlayerEquipment 에서 장착/해제 시 OnArmorEquipped / OnArmorUnequipped 호출
/// - setEffectDatabase 에 원소별 SetEffectData 에셋 등록 (5개)
/// </summary>
public class ArmorSetManager : MonoBehaviour
{
    // ─────────────────────── 참조 ───────────────────────

    [Header("참조")]
    [SerializeField] private PlayerStats playerStats;

    [Header("세트 효과 데이터베이스 (원소별 SetEffectData 5개 등록)")]
    [SerializeField] private List<SetEffectData> setEffectDatabase = new List<SetEffectData>();

    // ─────────────────────── 런타임 상태 ───────────────────────

    /// <summary>착용 중인 방어구 인스턴스 목록</summary>
    private readonly List<ArmorInstance> _equippedArmors = new List<ArmorInstance>();

    /// <summary>원소별 현재 착용 카운트</summary>
    private readonly Dictionary<RuneElement, int> _elementCounts
        = new Dictionary<RuneElement, int>();

    /// <summary>원소별 현재 활성 Tier</summary>
    private readonly Dictionary<RuneElement, SetTier> _activeTiers
        = new Dictionary<RuneElement, SetTier>();

    /// <summary>원소별 특수 효과 쿨다운 타이머 (원소 → 남은 시간)</summary>
    private readonly Dictionary<RuneElement, float> _cooldownTimers
        = new Dictionary<RuneElement, float>();

    // ─────────────────────── 초기화 ───────────────────────

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        // 모든 원소 카운트 0으로 초기화
        foreach (RuneElement e in System.Enum.GetValues(typeof(RuneElement)))
        {
            if (e == RuneElement.None) continue;
            _elementCounts[e] = 0;
            _activeTiers[e]   = SetTier.None;
        }
    }

    private void Update()
    {
        TickCooldowns();
    }

    // ─────────────────────── 외부 호출 (PlayerEquipment) ───────────────────────

    /// <summary>방어구 장착 시 PlayerEquipment 에서 호출</summary>
    public void OnArmorEquipped(ArmorInstance armor)
    {
        if (armor == null) return;
        if (_equippedArmors.Contains(armor)) return;

        _equippedArmors.Add(armor);
        RecalculateSetEffects();
    }

    /// <summary>방어구 해제 시 PlayerEquipment 에서 호출</summary>
    public void OnArmorUnequipped(ArmorInstance armor)
    {
        if (armor == null) return;
        if (_equippedArmors.Remove(armor) == false) return;

        RecalculateSetEffects();
    }

    // ─────────────────────── 세트 효과 재계산 ───────────────────────

    private void RecalculateSetEffects()
    {
        // 1. 이전 수치 보너스 전부 제거
        RemoveAllStatBonuses();

        // 2. 원소별 카운트 재집계
        foreach (RuneElement e in System.Enum.GetValues(typeof(RuneElement)))
        {
            if (e == RuneElement.None) continue;
            _elementCounts[e] = 0;
        }

        foreach (ArmorInstance armor in _equippedArmors)
        {
            foreach (RuneElement elem in armor.GetContributingElements())
            {
                if (_elementCounts.ContainsKey(elem))
                    _elementCounts[elem]++;
            }
        }

        // 3. 원소별 Tier 갱신 + 수치 보너스 즉시 적용
        foreach (RuneElement e in System.Enum.GetValues(typeof(RuneElement)))
        {
            if (e == RuneElement.None) continue;

            SetTier newTier = SetTierExtensions.FromCount(e, _elementCounts[e]);
            SetTier oldTier = _activeTiers[e];
            _activeTiers[e] = newTier;

            if (newTier == SetTier.None)
            {
                _cooldownTimers.Remove(e);
                continue;
            }

            // 수치 보너스 적용
            ApplyStatBonuses(e, newTier);

            // Tier3 이상 새로 활성화 시 쿨다운 등록
            if (newTier >= SetTier.Tier3 && oldTier < SetTier.Tier3)
                _cooldownTimers[e] = 0f;
            else if (newTier < SetTier.Tier3)
                _cooldownTimers.Remove(e);

            if (newTier != SetTier.None)
                Debug.Log($"[ArmorSetManager] {e} 세트 → {newTier} 활성화 (착용 수: {_elementCounts[e]})");
        }
    }

    // ─────────────────────── 수치 보너스 ───────────────────────

    private void ApplyStatBonuses(RuneElement element, SetTier tier)
    {
        SetEffectData data = GetData(element);
        if (data == null) return;

        foreach (StatBonus bonus in data.GetStatBonuses(tier))
            ApplySingleBonus(bonus, add: true);
    }

    private void RemoveAllStatBonuses()
    {
        foreach (System.Collections.Generic.KeyValuePair<RuneElement, SetTier> kv in _activeTiers)
        {
            if (kv.Value == SetTier.None) continue;
            SetEffectData data = GetData(kv.Key);
            if (data == null) continue;

            foreach (StatBonus bonus in data.GetStatBonuses(kv.Value))
                ApplySingleBonus(bonus, add: false);
        }
    }

    private void ApplySingleBonus(StatBonus bonus, bool add)
    {
        if (playerStats == null || bonus.bonusType == StatBonusType.None) return;
        float sign = add ? 1f : -1f;

        switch (bonus.bonusType)
        {
            case StatBonusType.Defense:
                playerStats.ModifyBaseDefense(bonus.percent * sign);
                break;
            case StatBonusType.Health:
                playerStats.ModifyHealth(bonus.percent * sign);
                break;
            case StatBonusType.Mental:
                playerStats.ModifyMental(bonus.percent * sign);
                break;
            // AttackDamage / AttackSpeed / MoveSpeed → PlayerCombat / PlayerController 연동 후 적용
            case StatBonusType.AttackDamage:
            case StatBonusType.AttackSpeed:
            case StatBonusType.MoveSpeed:
                Debug.Log($"[ArmorSetManager] {bonus.bonusType} {bonus.percent * sign:+0.#;-0.#}%" +
                          " → PlayerCombat/PlayerController 연동 후 적용 예정");
                break;
        }
    }

    // ─────────────────────── 특수 효과 쿨다운 ───────────────────────

    private void TickCooldowns()
    {
        System.Collections.Generic.List<RuneElement> keys = _cooldownTimers.Keys.ToList();
        foreach (RuneElement elem in keys)
        {
            _cooldownTimers[elem] -= Time.deltaTime;
            if (_cooldownTimers[elem] <= 0f)
            {
                TriggerSpecialEffect(elem);
                // 쿨다운 재시작
                float cd = GetCooldown(elem);
                _cooldownTimers[elem] = cd > 0f ? cd : 5f;
            }
        }
    }

    private void TriggerSpecialEffect(RuneElement elem)
    {
        if (_activeTiers.TryGetValue(elem, out SetTier tier) == false) return;
        SetEffectData data = GetData(elem);
        if (data == null) return;

        foreach (SpecialEffect effect in data.GetSpecialEffects(tier))
            ApplySpecialEffect(effect);
    }

    private void ApplySpecialEffect(SpecialEffect effect)
    {
        if (playerStats == null) return;

        switch (effect.effectType)
        {
            // ── 불 4세트: 공격 시 최대 체력 2% 회복 ──
            case SpecialEffectType.LifeStealOnHit:
                float healAmount = 100f * (effect.valuePercent / 100f); // 최대 체력 100 기준
                playerStats.ModifyHealth(healAmount);
                Debug.Log($"[SetEffect] 불4세트 체력 회복 +{healAmount:F1}");
                break;

            // ── 물 4세트: 피격 시 최대 체력 5% 방어막 ──
            case SpecialEffectType.ShieldOnHit:
                float shieldAmount = 100f * (effect.valuePercent / 100f);
                playerStats.ModifyBaseDefense(shieldAmount);
                Debug.Log($"[SetEffect] 물4세트 방어막 +{shieldAmount:F1}");
                break;

            // ── 원소 데미지 (불/물/바람/어둠 3·4세트): PlayerCombat 연동 후 적용 ──
            case SpecialEffectType.ElementalDamageOnHit:
            case SpecialEffectType.ElementalDamageFromDefense:
                Debug.Log($"[SetEffect] {effect.element} 원소 데미지 {effect.valuePercent}%" +
                          " → PlayerCombat 연동 후 적용 예정");
                break;
        }
    }

    private float GetCooldown(RuneElement elem)
    {
        SpecialEffect[] effects = GetData(elem)?.GetSpecialEffects(_activeTiers[elem]);
        if (effects == null || effects.Length == 0) return 5f;
        return effects[0].cooldown;
    }

    // ─────────────────────── 유틸 ───────────────────────

    private SetEffectData GetData(RuneElement elem) =>
        setEffectDatabase.FirstOrDefault(d => d != null && d.Element == elem);

    /// <summary>현재 원소의 활성 Tier 반환 (UI 표시용)</summary>
    public SetTier GetActiveTier(RuneElement elem) =>
        _activeTiers.TryGetValue(elem, out SetTier t) ? t : SetTier.None;

    /// <summary>현재 원소의 착용 카운트 반환 (UI 표시용)</summary>
    public int GetElementCount(RuneElement elem) =>
        _elementCounts.TryGetValue(elem, out int c) ? c : 0;

    /// <summary>특수 효과 쿨다운 남은 시간 반환 (UI 표시용)</summary>
    public float GetCooldownRemaining(RuneElement elem) =>
        _cooldownTimers.TryGetValue(elem, out float t) ? Mathf.Max(0f, t) : 0f;
}