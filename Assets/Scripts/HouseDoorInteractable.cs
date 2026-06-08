// HouseDoorInteractable.cs
// 마을과 플레이어 집 내부를 같은 씬 안에서 오가는 이동 지점
// 집 입구(마을 쪽)와 집 안 문(실내 쪽)에 각각 부착하고, 상대편 도착 지점을 targetPoint 로 연결한다.
// 씬 전환이 아니라 플레이어 오브젝트의 위치만 옮긴다. Collider(isTrigger=true) 필수.

using UnityEngine;

public class HouseDoorInteractable : MonoBehaviour
{
    [Header("이동 도착 지점")]
    [Tooltip("상호작용 시 플레이어가 이동할 위치. 집 입구면 실내 지점, 실내 문이면 집 앞 지점을 연결")]
    [SerializeField] private Transform targetPoint;

    [Header("UI 힌트")]
    [Tooltip("범위에 들어오면 켜질 안내 오브젝트. 없으면 비워둠")]
    [SerializeField] private GameObject hintObject;

    private bool _playerInRange = false;
    private GameObject _playerObj = null;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }
        _playerInRange = true;
        _playerObj = other.gameObject;

        SetHintVisible(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }
        _playerInRange = false;
        _playerObj = null;

        SetHintVisible(false);
    }

    private void Update()
    {
        if (_playerInRange == false)
        {
            return;
        }
        if (InputReader.Instance == null || InputReader.Instance.InteractPressed == false)
        {
            return;
        }

        Teleport();
    }

    // 플레이어를 도착 지점으로 이동
    private void Teleport()
    {
        if (targetPoint == null)
        {
            Debug.LogWarning("[HouseDoorInteractable] 도착 지점(targetPoint)이 연결되지 않음");
            return;
        }
        if (_playerObj == null)
        {
            return;
        }

        SetHintVisible(false);

        // Rigidbody 가 있으면 관성으로 미끄러지지 않도록 속도를 먼저 제거
        Rigidbody playerRb = _playerObj.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        _playerObj.transform.position = targetPoint.position;

        AudioManager.Instance?.PlaySFX(SFXClip.DoorOpen);
    }

    // 힌트 표시 또는 숨김 (중복 호출 방지)
    private void SetHintVisible(bool visible)
    {
        if (hintObject == null)
        {
            return;
        }
        if (hintObject.activeSelf == visible)
        {
            return;
        }
        hintObject.SetActive(visible);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 상호작용 범위
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, 2f);

        // 도착 지점 연결선
        if (targetPoint != null)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.7f);
            Gizmos.DrawLine(transform.position, targetPoint.position);
            Gizmos.DrawWireSphere(targetPoint.position, 0.4f);
        }
    }
#endif
}
