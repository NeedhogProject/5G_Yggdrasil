using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 세트 특수 효과 (확률/지속/범위형) 처리
///
/// [기획 반영]
/// - 불   3세트: 적중 시 확률로 [점화] (초당 피해, 적용 후 쿨타임)
///        4세트: [점화] 상태의 적 공격 시 범위 폭발 (플레이어 공격력 비례, 공격 1회당 1번, 단검은 첫 타격만 판정)
/// - 물   2세트: 적중한 적 이동 속도 감소
///        3세트: 적중 시 일정 시간 초당 최대 체력 % 회복 (적용 후 쿨타임)
///        4세트: 체력 기준 이상이면 공격력 증가, 미만이면 3세트 회복량 배율
/// - 바람 4세트: 기본 공격 N회마다 공격 방향으로 회오리
/// - 땅   3세트: 일정 주기마다 최대 체력 비례 보호막 갱신 (세트를 해제해도 주기 유지)
///        4세트: 보호막 유지 중 공격력 증가, 보호막 갱신 시 주변 적 기절 (보스 제외)
/// - 어둠 1세트: 처치/보스 적중 시 [영혼] 스택 (스택당 공격력, 보스 적중은 공격 1회당 1스택), 비전투 시 소멸
///        3세트: 체력 기준 미만이면 공격력 증가
///        4세트: 전투 중 사망 시 [영혼] 소모해 부활 + 일정 시간 흡혈 (쿨타임)
/// - 고정 수치 보너스(공격력/이동속도 등)는 Tables/SetBonus.xlsx 를 ArmorSetManager 가 처리
/// - 아래 수치는 Tables/SetEffect.xlsx 에서 관리 (엑셀은 % 단위, Awake 에서 비율로 변환)
///
/// [사용법]
/// - Player 오브젝트에 부착 (ArmorSetManager/PlayerCombat/PlayerDeath 가 자동 탐색)
/// </summary>
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerCombat))]
public class SetEffectController : MonoBehaviour
{
    [Header("공통")]
    [Tooltip("범위 효과가 감지할 적 레이어")]
    [SerializeField] private LayerMask enemyLayer = ~0;

    [Header("이펙트")]
    [Tooltip("불 3세트: 점화 중 적에게 붙는 타오르는 이펙트")]
    [SerializeField] private GameObject prefabIgniteVFX;
    [Tooltip("불 4세트: 폭발 이펙트")]
    [SerializeField] private GameObject prefabExplosionVFX;
    [Tooltip("바람 4세트: 회오리 이펙트")]
    [SerializeField] private GameObject prefabTornadoVFX;
    [Tooltip("땅 3세트: 보호막 유지 중 플레이어에 붙는 이펙트")]
    [SerializeField] private GameObject prefabShieldVFX;

    // ─────────────────────── 기획 수치 (Tables/SetEffect.xlsx, LoadTableValues 에서 채움) ───────────────────────

    private float combatTimeout;                  // 비전투 판정 시간 (초)
    private float igniteChance;                   // 비율
    private float igniteDuration;
    private float igniteDamagePerSecond;
    private float igniteCooldown;
    private float explosionRadius;
    private float explosionDamageRatio;           // 비율
    private float waterSlowPercent;               // 비율
    private float waterSlowDuration;
    private float regenDuration;
    private float regenPercentPerSecond;          // %
    private float regenCooldown;
    private float waterHealthThreshold;           // 비율
    private float waterHighHealthAttackBonus;     // 비율
    private float waterLowHealthRegenMultiplier;  // 배율
    private int tornadoAttackCount;
    private float tornadoDamage;
    private float tornadoHalfWidth;
    private float tornadoDistance;
    private float tornadoSpeed;
    private float shieldPercent;                  // %
    private float shieldInterval;
    private float shieldAttackBonus;              // 비율
    private float stunRadius;
    private float stunDuration;
    private float soulAttackPerStack;             // 비율
    private int soulMaxStacks;
    private float soulDecayInterval;
    private float darkHealthThreshold;            // 비율
    private float darkLowHealthAttackBonus;       // 비율
    private float reviveHealPercentPerStack;      // %
    private float reviveLifeSteal;                // 비율
    private float reviveLifeStealDuration;
    private float reviveCooldown;

    // ─────────────────────── 런타임 ───────────────────────

    private PlayerStats _stats;
    private PlayerCombat _combat;

    private readonly Dictionary<RuneElement, SetTier> _tiers = new Dictionary<RuneElement, SetTier>();

    private float _fIgniteCooldownTimer = 0f;
    private float _fRegenTimer = 0f;
    private float _fRegenCooldownTimer = 0f;
    private int _nAttackCount = 0;
    private bool _bExplodedThisAttack = false;
    private bool _bBossSoulThisAttack = false;
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

    private bool IsInCombat => Time.time - _stats.LastCombatTime < combatTimeout;

    private float HealthRatio => _stats.Health / _stats.MaxHealth;

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
            if (GetCount(RuneElement.Earth) >= 4 && _stats.Shield > 0f)
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
        LoadTableValues();
    }

    // 엑셀의 % 값은 100 으로 나눠 비율로 쓴다
    private void LoadTableValues()
    {
        combatTimeout = GameTables.GetSetEffectValue("Common_CombatTimeout");
        igniteChance = GameTables.GetSetEffectValue("Fire_IgniteChance") / 100f;
        igniteDuration = GameTables.GetSetEffectValue("Fire_IgniteDuration");
        igniteDamagePerSecond = GameTables.GetSetEffectValue("Fire_IgniteDamagePerSecond");
        igniteCooldown = GameTables.GetSetEffectValue("Fire_IgniteCooldown");
        explosionRadius = GameTables.GetSetEffectValue("Fire_ExplosionRadius");
        explosionDamageRatio = GameTables.GetSetEffectValue("Fire_ExplosionDamage") / 100f;
        waterSlowPercent = GameTables.GetSetEffectValue("Water_SlowPercent") / 100f;
        waterSlowDuration = GameTables.GetSetEffectValue("Water_SlowDuration");
        regenDuration = GameTables.GetSetEffectValue("Water_RegenDuration");
        regenPercentPerSecond = GameTables.GetSetEffectValue("Water_RegenPerSecond");
        regenCooldown = GameTables.GetSetEffectValue("Water_RegenCooldown");
        waterHealthThreshold = GameTables.GetSetEffectValue("Water_HealthThreshold") / 100f;
        waterHighHealthAttackBonus = GameTables.GetSetEffectValue("Water_HighHealthAttack") / 100f;
        waterLowHealthRegenMultiplier = GameTables.GetSetEffectValue("Water_LowHealthRegen") / 100f;
        tornadoAttackCount = Mathf.RoundToInt(GameTables.GetSetEffectValue("Wind_TornadoAttackCount"));
        tornadoDamage = GameTables.GetSetEffectValue("Wind_TornadoDamage");
        tornadoHalfWidth = GameTables.GetSetEffectValue("Wind_TornadoHalfWidth");
        tornadoDistance = GameTables.GetSetEffectValue("Wind_TornadoDistance");
        tornadoSpeed = GameTables.GetSetEffectValue("Wind_TornadoSpeed");
        shieldPercent = GameTables.GetSetEffectValue("Earth_ShieldPercent");
        shieldInterval = GameTables.GetSetEffectValue("Earth_ShieldInterval");
        shieldAttackBonus = GameTables.GetSetEffectValue("Earth_ShieldAttack") / 100f;
        stunRadius = GameTables.GetSetEffectValue("Earth_StunRadius");
        stunDuration = GameTables.GetSetEffectValue("Earth_StunDuration");
        soulAttackPerStack = GameTables.GetSetEffectValue("Darkness_SoulAttackPerStack") / 100f;
        soulMaxStacks = Mathf.RoundToInt(GameTables.GetSetEffectValue("Darkness_SoulMaxStacks"));
        soulDecayInterval = GameTables.GetSetEffectValue("Darkness_SoulDecayInterval");
        darkHealthThreshold = GameTables.GetSetEffectValue("Darkness_HealthThreshold") / 100f;
        darkLowHealthAttackBonus = GameTables.GetSetEffectValue("Darkness_LowHealthAttack") / 100f;
        reviveHealPercentPerStack = GameTables.GetSetEffectValue("Darkness_ReviveHealPerStack");
        reviveLifeSteal = GameTables.GetSetEffectValue("Darkness_ReviveLifeSteal") / 100f;
        reviveLifeStealDuration = GameTables.GetSetEffectValue("Darkness_ReviveLifeStealDuration");
        reviveCooldown = GameTables.GetSetEffectValue("Darkness_ReviveCooldown");
    }

    private void OnEnable()
    {
        EnemyBase.OnAnyDied += OnEnemyDied;
    }

    private void OnDisable()
    {
        EnemyBase.OnAnyDied -= OnEnemyDied;
    }

    // 원소 활성 단계를 착용 수(1~4)로 반환
    private int GetCount(RuneElement _element)
    {
        return _tiers.TryGetValue(_element, out SetTier tier) == true ? (int)tier : 0;
    }

    /// <summary>세트 단계 갱신 (ArmorSetManager 재계산 시 호출)</summary>
    public void ApplyTiers(Dictionary<RuneElement, SetTier> _activeTiers)
    {
        _tiers.Clear();
        foreach (KeyValuePair<RuneElement, SetTier> kv in _activeTiers)
        {
            _tiers[kv.Key] = kv.Value;
        }

        // 땅 3세트 해제 시 보호막 제거 (부여는 UpdateShield 에서 쿨타임이 끝났을 때만)
        bool bEarth = GetCount(RuneElement.Earth) >= 3;
        if (bEarth == false && _bEarthShieldActive == true)
        {
            _stats.SetShieldValue(0f);
        }
        _bEarthShieldActive = bEarth;

        // 어둠 세트 해제 시 영혼 초기화
        if (GetCount(RuneElement.Darkness) < 1)
        {
            SetSoulStacks(0);
        }
    }

    private void Update()
    {
        float fDt = Time.deltaTime;

        // 쿨타임/지속 시간 (0 이하면 끝난 상태)
        _fIgniteCooldownTimer -= fDt;
        _fRegenCooldownTimer -= fDt;
        _fReviveCooldownTimer -= fDt;
        _fLifeStealTimer -= fDt;
        _fShieldTimer -= fDt;

        UpdateRegen(fDt);
        UpdateShield();
        UpdateSoulDecay(fDt);
    }

    // ─────────────────────── 외부 호출 (PlayerCombat) ───────────────────────

    /// <summary>기본 공격 입력 시 판정 전에 호출 (공격 1회당 제한 초기화, 바람 4세트 회오리 카운트)</summary>
    public void OnBasicAttack()
    {
        _bExplodedThisAttack = false;
        _bBossSoulThisAttack = false;

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
        StartCoroutine(TornadoRoutine(transform.forward));
    }

    /// <summary>기본 공격으로 적에게 피해를 준 직후 호출 (_nHitIndex: 단검 타격 회차, 장검/창은 항상 0)</summary>
    public void OnEnemyHit(EnemyBase _enemy, float _fDamage, int _nHitIndex)
    {
        // 어둠 1세트: 보스 적중 시 영혼, 공격 1회당 1스택 (처치는 OnEnemyDied 에서 처리)
        if (GetCount(RuneElement.Darkness) >= 1 && _enemy is BossHead && _bBossSoulThisAttack == false)
        {
            _bBossSoulThisAttack = true;
            SetSoulStacks(_nSoulStacks + 1);
        }

        // 어둠 4세트 부활 후 흡혈
        if (_fLifeStealTimer > 0f)
        {
            _stats.ModifyHealth(_fDamage * reviveLifeSteal);
        }

        // 물 3세트: 적중 시 회복 시작 (적용 후 쿨타임)
        if (GetCount(RuneElement.Water) >= 3 && _fRegenCooldownTimer <= 0f)
        {
            _fRegenTimer = regenDuration;
            _fRegenCooldownTimer = regenCooldown;
        }

        if (_enemy.IsDead == true)
        {
            return;
        }

        EnemyStatusEffect status = EnemyStatusEffect.GetOrAdd(_enemy);

        // 불 4세트: 첫 타격에 맞은 적이 이미 점화 상태면 폭발, 공격 1회당 1번 (막 점화된 적은 제외하려고 점화보다 먼저 판정)
        if (GetCount(RuneElement.Fire) >= 4 && _nHitIndex == 0 && _bExplodedThisAttack == false && status.IsIgnited == true)
        {
            _bExplodedThisAttack = true;
            Explode(_enemy);
        }

        // 불 3세트: 확률 점화 (적용 후 쿨타임)
        if (GetCount(RuneElement.Fire) >= 3 && _fIgniteCooldownTimer <= 0f && UnityEngine.Random.value < igniteChance)
        {
            status.ApplyIgnite(igniteDuration, igniteDamagePerSecond, prefabIgniteVFX);
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
        if (GetCount(RuneElement.Darkness) < 4 || _nSoulStacks <= 0 || _fReviveCooldownTimer > 0f || IsInCombat == false)
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
        if (_fRegenTimer <= 0f || GetCount(RuneElement.Water) < 3)
        {
            return;
        }
        _fRegenTimer -= _fDt;

        float fMult = 1f;
        if (GetCount(RuneElement.Water) >= 4 && HealthRatio < waterHealthThreshold)
        {
            fMult = waterLowHealthRegenMultiplier;
        }
        _stats.ModifyHealth(_stats.MaxHealth * regenPercentPerSecond / 100f * fMult * _fDt);
    }

    // ─────────────────────── 땅 ───────────────────────

    private void UpdateShield()
    {
        bool bActive = GetCount(RuneElement.Earth) >= 3;

        // 쿨타임은 세트를 해제해도 Update 에서 계속 흐름 (재착용으로 즉시 재충전 방지)
        if (bActive == true && _fShieldTimer <= 0f)
        {
            _fShieldTimer = shieldInterval;
            _stats.SetShieldValue(_stats.MaxHealth * shieldPercent / 100f);

            // 땅 4세트: 보호막 갱신 시 주변 적 기절
            if (GetCount(RuneElement.Earth) >= 4)
            {
                StunNearby();
            }
        }

        // 보호막 이펙트 표시
        bool bShowVFX = bActive == true && _stats.Shield > 0f;
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

    // 어둠 1세트: 내가 막타 친 적 처치 시 영혼 (보스는 적중 시 획득하므로 제외, 멀티는 막타 PC 만)
    private void OnEnemyDied(EnemyBase _enemy)
    {
        if (GetCount(RuneElement.Darkness) >= 1 && _enemy.DropsForLocalPlayer == true && (_enemy is BossHead) == false)
        {
            SetSoulStacks(_nSoulStacks + 1);
        }
    }

    // ─────────────────────── 범위 피해 ───────────────────────

    private void Explode(EnemyBase _target)
    {
        Vector3 vCenter = _target.transform.position;
        float fDamage = _combat.AttackPower * explosionDamageRatio;

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
            enemy.TakeDamage(fDamage);
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
                    enemy.TakeDamage(tornadoDamage);
                }
            }
            yield return null;
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
