// BossHead.cs
// 니드호그 보스의 머리 한 개 (왼쪽/오른쪽/가운데 공용)
// EnemyBase 를 상속해 체력/피격/HP바/방어력 공식을 그대로 재사용한다.
// 역할(HeadRole)에 따라 공격 패턴이 다르며, 가운데 머리는 좌우 머리가 모두 죽기 전까지 무적이다.
// 공격 수치는 전부 public 으로 노출해 인스펙터에서 바로 조정할 수 있다.
// 공격 시 바닥에 빨간 예고 존(telegraphZonePrefab)을 공격 범위 크기에 맞춰 깔아 표시한다.
// 실제 공격 실행(투사체/장판/근접)은 역할별 메서드에서 처리한다. (2~4단계에서 채움)
// 담당: 김보민

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

    [Header("공격 구역 (이 구역 안에 플레이어가 있으면 공격 시도)")]
    [Tooltip("공격 구역 중심. 비우면 이 머리 자신의 위치를 사용")]
    public Transform attackZoneCenter;
    [Tooltip("공격 구역 반경 (왼쪽/오른쪽 머리에 적용)")]
    public float attackZoneRadius = 6f;

    [Header("공격 타이밍 (공통)")]
    [Tooltip("공격과 공격 사이 쿨다운 (초)")]
    public float attackCooldown = 2.5f;
    [Tooltip("빨간 예고 존이 뜬 뒤 실제 공격까지의 시간 (초)")]
    public float telegraphTime = 0.8f;

    [Header("빨간 공격 예고 존 (바닥 데칼 프리팹)")]
    [Tooltip("바닥에 깔릴 빨간 원형 존 프리팹 (기본 지름 1 유닛으로 제작하면 자동 스케일)")]
    public GameObject telegraphZonePrefab;

    // ─────────────────────── 왼쪽 머리 — 퇴로 차단 투사체 ───────────────────────
    [Header("[왼쪽] 투사체 공격 수치")]
    [Tooltip("발사할 투사체 프리팹")]
    public GameObject leftProjectilePrefab;
    [Tooltip("투사체 데미지")]
    public float leftProjectileDamage = 10f;
    [Tooltip("투사체 속도")]
    public float leftProjectileSpeed = 12f;
    [Tooltip("투사체 착탄(빨간 존) 반경")]
    public float leftImpactRadius = 1.5f;
    [Tooltip("최대 연속 발사 수 (기본 2발)")]
    public int leftMaxShots = 2;
    [Tooltip("2번째 발사가 나갈 확률 (%). 기획: 10~15%")]
    [Range(0f, 100f)]
    public float leftSecondShotChancePercent = 12.5f;
    [Tooltip("2번째 투사체가 플레이어 주변 랜덤으로 떨어지는 범위")]
    public float leftSecondShotSpread = 2.5f;

    // ─────────────────────── 오른쪽 머리 — 이동 저하 장판 ───────────────────────
    [Header("[오른쪽] 장판 공격 수치")]
    [Tooltip("설치할 장판 프리팹")]
    public GameObject rightSlowFieldPrefab;
    [Tooltip("장판 위에서의 이동/공격 속도 저하 (%). 기획: 20~30%")]
    [Range(0f, 100f)]
    public float rightSlowPercent = 25f;
    [Tooltip("장판(빨간 존) 반경")]
    public float rightFieldRadius = 2.5f;
    [Tooltip("장판 유지 시간 (초)")]
    public float rightFieldDuration = 5f;
    [Tooltip("동시에 유지되는 장판 최대 개수 (기획: 아마 안 바뀜)")]
    public int rightFieldMaxCount = 3;

    // ─────────────────────── 가운데 머리 — 근접 직접 공격 ───────────────────────
    [Header("[가운데] 근접 공격 수치")]
    [Tooltip("켜면 공격 범위가 맵 전체 — 플레이어가 어디 있든 공격한다")]
    public bool centerWholeMap = true;
    [Tooltip("근접 공격(빨간 존) 반경. 맵 전체가 꺼져 있을 때의 공격 범위로도 쓰인다")]
    public float centerAttackRadius = 4f;
    [Tooltip("근접 공격 데미지")]
    public float centerAttackDamage = 25f;
    [Tooltip("공격 전 이펙트 (예고)")]
    public GameObject centerPreEffect;
    [Tooltip("공격 후(적중) 이펙트")]
    public GameObject centerHitEffect;

    // ─────────────────────── 런타임 내부 상태 ───────────────────────

    // 무적 여부 (가운데 머리는 좌우가 죽기 전까지 true)
    private bool _invincible = false;

    // 플레이어 참조
    private Transform _player;

    // 공격 상태
    private float _attackCooldownTimer = 0f;
    private bool _isAttacking = false;

    // 이번 공격이 떨어질 위치와 반경 (예고 존과 실제 공격을 일치시키기 위해 보관)
    private Vector3 _pendingAttackPos = Vector3.zero;
    private float _pendingAttackRadius = 1f;

    // 현재 머리의 역할
    public HeadRole Role
    {
        get { return role; }
    }

    // 현재 무적 상태인지
    public bool IsInvincible
    {
        get { return _invincible; }
    }

    // 현재 공격 중인지 (빨간 예고가 뜬 순간부터 실제 공격 실행까지). 죽었으면 false
    public bool IsAttacking
    {
        get { return _isAttacking == true && IsDead == false; }
    }

    protected override void Awake()
    {
        base.Awake();

        // 가운데 머리는 시작 시 무적 (좌우 머리가 모두 죽으면 해제)
        if (role == HeadRole.Center)
        {
            _invincible = true;
        }
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead == true)
        {
            return;
        }
        if (_player == null)
        {
            return;
        }

        _attackCooldownTimer = _attackCooldownTimer - Time.deltaTime;

        // 공격 중이 아니고 쿨다운이 끝났고 플레이어가 공격 구역 안에 있으면 공격 시작
        if (_isAttacking == false && _attackCooldownTimer <= 0f)
        {
            if (IsPlayerInZone() == true)
            {
                StartAttack();
            }
        }
    }

    // 무적 상태 설정 (BossNidhogg 가 좌우 머리 사망 시 호출)
    public void SetVulnerable(bool vulnerable)
    {
        _invincible = (vulnerable == false);
    }

    // 피격 처리 — 무적이면 데미지를 무시한다
    public override void TakeDamage(float rawDamage)
    {
        if (_invincible == true)
        {
            // 무적일 때는 데미지를 받지 않는다 (추후 무적 피격 이펙트/사운드 추가 가능)
            return;
        }

        base.TakeDamage(rawDamage);
    }

    // 공격 구역 안에 플레이어가 있는지
    private bool IsPlayerInZone()
    {
        if (_player == null)
        {
            return false;
        }

        // 가운데 머리가 맵 전체 모드면 플레이어가 어디 있든 공격
        if (role == HeadRole.Center && centerWholeMap == true)
        {
            return true;
        }

        Vector3 center = transform.position;
        if (attackZoneCenter != null)
        {
            center = attackZoneCenter.position;
        }

        float dist = Vector3.Distance(center, _player.position);
        return dist <= attackZoneRadius;
    }

    // 이번 공격이 떨어질 위치와 반경을 역할별로 계산
    // (현재는 전부 플레이어 현재 위치를 기준으로 미리보기. 2단계에서 왼쪽은 "있던 자리"로 바뀜)
    private void GetAttackTargetAndRadius(out Vector3 pos, out float radius)
    {
        pos = transform.position;
        radius = 1f;

        if (_player != null)
        {
            pos = _player.position;
        }

        if (role == HeadRole.Left)
        {
            radius = leftImpactRadius;
        }
        else if (role == HeadRole.Right)
        {
            radius = rightFieldRadius;
        }
        else
        {
            radius = centerAttackRadius;
        }
    }

    // 공격 시작 (빨간 예고 존 표시 후 예고 시간 뒤 실제 공격)
    private void StartAttack()
    {
        // 가운데 머리 단독 공격 규칙 — 컨트롤러에 공격 가능 여부를 먼저 묻는다.
        // 가운데는 좌우가 공격 중이면 대기, 좌우는 가운데가 공격 중이면 대기한다.
        if (BossNidhogg.Instance != null)
        {
            if (BossNidhogg.Instance.CanHeadAttack(this) == false)
            {
                // 지금은 공격할 수 없다 (쿨다운은 건드리지 않고 다음 프레임에 다시 시도)
                return;
            }
        }

        _isAttacking = true;
        _attackCooldownTimer = attackCooldown;

        // 이번 공격 위치/반경 계산 후 보관
        Vector3 pos;
        float radius;
        GetAttackTargetAndRadius(out pos, out radius);
        _pendingAttackPos = pos;
        _pendingAttackRadius = radius;

        // 바닥에 빨간 예고 존 표시 (공격 범위 크기에 맞춰 스케일)
        ShowTelegraphZone(pos, radius);

        // 예고 시간 뒤에 실제 공격 실행
        Invoke(nameof(ExecuteAttack), telegraphTime);
    }

    // 바닥에 빨간 예고 존을 깐다 (반경에 맞춰 크기 자동 조정, 예고 시간 뒤 자동 삭제)
    private void ShowTelegraphZone(Vector3 pos, float radius)
    {
        if (telegraphZonePrefab == null)
        {
            return;
        }

        GameObject zone = Instantiate(telegraphZonePrefab, pos, Quaternion.identity);

        // 프리팹 기본 지름을 1 유닛으로 보고, 지름 = 반경 x 2 가 되도록 스케일
        float diameter = radius * 2f;
        zone.transform.localScale = new Vector3(diameter, zone.transform.localScale.y, diameter);

        // 예고 시간이 지나면 존 제거
        Destroy(zone, telegraphTime);
    }

    // 실제 공격 실행 — 역할별로 분기
    private void ExecuteAttack()
    {
        if (IsDead == true)
        {
            _isAttacking = false;
            return;
        }

        switch (role)
        {
            case HeadRole.Left:
                ExecuteLeftAttack();
                break;
            case HeadRole.Right:
                ExecuteRightAttack();
                break;
            case HeadRole.Center:
                ExecuteCenterAttack();
                break;
        }

        _isAttacking = false;
    }

    // 왼쪽 머리 — 퇴로 차단 투사체 (2단계에서 구현)
    private void ExecuteLeftAttack()
    {
        Debug.Log("[BossHead] 왼쪽 머리 공격 (투사체) — 2단계에서 구현 예정");
    }

    // 오른쪽 머리 — 이동 저하 장판 (3단계에서 구현)
    private void ExecuteRightAttack()
    {
        Debug.Log("[BossHead] 오른쪽 머리 공격 (장판) — 3단계에서 구현 예정");
    }

    // 가운데 머리 — 근접 직접 공격 (4단계에서 구현)
    private void ExecuteCenterAttack()
    {
        Debug.Log("[BossHead] 가운데 머리 공격 (근접) — 4단계에서 구현 예정");
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 가운데 머리가 맵 전체 모드면 공격 구역 기즈모는 생략 (맵 전체라 의미 없음)
        if (role == HeadRole.Center && centerWholeMap == true)
        {
            return;
        }

        // 공격 구역 시각화
        Vector3 center = transform.position;
        if (attackZoneCenter != null)
        {
            center = attackZoneCenter.position;
        }

        Gizmos.color = new Color(1f, 0.3f, 0f, 0.25f);
        Gizmos.DrawWireSphere(center, attackZoneRadius);
    }
#endif
}