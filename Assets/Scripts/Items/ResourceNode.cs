using UnityEngine;

/// <summary>
/// 자원 채집 노드
///
/// [기획 반영]
/// - 공격키(마우스 좌클릭)로 공격 시 자원 드롭
/// - 바닥에 아이템 프리팹 스폰 → 플레이어가 직접 주워야 함
/// - 층당 1회만 채집 가능 (재입장 시 재생성)
/// - 드롭 수량: 랜덤 1~3개
/// - 위그드라실에서 멀수록 희귀 자원 배치 (씬에서 직접 설정)
///
/// [씬 설정]
/// - 자원 노드 오브젝트에 ResourceNode + Collider 부착
/// - resourceData 에 드롭할 자원 ScriptableObject 연결
/// - droppedItemPrefab 에 바닥에 떨어지는 아이템 프리팹 연결
/// </summary>
public class ResourceNode : MonoBehaviour
{
    // ─────────────────────── 설정 ───────────────────────

    [Header("자원 데이터")]
    [Tooltip("이 노드에서 드롭할 자원 종류")]
    [SerializeField] private ResourceData resourceData;

    [Header("드롭 수량")]
    [SerializeField] [Range(1, 10)] private int minDrop = 1;
    [SerializeField] [Range(1, 10)] private int maxDrop = 3;

    [Header("드롭 아이템 프리팹")]
    [Tooltip("바닥에 스폰할 아이템 오브젝트 프리팹\n" +
             "DroppedItem 컴포넌트 부착 권장")]
    [SerializeField] private GameObject droppedItemPrefab;

    [Header("드롭 위치 설정")]
    [Tooltip("아이템이 퍼지는 반경")]
    [SerializeField] private float dropSpreadRadius = 0.8f;

    [Tooltip("아이템 스폰 높이 오프셋")]
    [SerializeField] private float dropHeightOffset = 0.2f;

    [Header("채집 후 비주얼")]
    [Tooltip("채집 완료 후 노드 오브젝트를 숨길지 여부")]
    [SerializeField] private bool hideOnHarvested = true;

    [Tooltip("채집 완료 후 표시할 오브젝트 (시든 모습 등). 없으면 그냥 숨김)")]
    [SerializeField] private GameObject harvestedVisual;

    // ─────────────────────── 상태 ───────────────────────

    private void Awake()
    {
        // 어둠 원소는 자원 노드에 배치 불가 — 몬스터 드롭 전용
        if (resourceData != null && resourceData.ResourceType == ResourceType.Darkness)
        {
            Debug.LogError($"[ResourceNode] 어둠 원소는 자원 노드에 배치할 수 없습니다. " +
                           $"몬스터 LootTable 에서만 드롭 가능합니다. — {gameObject.name}");
            gameObject.SetActive(false);
        }
    }

    // ─────────────────────── 상태 프로퍼티 ───────────────────────

    /// <summary>채집 완료 여부 — OnHit, Harvest, OnDrawGizmosSelected 에서 참조</summary>
    public bool IsHarvested { get; private set; } = false;

    // ─────────────────────── 피격 감지 ───────────────────────

    /// <summary>
    /// PlayerCombat 의 HitboxSystem 에서 피격 감지 시 호출
    /// EnemyBase.TakeDamage 와 동일한 방식으로 연동
    /// </summary>
    public void OnHit()
    {
        if (IsHarvested) return;

        Harvest();
    }

    // ─────────────────────── 채집 처리 ───────────────────────

    private void Harvest()
    {
        IsHarvested = true;

        int dropCount = Random.Range(minDrop, maxDrop + 1);

        // 자원 아이템 바닥에 스폰
        for (int i = 0; i < dropCount; i++)
            SpawnDroppedItem();

        Debug.Log($"[ResourceNode] {resourceData?.ItemName ?? "자원"} x{dropCount} 드롭");

        // 채집 후 비주얼 처리
        ApplyHarvestedVisual();
    }

    private void SpawnDroppedItem()
    {
        if (droppedItemPrefab == null)
        {
            Debug.LogWarning($"[ResourceNode] droppedItemPrefab 미설정 — {resourceData?.ItemName} 드롭 건너뜀");
            return;
        }

        // 랜덤 위치에 스폰
        Vector2 spread   = Random.insideUnitCircle * dropSpreadRadius;
        Vector3 spawnPos = transform.position
                         + new Vector3(spread.x, dropHeightOffset, spread.y);

        GameObject dropped = Instantiate(droppedItemPrefab, spawnPos, Quaternion.identity);

        // DroppedItem 컴포넌트에 자원 데이터 주입 (DroppedItem 완성 후 연동)
        DroppedItem droppedItem = dropped.GetComponent<DroppedItem>();
        if (droppedItem != null)
            droppedItem.Initialize(new ResourceInstance(resourceData, 1));
        else
            Debug.Log($"[ResourceNode] {resourceData?.ItemName} 스폰 @ {spawnPos}");
    }

    private void ApplyHarvestedVisual()
    {
        if (harvestedVisual != null)
        {
            // 기존 비주얼 숨기고 채집 완료 비주얼 표시
            harvestedVisual.SetActive(true);
            if (hideOnHarvested)
            {
                // 자식 오브젝트 중 harvestedVisual 제외하고 숨김
                foreach (Transform child in transform)
                    if (child.gameObject != harvestedVisual)
                        child.gameObject.SetActive(false);
            }
        }
        else if (hideOnHarvested)
        {
            gameObject.SetActive(false);
        }
    }

    // ─────────────────────── 에디터 기즈모 ───────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsHarvested
            ? new Color(0.5f, 0.5f, 0.5f, 0.3f)
            : new Color(0f, 1f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, dropSpreadRadius);
    }

    private void OnDrawGizmos()
    {
        // 자원 종류별 색상 표시
        if (resourceData == null) return;
        Gizmos.color = resourceData.ResourceType switch
        {
            ResourceType.Fire     => new Color(1f, 0.3f, 0f, 0.6f),
            ResourceType.Water    => new Color(0f, 0.5f, 1f, 0.6f),
            ResourceType.Wind     => new Color(0.5f, 1f, 0.5f, 0.6f),
            ResourceType.Earth    => new Color(0.6f, 0.4f, 0f, 0.6f),
            ResourceType.Darkness => new Color(0.4f, 0f, 0.6f, 0.6f),
            _                     => Color.white
        };
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.2f);
    }
#endif
}
