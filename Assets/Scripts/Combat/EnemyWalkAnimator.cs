using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적 이동 애니메이션 구동 컴포넌트 (Walk 전용)
///
/// [개요]
/// - NavMeshAgent 의 이동 속도로 Walk 모션만 켜고 끔
/// - 멈춰 있을 때는 Animator Controller 의 기본 상태(Idle 또는 Empty)로 복귀
/// - 코드는 Idle 상태를 직접 건드리지 않으므로 Idle 이 빈 상태든 클립이든 동작
/// - 애니메이션이 필요한 일부 프리팹에만 수동 부착 (전체 강제 아님)
///
/// [사용법]
/// 1. 애니메이터가 있는 적 프리팹에 이 컴포넌트 부착
/// 2. Animator Controller 에 bool 파라미터 1개 추가 (기본 이름 IsWalking)
/// 3. 트랜지션 2개 설정
///    기본 상태(Idle/Empty) 에서 Walk 로: 조건 IsWalking == true
///    Walk 에서 기본 상태로: 조건 IsWalking == false
///
/// [컴포넌트 설정]
/// - NavMeshAgent 필요 (EnemyAI 가 사용하는 것 그대로 사용)
/// - Animator 는 루트 또는 자식 모델 어디에 있어도 자동 탐색
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyWalkAnimator : MonoBehaviour
{
    // ─────────────────────── 설정 ───────────────────────

    [Header("애니메이터 파라미터")]
    [Tooltip("이동 중 여부를 전달할 Animator bool 파라미터 이름")]
    [SerializeField] private string walkParameterName = "IsWalking";

    [Header("이동 판정")]
    [Tooltip("이 속도 이상이면 걷는 것으로 판정 (단위: m/s)")]
    [SerializeField] private float walkSpeedThreshold = 0.1f;

    // ─────────────────────── 참조 ───────────────────────

    private NavMeshAgent _agent;
    private Animator     _animator;

    // ─────────────────────── 런타임 상태 ───────────────────────

    private bool _isWalking = false;

    // ─────────────────────── 초기화 ───────────────────────

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        // 애니메이터는 루트 또는 자식 모델에 있을 수 있음
        _animator = GetComponentInChildren<Animator>();

        // 옵트인 컴포넌트 — 애니메이터가 없으면 조용히 비활성화
        if (_animator == null)
        {
            enabled = false;
        }
    }

    // ─────────────────────── 업데이트 ───────────────────────

    private void Update()
    {
        // 에이전트가 비활성(예: 사망 처리로 disable)이면 멈춘 것으로 간주
        float currentSpeed = _agent.enabled ? _agent.velocity.magnitude : 0f;
        bool  shouldWalk    = currentSpeed > walkSpeedThreshold;

        // 상태가 바뀔 때만 파라미터 갱신
        if (shouldWalk != _isWalking)
        {
            _isWalking = shouldWalk;
            _animator.SetBool(walkParameterName, _isWalking);
        }
    }
}
