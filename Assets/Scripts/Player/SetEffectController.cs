using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 세트 특수 효과 (확률/지속/범위형) 처리
///
/// [기획 반영]
/// - 불   3세트: 적중 시 확률로 [점화] (초당 피해, 적용 후 쿨타임)
///        4세트: [점화] 상태의 적 공격 시 범위 폭발 (플레이어 공격력 비례)
/// - 물   2세트: 적중한 적 이동 속도 감소
///        3세트: 적중 시 일정 시간 초당 최대 체력 % 회복 (적용 후 쿨타임)
///        4세트: 체력 기준 이상이면 공격력 증가, 미만이면 3세트 회복량 배율
/// - 바람 4세트: 기본 공격 N회마다 공격 방향으로 회오리
/// - 땅   3세트: 일정 주기마다 최대 체력 비례 보호막 갱신
///        4세트: 보호막 유지 중 공격력 증가, 보호막 갱신 시 주변 적 기절 (보스 제외)
/// - 어둠 1세트: 처치/보스 적중 시 [영혼] 스택 (스택당 공격력), 비전투 시 소멸
///        3세트: 체력 기준 미만이면 공격력 증가
///        4세트: 전투 중 사망 시 [영혼] 소모해 부활 + 일정 시간 흡혈 (쿨타임)
/// - 고정 수치 보너스(공격력/이동속도 등)는 SetEffectData 에셋에서 ArmorSetManager 가 처리
///
/// [사용법]
/// - Player 오브젝트에 부착 (ArmorSetManager/PlayerCombat/PlayerDeath 가 자동 탐색)
/// </summary>
public class SetEffectController : MonoBehaviour
{
    [Header("공통")]
    [Tooltip("범위 효과가 감지할 적 레이어")]
    [SerializeField] private LayerMask enemyLayer = ~0;
    [Tooltip("마지막 공격/피격 후 이 시간(초)이 지나면 비전투")]
    [SerializeField] private float combatTimeout = 5f;

    [Header("불 3세트 [점화]")]
    [SerializeField] [Range(0f, 1f)] private float igniteChance = 0.25f;
    [SerializeField] private float igniteDuration = 3f;
    [SerializeField] private float igniteDamagePerSecond = 10f;
    [Tooltip("점화 적용 시점부터 다음 점화까지 쿨타임 (초)")]
    [SerializeField] private float igniteCooldown = 3f;
    [Tooltip("점화 중 적에게 붙는 타오르는 이펙트")]
    [SerializeField] private GameObject prefabIgniteVFX;

    [Header("불 4세트 폭발")]
    [SerializeField] private float explosionRadius = 3f;
    [Tooltip("플레이어 공격력 대비 폭발 피해 비율 (0.13 = 13%)")]
    [SerializeField] private float explosionDamageRatio = 0.13f;
    [SerializeField] private GameObject prefabExplosionVFX;

    [Header("물 2세트 둔화")]
    [SerializeField] [Range(0f, 1f)] private float waterSlowPercent = 0.05f;
    [SerializeField] private float waterSlowDuration = 2f;

    [Header("물 3세트 회복")]
    [SerializeField] private float regenDuration = 3f;
    [Tooltip("초당 최대 체력 대비 회복 % (3 = 3%)")]
    [SerializeField] private float regenPercentPerSecond = 3f;
    [Tooltip("회복 적용 시점부터 다음 회복까지 쿨타임 (초)")]
    [SerializeField] private float regenCooldown = 5f;

    [Header("물 4세트")]
    [SerializeField] [Range(0f, 1f)] private float waterHealthThreshold = 0.5f;
    [SerializeField] private float waterHighHealthAttackBonus = 0.15f;
    [SerializeField] private float waterLowHealthRegenMultiplier = 2f;

    [Header("바람 4세트 회오리")]
    [SerializeField] private int tornadoAttackCount = 3;
    [SerializeField] private float tornadoDamage = 10f;
    [Tooltip("좌우 범위 (중심에서 한쪽 폭)")]
    [SerializeField] private float tornadoHalfWidth = 1f;
    [Tooltip("앞 거리")]
    [SerializeField] private float tornadoDistance = 8f;
    [SerializeField] private float tornadoSpeed = 12f;
    [SerializeField] private GameObject prefabTornadoVFX;

    [Header("땅 3세트 보호막")]
    [Tooltip("최대 체력 대비 보호막 % (7 = 7%)")]
    [SerializeField] private float shieldPercent = 7f;
    [SerializeField] private float shieldInterval = 15f;
    [Tooltip("보호막 유지 중 플레이어에 붙는 이펙트")]
    [SerializeField] private GameObject prefabShieldVFX;

    [Header("땅 4세트")]
    [SerializeField] private float shieldAttackBonus = 0.05f;
    [SerializeField] private float stunRadius = 4f;
    [SerializeField] private float stunDuration = 1.5f;

    [Header("어둠 1세트 [영혼]")]
    [SerializeField] private float soulAttackPerStack = 0.03f;
    [SerializeField] private int soulMaxStacks = 10;
    [Tooltip("비전투 시 스택 1개 소멸 간격 (초)")]
    [SerializeField] private float soulDecayInterval = 1f;

    [Header("어둠 3세트")]
    [SerializeField] [Range(0f, 1f)] private float darkHealthThreshold = 0.5f;
    [SerializeField] private float darkLowHealthAttackBonus = 0.09f;

    [Header("어둠 4세트 부활")]
    [Tooltip("영혼 스택당 최대 체력 회복 % (5 = 5%)")]
    [SerializeField] private float reviveHealPercentPerStack = 5f;
    [SerializeField] private float reviveLifeSteal = 0.04f;
    [SerializeField] private float reviveLifeStealDuration = 5f;
    [SerializeField] private float reviveCooldown = 90f;

    // ─────────────────────── 런타임 ───────────────────────

    private PlayerStats _stats;
    private PlayerCombat _combat;

    private readonly Dictionary<RuneElement, SetTier> _tiers = new Dictionary<RuneElement, SetTier>();

    private float _fIgniteCooldownTimer = 0f;
    private float _fRegenTimer = 0f;
    private float _fRegenCooldownTimer = 0f;
    private int _nAttackCount = 0;
    private float _fShieldTimer = 0f;
    private bool _bEarthShieldActive = false;
    private GameObject _objShieldVFX;
    private int _nSoulStacks = 0;
    private float _fSoulDecayTimer = 0f;
    private float _fReviveCooldownTimer = 0f;
    private float _fLifeStealTimer = 0f;
    private bool _bReviving = false;

    /// <summary>[영혼] 스택 변경 (UI 표시용, 현재 스택 전달)</summary>
    public event Action<int> OnSoulStacksChanged;

    public int SoulStacks => _nSoulStacks;
    public int SoulMaxStacks => soulMaxStacks;

    private bool IsInCombat => _stats != null && Time.time - _stats.LastCombatTime < combatTimeout;

    private float HealthRatio => _stats != null && _stats.MaxHealth > 0f ? _stats.Health / _stats.MaxHealth : 1f;

    /// <summary>조건부 공격력 보너스 합계 (PlayerCombat 데미지 계산에서 매번 조회)</summary>
    public float DynamicAttackBonus
    {
        get
        {
            float fBonus = 0f;
            if (GetCount(RuneElement.Water) >= 4 && HealthRatio >= waterHealthThreshold)
            {
                fBonus += waterHighHealthAttackBonus;
            }
            if (GetCount(RuneElement.Earth) >= 4 && _stats != null && _stats.Shield > 0f)
            {
                fBonus += shieldAttackBonus;
            }
            if (GetCount(RuneElement.Darkness) >= 1)
            {
                fBonus += _nSoulStacks * soulAttackPerStack;
            }
            if (GetCount(RuneElement.Darkness) >= 3 && HealthRatio < darkHealthThreshold)
            {
                fBonus += darkLowHealthAttackBonus;
            }
            return fBonus;
        }
    }

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
        _combat = GetComponent<PlayerCombat>();
    }

    // 원소 활성 단계를 착용 수(1~4)로 반환
    private int GetCount(RuneElement _element)
    {
        SetTier tier;
        if (_tiers.TryGetValue(_element, out tier) == false)
        {
            return 0;
        }
        return (int)tier;
    }

    /// <summary>세트 단계 갱신 (ArmorSetManager 재계산 시 호출)</summary>
    public void ApplyTiers(Dictionary<RuneElement, SetTier> _activeTiers)
    {
        // 위험: 로드 순서상 Awake 보다 먼저 호출될 수 있어 참조 보장
        if (_stats == null)
        {
            _stats = GetComponent<PlayerStats>();
        }
        _tiers.Clear();
        foreach (KeyValuePair<RuneElement, SetTier> kv in _activeTiers)
        {
            _tiers[kv.Key] = kv.Value;
        }

        // 땅 3세트 보호막: 새로 맞췄을 때만 부여 (장비 교체로 재충전 방지), 해제 시 제거
        bool bEarth = GetCount(RuneElement.Earth) >= 3;
        if (_stats != null && bEarth != _bEarthShieldActive)
        {
            _stats.SetShieldValue(bEarth == true ? _stats.MaxHealth * shieldPercent / 100f : 0f);
            _fShieldTimer = shieldInterval;
        }
        _bEarthShieldActive = bEarth;

        // 어둠 세트 해제 시 영혼 초기화
        if (GetCount(RuneElement.Darkness) < 1)
        {
            SetSoulStacks(0);
        }
        if (GetCount(RuneElement.Wind) < 4)
        {
            _nAttackCount = 0;
        }
    }

    private void Update()
    {
        float fDt = Time.deltaTime;

        if (_fIgniteCooldownTimer > 0f)
        {
            _fIgniteCooldownTimer -= fDt;
        }
        if (_fRegenCooldownTimer > 0f)
        {
            _fRegenCooldownTimer -= fDt;
        }
        if (_fReviveCooldownTimer > 0f)
        {
            _fReviveCooldownTimer -= fDt;
        }
        if (_fLifeStealTimer > 0f)
        {
            _fLifeStealTimer -= fDt;
        }

        UpdateRegen(fDt);
        UpdateShield(fDt);
        UpdateSoulDecay(fDt);
    }

    // ─────────────────────── 외부 호출 (PlayerCombat) ───────────────────────

    /// <summary>기본 공격 실행 시 호출 (바람 4세트 회오리 카운트)</summary>
    public void OnBasicAttack(Vector3 _vDirection)
    {
        if (GetCount(RuneElement.Wind) < 4)
        {
            return;
        }

        _nAttackCount++;
        if (_nAttackCount < tornadoAttackCount)
        {
            return;
        }
        _nAttackCount = 0;

        _vDirection.y = 0f;
        if (_vDirection.sqrMagnitude < 0.001f)
        {
            _vDirection = transform.forward;
        }
        StartCoroutine(TornadoRoutine(_vDirection.normalized));
    }

    /// <summary>기본 공격으로 적에게 피해를 준 직후 호출</summary>
    public void OnEnemyHit(EnemyBase _enemy, float _fDamage)
    {
        if (_enemy == null)
        {
            return;
        }

        bool bKilled = _enemy.IsDead;
        bool bBoss = _enemy is BossHead;

        // 어둠 1세트: 처치 또는 보스 적중 시 영혼
        if (GetCount(RuneElement.Darkness) >= 1 && (bKilled == true || bBoss == true))
        {
            SetSoulStacks(_nSoulStacks + 1);
        }

        // 어둠 4세트 부활 후 흡혈
        if (_fLifeStealTimer > 0f && _stats != null)
        {
            _stats.ModifyHealth(_fDamage * reviveLifeSteal);
        }

        // 물 3세트: 적중 시 회복 시작 (적용 후 쿨타임)
        if (GetCount(RuneElement.Water) >= 3 && _fRegenCooldownTimer <= 0f)
        {
            _fRegenTimer = regenDuration;
            _fRegenCooldownTimer = regenCooldown;
        }

        if (bKilled == true)
        {
            return;
        }

        EnemyStatusEffect status = EnemyStatusEffect.GetOrAdd(_enemy);

        // 불 4세트: 이미 점화된 적이면 폭발 (이번 적중으로 막 점화된 경우는 제외하려고 점화보다 먼저 판정)
        if (GetCount(RuneElement.Fire) >= 4 && status.IsIgnited == true)
        {
            Explode(_enemy);
        }

        // 불 3세트: 확률 점화 (적용 후 쿨타임)
        if (GetCount(RuneElement.Fire) >= 3 && _fIgniteCooldownTimer <= 0f && UnityEngine.Random.value < igniteChance)
        {
            status.ApplyIgnite(igniteDuration, igniteDamagePerSecond, prefabIgniteVFX, OnKillBySetEffect);
            _fIgniteCooldownTimer = igniteCooldown;
        }

        // 물 2세트: 이동 속도 감소
        if (GetCount(RuneElement.Water) >= 2)
        {
            status.ApplySlow(waterSlowPercent, waterSlowDuration);
        }
    }

    // ─────────────────────── 외부 호출 (PlayerDeath) ───────────────────────

    /// <summary>어둠 4세트 부활 시도. 성공하면 true (사망 처리 생략)</summary>
    public bool TryRevive()
    {
        if (_bReviving == true)
        {
            return true;
        }
        if (GetCount(RuneElement.Darkness) < 4 || _nSoulStacks <= 0 || _fReviveCooldownTimer > 0f)
        {
            return false;
        }
        if (IsInCombat == false)
        {
            return false;
        }

        _bReviving = true;
        StartCoroutine(ReviveRoutine());
        return true;
    }

    // 위험: 체력 변경 이벤트 처리 중에 체력을 다시 바꾸면 HUD 가 0 으로 덮어쓰므로 다음 프레임에 회복
    private IEnumerator ReviveRoutine()
    {
        yield return null;

        float fHeal = _stats.MaxHealth * reviveHealPercentPerStack / 100f * _nSoulStacks;
        SetSoulStacks(0);
        _fReviveCooldownTimer = reviveCooldown;
        _fLifeStealTimer = reviveLifeStealDuration;
        _bReviving = false;
        _stats.ModifyHealth(fHeal);

        Debug.Log($"[SetEffectController] 어둠 4세트 부활 — 회복 {fHeal:F1}");
    }

    // ─────────────────────── 물 ───────────────────────

    private void UpdateRegen(float _fDt)
    {
        if (_fRegenTimer <= 0f || _stats == null)
        {
            return;
        }
        _fRegenTimer -= _fDt;

        // 세트 해제 시 남은 회복 중단
        if (GetCount(RuneElement.Water) < 3)
        {
            _fRegenTimer = 0f;
            return;
        }

        float fMult = 1f;
        if (GetCount(RuneElement.Water) >= 4 && HealthRatio < waterHealthThreshold)
        {
            fMult = waterLowHealthRegenMultiplier;
        }
        _stats.ModifyHealth(_stats.MaxHealth * regenPercentPerSecond / 100f * fMult * _fDt);
    }

    // ─────────────────────── 땅 ───────────────────────

    private void UpdateShield(float _fDt)
    {
        bool bActive = GetCount(RuneElement.Earth) >= 3;

        if (bActive == true && _stats != null)
        {
            _fShieldTimer -= _fDt;
            if (_fShieldTimer <= 0f)
            {
                _fShieldTimer = shieldInterval;
                _stats.SetShieldValue(_stats.MaxHealth * shieldPercent / 100f);

                // 땅 4세트: 보호막 갱신 시 주변 적 기절
                if (GetCount(RuneElement.Earth) >= 4)
                {
                    StunNearby();
                }
            }
        }

        // 보호막 이펙트 표시
        bool bShowVFX = bActive == true && _stats != null && _stats.Shield > 0f;
        if (bShowVFX == true && _objShieldVFX == null && prefabShieldVFX != null)
        {
            _objShieldVFX = Instantiate(prefabShieldVFX, transform.position, Quaternion.identity, transform);
        }
        if (_objShieldVFX != null && _objShieldVFX.activeSelf != bShowVFX)
        {
            _objShieldVFX.SetActive(bShowVFX);
        }
    }

    private void StunNearby()
    {
        foreach (EnemyBase enemy in FindEnemies(transform.position, stunRadius))
        {
            // 보스는 기절 제외
            if (enemy is BossHead)
            {
                continue;
            }
            EnemyStatusEffect.GetOrAdd(enemy).ApplyStun(stunDuration);
        }
    }

    // ─────────────────────── 어둠 ───────────────────────

    private void SetSoulStacks(int _nValue)
    {
        int nClamped = Mathf.Clamp(_nValue, 0, soulMaxStacks);
        if (nClamped == _nSoulStacks)
        {
            return;
        }
        _nSoulStacks = nClamped;
        if (OnSoulStacksChanged != null)
        {
            OnSoulStacksChanged.Invoke(_nSoulStacks);
        }
    }

    private void UpdateSoulDecay(float _fDt)
    {
        if (_nSoulStacks <= 0 || IsInCombat == true)
        {
            _fSoulDecayTimer = soulDecayInterval;
            return;
        }

        _fSoulDecayTimer -= _fDt;
        if (_fSoulDecayTimer <= 0f)
        {
            _fSoulDecayTimer = soulDecayInterval;
            SetSoulStacks(_nSoulStacks - 1);
        }
    }

    // 세트 특수 효과(점화/폭발/회오리)로 적 처치 시 영혼
    private void OnKillBySetEffect()
    {
        if (GetCount(RuneElement.Darkness) >= 1)
        {
            SetSoulStacks(_nSoulStacks + 1);
        }
    }

    // ─────────────────────── 범위 피해 ───────────────────────

    private void Explode(EnemyBase _target)
    {
        Vector3 vCenter = _target.transform.position;
        float fDamage = (_combat != null ? _combat.AttackPower : 0f) * explosionDamageRatio;

        if (prefabExplosionVFX != null)
        {
            Instantiate(prefabExplosionVFX, vCenter, Quaternion.identity);
        }

        foreach (EnemyBase enemy in FindEnemies(vCenter, explosionRadius))
        {
            if (enemy == _target)
            {
                continue;
            }
            DealDamage(enemy, fDamage);
        }
    }

    private IEnumerator TornadoRoutine(Vector3 _vDirection)
    {
        Vector3 vPos = transform.position;
        float fTravelled = 0f;
        HashSet<EnemyBase> hitSet = new HashSet<EnemyBase>();

        // 코루틴이 중간에 끊겨도 남지 않도록 이동 시간 뒤 자동 제거
        GameObject objVFX = null;
        if (prefabTornadoVFX != null)
        {
            objVFX = Instantiate(prefabTornadoVFX, vPos, Quaternion.LookRotation(_vDirection));
            Destroy(objVFX, tornadoDistance / tornadoSpeed);
        }

        while (fTravelled < tornadoDistance)
        {
            float fStep = tornadoSpeed * Time.deltaTime;
            vPos += _vDirection * fStep;
            fTravelled += fStep;

            if (objVFX != null)
            {
                objVFX.transform.position = vPos;
            }

            // 경로 상 적은 1회만 피격
            foreach (EnemyBase enemy in FindEnemies(vPos, tornadoHalfWidth))
            {
                if (hitSet.Add(enemy) == true)
                {
                    DealDamage(enemy, tornadoDamage);
                }
            }
            yield return null;
        }
    }

    private void DealDamage(EnemyBase _enemy, float _fDamage)
    {
        if (_enemy.IsDead == true)
        {
            return;
        }
        _enemy.TakeDamage(_fDamage);
        if (_enemy.IsDead == true)
        {
            OnKillBySetEffect();
        }
    }

    // 반경 내 살아있는 적 목록 (콜라이더 중복 제거)
    private List<EnemyBase> FindEnemies(Vector3 _vCenter, float _fRadius)
    {
        List<EnemyBase> result = new List<EnemyBase>();
        Collider[] hits = Physics.OverlapSphere(_vCenter, _fRadius, enemyLayer);
        foreach (Collider hit in hits)
        {
            EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
            if (enemy == null || enemy.IsDead == true || result.Contains(enemy) == true)
            {
                continue;
            }
            result.Add(enemy);
        }
        return result;
    }
}
