using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 15, -8);
    [SerializeField] private float smoothSpeed = 5f;

    // 추적 사용 여부 (집 안 등에서 화면 고정 시 false)
    private bool _bFollowEnabled = true;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        FindPlayer();
    }

    // 씬 전환 시 새 플레이어 자동 탐색
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 이전 씬 플레이어 참조 초기화 후 재탐색
        target = null;
        FindPlayer();
    }

    private void FindPlayer()
    {
        if (target != null) return;

        if (PlayerController.Instance != null)
        {
            target = PlayerController.Instance.transform;
            return;
        }

        // 싱글턴 없으면 태그로 탐색
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            target = playerObj.transform;
    }

    private void LateUpdate()
    {
        if (_bFollowEnabled == false) return;
        if (target == null) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position, desired, smoothSpeed * Time.deltaTime);
    }

    // 대상 위치로 즉시 이동 (텔레포트 직후 호출, Lerp 가로지름 방지)
    public void SnapToTarget()
    {
        if (target == null) return;

        transform.position = target.position + offset;
    }

    // 추적 켜기/끄기 (집 안 화면 고정 등)
    // 끄면 현재 카메라 위치 그대로 멈춤, 켜면 다시 플레이어 추적
    public void SetFollowEnabled(bool _bEnabled)
    {
        _bFollowEnabled = _bEnabled;
    }

    // 지정 오브젝트 위치로 카메라를 옮기고 추적 정지 (회전은 마을과 동일하게 현재 각도 유지)
    // 집 안 시작 등 고정 위치가 필요할 때 호출
    public void MoveToFixedPoint(Transform _trPoint)
    {
        if (_trPoint == null) return;

        transform.position = _trPoint.position;
        _bFollowEnabled = false;
    }
}