// StorageInteractable.cs
// 창고 오브젝트 — 상호작용 시 창고 UI 열기
// 집 안의 창고 오브젝트에 부착, Collider(isTrigger=true) 필요

using UnityEngine;
using UnityEngine.InputSystem;

public class StorageInteractable : MonoBehaviour
{
    [Header("상호작용 범위")]
    [SerializeField] private float interactRange = 2f;

    [Header("UI 힌트")]
    [SerializeField] private string interactHint = "E - 창고 열기";

    private bool _playerInRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }
        _playerInRange = true;
        Debug.Log($"[StorageInteractable] {interactHint}");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }
        _playerInRange = false;
    }

    private void Update()
    {
        if (_playerInRange == false)
        {
            return;
        }

        InputAction interactAction = KeyBindingManager.Instance?.FindAction("Interact");
        if (interactAction != null && interactAction.WasPressedThisFrame() == true)
        {
            Debug.Log("[StorageInteractable] E 눌림! StorageUI = " + StorageUI.Instance);  // 임시

            if (StorageUI.Instance != null)
            {
                StorageUI.Instance.OpenStorage();
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}