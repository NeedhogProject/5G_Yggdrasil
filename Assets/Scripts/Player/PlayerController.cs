using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 이동 컨트롤러 — 하데스/디아블로2 스타일 탑뷰
///
/// - 이동  : WASD (월드 축 기준)
/// - 회전  : 마우스 커서 방향으로 캐릭터가 항상 바라봄
/// - 달리기: Shift 홀드 (누르는 동안만 유지, 정신력 소모)
///
/// [컴포넌트 설정]
/// 1. Rigidbody → Constraints → Freeze Rotation X/Y/Z 체크
/// 2. Capsule Collider 부착
/// 3. PlayerInput → Behavior → Send Messages
/// 4. Input Actions 에 Move(Vector2), Sprint(Button) 액션 등록
/// 5. 탑뷰 카메라는 플레이어 정면 위에서 수직으로 내려다보는 구도 권장
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

    private Rigidbody   _rb;
    private PlayerStats _stats;
    private Camera      _mainCamera;
    private Animator    _animator;

    // ─────────────────────── 입력 상태 ───────────────────────

    private Vector2 _moveInput;
    private bool    _isSprinting;

    // 낙하 복귀 상태
    private Vector3 _lastSafePosition;
    private bool    _hasSafePosition = false;
    private float   _safeRecordTimer = 0f;

    // ─────────────────────── 공개 상태 ───────────────────────

    /// <summary>현재 달리는 중인지 (이동 중 + Shift 홀드)</summary>
    public bool IsSprinting => _isSprinting && _moveInput.sqrMagnitude > 0.01f;

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

        // 이동 입력도 InputReader 에서 받음
        if (InputReader.Instance != null)
            _moveInput = InputReader.Instance.MoveInput;

        RotateTowardsMouse();
        HandleSprintMentalCost();
        RecordSafePosition();
        CheckFallRespawn();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        Move();
    }

    // ─────────────────────── 이동 ───────────────────────

    private void Move()
    {
        if (_moveInput.sqrMagnitude < 0.01f)
        {
            // 입력 없으면 수평 속도 감쇠
            Vector3 vel = _rb.linearVelocity;
            _rb.linearVelocity = new Vector3(vel.x * 0.85f, vel.y, vel.z * 0.85f);
            return;
        }

        // 카메라 기준 이동 — 카메라가 바라보는 방향을 화면 기준 전진으로 사용
        Vector3 camForward = Vector3.forward;
        Vector3 camRight   = Vector3.right;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera != null)
        {
            // 카메라의 forward/right 를 수평면(XZ)에 투영
            camForward = _mainCamera.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            camRight = _mainCamera.transform.right;
            camRight.y = 0f;
            camRight.Normalize();
        }

        Vector3 moveDir = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;

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

    // ─────────────────────── 마우스 커서 방향으로 회전 ───────────────────────

    private void RotateTowardsMouse()
    {
        if (_mainCamera == null) return;

        // 마우스 스크린 좌표 → 월드 좌표 (플레이어 y 평면)
        Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float distance) == false) return;

        Vector3 worldMousePos = ray.GetPoint(distance);
        Vector3 direction     = worldMousePos - transform.position;
        direction.y           = 0f;

        // 너무 가까우면 회전 무시 (떨림 방지)
        if (direction.sqrMagnitude < minAimDistance * minAimDistance) return;

        Quaternion targetRot  = Quaternion.LookRotation(direction);
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

        float fSpeed = _moveInput.sqrMagnitude < 0.01f ? 0f : _moveInput.magnitude;
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