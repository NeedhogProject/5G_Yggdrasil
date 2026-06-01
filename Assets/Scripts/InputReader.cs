using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 입력 중앙 관리 싱글턴
/// 모든 키 입력을 Input Actions 를 통해 받아 전역으로 제공
///
/// [키 리바인딩 호환]
/// 모든 입력이 PlayerInputActions 에셋의 액션을 거치므로
/// 타이틀에서 키를 변경하면 전체에 자동 반영됨
///
/// [씬 설정]
/// GameCore 같은 항상 존재하는 오브젝트에 부착
/// PlayerInput 컴포넌트 함께 부착, Behavior: Invoke Unity Events 또는 Send Messages
/// </summary>
public class InputReader : MonoBehaviour
{
    public static InputReader Instance { get; private set; }

    // ─────────────────────── 이동 / 달리기 ───────────────────────

    /// <summary>이동 입력 (WASD / 스틱)</summary>
    public Vector2 MoveInput { get; private set; }

    /// <summary>달리기 홀드 여부</summary>
    public bool SprintHeld { get; private set; }

    // ─────────────────────── 액션 트리거 (이번 프레임 눌림) ───────────────────────

    /// <summary>공격 입력 (이번 프레임)</summary>
    public bool AttackPressed { get; private set; }

    /// <summary>상호작용 입력 (이번 프레임) — E</summary>
    public bool InteractPressed { get; private set; }

    /// <summary>인벤토리 토글 (이번 프레임) — I</summary>
    public bool InventoryPressed { get; private set; }

    /// <summary>마을맵 토글 (이번 프레임) — M</summary>
    public bool MapPressed { get; private set; }

    /// <summary>취소/닫기 (이번 프레임) — ESC</summary>
    public bool CancelPressed { get; private set; }

    // ─────────────────────── UI 차단 플래그 ───────────────────────

    /// <summary>UI 가 열려 게임플레이 입력을 막아야 할 때 true</summary>
    public bool UIBlocking { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        // 트리거성 입력은 프레임 끝에 초기화 (한 프레임만 true)
        AttackPressed    = false;
        InteractPressed  = false;
        InventoryPressed = false;
        MapPressed       = false;
        CancelPressed    = false;
    }

    // ─────────────────────── PlayerInput 콜백 (Send Messages) ───────────────────────

    public void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        SprintHeld = value.isPressed;
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && UIBlocking == false)
            AttackPressed = true;
    }

    public void OnInteract(InputValue value)
    {
        if (value.isPressed && UIBlocking == false)
            InteractPressed = true;
    }

    public void OnInventory(InputValue value)
    {
        if (value.isPressed)
            InventoryPressed = true;
    }

    public void OnMap(InputValue value)
    {
        if (value.isPressed)
            MapPressed = true;
    }

    public void OnCancel(InputValue value)
    {
        if (value.isPressed)
            CancelPressed = true;
    }
}
