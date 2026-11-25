using UnityEngine;
using System.Collections.Generic;

// ─────────────────────── 스폰 프리팹 가중치 항목 ───────────────────────

/// <summary>
/// 스폰 포인트의 몬스터 프리팹 + 가중치
/// 여러 종류를 등록하면 가중치 기반으로 랜덤 선택
/// </summary>
[System.Serializable]
public class EnemySpawnEntry
{
    [Tooltip("스폰할 적 프리팹")]
    public GameObject prefab;

    [Tooltip("선택 가중치. 높을수록 자주 스폰")]
    [Range(1, 100)]
    public int weight = 10;
}

/// <summary>
/// 개별 스폰 포인트
/// 플레이어가 activationRange 안에 들어오면 적 생성
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Header("스폰 설정")]
    [Tooltip("스폰 가능한 적 프리팹 목록 (가중치 기반 랜덤 선택)\n" +
             "1개만 등록하면 항상 그 적만 스폰")]
    [SerializeField] private List<EnemySpawnEntry> enemyEntries = new List<EnemySpawnEntry>();

    [Tooltip("이 포인트에서 스폰할 마리 수")]
    [SerializeField] [Range(1, 20)] private int spawnCount = 1;

    [Tooltip("플레이어가 이 거리 안에 들어오면 스폰")]
    [SerializeField] private float activationRange = 8f;

    [Tooltip("스폰 시 적을 중심에서 퍼뜨릴 반경")]
    [SerializeField] private float spawnSpreadRadius = 1.5f;

    // ─────────────────────── 상태 ───────────────────────

    /// <summary>지금까지 스폰한 마리 수</summary>
    public int SpawnedCount { get; private set; } = 0;

    /// <summary>이 포인트의 스폰이 모두 끝났는지</summary>
    public bool HasSpawned => SpawnedCount >= spawnCount;

    /// <summary>마지막으로 스폰된 적 오브젝트</summary>
    public GameObject SpawnedEnemy { get; private set; }

    public int SpawnCount => spawnCount;

    public float ActivationRange   => activationRange;
    public float SpawnSpreadRadius => spawnSpreadRadius;

    /// <summary>
    /// 가중치 기반 랜덤 프리팹 선택
    /// EnemySpawner 에서 호출
    /// </summary>
    public GameObject SelectRandomPrefab()
    {
        if (enemyEntries == null || enemyEntries.Count == 0) return null;

        int nCurrentFloor = FloorManager.Instance != null ? FloorManager.Instance.CurrentFloor : 1;

        // 총 가중치 합산 (현재 층 등장 불가 프리팹 제외)
        int nTotalWeight = 0;
        foreach (EnemySpawnEntry e in enemyEntries)
        {
            if (e.prefab == null) continue;
            if (IsAvailableOnFloor(e.prefab, nCurrentFloor) == false) continue;
            nTotalWeight += e.weight;
        }

        if (nTotalWeight <= 0) return null;

        int nRoll = Random.Range(0, nTotalWeight);
        int nCumulative = 0;
        GameObject lastValid = null;
        foreach (EnemySpawnEntry e in enemyEntries)
        {
            if (e.prefab == null) continue;
            if (IsAvailableOnFloor(e.prefab, nCurrentFloor) == false) continue;

            lastValid = e.prefab;
            nCumulative += e.weight;
            if (nRoll < nCumulative) return e.prefab;
        }

        return lastValid;
    }

    // 자원 몬스터의 층 제한 검사 (일반 몬스터는 제한 없음)
    private static bool IsAvailableOnFloor(GameObject _prefab, int _nFloor)
    {
        ResourceDropEnemy resourceEnemy = _prefab.GetComponent<ResourceDropEnemy>();
        if (resourceEnemy == null) return true;
        return resourceEnemy.AvailableFloors.Contains(_nFloor);
    }

    /// <summary>스폰 1마리 완료 처리 (EnemySpawner 에서 호출)</summary>
    public void MarkSpawned(GameObject _enemy)
    {
        // 프리팹이 없는 포인트는 재시도하지 않도록 전부 소진 처리
        if (_enemy == null)
        {
            SpawnedCount = spawnCount;
            return;
        }

        SpawnedCount++;
        SpawnedEnemy = _enemy;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = HasSpawned
            ? new Color(0.5f, 0.5f, 0.5f, 0.2f)
            : new Color(1f, 0.3f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, activationRange);

        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
#endif
}
