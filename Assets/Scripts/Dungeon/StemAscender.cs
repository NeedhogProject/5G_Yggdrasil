using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 위로 올라가는 줄기 오브젝트
///
/// [기획 반영]
/// - 각 층 중앙 위그드라실 줄기 근처에 배치
/// - E키 상호작용 → 한 층 위로 이동
/// - 1층에서 사용 시 마을로 복귀 (FloorManager.GoUpOneFloor 에서 처리)
/// - StemConnector(내려가기)와 동일한 방식
///
/// [씬 설정]
/// - 위그드라실 줄기 오브젝트에 부착
/// - Collider(isTrigger=true) 부착
/// </summary>
public class StemAscender : MonoBehaviour
{
    // ─────────────────────── 설정 ───────────────────────

    [Header("상호작용 범위")]
    [SerializeField] private float interactRange = 2f;

    [Header("UI 힌트 메시지")]
    [Tooltip("플레이어가 범위 안에 들어왔을 때 표시할 메시지")]
    [SerializeField] private string interactHint = "E - 위로 올라가기";

    // ─────────────────────── 상태 ───────────────────────

    private bool _playerInRange = false;
    private GameObject _playerObj;

    // ─────────────────────── 트리거 감지 ───────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") == false) return;
        _playerInRange = true;
        _playerObj     = other.gameObject;

        // TODO: HUD 에 상호작용 힌트 표시
        Debug.Log($"[StemAscender] {interactHint}");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") == false) return;
        _playerInRange = false;
        _playerObj     = null;

        // TODO: HUD 힌트 숨김
    }

    // ─────────────────────── E키 상호작용 ───────────────────────

    private void Update()
    {
        if (_playerInRange == false) return;
        if (Keyboard.current.eKey.wasPressedThisFrame == false) return;

        GoUp();
    }

    private void GoUp()
    {
        int currentFloor = FloorManager.Instance?.CurrentFloor ?? 1;

        if (currentFloor <= 1)
            Debug.Log("[StemAscender] 1층 → 마을로 복귀");
        else
            Debug.Log($"[StemAscender] {currentFloor}층 → {currentFloor - 1}층으로 이동");

        FloorManager.Instance?.GoUpOneFloor();
    }

    // ─────────────────────── 에디터 기즈모 ───────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}
