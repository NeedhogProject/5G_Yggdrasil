// BossHead.cs
// 니드호그 보스의 머리 한 개 (왼쪽/오른쪽/가운데 공용)
// EnemyBase 를 상속해 체력/피격/HP바/방어력 공식을 그대로 재사용한다.
// 역할(HeadRole)에 따라 공격 패턴이 다르며, 가운데 머리는 좌우 머리가 모두 죽기 전까지 무적이다.
// 머리마다 담당 이동 구역(BossPlayerZone)에 플레이어가 들어오면 공격하고, 공격 수치는 BossNidhogg 에서 관리한다.
// 멀티: 공격 결정과 체력 계산은 호스트만 한다. 호스트가 보낸 공격을 각 PC 가 재생하고, 피해는 각 PC 가 자기 플레이어에게 적용한다.
// 담당: 김보민 (보스 패턴 구현: 정건)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossHead : EnemyBase
{
    // 머리 역할 구분
    public enum HeadRole
    {
        Left,    // 왼쪽 — 퇴로 차단 (투사체)
        Right,   // 오른쪽 — 공간 제한 (장판)
        Center   // 가운데 — 본체, 접근 차단 (근접)
    }

    [Header("머리 역할")]
    public HeadRole role = HeadRole.Center;

    [Header("담당 이동 구역 (플레이어가 이 구역 안에 있으면 공격)")]
    [SerializeField] private BossPlayerZone playerZone;

    [Header("투사체 발사 위치 (왼쪽 머리, 비우면 머리 위치)")]
    [SerializeField] private Transform trMouth;

    [Header("연출")]
    [Tooltip("공격 시 Attack 트리거를 받을 애니메이터 (선택)")]
    [SerializeField] private Animator animator;
    [Tooltip("죽었을 때 회색으로 바꿀 렌더러들 (비우면 자식 전체)")]
    [SerializeField] private Renderer[] deadTintRenderers;
    [SerializeField] private Color deadColor = new Color(0.35f, 0.35f, 0.35f);

    private bool _bInvincible = false;
    private bool _bStopped = false;
    private bool _bAttacking = false;
    private float _fCooldownTimer = 0f;
    private Transform _trPlayer;

    // 오른쪽 머리: 설치한 장판 (종류별 최대 개수 관리)
    private readonly List<GameObject> _moveFields = new List<GameObject>();
    private readonly List<GameObject> _attackFields = new List<GameObject>();

    public HeadRole Role => role;

    // 무적이거나 죽은 머리는 피해와 적중 효과를 받지 않음
    public override bool IsDamageable => base.IsDamageable == true && _bInvincible == false;

    // 현재 공격 중인지 (예고가 뜬 순간부터 공격 실행까지). 죽었으면 false
    public bool IsAttacking => _bAttacking == true && IsDead == false;

    private BossNidhogg Boss => BossNidhogg.Instance;

    protected override void Awake()
    {
        base.Awake();

        // 가운데 머리는 시작 시 무적 (좌우 머리가 모두 죽으면 해제)
        if (role == HeadRole.Center)
        {
            _bInvincible = true;
        }
    }

    private void Start()
    {
        GameObject objPlayer = GameObject.FindWithTag("Player");
        if (objPlayer != null)
        {
            _trPlayer = objPlayer.transform;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead == true || _bStopped == true || _trPlayer == null || Boss == null)
        {
            return;
        }

        // 멀티 참가자는 공격을 결정하지 않음 (호스트가 보낸 공격만 재생)
        if (NetworkPlayerAvatar.IsSessionClient == true)
        {
            return;
        }

        _fCooldownTimer -= Time.deltaTime;

        if (_bAttacking == false && _fCooldownTimer <= 0f && Boss.CanHeadAttack(this) == true)
        {
            Transform trTarget = FindTargetInZone();
            if (trTarget != null)
            {
                StartCoroutine(AttackRoutine(trTarget.position));
            }
        }
    }

    // 체력 설정 (BossNidhogg 가 시작 시 호출)
    public void InitHealth(float _fHealth)
    {
        maxHealth = _fHealth;
        _currentHealth = _fHealth;
    }

    // 무적 상태 설정 (BossNidhogg 가 좌우 머리 사망 시 호출)
    public void SetVulnerable(bool _bVulnerable)
    {
        _bInvincible = _bVulnerable == false;
    }

    // 피격 — 무적이면 무시, 아니면 머리 추가 피해와 그로기 피해 증가 적용
    public override void TakeDamage(float rawDamage)
    {
        if (_bInvincible == true)
        {
            return;
        }

        // 멀티 참가자는 피해를 호스트로 보냄 (체력과 피해 배율 계산은 호스트만)
        if (NetworkPlayerAvatar.RouteBossDamage(role, rawDamage) == true)
        {
            return;
        }

        float fMult = Boss != null ? Boss.DamageMultiplier : 1f;
        base.TakeDamage(rawDamage * fMult);

        // 멀티 호스트는 바뀐 체력을 참가자에게 알림
        NetworkPlayerAvatar.SendBossHealth(role, _currentHealth);
    }

    // 사망 — 오브젝트를 없애지 않고 남김 (좌우 머리는 회색)
    protected override void Die()
    {
        // 멀티 세션에서는 호스트가 모든 PC 에 사망을 알리고, 각 PC 가 ProcessDeath 로 처리
        if (NetworkPlayerAvatar.RouteBossDeath(role) == true)
        {
            return;
        }

        ProcessDeath(true);
    }

    /// <summary>사망 처리 (멀티에서는 NetworkPlayerAvatar 가 모든 PC 에서 호출). _bIsKiller: 내가 막타를 쳤는지</summary>
    public void ProcessDeath(bool _bIsKiller)
    {
        if (IsDead == true)
        {
            return;
        }

        DieLocal(_bIsKiller);

        // 위험: EnemyBase.Die 가 0.5초 뒤 오브젝트를 파괴하므로 취소 (죽은 머리를 남겨둠)
        CancelInvoke("DestroyEnemy");
        StopAllCoroutines();
        _bAttacking = false;

        // 좌우 머리만 회색으로 남김 (가운데 머리는 Die 애니메이션으로 사라지는 연출)
        if (role != HeadRole.Center)
        {
            ApplyDeadTint();
        }

        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
    }

    // 보스 사망 시 모든 공격 중단 (BossNidhogg 에서 호출, Die 애니메이션은 계속 재생)
    public void StopAll()
    {
        _bStopped = true;
        _bAttacking = false;
        StopAllCoroutines();
    }

    private void ApplyDeadTint()
    {
        Renderer[] renderers = deadTintRenderers != null && deadTintRenderers.Length > 0 ? deadTintRenderers : GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            // URP Lit 의 기본 색 (Base Map 에 곱해짐)
            rend.material.SetColor("_BaseColor", deadColor);
        }
    }

    // 담당 구역 안의 공격 대상 (살아 있는 내 캐릭터 우선, 없으면 구역 안의 상대 플레이어 분신). 없으면 null
    private Transform FindTargetInZone()
    {
        if (playerZone == null)
        {
            return null;
        }
        if (playerZone.IsPlayerInside == true && NetworkPlayerAvatar.IsLocalPlayerDead == false)
        {
            return _trPlayer;
        }
        return NetworkPlayerAvatar.FindRemoteInZone(playerZone);
    }

    // ─────────────────────── 공격 ───────────────────────

    // 공격 결정 (싱글 또는 멀티 호스트만 실행). 대상 위치와 무작위 값을 정해 모든 PC 에서 재생시킨다
    private IEnumerator AttackRoutine(Vector3 _vTargetPos)
    {
        _bAttacking = true;

        if (role == HeadRole.Left)
        {
            yield return LeftAttack(_vTargetPos);
        }
        else if (role == HeadRole.Right)
        {
            RightAttack(_vTargetPos);
        }
        else
        {
            float fTelegraph = Boss.CurrentMeleeTelegraphTime;
            SendAttack(_vTargetPos, Vector3.zero, fTelegraph);
            yield return new WaitForSeconds(fTelegraph);
        }

        _fCooldownTimer = Boss.CurrentAttackDelay;
        _bAttacking = false;
    }

    // 정한 공격을 재생 (멀티 세션이면 모든 PC 에서, 아니면 바로)
    private void SendAttack(Vector3 _vA, Vector3 _vB, float _fTime)
    {
        if (NetworkPlayerAvatar.SendBossAttack(role, _vA, _vB, _fTime) == false)
        {
            ExecuteAttack(_vA, _vB, _fTime);
        }
    }

    /// <summary>
    /// 공격 재생 (멀티에서는 NetworkPlayerAvatar 가 모든 PC 에서 호출). 피해는 각 PC 가 자기 플레이어에게 적용
    /// 왼쪽: _vA 로 투사체 1발 / 오른쪽: _vA 에 이동 속도 장판, _vB 에 공격 속도 장판 / 가운데: _vA 에 _fTime 초 예고 후 근접
    /// </summary>
    public void ExecuteAttack(Vector3 _vA, Vector3 _vB, float _fTime)
    {
        if (IsDead == true || _bStopped == true || Boss == null || _trPlayer == null)
        {
            return;
        }

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        if (role == HeadRole.Left)
        {
            FireProjectile(_vA);
        }
        else if (role == HeadRole.Right)
        {
            AddWithLimit(_moveFields, SpawnField(Boss.moveSlowFieldPrefab, _vA, BossGroundField.FieldType.MoveSlow, Boss.fieldDuration));
            AddWithLimit(_attackFields, SpawnField(Boss.attackSlowFieldPrefab, _vB, BossGroundField.FieldType.AttackSlow, Boss.fieldDuration));
        }
        else
        {
            StartCoroutine(CenterAttack(_vA, _fTime));
        }
    }

    // 왼쪽: 시전 순간의 대상 위치 1발 + 확률로 대상 주변 랜덤 위치 추가 발사
    // 투사체가 날아가는 동안 플레이어가 떠난 자리에 떨어져 퇴로를 막음 (플레이어를 따라가지 않음)
    private IEnumerator LeftAttack(Vector3 _vTargetPos)
    {
        int nShots = 1;
        while (nShots < Boss.maxConsecutiveShots && Random.Range(0f, 100f) < Boss.extraShotChance)
        {
            nShots++;
        }

        float fLongestFlight = 0f;
        for (int i = 0; i < nShots; i++)
        {
            Vector3 vTarget = i == 0 ? _vTargetPos : GetPointAround(_vTargetPos, Boss.extraShotSpread);
            fLongestFlight = Mathf.Max(fLongestFlight, GetFlightTime(vTarget));
            SendAttack(vTarget, Vector3.zero, 0f);
        }

        // 마지막 투사체가 떨어질 때까지 공격 중으로 유지
        yield return new WaitForSeconds(fLongestFlight);
    }

    // 대상 주변 랜덤 지점 (대상 발밑은 피함)
    private Vector3 GetPointAround(Vector3 _vCenter, float _fRange)
    {
        float fMin = Mathf.Min(Boss.projectileImpactRadius + 0.5f, _fRange);
        Vector2 vDir = Random.insideUnitCircle.normalized;
        float fDist = Random.Range(fMin, _fRange);
        return _vCenter + new Vector3(vDir.x, 0f, vDir.y) * fDist;
    }

    // 투사체 비행 시간
    private float GetFlightTime(Vector3 _vTarget)
    {
        Vector3 vStart = trMouth != null ? trMouth.position : transform.position;
        return Vector3.Distance(vStart, _vTarget) / Mathf.Max(0.1f, Boss.projectileSpeed);
    }

    // 투사체 발사
    private void FireProjectile(Vector3 _vTarget)
    {
        Vector3 vStart = trMouth != null ? trMouth.position : transform.position;

        // 착탄 지점 예고
        StartCoroutine(Telegraph(_vTarget, Boss.projectileImpactRadius, GetFlightTime(_vTarget)));

        GameObject objProjectile = Boss.projectilePrefab != null ? Instantiate(Boss.projectilePrefab, vStart, Quaternion.identity) : new GameObject("BossProjectile");
        objProjectile.transform.position = vStart;
        BossProjectile projectile = objProjectile.GetComponent<BossProjectile>();
        if (projectile == null)
        {
            projectile = objProjectile.AddComponent<BossProjectile>();
        }

        float fDamage = attackPower;
        projectile.Launch(_vTarget, Boss.projectileSpeed, () => OnProjectileImpact(_vTarget, fDamage));
    }

    private void OnProjectileImpact(Vector3 _vTarget, float _fDamage)
    {
        if (Boss == null)
        {
            return;
        }

        DamagePlayerInRadius(_vTarget, Boss.projectileImpactRadius, _fDamage);

        if (Boss.leaveHazardZone == true)
        {
            SpawnField(Boss.hazardZonePrefab, _vTarget, BossGroundField.FieldType.Damage, Boss.hazardDuration);
        }
    }

    // 오른쪽: 대상 주변에 이동 속도 장판, 보스 주위에 공격 속도 장판을 동시에 설치
    private void RightAttack(Vector3 _vTargetPos)
    {
        Vector3 vMovePos = GetPointAround(_vTargetPos, Boss.fieldDistance);

        Vector2 vRand = Random.insideUnitCircle * Boss.fieldDistance;
        Vector3 vAttackPos = Boss.BossCenterPosition + new Vector3(vRand.x, 0f, vRand.y);
        vAttackPos.y = _vTargetPos.y;

        SendAttack(vMovePos, vAttackPos, 0f);
    }

    // 장판 최대 개수를 넘으면 가장 오래된 것부터 제거
    private void AddWithLimit(List<GameObject> _fields, GameObject _objField)
    {
        _fields.RemoveAll(f => f == null);
        _fields.Add(_objField);
        while (_fields.Count > Boss.maxFieldCount)
        {
            Destroy(_fields[0]);
            _fields.RemoveAt(0);
        }
    }

    private GameObject SpawnField(GameObject _prefab, Vector3 _vPos, BossGroundField.FieldType _type, float _fDuration)
    {
        GameObject objField = _prefab != null ? Instantiate(_prefab, _vPos, Quaternion.identity) : new GameObject("BossField_" + _type);
        objField.transform.position = _vPos;
        BossGroundField field = objField.GetComponent<BossGroundField>();
        if (field == null)
        {
            field = objField.AddComponent<BossGroundField>();
        }
        field.Init(_type, Boss.fieldRadius, _fDuration);
        return objField;
    }

    // 가운데: 지정 위치에 범위 예고(노란색에서 빨간색) 후 머리를 휘둘러 공격
    private IEnumerator CenterAttack(Vector3 _vTarget, float _fTelegraph)
    {
        yield return Telegraph(_vTarget, Boss.meleeRange, _fTelegraph);
        DamagePlayerInRadius(_vTarget, Boss.meleeRange, attackPower);
    }

    // 바닥 예고 존 표시 (노란색, 공격 직전 빨간색), 시간이 끝나면 제거
    private IEnumerator Telegraph(Vector3 _vPos, float _fRadius, float _fDuration)
    {
        if (Boss.telegraphZonePrefab == null)
        {
            yield return new WaitForSeconds(_fDuration);
            yield break;
        }

        Vector3 vGround = new Vector3(_vPos.x, _trPlayer.position.y + 0.05f, _vPos.z);
        GameObject objZone = Instantiate(Boss.telegraphZonePrefab, vGround, Quaternion.identity);
        // 위험: 머리가 죽거나 보스가 멈춰 코루틴이 중단되면 아래 Destroy 까지 오지 않으므로 미리 예약
        Destroy(objZone, _fDuration);
        float fDiameter = _fRadius * 2f;
        objZone.transform.localScale = new Vector3(fDiameter, objZone.transform.localScale.y, fDiameter);

        Renderer rend = objZone.GetComponentInChildren<Renderer>();
        SetZoneColor(rend, Color.yellow);

        float fYellowTime = Mathf.Max(0f, _fDuration - Boss.redWarningTime);
        yield return new WaitForSeconds(fYellowTime);
        SetZoneColor(rend, Color.red);
        yield return new WaitForSeconds(_fDuration - fYellowTime);
    }

    private void SetZoneColor(Renderer _rend, Color _color)
    {
        if (_rend == null)
        {
            return;
        }
        // 반투명 유지 (알파는 프리팹 머티리얼 값 사용), 머티리얼 복제 없이 색만 바꿈
        Color cur = _rend.sharedMaterial.GetColor("_BaseColor");
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", new Color(_color.r, _color.g, _color.b, cur.a));
        _rend.SetPropertyBlock(block);
    }

    private void DamagePlayerInRadius(Vector3 _vCenter, float _fRadius, float _fDamage)
    {
        if (_trPlayer == null || PlayerStats.Instance == null)
        {
            return;
        }

        Vector3 vDiff = _trPlayer.position - _vCenter;
        vDiff.y = 0f;
        if (vDiff.magnitude <= _fRadius)
        {
            PlayerStats.Instance.TakeDamage(_fDamage);
        }
    }
}
