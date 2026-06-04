// StorageInteractable.cs
// 창고 오브젝트 — 플레이어가 근처에 오면 힌트 표시, 창고 키로 열기/닫기
// 집 안의 창고 오브젝트에 부착, Collider(isTrigger=true) 필요

using UnityEngine;
using UnityEngine.InputSystem;

public class StorageInteractable : MonoBehaviour
{
    [Header("UI 힌트")]
    [SerializeField] private GameObject hintObject;  // "G - 창고 열기" 표시용 UI 오브젝트

    private bool _playerInRange = false;

    private void Start()
    {
        // 시작 시 힌트 숨김
        SetHintVisible(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }
        _playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }
        _playerInRange = false;
        SetHintVisible(false);
    }

    private void Update()
    {
        // 힌트는 범위 안 + 창고가 닫혀있을 때만 표시
        bool storageClosed = StorageUI.Instance == null || StorageUI.Instance.IsOpen == false;
        SetHintVisible(_playerInRange == true && storageClosed == true);

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

    // 창고 키 입력 확인
    // KeyBindingManager 와 Storage 액션이 있으면 설정된 키, 없으면 기본 G 폴백
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
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
#endif
}