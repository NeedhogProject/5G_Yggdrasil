// StorageInteractable.cs
// 창고 오브젝트 — 플레이어가 일정 거리 안에 오면 힌트 표시, 창고 키로 열기/닫기
// 거리 기반 감지 (콜라이더 트리거 불필요)

using UnityEngine;
using UnityEngine.InputSystem;

public class StorageInteractable : MonoBehaviour
{
    [Header("상호작용 범위")]
    [SerializeField] private float interactRange = 1.5f;

    [Header("UI 힌트")]
    [SerializeField] private GameObject hintObject;

    private Transform _player = null;
    private bool _playerInRange = false;

    // 상호작용 범위 안에 창고가 하나라도 있는지 (인벤 드롭 키와 창고 키 충돌 방지용)
    private static int _playersInRangeCount = 0;
    public static bool IsPlayerNearStorage
    {
        get { return _playersInRangeCount > 0; }
    }

    // 이 창고가 전역 카운트에 범위 진입으로 반영돼 있는지
    private bool _countedInRange = false;

    private void Start()
    {
        SetHintVisible(false);
    }

    private void OnDisable()
    {
        // 씬 전환/비활성화 시 전역 카운트가 어긋나지 않도록 정리
        if (_countedInRange == true)
        {
            _playersInRangeCount--;
            _countedInRange = false;
        }
    }

    private void Update()
    {
        // 플레이어 참조 확보 (한 번만)
        if (_player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _player = playerObj.transform;
            }
        }

        // 거리로 범위 안인지 판단
        _playerInRange = false;
        if (_player != null)
        {
            float distance = Vector3.Distance(transform.position, _player.position);
            if (distance <= interactRange)
            {
                _playerInRange = true;
            }
        }

        // 범위 진입/이탈 시 전역 카운트 갱신 (InventoryUI 드롭 차단 판단용)
        if (_playerInRange != _countedInRange)
        {
            if (_playerInRange == true)
            {
                _playersInRangeCount++;
            }
            else
            {
                _playersInRangeCount--;
            }
            _countedInRange = _playerInRange;
        }

        // 힌트는 범위 안 + 창고 닫힘 + 설정창 닫힘 상태에서만 표시 (설정창 위로 힌트가 보이지 않게)
        bool storageClosed = StorageUI.Instance == null || StorageUI.Instance.IsOpen == false;
        bool settingsClosed = PauseMenuManager.Instance == null || PauseMenuManager.Instance.IsSettingOpen == false;
        SetHintVisible(_playerInRange == true && storageClosed == true && settingsClosed == true);

        if (_playerInRange == false)
        {
            return;
        }

        // 창고 키(기본 G, 설정 시 리바인딩 반영)로 열기/닫기
        if (WasStorageKeyPressed() == true)
        {
            if (StorageUI.Instance == null)
            {
                return;
            }

            if (StorageUI.Instance.IsOpen == true)
            {
                StorageUI.Instance.CloseStorage();
            }
            else
            {
                StorageUI.Instance.OpenStorage();
            }
        }
    }

    // 창고 키 입력 확인 (Storage 액션, 없으면 기본 G 폴백)
    private bool WasStorageKeyPressed()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        if (KeyBindingManager.Instance != null)
        {
            InputAction storageAction = KeyBindingManager.Instance.FindAction("Storage");
            if (storageAction != null)
            {
                return storageAction.WasPressedThisFrame();
            }
        }

        return Keyboard.current.gKey.wasPressedThisFrame;
    }

    // 힌트 표시/숨김 (중복 호출 방지)
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
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}