using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 자원 드랍 몬스터 컴포넌트 — EnemyBase 에 추가
///
/// [기획 반영]
/// - 일반 몬스터와 외관으로 구분 (다른 프리팹)
/// - 자원 드랍 몬스터만 자원 드롭 (일반 몬스터는 자원 미드롭)
/// - 층별 등장 종류:
///   · 1층: 불, 물, 어둠
///   · 2층: 땅, 바람, 어둠
///   · 3층: 불, 물, 바람, 땅, 어둠 전부
/// - 어둠 자원은 이 컴포넌트로만 획득 가능 (자원 노드 없음)
///
/// [사용법]
/// 1. 자원 드랍 몬스터 프리팹에 EnemyBase + ResourceDropEnemy 부착
/// 2. dropResourceData 에 드랍할 자원 ScriptableObject 연결
/// 3. EnemyBase.OnDied 이벤트에 자동 연동
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class ResourceDropEnemy : MonoBehaviour
{
    // ─────────────────────── 설정 ───────────────────────

    [Header("드랍 자원")]
    [Tooltip("이 몬스터가 드랍할 자원 종류")]
    [SerializeField] private ResourceData dropResourceData;

    [Header("드랍 수량")]
    [SerializeField] [Range(1, 5)] private int minDrop = 1;
    [SerializeField] [Range(1, 5)] private int maxDrop = 2;

    [Header("드랍 아이템 프리팹")]
    [Tooltip("바닥에 스폰할 DroppedItem 프리팹")]
    [SerializeField] private GameObject droppedItemPrefab;

    [Header("드랍 위치")]
    [SerializeField] private float dropSpreadRadius = 0.6f;
    [SerializeField] private float dropHeightOffset = 0.2f;

    [Header("등장 가능 층 설정")]
    [Tooltip("이 몬스터가 등장할 수 있는 층 번호 목록\n" +
             "불/물/어둠 → {1,3} / 땅/바람/어둠 → {2,3} / 3층전용 → {3}")]
    [SerializeField] private List<int> availableFloors = new List<int> { 1, 2, 3 };

    // ─────────────────────── 참조 ───────────────────────

    private EnemyBase _enemyBase;

    // ─────────────────────── 초기화 ───────────────────────

    private void Awake()
    {
        _enemyBase = GetComponent<EnemyBase>();
        _enemyBase.OnDied += OnEnemyDied;

        // 현재 층에 등장 불가한 경우 비활성화
        ValidateFloor();
    }

    private void OnDestroy()
    {
        if (_enemyBase != null)
            _enemyBase.OnDied -= OnEnemyDied;
    }

    // ─────────────────────── 층 유효성 검사 ───────────────────────

    private void ValidateFloor()
    {
        int currentFloor = FloorManager.Instance?.CurrentFloor ?? 1;
        if (!availableFloors.Contains(currentFloor))
        {
            // 이 층에 등장하면 안 되는 몬스터 — 오브젝트 비활성화
            Debug.LogWarning($"[ResourceDropEnemy] {gameObject.name} — " +
                             $"{currentFloor}층에 등장 불가 ({dropResourceData?.ResourceType}). 비활성화.");
            gameObject.SetActive(false);
        }
    }

    // ─────────────────────── 사망 → 자원 드랍 ───────────────────────

    private void OnEnemyDied(EnemyBase enemy)
    {
        if (dropResourceData == null)
        {
            Debug.LogWarning($"[ResourceDropEnemy] {gameObject.name} — dropResourceData 미설정");
            return;
        }

        int dropCount = Random.Range(minDrop, maxDrop + 1);
        for (int i = 0; i < dropCount; i++)
            SpawnDroppedResource();

        Debug.Log($"[ResourceDropEnemy] {dropResourceData.ResourceType} 원소 x{dropCount} 드랍");
    }

    private void SpawnDroppedResource()
    {
        if (droppedItemPrefab == null)
        {
            Debug.LogWarning($"[ResourceDropEnemy] droppedItemPrefab 미설정");
            return;
        }

        Vector2 spread   = Random.insideUnitCircle * dropSpreadRadius;
        Vector3 spawnPos = transform.position
                         + new Vector3(spread.x, dropHeightOffset, spread.y);

        GameObject dropped = Instantiate(droppedItemPrefab, spawnPos, Quaternion.identity);

        var droppedItem = dropped.GetComponent<DroppedItem>();
        if (droppedItem != null)
            droppedItem.Initialize(new ResourceInstance(dropResourceData, 1));
    }

    // ─────────────────────── 프로퍼티 ───────────────────────

    public ResourceType ResourceType    => dropResourceData?.ResourceType ?? ResourceType.Fire;
    public List<int>    AvailableFloors => availableFloors;

    // ─────────────────────── 에디터 유효성 검사 ───────────────────────
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (dropResourceData == null) return;

        // 층별 등장 규칙 자동 설정 힌트
        var type = dropResourceData.ResourceType;
        string hint = type switch
        {
            ResourceType.Fire     => "권장 등장 층: 1, 3",
            ResourceType.Water    => "권장 등장 층: 1, 3",
            ResourceType.Wind     => "권장 등장 층: 2, 3",
            ResourceType.Earth    => "권장 등장 층: 2, 3",
            ResourceType.Darkness => "권장 등장 층: 1, 2, 3",
            _                     => ""
        };
        if (!string.IsNullOrEmpty(hint))
            Debug.Log($"[ResourceDropEnemy: {name}] {hint}");
    }
#endif
}
