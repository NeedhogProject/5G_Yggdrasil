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
/// - 세트 효과는 중첩 적용 (예: 불 4세트면 2/3/4 효과 모두)
/// - 수치 보너스는 % 배율 — 계산 순서: 기본 → 장비 → 세트
/// - Tier2 이상 수치 보너스 → 착용 즉시 PlayerStats/PlayerCombat/PlayerController 적용
/// - Tier3/4 특수 효과 → 쿨다운 있는 발동형
///
/// [사용법]
/// - PlayerEquipment 에서 장착/해제 시 OnArmorEquipped / OnArmorUnequipped 호출
/// - setEffectDatabase 에 원소별 SetEffectData 에셋 등록 (5개)
/// - Player 오브젝트에 부착 (PlayerStats/PlayerCombat/PlayerController 자동 탐색)
/// </summary>
public class ArmorSetManager : MonoBehaviour
{
    // ─────────────────────── 참조 ───────────────────────

    [Header("참조")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private PlayerController playerController;

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
        if (playerCombat == null)
            playerCombat = GetComponent<PlayerCombat>();
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

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

    /// <summary>각인 변경 시(각인술사) 재계산용 — 착용 중 방어구의 각인이 바뀌면 호출</summary>
    public void RefreshSetEffects()
    {
        RecalculateSetEffects();
    }

    // ─────────────────────── 세트 효과 재계산 ───────────────────────

    private void RecalculateSetEffects()
    {
        // 1. 원소별 카운트 재집계
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

        // 2. 원소별 Tier 갱신 + 특수 효과 쿨다운 등록
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

            // Tier3 이상 새로 활성화 시 쿨다운 등록
            if (newTier >= SetTier.Tier3 && oldTier < SetTier.Tier3)
                _cooldownTimers[e] = 0f;
            else if (newTier < SetTier.Tier3)
                _cooldownTimers.Remove(e);

            Debug.Log($"[ArmorSetManager] {e} 세트 → {newTier} 활성화 (착용 수: {_elementCounts[e]})");
        }

        // 3. 활성 Tier 전체의 수치 보너스를 합산해 일괄 적용 (제거/적용 쌍 없이 오차 누적 방지)
        ApplyAllStatBonuses();
    }

    // ─────────────────────── 수치 보너스 (합산 일괄 적용) ───────────────────────

    private void ApplyAllStatBonuses()
    {
        float attackPercent      = 0f;
        float defensePercent     = 0f;
        float healthPercent      = 0f;
        float mentalPercent      = 0f;
        float attackSpeedPercent = 0f;
        float moveSpeedPercent   = 0f;

        foreach (KeyValuePair<RuneElement, SetTier> kv in _activeTiers)
        {
            if (kv.Value == SetTier.None) continue;
            SetEffectData data = GetData(kv.Key);
            if (data == null) continue;

            // 기획: 세트 효과 중첩 적용 — Tier1 부터 현재 Tier 까지 전부 합산
            for (SetTier t = SetTier.Tier1; t <= kv.Value; t++)
            {
                foreach (StatBonus bonus in data.GetStatBonuses(t))
                {
                    switch (bonus.bonusType)
                    {
                        case StatBonusType.AttackDamage: attackPercent      += bonus.percent; break;
                        case StatBonusType.Defense:      defensePercent     += bonus.percent; break;
                        case StatBonusType.Health:       healthPercent      += bonus.percent; break;
                        case StatBonusType.Mental:       mentalPercent      += bonus.percent; break;
                        case StatBonusType.AttackSpeed:  attackSpeedPercent += bonus.percent; break;
                        case StatBonusType.MoveSpeed:    moveSpeedPercent   += bonus.percent; break;
                    }
                }
            }
        }

        // PlayerStats: 최대 체력 / 방어력 / 정신력 % 배율
        if (playerStats != null)
            playerStats.SetSetBonusPercents(healthPercent, defensePercent, mentalPercent);

        // PlayerCombat: 공격력 / 공격속도 (비율로 전달)
        if (playerCombat != null)
        {
            playerCombat.SetAttackDamageBonus(attackPercent / 100f);
            playerCombat.SetAttackSpeedBonus(attackSpeedPercent / 100f);
        }

        // PlayerController: 이동속도 (비율로 전달)
        if (playerController != null)
            playerController.SetMoveSpeedBonus(moveSpeedPercent / 100f);
    }

    // ─────────────────────── 특수 효과 쿨다운 ───────────────────────

    private void TickCooldowns()
    {
        List<RuneElement> keys = _cooldownTimers.Keys.ToList();
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
                float healAmount = playerStats.MaxHealth * (effect.valuePercent / 100f);
                playerStats.ModifyHealth(healAmount);
                Debug.Log($"[SetEffect] 불4세트 체력 회복 +{healAmount:F1}");
                break;

            // ── 물 4세트: 피격 시 최대 체력 5% 방어막 ──
            case SpecialEffectType.ShieldOnHit:
                float shieldAmount = playerStats.MaxHealth * (effect.valuePercent / 100f);
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
