using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적 AI — 순찰/추적/공격 상태머신
///
/// [상태]
/// - Idle    : 대기 (시작 상태)
/// - Patrol  : 순찰 (랜덤 waypoint 이동)
/// - Chase   : 플레이어 추적
/// - Attack  : 공격 (사거리 안에서 쿨다운마다 공격)
/// - Dead    : 사망
///
/// [전환 조건]
/// Idle/Patrol → Chase  : 플레이어가 감지 범위 안에 들어옴
/// Chase       → Attack : 플레이어가 공격 범위 안에 들어옴
/// Attack      → Chase  : 플레이어가 공격 범위 밖으로 나감
/// Chase/Attack→ Patrol : 플레이어가 감지 범위 밖으로 나감
///
/// [컴포넌트 설정]
/// - NavMeshAgent 부착 필수
/// - EnemyBase 와 같은 오브젝트에 부착
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyBase))]
public class EnemyAI : MonoBehaviour
{
    // ─────────────────────── 상태 열거형 ───────────────────────

    public enum AIState { Idle, Patrol, Chase, Attack, Dead }

    // ─────────────────────── 감지 설정 ───────────────────────

    [Header("감지 범위")]
    [Tooltip("플레이어를 감지하기 시작하는 거리")]
    [SerializeField] private float detectionRange  = 8f;

    [Tooltip("감지 후 플레이어가 이 거리 밖으로 나가면 추적 포기")]
    [SerializeField] private float loseTargetRange = 12f;

    // ─────────────────────── 순찰 설정 ───────────────────────

    [Header("순찰")]
    [Tooltip("순찰 반경 (스폰 지점 기준)")]
    [SerializeField] private float patrolRadius   = 5f;

    [Tooltip("순찰 지점 도착 후 대기 시간 (초)")]
    [SerializeField] private float patrolWaitTime = 2f;

    // ─────────────────────── 공격 설정 ───────────────────────

    [Header("공격")]
    [Tooltip("공격 쿨다운 (초)")]
    [SerializeField] private float attackCooldown = 1.5f;

    // ─────────────────────── 내부 참조 ───────────────────────

    private NavMeshAgent _agent;
    private EnemyBase    _base;
    private Transform    _player;

    // ─────────────────────── 런타임 상태 ───────────────────────

    public AIState CurrentState { get; private set; } = AIState.Idle;

    private Vector3 _spawnPosition;
    private float   _stateTimer       = 0f;
    private float   _attackCooldownTimer = 0f;

    // ─────────────────────── 초기화 ───────────────────────

    private void Awake()
    {
        _agent         = GetComponent<NavMeshAgent>();
        _base          = GetComponent<EnemyBase>();
        _spawnPosition = transform.position;

        // EnemyBase 사망 이벤트 구독
        _base.OnDied += _ => TransitionTo(AIState.Dead);
    }

    private void Start()
    {
        // 플레이어 자동 탐색
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;

        // NavMeshAgent 스탯 동기화
        _agent.speed = _base.MoveSpeed;

        TransitionTo(AIState.Patrol);
    }

    // ─────────────────────── 업데이트 ───────────────────────

    private void Update()
    {
        if (CurrentState == AIState.Dead) return;

        _stateTimer          -= Time.deltaTime;
        _attackCooldownTimer -= Time.deltaTime;

        switch (CurrentState)
        {
            case AIState.Idle:    UpdateIdle();    break;
            case AIState.Patrol:  UpdatePatrol();  break;
            case AIState.Chase:   UpdateChase();   break;
            case AIState.Attack:  UpdateAttack();  break;
        }
    }

    // ─────────────────────── 상태별 업데이트 ───────────────────────

    private void UpdateIdle()
    {
        if (_stateTimer <= 0f) TransitionTo(AIState.Patrol);
        if (IsPlayerInRange(detectionRange)) TransitionTo(AIState.Chase);
    }

    private void UpdatePatrol()
    {
        if (IsPlayerInRange(detectionRange))
        {
            TransitionTo(AIState.Chase);
            return;
        }

        // 목적지 도착 시 대기 후 새 목적지
        if (_agent.pathPending == false && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            _stateTimer = patrolWaitTime;
            TransitionTo(AIState.Idle);
        }
    }

    private void UpdateChase()
    {
        if (_player == null) { TransitionTo(AIState.Patrol); return; }

        // 플레이어 위치 추적
        _agent.SetDestination(_player.position);

        // 공격 범위 안 → 공격
        if (IsPlayerInRange(_base.AttackRange))
        {
            TransitionTo(AIState.Attack);
            return;
        }

        // 감지 범위 밖 → 순찰 복귀
        if (IsPlayerInRange(loseTargetRange) == false)
            TransitionTo(AIState.Patrol);
    }

    private void UpdateAttack()
    {
        if (_player == null) { TransitionTo(AIState.Patrol); return; }

        // 플레이어 바라보기
        LookAtPlayer();

        // 공격 범위 밖 → 추적
        if (IsPlayerInRange(_base.AttackRange) == false)
        {
            TransitionTo(AIState.Chase);
            return;
        }

        // 감지 범위 밖 → 순찰
        if (IsPlayerInRange(loseTargetRange) == false)
        {
            TransitionTo(AIState.Patrol);
            return;
        }

        // 공격 쿨다운마다 공격
        if (_attackCooldownTimer <= 0f)
            PerformAttack();
    }

    // ─────────────────────── 상태 전환 ───────────────────────

    private void TransitionTo(AIState newState)
    {
        // 이전 상태 종료
        OnExitState(CurrentState);
        CurrentState = newState;
        // 새 상태 진입
        OnEnterState(newState);
    }

    private void OnEnterState(AIState state)
    {
        switch (state)
        {
            case AIState.Idle:
                _agent.isStopped = true;
                _stateTimer = patrolWaitTime;
                break;

            case AIState.Patrol:
                _agent.isStopped = false;
                SetRandomPatrolDestination();
                break;

            case AIState.Chase:
                _agent.isStopped = false;
                _agent.speed     = _base.MoveSpeed;
                break;

            case AIState.Attack:
                _agent.isStopped = true;
                _agent.ResetPath();
                break;

            case AIState.Dead:
                _agent.isStopped = true;
                _agent.enabled   = false;
                enabled          = false;
                break;
        }
    }

    private void OnExitState(AIState state) { }

    // ─────────────────────── 공격 ───────────────────────

    private void PerformAttack()
    {
        _attackCooldownTimer = attackCooldown;

        // 플레이어에게 데미지 전달
        if (_player == null) return;

        PlayerStats playerStats = _player.GetComponent<PlayerStats>();
        if (playerStats == null) return;

        float actualDamage = playerStats.TakeDamage(_base.AttackPower);
        Debug.Log($"[{gameObject.name}] 플레이어 공격 — 데미지: {actualDamage:F1}");
    }

    // ─────────────────────── 유틸 ───────────────────────

    private bool IsPlayerInRange(float range)
    {
        if (_player == null) return false;
        return Vector3.Distance(transform.position, _player.position) <= range;
    }

    private void LookAtPlayer()
    {
        if (_player == null) return;
        Vector3 dir = (_player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }

    private void SetRandomPatrolDestination()
    {
        // 스폰 지점 기반 랜덤 순찰 위치
        Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
        randomDir.y = 0f;
        Vector3 target = _spawnPosition + randomDir;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }

    // ─────────────────────── 에디터 기즈모 ───────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 감지 범위
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 추적 포기 범위
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);

        // 순찰 범위
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        Gizmos.DrawWireSphere(Application.isPlaying ? _spawnPosition : transform.position, patrolRadius);
    }
#endif
}
