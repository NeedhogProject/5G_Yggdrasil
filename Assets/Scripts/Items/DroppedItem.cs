using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 바닥에 떨어진 아이템 오브젝트
/// </summary>
public class DroppedItem : MonoBehaviour
{
    [Header("획득 범위")]
    [SerializeField] private float pickupRange = 1.5f;

    [Header("회전 연출")]
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private float bobSpeed    = 2f;
    [SerializeField] private float bobHeight   = 0.15f;

    [Header("테스트용 — 씬에 직접 배치 시 연결")]
    [SerializeField] private ItemData testItemData;

    private ItemInstance _itemInstance;
    private bool _playerInRange = false;
    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.position;

        // 씬에 직접 배치한 경우 testItemData로 자동 초기화
        if (_itemInstance == null && testItemData != null)
        {
            Initialize(new ItemInstance(testItemData));
            Debug.Log($"[DroppedItem] testItemData로 자동 초기화: {testItemData.ItemName}");
        }
        else if (_itemInstance == null)
        {
            Debug.LogWarning("[DroppedItem] ItemData 없음 — testItemData 슬롯에 에셋 연결 필요");
        }
    }

    /// <summary>ResourceNode / LootTable 에서 스폰 후 호출</summary>
    public void Initialize(ItemInstance item)
    {
        _itemInstance = item;
        _startPos     = transform.position;

        SpawnModel();

        Debug.Log($"[DroppedItem] {item?.Data?.ItemName ?? "아이템"} 바닥에 스폰");
    }

    // 아이템 데이터의 3D 프리팹을 자식으로 생성 (드롭 비주얼)
    private void SpawnModel()
    {
        GameObject prefab = _itemInstance?.Data?.Prefab3D;
        if (prefab == null)
        {
            Debug.LogWarning($"[DroppedItem] {_itemInstance?.Data?.ItemName ?? "아이템"} 의 Prefab3D 미설정 — 모델 표시 불가");
            return;
        }

        GameObject model = Instantiate(prefab, transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
    }

    private void Update()
    {
        // 회전 + 둥실둥실 연출 (unscaled time 사용)
        transform.Rotate(Vector3.up, rotateSpeed * Time.unscaledDeltaTime);
        float newY = _startPos.y + Mathf.Sin(Time.unscaledTime * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // E키 획득 — unscaledTime 기반으로 체크
        if (_playerInRange && InputReader.Instance != null && InputReader.Instance.InteractPressed)
            TryPickup();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") == false && other.GetComponent<PlayerStats>() == null) return;
        _playerInRange = true;
        Debug.Log($"[DroppedItem] 범위 진입 — E키로 {_itemInstance?.Data?.ItemName ?? "아이템"} 획득");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") == false && other.GetComponent<PlayerStats>() == null) return;
        _playerInRange = false;
    }

    private void TryPickup()
    {
        if (_itemInstance == null)
        {
            Debug.LogWarning("[DroppedItem] _itemInstance null — testItemData 슬롯 확인 필요");
            return;
        }

        // 자원 아이템은 자원 인벤토리로, 그 외에는 장비 인벤토리로
        if (_itemInstance.Data is ResourceData resourceData)
        {
            TryPickupResource(resourceData);
            return;
        }

        InventorySystem inventory = InventorySystem.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("[DroppedItem] InventorySystem.Instance 없음");
            Destroy(gameObject);
            return;
        }

        Debug.Log($"[DroppedItem] 획득 시도: {_itemInstance.Data?.ItemName} / 슬롯 수: {inventory.slots.Count}");

        if (inventory.AddItem(_itemInstance.Data))
        {
            AudioManager.Instance?.PlaySFX(SFXClip.ItemPickup);
            Debug.Log($"[DroppedItem] {_itemInstance.Data.ItemName} 획득 성공");
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning("[DroppedItem] 인벤토리 가득 참");
        }
    }

    // 자원 아이템 획득 — 자원 인벤토리에 원소별로 누적
    private void TryPickupResource(ResourceData resourceData)
    {
        ResourceInventory resourceInventory = ResourceInventory.Instance;
        if (resourceInventory == null)
        {
            Debug.LogWarning("[DroppedItem] ResourceInventory.Instance 없음");
            return;
        }

        InscriptionType type = resourceData.ToInscriptionType();
        if (type == InscriptionType.None)
        {
            Debug.LogWarning($"[DroppedItem] {resourceData.ItemName} 원소 변환 실패 — 획득 불가");
            return;
        }

        int amount = _itemInstance.StackCount;
        resourceInventory.AddResource(type, amount);
        AudioManager.Instance?.PlaySFX(SFXClip.ItemPickup);
        Debug.Log($"[DroppedItem] 자원 획득: {resourceData.ItemName} ({type}) x{amount}");
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