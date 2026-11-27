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

    [Header("강조 표시")]
    [Tooltip("범위에 들어오면 색이 깜빡일 오브젝트들(여러 개 가능, 자식 포함). 비우면 이 오브젝트 자신. 지정하면 거리도 이 오브젝트들 기준으로 잰다")]
    [SerializeField] private GameObject[] highlightObjects;

    // 거리 계산용으로 모아둔 렌더러 (처음 한 번만 모음)
    private Renderer[] _centerRenderers = null;

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

    // 거리 계산 기준점. 모델 기준점(pivot)이 집 원점 등 엉뚱한 곳에 있어도 상자 모양의 실제 중심을 쓴다
    private Vector3 GetInteractCenter()
    {
        // 강조할 모델을 지정했으면 그 모델 기준, 아니면 이 오브젝트와 자식 기준
        if (_centerRenderers == null || Application.isPlaying == false)
        {
            _centerRenderers = InteractOutlineEffect.CollectRenderers(highlightObjects);
            if (_centerRenderers.Length == 0)
            {
                _centerRenderers = GetComponentsInChildren<Renderer>();
            }
        }
        Renderer[] renderers = _centerRenderers;
        bool hasBounds = false;
        Bounds bounds = new Bounds(transform.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }
            // 3D 메시만 사용 (힌트 글자, 파티클 등은 제외)
            if ((renderer is MeshRenderer || renderer is SkinnedMeshRenderer) == false)
            {
                continue;
            }
            if (renderer.GetComponent<TMPro.TMP_Text>() != null)
            {
                continue;
            }

            if (hasBounds == false)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (hasBounds == false)
        {
            return transform.position;
        }
        return bounds.center;
    }

    private void Start()
    {
        SetHintVisible(false);
    }

    private void OnDisable()
    {
        // 창고가 꺼질 때 테두리가 남지 않도록 정리
        InteractOutlineEffect.Hide(transform);

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
            // 상자 모양의 실제 중심 기준, 높이 차이는 빼고 바닥 평면 거리로 잰다
            Vector3 center = GetInteractCenter();
            Vector3 flatOffset = _player.position - center;
            flatOffset.y = 0f;
            float distance = flatOffset.magnitude;
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
        // 힌트와 함께 상자 색도 깜빡이게 (힌트 오브젝트가 없어도 동작)
        if (visible == true)
        {
            InteractOutlineEffect.Show(transform, highlightObjects);
        }
        else
        {
            InteractOutlineEffect.Hide(transform);
        }

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
        Gizmos.DrawWireSphere(GetInteractCenter(), interactRange);
    }
#endif
}