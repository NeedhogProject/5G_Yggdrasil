using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 15, -8);
    [SerializeField] private float smoothSpeed = 5f;

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
        if (target == null) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}