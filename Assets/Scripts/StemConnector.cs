using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 줄기 방향 (동서남북)
/// </summary>
public enum StemDirection
{
    North,  // 북
    South,  // 남
    East,   // 동
    West    // 서
}

/// <summary>
/// 줄기 이동 모드
/// </summary>
public enum StemMode
{
    DownOnly,       // 1층: 하강만 가능 (열쇠 필요)
    UpOrDown,       // 2~3층: 열쇠 삽입 후 위/아래 선택
    DownOnlyFixed,  // 3→4층: 고정 줄기, 하강만 (열쇠 필요)
}

/// <summary>
/// 개별 줄기 오브젝트
///
/// [기획 반영]
/// - 1층    : 열쇠 삽입 → 2층으로 하강만 가능
/// - 2~3층  : 열쇠 삽입 → 위층 or 아래층 선택 UI 표시
/// - 3→4층  : 줄기 1개 고정, 열쇠 삽입 → 4층으로 하강만
/// - 시각적 구분 없음 (탐색 필요)
/// - 열쇠 삽입 시 구멍 연출 후 이동
/// </summary>
public class StemConnector : MonoBehaviour
{
    // ─────────────────────── 설정 ───────────────────────

    [Header("줄기 설정")]
    [SerializeField] private StemDirection direction = StemDirection.North;
    [SerializeField] private StemMode      stemMode  = StemMode.DownOnly;

    [Header("상호작용 범위")]
    [SerializeField] private float interactRange = 2f;

    [Header("구멍 연출")]
    [SerializeField] private GameObject holeVFX;
    [SerializeField] [Range(0f, 5f)] private float transitionDelay = 1.5f;

    [Header("방향 선택 UI (UpOrDown 모드 전용)")]
    [Tooltip("위/아래 선택 패널 (2~3층 전용)")]
    [SerializeField] private GameObject directionChoicePanel;

    // ─────────────────────── 상태 ───────────────────────

    public bool IsUnlocked { get; private set; } = false;
    private bool _playerInRange = false;
    private GameObject _playerObj;

    public StemDirection Direction => direction;
    public StemMode      Mode      => stemMode;

    // ─────────────────────── 트리거 감지 ───────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        _playerObj     = other.gameObject;
        Debug.Log($"[StemConnector] {direction} 줄기 범위 진입");
        // TODO: HUD 힌트 표시 ("E - 열쇠 삽입")
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        _playerObj     = null;
        HideDirectionChoice();
    }

    // ─────────────────────── E키 상호작용 ───────────────────────

    private void Update()
    {
        if (!_playerInRange) return;
        if (IsUnlocked) return;
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        StemManager.Instance?.TryInsertKey(_playerObj, this);
    }

    // ─────────────────────── 열쇠 삽입 성공 ───────────────────────

    /// <summary>StemManager 에서 열쇠 확인 후 호출</summary>
    public void OnKeyInserted()
    {
        IsUnlocked = true;

        switch (stemMode)
        {
            case StemMode.DownOnly:
            case StemMode.DownOnlyFixed:
                // 바로 하강 연출 시작
                PlayHoleVFX();
                Invoke(nameof(GoDown), transitionDelay);
                break;

            case StemMode.UpOrDown:
                // 위/아래 선택 UI 표시
                ShowDirectionChoice();
                break;
        }
    }

    // ─────────────────────── 방향 선택 UI (2~3층) ───────────────────────

    private void ShowDirectionChoice()
    {
        if (directionChoicePanel != null)
        {
            directionChoicePanel.SetActive(true);
            Debug.Log("[StemConnector] 방향 선택 UI 표시 (위/아래)");
        }
        else
        {
            // UI 미설정 시 임시: 로그로 선택지 표시
            Debug.Log("[StemConnector] 방향 선택 — UI 미설정. 기본 하강 처리");
            GoDown();
        }
    }

    private void HideDirectionChoice()
    {
        if (directionChoicePanel != null)
            directionChoicePanel.SetActive(false);
    }

    /// <summary>위로 이동 버튼 — UI 버튼 OnClick 에 연결</summary>
    public void OnChooseUp()
    {
        HideDirectionChoice();
        PlayHoleVFX();
        Invoke(nameof(GoUp), transitionDelay);
    }

    /// <summary>아래로 이동 버튼 — UI 버튼 OnClick 에 연결</summary>
    public void OnChooseDown()
    {
        HideDirectionChoice();
        PlayHoleVFX();
        Invoke(nameof(GoDown), transitionDelay);
    }

    // ─────────────────────── 실제 이동 ───────────────────────

    private void GoDown()
    {
        Debug.Log($"[StemConnector] {direction} 줄기 → 아래층으로");
        FloorManager.Instance?.GoDownOneFloor();
    }

    private void GoUp()
    {
        Debug.Log($"[StemConnector] {direction} 줄기 → 위층으로");
        FloorManager.Instance?.GoUpOneFloor();
    }

    // ─────────────────────── 구멍 연출 ───────────────────────

    private void PlayHoleVFX()
    {
        if (holeVFX != null)
            Instantiate(holeVFX, transform.position, Quaternion.identity);
        Debug.Log($"[StemConnector] {direction} 줄기 — 구멍 연출 시작");
    }

    // ─────────────────────── 에디터 기즈모 ───────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = IsUnlocked
            ? new Color(0f, 1f, 0f, 0.4f)
            : new Color(1f, 0.3f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactRange);

        // 방향 표시
        Gizmos.color = Color.yellow;
        Vector3 dir = direction switch
        {
            StemDirection.North => Vector3.forward,
            StemDirection.South => Vector3.back,
            StemDirection.East  => Vector3.right,
            StemDirection.West  => Vector3.left,
            _                   => Vector3.forward
        };
        Gizmos.DrawRay(transform.position, dir * 2f);
    }
#endif
}
