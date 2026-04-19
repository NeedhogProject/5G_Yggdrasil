using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 바닥에 떨어진 아이템 오브젝트
///
/// [기획 반영]
/// - 자원 노드 채집 시 바닥에 스폰
/// - 플레이어가 범위 안에서 E키로 획득
/// - 획득 시 인벤토리에 추가 (InventorySystem 연동 후 활성화)
/// </summary>
public class DroppedItem : MonoBehaviour
{
    // ─────────────────────── 설정 ───────────────────────

    [Header("획득 범위")]
    [SerializeField] private float pickupRange = 1.5f;

    [Header("회전 연출")]
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private float bobSpeed    = 2f;
    [SerializeField] private float bobHeight   = 0.15f;

    // ─────────────────────── 런타임 ───────────────────────

    private ItemInstance _itemInstance;
    private bool _playerInRange = false;
    private Vector3 _startPos;

    // ─────────────────────── 초기화 ───────────────────────

    /// <summary>ResourceNode / LootTable 에서 스폰 후 호출</summary>
    public void Initialize(ItemInstance item)
    {
        _itemInstance = item;
        _startPos     = transform.position;
        Debug.Log($"[DroppedItem] {item?.Data?.ItemName ?? "아이템"} 바닥에 스폰");
    }

    private void Update()
    {
        // 회전 + 둥실둥실 연출
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        float newY = _startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // E키 획득
        if (_playerInRange && Keyboard.current.eKey.wasPressedThisFrame)
            TryPickup();
    }

    // ─────────────────────── 트리거 감지 ───────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        Debug.Log($"[DroppedItem] {_itemInstance?.Data?.ItemName} — E키로 획득");
        // TODO: HUD 힌트 표시
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
    }

    // ─────────────────────── 획득 ───────────────────────

    private void TryPickup()
    {
        if (_itemInstance == null) return;

        // InventorySystem 완성 후 주석 해제
        // var inventory = FindFirstObjectByType<InventorySystem>();
        // if (inventory != null && inventory.AddItem(_itemInstance))
        // {
        //     Debug.Log($"[DroppedItem] {_itemInstance.Data.ItemName} 획득");
        //     Destroy(gameObject);
        // }
        // else Debug.Log("[DroppedItem] 인벤토리 가득 참");

        // 임시: 바로 획득 처리
        Debug.Log($"[DroppedItem] {_itemInstance?.Data?.ItemName} 획득 (InventorySystem 연동 전)");
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
#endif
}
