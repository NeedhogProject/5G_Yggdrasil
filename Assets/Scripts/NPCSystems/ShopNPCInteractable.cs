// ShopNPCInteractable.cs
// 상인 NPC(벨라) — 플레이어가 근처에 오면 힌트 표시, 상호작용 키로 상점 열기
// 거리 기반 감지 (콜라이더 트리거 불필요)

using UnityEngine;
using UnityEngine.InputSystem;

public class ShopNPCInteractable : MonoBehaviour
{
    [Header("상호작용 범위")]
    [SerializeField] private float interactRange = 2.5f;

    [Header("참조")]
    [SerializeField] private ShopSystem shopSystem;

    [Header("UI 힌트")]
    [SerializeField] private GameObject hintObject;

    private Transform _player = null;
    private bool _playerInRange = false;

    private void Awake()
    {
        if (shopSystem == null)
        {
            shopSystem = FindFirstObjectByType<ShopSystem>();
        }
    }

    private void Start()
    {
        SetHintVisible(false);
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

        // 힌트는 범위 안 + 상점 닫힘 상태에서만 표시
        bool shopClosed = shopSystem == null || shopSystem.IsOpen == false;
        SetHintVisible(_playerInRange == true && shopClosed == true);

        if (_playerInRange == false)
        {
            return;
        }

        // 상호작용 키(E)로 상점 열기
        if (WasInteractPressed() == true)
        {
            if (shopSystem != null && shopSystem.IsOpen == false)
            {
                shopSystem.OpenShop();
            }
        }
    }

    // 상호작용 키 입력 확인 (Interact 액션, 없으면 기본 E 폴백)
    private bool WasInteractPressed()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        if (KeyBindingManager.Instance != null)
        {
            InputAction interactAction = KeyBindingManager.Instance.FindAction("Interact");
            if (interactAction != null)
            {
                return interactAction.WasPressedThisFrame();
            }
        }

        return Keyboard.current.eKey.wasPressedThisFrame;
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
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}