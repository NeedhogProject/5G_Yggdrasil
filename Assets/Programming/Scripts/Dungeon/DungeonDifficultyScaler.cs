using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 층별 난이도 설정 데이터
/// </summary>
[System.Serializable]
public class FloorDifficultyData
{
    [Tooltip("층 번호 (1~3)")]
    public int floor = 1;

    [Tooltip("이 층의 총 스폰 수 (SpawnPoint 수와 일치시킬 것)")]
    public int totalSpawnCount = 50;

    [Tooltip("적 체력 배율 (1.0 = 기본)")]
    [Range(0.5f, 10f)] public float enemyHealthMult  = 1f;

    [Tooltip("적 공격력 배율")]
    [Range(0.5f, 10f)] public float enemyAttackMult  = 1f;

    [Tooltip("드롭 희귀도 보너스 (LootTable.RarityBonus 에 적용)")]
    [Range(0f, 10f)]   public float rarityBonus       = 0f;
}

/// <summary>
/// 던전 층별 난이도 상승 시스템
///
/// [기획 반영]
/// - 난이도 기준: 층 번호 (1, 2, 3층)
/// - 3층은 일반 몬스터(층 표)와 보스(보스 배율)를 따로 조정
/// - 층 깊어질수록: 적 체력↑ / 적 공격력↑ / 드롭 희귀도↑ / 적 수↑
/// - EnemySpawner, LootTable 에 배율 자동 적용
/// - 층 입장 시 FloorManager/GameManager 에서 ApplyFloorDifficulty() 호출
///
/// [씬 설정]
/// - 각 층 씬에 DungeonDifficultyScaler 배치
/// - floorDifficulties 에 1~3층 데이터 입력
/// - 3층 보스 배율은 bossHealthMult / bossAttackMult
/// </summary>
public class DungeonDifficultyScaler : MonoBehaviour
{
    public static DungeonDifficultyScaler Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─────────────────────── 층별 난이도 설정 ───────────────────────

    [Header("층별 난이도 설정 (1~3층, 일반 몬스터)")]
    [SerializeField] private List<FloorDifficultyData> floorDifficulties = new List<FloorDifficultyData>
    {
        new FloorDifficultyData { floor=1, totalSpawnCount=50,  enemyHealthMult=1.0f, enemyAttackMult=1.0f, rarityBonus=0f   },
        new FloorDifficultyData { floor=2, totalSpawnCount=125, enemyHealthMult=1.5f, enemyAttackMult=1.3f, rarityBonus=1f   },
        new FloorDifficultyData { floor=3, totalSpawnCount=125, enemyHealthMult=2.2f, enemyAttackMult=1.7f, rarityBonus=2.5f },
    };

    // ─────────────────────── 3층 보스 난이도 ───────────────────────

    // 보스가 있는 층 (4층 제거 후 3층 보스 구역)
    private const int BOSS_FLOOR = 3;

    [Header("3층 보스 배율 (일반 몬스터와 별도)")]
    [Tooltip("보스(BossHead) 체력 배율")]
    [SerializeField] [Range(0.5f, 10f)] private float bossHealthMult = 3.5f;

    [Tooltip("보스(BossHead) 공격력 배율")]
    [SerializeField] [Range(0.5f, 10f)] private float bossAttackMult = 2.5f;

    // 이미 배율을 적용한 보스 (중복 적용 시 체력이 계속 곱해짐)
    private readonly HashSet<EnemyBase> _scaledBosses = new HashSet<EnemyBase>();

    // ─────────────────────── 현재 적용 난이도 ───────────────────────

    public FloorDifficultyData CurrentDifficulty { get; private set; }

    // ─────────────────────── 난이도 적용 ───────────────────────

    private void Start()
    {
        int floor = GameManager.Instance?.CurrentFloor ?? 1;
        ApplyFloorDifficulty(floor);
    }

    /// <summary>
    /// 층 입장 시 호출 — 해당 층 난이도를 EnemySpawner / LootTable 에 적용
    /// </summary>
    public void ApplyFloorDifficulty(int floor)
    {
        FloorDifficultyData data = GetDifficultyData(floor);
        if (data == null)
        {
            Debug.LogWarning($"[DifficultyScaler] {floor}층 데이터 없음 — 기본값 사용");
            data = new FloorDifficultyData { floor = floor };
        }

        CurrentDifficulty = data;

        // EnemySpawner 에 배율 + 스폰 수 적용
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.SetDifficultyScale(data.enemyHealthMult, data.enemyAttackMult);
            spawner.SetMaxSpawnCount(data.totalSpawnCount);
        }

        // 보스 층이면 씬에 배치된 보스(비활성 포함)에 보스 배율 적용
        if (floor == BOSS_FLOOR)
        {
            foreach (BossHead head in FindObjectsByType<BossHead>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                ApplyBossScale(head);
            }
        }

        // LootTable 전체에 희귀도 보너스 적용
        foreach (LootTable loot in FindObjectsByType<LootTable>(FindObjectsSortMode.None))
            loot.RarityBonus = data.rarityBonus;

        Debug.Log($"[DifficultyScaler] {floor}층 난이도 적용 — " +
                  $"체력x{data.enemyHealthMult} / 공격x{data.enemyAttackMult} / " +
                  $"희귀도+{data.rarityBonus}");
    }

    /// <summary>보스 배율 적용 (보스를 나중에 생성하면 생성 직후 호출)</summary>
    public void ApplyBossScale(EnemyBase _boss)
    {
        if (_boss == null || _scaledBosses.Contains(_boss) == true)
        {
            return;
        }

        _scaledBosses.Add(_boss);
        _boss.ApplyDifficultyScale(bossHealthMult, bossAttackMult);
        Debug.Log($"[DifficultyScaler] 보스 배율 적용 {_boss.name} 체력x{bossHealthMult} / 공격x{bossAttackMult}");
    }

    // ─────────────────────── 조회 ───────────────────────

    private FloorDifficultyData GetDifficultyData(int floor)
    {
        foreach (FloorDifficultyData data in floorDifficulties)
            if (data.floor == floor) return data;
        return null;
    }

    /// <summary>현재 층 적 체력 배율</summary>
    public float EnemyHealthMult  => CurrentDifficulty?.enemyHealthMult  ?? 1f;

    /// <summary>현재 층 적 공격력 배율</summary>
    public float EnemyAttackMult  => CurrentDifficulty?.enemyAttackMult  ?? 1f;

    /// <summary>현재 층 드롭 희귀도 보너스</summary>
    public float RarityBonus      => CurrentDifficulty?.rarityBonus      ?? 0f;

    /// <summary>현재 층 총 스폰 수</summary>
    public int TotalSpawnCount    => CurrentDifficulty?.totalSpawnCount   ?? 50;

#if UNITY_EDITOR
    [ContextMenu("현재 층 난이도 재적용")]
    private void ReapplyDifficulty()
    {
        int floor = GameManager.Instance?.CurrentFloor ?? 1;
        ApplyFloorDifficulty(floor);
    }
#endif
}
