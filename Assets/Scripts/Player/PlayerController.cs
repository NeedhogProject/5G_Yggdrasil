using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 이동 컨트롤러 — 하데스/디아블로2 스타일 탑뷰
///
/// - 이동  : 마우스 우클릭 (클릭/홀드 지점으로 NavMesh 경로 탐색 이동)
/// - 회전  : 이동 중에는 진행 방향, 공격 시에는 마우스 커서 방향
/// - 달리기: Shift 홀드 (누르는 동안만 유지, 정신력 소모)
///
/// [컴포넌트 설정]
/// 1. Rigidbody → Constraints → Freeze Rotation X/Y/Z 체크
/// 2. Capsule Collider 부착
/// 3. PlayerInput → Behavior → Send Messages
/// 4. Input Actions 에 MoveHold(Button), Sprint(Button) 액션 등록
/// 5. 탑뷰 카메라는 플레이어 정면 위에서 수직으로 내려다보는 구도 권장
/// 6. 플레이어가 다니는 모든 씬에 NavMesh 베이크 필수 (경로 탐색 이동)
///
/// [싱글턴]
/// InventorySystem.DropItem() 에서 PlayerController.Instance 로 위치 참조
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    // ─────────────────────── 싱글턴 ───────────────────────

    public static PlayerController Instance { get; private set; }

    // ─────────────────────── 이동 설정 ───────────────────────

    [Header("이동 속도")]
    [SerializeField] private float walkSpeed   = 5f;
    [SerializeField] private float sprintSpeed = 9f;

    // ─────────────────────── 달리기 설정 ───────────────────────

    [Header("달리기")]
    [Tooltip("달리기 중 초당 정신력 소모량. 0이면 무제한.")]
    [SerializeField] private float sprintMentalCostPerSec = 2f;

    // ─────────────────────── 마우스 회전 설정 ───────────────────────

    [Header("마우스 조준 회전")]
    [Tooltip("캐릭터가 마우스 방향으로 회전하는 속도. 높을수록 즉각 반응.")]
    [SerializeField] private float rotationSpeed = 20f;

    [Tooltip("캐릭터 기준 이 거리 이내에 커서가 있으면 회전 무시 (떨림 방지)")]
    [SerializeField] private float minAimDistance = 0.5f;

    // ─────────────────────── 지면 감지 ───────────────────────

    [Header("지면 감지")]
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private LayerMask groundLayer      = ~0;

    [Header("낭떠러지 진입 차단")]
    [Tooltip("진행 방향 앞에 지면이 없으면 이동을 막음 (섬 가장자리 추락 방지)")]
    [SerializeField] private bool bBlockCliffEdges = true;
    [Tooltip("플레이어 앞 어느 거리의 지점을 검사할지")]
    [SerializeField] private float edgeCheckDistance = 0.6f;
    [Tooltip("검사 지점에서 아래로 쏘는 레이 길이 (내리막 경사 허용 범위)")]
    [SerializeField] private float edgeRayLength = 3f;

    [Header("낙하 복귀")]
    [Tooltip("플레이어 Y 가 이 값 아래로 떨어지면 마지막 안전 위치로 복귀")]
    [SerializeField] private float fallYThreshold = -10f;
    [Tooltip("안전 위치 기록 주기 (초). 지면 위에 있을 때만 기록")]
    [SerializeField] private float safeRecordInterval = 0.5f;

    // ─────────────────────── 내부 참조 ───────────────────────

    private Rigidbody    _rb;
    private PlayerStats  _stats;
    private Camera       _mainCamera;
    private Animator     _animator;
    private PlayerCombat _combat;

    // ─────────────────────── 입력 상태 ───────────────────────

    // 이번 프레임 이동 방향 (경로의 다음 코너 방향, 공격 중이거나 정지 상태면 zero)
    private Vector3 _vMoveDirection = Vector3.zero;
    private bool    _isSprinting;

    // 경로 탐색 이동 상태
    private NavMeshPath _navPath;
    private bool        _bHasPath = false;
    private int         _nCornerIndex = 0;
    private Vector3     _vPathDestination = Vector3.zero;
    private Vector3     _vPrevPosition = Vector3.zero;

    // 경로 탐색 상수
    private const float REPATH_DISTANCE     = 0.25f;  // 목적지가 이만큼 바뀌면 경로 재계산
    private const float ARRIVE_DISTANCE     = 0.2f;   // 코너/목적지 도착 판정 거리
    private const float NAVMESH_SNAP_RADIUS = 2f;     // 커서 지점의 NavMesh 스냅 반경
    private const float TELEPORT_DISTANCE   = 3f;     // 한 프레임 이동이 이 이상이면 텔레포트로 간주

    // 낙하 복귀 상태
    private Vector3 _lastSafePosition;
    private bool    _hasSafePosition = false;
    private float   _safeRecordTimer = 0f;

    // ─────────────────────── 공개 상태 ───────────────────────

    /// <summary>현재 달리는 중인지 (이동 중 + Shift 홀드)</summary>
    public bool IsSprinting => _isSprinting && _vMoveDirection.sqrMagnitude > 0.01f;

    /// <summary>현재 이동 속도</summary>
    public float CurrentSpeed => (IsSprinting ? sprintSpeed : walkSpeed) * (1f + _moveSpeedBonus) * MentalSpeedFactor;

    // 정신력 패널티 이동 배율 (stats 없으면 1)
    private float MentalSpeedFactor => _stats != null ? _stats.MentalMoveSpeedMultiplier : 1f;

    // 세트 효과 이동속도 보너스 (비율. 0.05 = 5% 증가)
    private float _moveSpeedBonus = 0f;

    /// <summary>이동속도 보너스 설정 (ArmorSetManager 에서 호출)</summary>
    public void SetMoveSpeedBonus(float bonus)
    {
        _moveSpeedBonus = bonus;
    }

    // ─────────────────────── 초기화 ───────────────────────

    private void Awake()
    {
        // 싱글턴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // DontDestroyOnLoad 는 루트 오브젝트에만 동작하므로 자식이면 루트로 분리
        if (transform.parent != null)
        {
            Debug.LogWarning("[PlayerController] 부모('" + transform.parent.name + "') 아래에 있어 루트로 분리함 - 씬에서 루트로 배치 권장");
            transform.SetParent(null);
        }

        // 모든 씬에서 플레이어(인벤토리/장비/스탯 단위)를 유지
        DontDestroyOnLoad(gameObject);

        _rb         = GetComponent<Rigidbody>();
        _stats      = GetComponent<PlayerStats>();
        _mainCamera = Camera.main;
        _animator   = GetComponentInChildren<Animator>();
        _combat     = GetComponent<PlayerCombat>();

        _navPath       = new NavMeshPath();
        _vPrevPosition = transform.position;

        _rb.freezeRotation = true;
    }

    // ─────────────────────── 업데이트 ───────────────────────

    private void Update()
    {
        // 달리기 — InputReader 에서 받음 (리바인딩 호환)
        bool sprintInput = InputReader.Instance != null && InputReader.Instance.SprintHeld;
        if (_stats != null && _stats.Mental <= 0f)
            _isSprinting = false;
        else
            _isSprinting = sprintInput;

        // 외부 텔레포트(집 문, 스폰 이동, 낙하 복귀) 감지 시 기존 경로 폐기
        DetectTeleport();

        // 우클릭 입력으로 경로 갱신 후 이동 방향 계산 (공격 중이면 즉시 이동 상태 해제)
        UpdateMoveDirection();

        UpdateRotation();
        HandleSprintMentalCost();
        RecordSafePosition();
        CheckFallRespawn();
        UpdateAnimator();

        _vPrevPosition = transform.position;
    }

    private void FixedUpdate()
    {
        Move();
    }

    // ─────────────────────── 이동 ───────────────────────

    private void Move()
    {
        // 공격 중이면 수평 속도 즉시 제거 (이동 상태 완전 탈출)
        if (_combat != null && _combat.IsAttacking == true)
        {
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
            return;
        }

        if (_vMoveDirection.sqrMagnitude < 0.01f)
        {
            // 입력 없으면 수평 속도 감쇠
            Vector3 vel = _rb.linearVelocity;
            _rb.linearVelocity = new Vector3(vel.x * 0.85f, vel.y, vel.z * 0.85f);
            return;
        }

        Vector3 moveDir = _vMoveDirection;

        // 낭떠러지 차단 — 앞에 지면이 없으면 해당 방향 성분 제거
        if (bBlockCliffEdges == true)
        {
            moveDir = FilterCliffDirection(moveDir);
            if (moveDir.sqrMagnitude < 0.01f)
            {
                Vector3 blockedVel = _rb.linearVelocity;
                _rb.linearVelocity = new Vector3(blockedVel.x * 0.85f, blockedVel.y, blockedVel.z * 0.85f);
                return;
            }
        }

        float   speed   = (IsSprinting ? sprintSpeed : walkSpeed) * (1f + _moveSpeedBonus) * MentalSpeedFactor;

        _rb.linearVelocity = new Vector3(
            moveDir.x * speed,
            _rb.linearVelocity.y,
            moveDir.z * speed);
    }

    // ─────────────────────── 이동 방향 계산 (NavMesh 경로 추종) ───────────────────────

    // 우클릭 입력으로 목적지 갱신 후 경로를 따라 이동 방향 계산 (공격 시작 시 경로 폐기)
    private void UpdateMoveDirection()
    {
        _vMoveDirection = Vector3.zero;

        // 공격 시작 즉시 이동 상태 완전 탈출 (홀드 중이었다면 공격 종료 후 재입력으로 자동 재개)
        if (_combat != null && _combat.IsAttacking == true)
        {
            ClearPath();
            return;
        }

        bool bMoveHeld = InputReader.Instance != null && InputReader.Instance.MoveHeld;
        if (bMoveHeld == true)
        {
            UpdateDestinationFromCursor();
        }

        FollowPath();
    }

    // 커서 지면 지점을 NavMesh 위로 스냅해 목적지로 설정 (변화가 작으면 재계산 생략)
    private void UpdateDestinationFromCursor()
    {
        if (TryGetCursorPoint(out Vector3 vCursorPoint) == false)
        {
            return;
        }

        if (NavMesh.SamplePosition(vCursorPoint, out NavMeshHit hit, NAVMESH_SNAP_RADIUS, NavMesh.AllAreas) == false)
        {
            return;
        }

        Vector3 vDestination = hit.position;

        // 커서가 사실상 플레이어 위치면 정지 (홀드 중 제자리 경로 재계산 방지)
        Vector3 vToDestination = vDestination - transform.position;
        vToDestination.y = 0f;
        if (vToDestination.sqrMagnitude < ARRIVE_DISTANCE * ARRIVE_DISTANCE)
        {
            ClearPath();
            return;
        }

        // 기존 목적지와 거의 같으면 경로 재계산 생략
        if (_bHasPath == true
            && (vDestination - _vPathDestination).sqrMagnitude < REPATH_DISTANCE * REPATH_DISTANCE)
        {
            return;
        }

        CalculatePathTo(vDestination);
    }

    // 현재 위치에서 목적지까지 NavMesh 경로 계산 (부분 경로 허용)
    private void CalculatePathTo(Vector3 _vDestination)
    {
        // 시작점도 NavMesh 위로 스냅 (플레이어가 메시를 살짝 벗어나 있어도 계산 가능하게)
        Vector3 vStart = transform.position;
        if (NavMesh.SamplePosition(vStart, out NavMeshHit startHit, NAVMESH_SNAP_RADIUS, NavMesh.AllAreas) == true)
        {
            vStart = startHit.position;
        }

        if (NavMesh.CalculatePath(vStart, _vDestination, NavMesh.AllAreas, _navPath) == false
            || _navPath.status == NavMeshPathStatus.PathInvalid)
        {
            ClearPath();
            return;
        }

        _vPathDestination = _vDestination;
        _bHasPath = true;
        // corners[0] 은 시작점이므로 다음 코너부터 추종
        _nCornerIndex = 1;
    }

    // 경로의 코너를 순서대로 따라가며 이동 방향 산출, 최종 도착 시 경로 폐기
    private void FollowPath()
    {
        if (_bHasPath == false)
        {
            return;
        }

        Vector3[] vCorners = _navPath.corners;

        // 현재 코너에 충분히 가까우면 다음 코너로 진행
        while (_nCornerIndex < vCorners.Length)
        {
            Vector3 vToCorner = vCorners[_nCornerIndex] - transform.position;
            vToCorner.y = 0f;

            if (vToCorner.sqrMagnitude > ARRIVE_DISTANCE * ARRIVE_DISTANCE)
            {
                break;
            }
            _nCornerIndex++;
        }

        if (_nCornerIndex >= vCorners.Length)
        {
            ClearPath();
            return;
        }

        Vector3 vDirection = vCorners[_nCornerIndex] - transform.position;
        vDirection.y = 0f;
        _vMoveDirection = vDirection.normalized;
    }

    // 경로/목적지 폐기 (정지 상태로 전환)
    private void ClearPath()
    {
        _bHasPath = false;
        _nCornerIndex = 0;
    }

    // 한 프레임에 기준 거리 이상 이동했으면 텔레포트로 보고 경로 폐기 (옛 목적지로 회귀 방지)
    private void DetectTeleport()
    {
        if ((transform.position - _vPrevPosition).sqrMagnitude > TELEPORT_DISTANCE * TELEPORT_DISTANCE)
        {
            ClearPath();
        }
    }

    // 커서의 지면 평면상 월드 지점 계산
    private bool TryGetCursorPoint(out Vector3 _vPoint)
    {
        _vPoint = Vector3.zero;

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                return false;
            }
        }

        Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float fDistance) == false)
        {
            return false;
        }

        _vPoint = ray.GetPoint(fDistance);
        return true;
    }

    // ─────────────────────── 낭떠러지 진입 차단 ───────────────────────

    // 진행 방향 앞 지면 검사 — 없으면 X/Z 축별로 걸러 가장자리를 따라 미끄러지게 함
    private Vector3 FilterCliffDirection(Vector3 _vMoveDir)
    {
        if (HasGroundAhead(_vMoveDir) == true)
        {
            return _vMoveDir;
        }

        Vector3 vFiltered = Vector3.zero;

        Vector3 vDirX = new Vector3(_vMoveDir.x, 0f, 0f);
        if (vDirX.sqrMagnitude > 0.0001f && HasGroundAhead(vDirX.normalized) == true)
        {
            vFiltered += vDirX;
        }

        Vector3 vDirZ = new Vector3(0f, 0f, _vMoveDir.z);
        if (vDirZ.sqrMagnitude > 0.0001f && HasGroundAhead(vDirZ.normalized) == true)
        {
            vFiltered += vDirZ;
        }

        if (vFiltered.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }
        return vFiltered.normalized;
    }

    // 지정 방향 앞 지점 아래에 지면이 있는지 검사
    private bool HasGroundAhead(Vector3 _vDir)
    {
        Vector3 vOrigin = transform.position + _vDir * edgeCheckDistance + Vector3.up * 1f;
        return Physics.Raycast(vOrigin, Vector3.down, edgeRayLength, groundLayer,
                               QueryTriggerInteraction.Ignore);
    }

    // ─────────────────────── 회전 ───────────────────────

    // 회전 분기: 공격 중에는 커서 방향, 이동 중에는 진행 방향, 정지 시 회전 유지
    private void UpdateRotation()
    {
        bool bAttacking = _combat != null && _combat.IsAttacking == true;
        if (bAttacking == true)
        {
            RotateTowardsMouse();
            return;
        }

        RotateTowardsMoveDirection();
    }

    // 이동 중 진행 방향(경로 방향)을 바라보도록 회전
    private void RotateTowardsMoveDirection()
    {
        if (_vMoveDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion qTargetRot = Quaternion.LookRotation(_vMoveDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, qTargetRot, rotationSpeed * Time.deltaTime);
    }

    /// <summary>공격 시작 시 커서 방향으로 즉시 회전 (PlayerCombat 호출, 같은 프레임 판정이 조준과 일치하도록)</summary>
    public void SnapRotationToCursor()
    {
        if (TryGetCursorPoint(out Vector3 vCursorPoint) == false)
        {
            return;
        }

        Vector3 vDirection = vCursorPoint - transform.position;
        vDirection.y = 0f;

        // 커서가 몸 위면 회전 생략 (조준 방향 불명확)
        if (vDirection.sqrMagnitude < minAimDistance * minAimDistance)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(vDirection.normalized);
    }

    private void RotateTowardsMouse()
    {
        if (TryGetCursorPoint(out Vector3 vCursorPoint) == false) return;

        Vector3 vDirection = vCursorPoint - transform.position;
        vDirection.y = 0f;

        // 너무 가까우면 회전 무시 (떨림 방지)
        if (vDirection.sqrMagnitude < minAimDistance * minAimDistance) return;

        Quaternion targetRot  = Quaternion.LookRotation(vDirection);
        transform.rotation    = Quaternion.Slerp(
            transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    // ─────────────────────── 달리기 정신력 소모 ───────────────────────

    private void HandleSprintMentalCost()
    {
        if (IsSprinting == false) return;
        if (sprintMentalCostPerSec <= 0f) return;
        if (_stats == null) return;

        _stats.ConsumeMental(sprintMentalCostPerSec * Time.deltaTime);

        // 정신력 소진 시 달리기 강제 해제
        if (_stats.Mental <= 0f)
            _isSprinting = false;
    }

    // ─────────────────────── 지면 감지 ───────────────────────

    // 이동 상태를 Animator 에 전달 (Speed: 입력 크기, IsSprinting: 달리기 여부)
    // 이동 클립(idle/walk/run)이 임포트되면 별도 코드 없이 동작
    private void UpdateAnimator()
    {
        if (_animator == null) return;

        float fSpeed = _vMoveDirection.sqrMagnitude < 0.01f ? 0f : 1f;
        _animator.SetFloat("Speed", fSpeed);
        _animator.SetBool("IsSprinting", IsSprinting);
    }

    public bool IsGrounded()
    {
        return Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            groundCheckDistance + 0.1f,
            groundLayer);
    }

    // ─────────────────────── 낙하 복귀 ───────────────────────

    // 지면 위에 있을 때 주기적으로 안전 위치 기록
    private void RecordSafePosition()
    {
        _safeRecordTimer += Time.deltaTime;
        if (_safeRecordTimer < safeRecordInterval) return;
        _safeRecordTimer = 0f;

        if (IsGrounded() == false) return;

        _lastSafePosition = transform.position;
        _hasSafePosition  = true;
    }

    // 기준 높이 아래로 떨어지면 마지막 안전 위치로 복귀
    private void CheckFallRespawn()
    {
        if (_hasSafePosition == false) return;
        if (transform.position.y > fallYThreshold) return;

        _rb.linearVelocity  = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        transform.position  = _lastSafePosition + Vector3.up * 0.5f;

        Debug.LogWarning("[PlayerController] 낙하 감지 - 마지막 안전 위치로 복귀");
    }

    // ─────────────────────── 에디터 기즈모 ───────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 지면 감지 레이
        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            transform.position + Vector3.up * 0.1f,
            transform.position + Vector3.up * 0.1f + Vector3.down * (groundCheckDistance + 0.1f));

        // 마우스 조준 최소 거리
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, minAimDistance);
    }
#endif
}