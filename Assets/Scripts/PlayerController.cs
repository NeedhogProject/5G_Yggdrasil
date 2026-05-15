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

    // ─────────────────────── 내부 참조 ───────────────────────

    private Rigidbody   _rb;
    private PlayerStats _stats;
    private Camera      _mainCamera;

    // ─────────────────────── 입력 상태 ───────────────────────

    private Vector2 _moveInput;
    private bool    _isSprinting;

    // ─────────────────────── 공개 상태 ───────────────────────

    /// <summary>현재 달리는 중인지 (이동 중 + Shift 홀드)</summary>
    public bool IsSprinting => _isSprinting && _moveInput.sqrMagnitude > 0.01f;

    /// <summary>현재 이동 속도</summary>
    public float CurrentSpeed => IsSprinting ? sprintSpeed : walkSpeed;

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

        _rb         = GetComponent<Rigidbody>();
        _stats      = GetComponent<PlayerStats>();
        _mainCamera = Camera.main;

        _rb.freezeRotation = true;
    }

    // ─────────────────────── 업데이트 ───────────────────────

    private void Update()
    {
        // Shift 키 상태를 매 프레임 직접 체크 — 누르는 동안만 true, 떼면 즉시 false
        if (_stats != null && _stats.Mental <= 0f)
            _isSprinting = false;
        else
            _isSprinting = Keyboard.current.leftShiftKey.isPressed;

        RotateTowardsMouse();
        HandleSprintMentalCost();
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

        // 탑뷰 월드 축 기준 — X 입력 → 월드 Right, Y 입력 → 월드 Forward
        Vector3 moveDir = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
        float   speed   = IsSprinting ? sprintSpeed : walkSpeed;

        _rb.linearVelocity = new Vector3(
            moveDir.x * speed,
            _rb.linearVelocity.y,
            moveDir.z * speed);
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

    public bool IsGrounded()
    {
        return Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            groundCheckDistance + 0.1f,
            groundLayer);
    }

    // ─────────────────────── New Input System 콜백 ───────────────────────

    /// <summary>Move 액션 콜백 (WASD / 좌측 스틱)</summary>
    public void OnMove(InputValue value)
    {
        _moveInput = value.Get<Vector2>();
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