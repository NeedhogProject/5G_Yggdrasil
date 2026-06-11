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
/// - 크로스 세트    : 불2+물2 → 불 Tier2 + 물 Tier2 각각 독립 발동 (원소 간 합산)
/// - 같은 원소의 Tier 는 비중첩 — 단계가 오르면 이전 Tier 효과는 지워지고 현재 Tier 효과로 교체
/// - 수치 보너스 단위: 정신력은 포인트(최대치 가산), 나머지는 %
/// - 흡혈(LifeSteal) → PlayerCombat / 보호막(Shield) → PlayerStats (비전투 15초 후 재충전)
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

        // 2. 원소별 Tier 갱신
        foreach (RuneElement e in System.Enum.GetValues(typeof(RuneElement)))
        {
            if (e == RuneElement.None) continue;

            _activeTiers[e] = SetTierExtensions.FromCount(e, _elementCounts[e]);

            if (_activeTiers[e] != SetTier.None)
                Debug.Log($"[ArmorSetManager] {e} 세트 → {_activeTiers[e]} 활성화 (착용 수: {_elementCounts[e]})");
        }

        // 3. 활성 원소들의 현재 Tier 보너스를 합산해 일괄 적용
        ApplyAllStatBonuses();
    }

    // ─────────────────────── 수치 보너스 (현재 Tier 만, 원소 간 합산) ───────────────────────

    private void ApplyAllStatBonuses()
    {
        float attackPercent      = 0f;
        float defensePercent     = 0f;
        float healthPercent      = 0f;
        float mentalBonus        = 0f;   // 포인트 (최대 정신력 가산)
        float attackSpeedPercent = 0f;
        float moveSpeedPercent   = 0f;
        float lifeStealPercent   = 0f;
        float shieldPercent      = 0f;

        foreach (KeyValuePair<RuneElement, SetTier> kv in _activeTiers)
        {
            if (kv.Value == SetTier.None) continue;
            SetEffectData data = GetData(kv.Key);
            if (data == null) continue;

            // 비중첩 — 현재 Tier 의 보너스만 적용 (이전 Tier 효과는 현재 값으로 교체된 상태)
            foreach (StatBonus bonus in data.GetStatBonuses(kv.Value))
            {
                switch (bonus.bonusType)
                {
                    case StatBonusType.AttackDamage: attackPercent      += bonus.percent; break;
                    case StatBonusType.Defense:      defensePercent     += bonus.percent; break;
                    case StatBonusType.Health:       healthPercent      += bonus.percent; break;
                    case StatBonusType.Mental:       mentalBonus        += bonus.percent; break;
                    case StatBonusType.AttackSpeed:  attackSpeedPercent += bonus.percent; break;
                    case StatBonusType.MoveSpeed:    moveSpeedPercent   += bonus.percent; break;
                    case StatBonusType.LifeSteal:    lifeStealPercent   += bonus.percent; break;
                    case StatBonusType.Shield:       shieldPercent      += bonus.percent; break;
                }
            }
        }

        // PlayerStats: 체력 % / 방어 % / 정신력 포인트 / 보호막 %
        if (playerStats != null)
            playerStats.SetSetBonusPercents(healthPercent, defensePercent, mentalBonus, shieldPercent);

        // PlayerCombat: 공격력 / 공격속도 / 흡혈 (비율로 전달)
        if (playerCombat != null)
        {
            playerCombat.SetAttackDamageBonus(attackPercent / 100f);
            playerCombat.SetAttackSpeedBonus(attackSpeedPercent / 100f);
            playerCombat.SetLifeStealPercent(lifeStealPercent / 100f);
        }

        // PlayerController: 이동속도 (비율로 전달)
        if (playerController != null)
            playerController.SetMoveSpeedBonus(moveSpeedPercent / 100f);
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
}
