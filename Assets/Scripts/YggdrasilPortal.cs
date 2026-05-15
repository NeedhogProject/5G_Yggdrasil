using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 중앙 위그드라실 포탈
///
/// [기획 반영]
/// - 마을과 1층을 이어주는 중앙 줄기
/// - 열쇠 불필요
/// - 마을에서 E키 → 1층 입장
/// - 1층에서 E키 → 마을 복귀
/// - 각 씬(마을/1층)에 1개씩 배치
/// </summary>
public class YggdrasilPortal : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("현재 이 포탈이 마을 쪽인지 1층 쪽인지")]
    [SerializeField] private bool isTownSide = true;

    [Header("상호작용 범위")]
    [SerializeField] private float interactRange = 2.5f;

    [Header("연출")]
    [SerializeField] private GameObject enterVFX;
    [SerializeField] private float transitionDelay = 0.5f;

    private bool _playerInRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") == false) return;
        _playerInRange = true;
        string hint = isTownSide ? "E - 위그드라실로 입장" : "E - 마을로 복귀";
        Debug.Log($"[YggdrasilPortal] {hint}");
        // TODO: HUD 힌트 표시
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") == false) return;
        _playerInRange = false;
        // TODO: HUD 힌트 숨김
    }

    private void Update()
    {
        if (_playerInRange == false) return;
        if (Keyboard.current.eKey.wasPressedThisFrame == false) return;

        Interact();
    }

    private void Interact()
    {
        if (enterVFX != null)
            Instantiate(enterVFX, transform.position, Quaternion.identity);

        Invoke(nameof(DoTransition), transitionDelay);
    }

    private void DoTransition()
    {
        if (isTownSide)
        {
            // 마을 → 1층
            FloorManager.Instance?.LoadFloor(1);
            GameManager.Instance?.SyncFloor(1);
            Debug.Log("[YggdrasilPortal] 마을 → 1층");
        }
        else
        {
            // 1층 → 마을
            GameManager.Instance?.ReturnToTown();
            Debug.Log("[YggdrasilPortal] 1층 → 마을");
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isTownSide
            ? new Color(0f, 1f, 0.5f, 0.4f)
            : new Color(0.5f, 0f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}
