using UnityEngine;
using System.Collections.Generic;

// ─────────────────────── 드롭 항목 구조체 ───────────────────────

/// <summary>
/// 드롭 테이블의 개별 항목
/// </summary>
[System.Serializable]
public class LootEntry
{
    [Tooltip("드롭할 아이템 데이터 (ItemData 또는 하위 클래스)")]
    public ItemData item;

    [Tooltip("드롭 가중치. 높을수록 자주 드롭\n" +
             "ex) A=10, B=5, C=1 → A:B:C = 10:5:1 확률")]
    [Range(0, 100)]
    public int weight = 10;

    [Tooltip("최소 드롭 수량")]
    [Range(1, 99)]
    public int minCount = 1;

    [Tooltip("최대 드롭 수량")]
    [Range(1, 99)]
    public int maxCount = 1;
}

// ─────────────────────── LootTable ───────────────────────

/// <summary>
/// 적 드롭 테이블 컴포넌트
///
/// [기획 반영]
/// - 자원(원석), 소모품(물약) 등 드롭 가능
/// - 열쇠는 StemManager 에서 별도 처리 — LootTable 불필요
/// - 위그드라실 중심에서 멀수록 희귀 등급 가중치 증가
///   (DungeonDifficultyScaler 에서 rarityBonus 설정)
/// - 드롭 항목은 인스펙터에서 자유롭게 추가 (아이템 확정 전 비워둬도 됨)
/// - EnemyBase.Die() 에서 RollDrop() 호출
///
/// [사용법]
/// 1. 적 프리팹에 LootTable 부착
/// 2. lootEntries 에 드롭할 아이템과 가중치 입력
/// 3. dropCount 로 한 번에 드롭할 항목 수 설정
/// </summary>
public class LootTable : MonoBehaviour
{
    // ─────────────────────── 드롭 설정 ───────────────────────

    [Header("드롭 항목 (아이템 확정 후 채워넣기)")]
    [SerializeField] private List<LootEntry> lootEntries = new List<LootEntry>();

    [Header("드롭 횟수")]
    [Tooltip("사망 시 드롭을 시도할 횟수 (중복 드롭 가능)")]
    [SerializeField] [Range(0, 5)] private int dropCount = 1;

    [Tooltip("드롭 자체를 건너뛸 확률 (%). 0이면 항상 드롭 시도")]
    [SerializeField] [Range(0, 100)] private float skipChance = 0f;

    [Header("드롭 위치 오프셋")]
    [Tooltip("아이템이 스폰될 위치의 랜덤 반경")]
    [SerializeField] private float dropSpreadRadius = 0.5f;

    // ─────────────────────── 희귀도 보너스 ───────────────────────
    // DungeonDifficultyScaler 에서 위그드라실 거리 기반으로 설정

    /// <summary>
    /// 희귀 아이템 가중치 추가 배율
    /// DungeonDifficultyScaler 에서 호출 (멀수록 높게 설정)
    /// </summary>
    public float RarityBonus { get; set; } = 0f;

    // ─────────────────────── 드롭 실행 ───────────────────────

    /// <summary>
    /// 드롭 실행 — EnemyBase.Die() 에서 호출
    /// 결과 드롭 목록 반환 (InventorySystem 연동 후 인벤에 추가)
    /// </summary>
    public List<(ItemData item, int count)> RollDrop(Vector3 dropPosition)
    {
        var results = new List<(ItemData, int)>();

        if (lootEntries == null || lootEntries.Count == 0) return results;

        for (int i = 0; i < dropCount; i++)
        {
            // 드롭 스킵 확률 체크
            if (skipChance > 0f && Random.Range(0f, 100f) < skipChance) continue;

            LootEntry entry = SelectEntry();
            if (entry == null || entry.item == null) continue;

            int count = Random.Range(entry.minCount, entry.maxCount + 1);
            results.Add((entry.item, count));

            // 월드에 아이템 스폰 (프리팹 있을 경우)
            SpawnItemInWorld(entry.item, dropPosition, count);
        }

        if (results.Count > 0)
            Debug.Log($"[LootTable] {gameObject.name} 드롭: {results.Count}종");

        return results;
    }

    // ─────────────────────── 가중치 추첨 ───────────────────────

    /// <summary>
    /// 가중치 기반 랜덤 항목 선택
    /// RarityBonus 가 높을수록 weight 가 낮은(희귀) 항목의 선택 확률 증가
    /// </summary>
    private LootEntry SelectEntry()
    {
        if (lootEntries.Count == 0) return null;

        // 전체 가중치 합산
        float totalWeight = 0f;
        foreach (var entry in lootEntries)
        {
            if (entry.item == null) continue;
            totalWeight += GetAdjustedWeight(entry);
        }

        if (totalWeight <= 0f) return null;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in lootEntries)
        {
            if (entry.item == null) continue;
            cumulative += GetAdjustedWeight(entry);
            if (roll <= cumulative) return entry;
        }

        return lootEntries[lootEntries.Count - 1];
    }

    /// <summary>
    /// RarityBonus 반영 가중치 계산
    /// 희귀 아이템(weight 낮음)일수록 보너스 효과 크게 적용
    /// </summary>
    private float GetAdjustedWeight(LootEntry entry)
    {
        float w = entry.weight;
        if (RarityBonus > 0f && w < 10f)
            w += RarityBonus * (10f - w); // 희귀할수록 보너스 크게
        return Mathf.Max(w, 0.01f);
    }

    // ─────────────────────── 월드 스폰 ───────────────────────

    private void SpawnItemInWorld(ItemData item, Vector3 position, int count)
    {
        if (item.Prefab3D == null)
        {
            // 프리팹 없으면 로그만
            Debug.Log($"[LootTable] 드롭: {item.ItemName} x{count} (프리팹 미설정)");
            return;
        }

        // 살짝 랜덤한 위치에 스폰
        Vector2 spread   = Random.insideUnitCircle * dropSpreadRadius;
        Vector3 spawnPos = position + new Vector3(spread.x, 0.1f, spread.y);

        GameObject dropped = Instantiate(item.Prefab3D, spawnPos, Quaternion.identity);

        // 드롭 아이템 컴포넌트에 데이터 주입 (DroppedItem 완성 후 연동)
        // dropped.GetComponent<DroppedItem>()?.Initialize(item, count);
        Debug.Log($"[LootTable] {item.ItemName} x{count} 스폰 at {spawnPos}");
    }

    // ─────────────────────── 유틸 ───────────────────────

    /// <summary>드롭 항목 런타임 추가 (EnemySpawner 에서 난이도별 추가 시)</summary>
    public void AddEntry(LootEntry entry)
    {
        if (entry != null) lootEntries.Add(entry);
    }

    /// <summary>현재 드롭 테이블 항목 수</summary>
    public int EntryCount => lootEntries?.Count ?? 0;

#if UNITY_EDITOR
    [ContextMenu("드롭 테스트 (에디터)")]
    private void TestRoll()
    {
        var results = RollDrop(transform.position);
        foreach (var (item, count) in results)
            Debug.Log($"  → {item.ItemName} x{count}");
    }
#endif
}
