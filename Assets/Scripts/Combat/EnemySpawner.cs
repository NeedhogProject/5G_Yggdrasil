using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 층별 적 스폰 관리자
///
/// [기획 반영]
/// - 미리 배치된 SpawnPoint 에서 생성
/// - 플레이어가 SpawnPoint 근처에 접근하면 적 생성 (탐색하면서 만나는 느낌)
/// - 1층: 50마리 / 2·3층: 최소 125마리 기준
/// - 재입장 시 모든 SpawnPoint 리셋 → 적 재생성
/// - 열쇠 소유 적 4마리를 StemManager 에 등록
///
/// [씬 설정]
/// 1. 씬에 SpawnPoint 오브젝트들을 배치
/// 2. EnemySpawner 의 spawnPoints 리스트에 등록
/// 3. keyEnemyCount 를 4로 설정 (열쇠 소유 적 수)
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    // ─────────────────────── 설정 ───────────────────────

    [Header("스폰 포인트 목록")]
    [Tooltip("씬에 배치된 SpawnPoint 오브젝트 등록")]
    [SerializeField] private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

    [Header("열쇠 소유 적 수")]
    [Tooltip("기획: 4마리가 열쇠 1개씩 소유. 3→4층은 1로 설정")]
    [SerializeField] [Range(1, 4)] private int keyEnemyCount = 4;

    [Header("스폰 체크 간격 (초)")]
    [Tooltip("플레이어 거리 체크 주기. 낮을수록 정확하지만 부하 증가")]
    [SerializeField] private float checkInterval = 0.3f;

    [Header("난이도 배율 (DungeonDifficultyScaler 연동)")]
    [SerializeField] private float healthMultiplier = 1f;
    [SerializeField] private float attackMultiplier = 1f;

    [Header("최대 스폰 수 (DungeonDifficultyScaler 에서 자동 설정)")]
    [Tooltip("1층=50 / 2·3층=125 / 4층=1(보스)")]
    [SerializeField] private int maxSpawnCount = 50;

    // ─────────────────────── 내부 상태 ───────────────────────

    private Transform _player;
    private float     _checkTimer = 0f;

    /// <summary>스폰된 적 중 열쇠 소유 적 목록</summary>
    private readonly List<GameObject> _keyEnemies = new List<GameObject>();

    /// <summary>열쇠를 부여할 스폰 포인트 인덱스 목록</summary>
    private readonly List<int> _keySpawnIndices = new List<int>();

    /// <summary>현재 살아있는 적 수</summary>
    public int AliveEnemyCount { get; private set; } = 0;

    /// <summary>전체 스폰 완료 여부</summary>
    public bool AllSpawned => spawnPoints.TrueForAll(sp => sp.HasSpawned);

    // ─────────────────────── 초기화 ───────────────────────

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;

        // 열쇠 소유 적 선정 (랜덤 스폰 포인트 keyEnemyCount 개 선택)
        SelectKeySpawnPoints();
    }

    private void Update()
    {
        if (_player == null) return;

        _checkTimer -= Time.deltaTime;
        if (_checkTimer > 0f) return;
        _checkTimer = checkInterval;

        CheckSpawnPoints();
    }

    // ─────────────────────── 스폰 포인트 체크 ───────────────────────

    private void CheckSpawnPoints()
    {
        foreach (SpawnPoint sp in spawnPoints)
        {
            if (sp == null || sp.HasSpawned) continue;
            if (sp.SelectRandomPrefab() == null) continue;
            if (AliveEnemyCount >= maxSpawnCount) return; // 최대 스폰 수 초과 시 중단

            float fDist = Vector3.Distance(_player.position, sp.transform.position);
            if (fDist <= sp.ActivationRange)
                SpawnEnemy(sp);
        }
    }

    // ─────────────────────── 적 생성 ───────────────────────

    private void SpawnEnemy(SpawnPoint _sp)
    {
        // 가중치 기반 랜덤 프리팹 선택
        GameObject prefab = _sp.SelectRandomPrefab();
        if (prefab == null)
        {
            Debug.LogWarning($"[EnemySpawner] {_sp.name} 스폰 프리팹 없음 — 건너뜀");
            _sp.MarkSpawned(null);
            return;
        }

        // 스폰 위치에 살짝 랜덤 오프셋
        Vector2 vSpread  = Random.insideUnitCircle * _sp.SpawnSpreadRadius;
        Vector3 vSpawnPos = _sp.transform.position
                          + new Vector3(vSpread.x, 0f, vSpread.y);

        GameObject enemy = Instantiate(prefab, vSpawnPos,
                                       Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

        // 난이도 배율 적용
        EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();
        if (enemyBase != null)
        {
            enemyBase.ApplyDifficultyScale(healthMultiplier, attackMultiplier);
            enemyBase.OnDied += OnEnemyDied;
        }

        _sp.MarkSpawned(enemy);
        AliveEnemyCount++;

        // 열쇠 소유 적 등록 체크
        TryRegisterKeyEnemy(_sp, enemy);

        Debug.Log($"[EnemySpawner] {enemy.name} 스폰 @ {vSpawnPos}");
    }

    // ─────────────────────── 열쇠 소유 적 선정 ───────────────────────

    /// <summary>
    /// 전체 스폰 포인트 중 keyEnemyCount 개를 랜덤 선택
    /// 해당 스폰 포인트에서 생성된 적이 열쇠를 소유
    /// </summary>
    private void SelectKeySpawnPoints()
    {
        if (spawnPoints.Count < keyEnemyCount)
        {
            Debug.LogWarning($"[EnemySpawner] 스폰 포인트({spawnPoints.Count})가 " +
                             $"열쇠 수({keyEnemyCount})보다 적습니다.");
            return;
        }

        // 스폰 포인트 인덱스 셔플
        List<int> indices = new List<int>();
        for (int i = 0; i < spawnPoints.Count; i++) indices.Add(i);

        for (int i = indices.Count - 1; i > 0; i--)
        {
            int nJ = Random.Range(0, i + 1);
            (indices[i], indices[nJ]) = (indices[nJ], indices[i]);
        }

        // 앞 keyEnemyCount 개를 열쇠 스폰 포인트로 지정
        // 실제 열쇠 분배는 적 생성 후 StemManager.RegisterEnemies() 에서 처리
        _keySpawnIndices.Clear();
        for (int i = 0; i < keyEnemyCount; i++)
            _keySpawnIndices.Add(indices[i]);

        Debug.Log($"[EnemySpawner] 열쇠 소유 스폰 포인트 {keyEnemyCount}개 선정 완료");
    }

    /// <summary>
    /// 스폰된 적이 열쇠 소유 대상인지 확인 후 _keyEnemies 에 추가
    /// SpawnEnemy 내부에서 호출
    /// </summary>
    private void TryRegisterKeyEnemy(SpawnPoint _sp, GameObject _enemy)
    {
        int nIdx = spawnPoints.IndexOf(_sp);
        if (_keySpawnIndices.Contains(nIdx) == false) return;

        _keyEnemies.Add(_enemy);

        // 열쇠 소유 적이 모두 모이면 StemManager 에 등록
        if (_keyEnemies.Count >= keyEnemyCount)
            StemManager.Instance?.RegisterEnemies(_keyEnemies);
    }

    // ─────────────────────── 적 사망 콜백 ───────────────────────

    private void OnEnemyDied(EnemyBase _enemy)
    {
        AliveEnemyCount = Mathf.Max(0, AliveEnemyCount - 1);
        Debug.Log($"[EnemySpawner] 잔여 적: {AliveEnemyCount}");
    }

    // ─────────────────────── 재입장 시 리셋 ───────────────────────

    /// <summary>
    /// 던전 재입장 시 FloorManager 에서 호출
    /// 모든 SpawnPoint 리셋 → 다음 입장 시 재생성
    /// </summary>
    public void ResetSpawner()
    {
        _keyEnemies.Clear();
        _keySpawnIndices.Clear();
        AliveEnemyCount = 0;

        // SpawnPoint 리셋은 씬 재로드 시 자동으로 초기화됨
        SelectKeySpawnPoints();
        Debug.Log("[EnemySpawner] 스포너 리셋 완료");
    }

    // ─────────────────────── 난이도 설정 ───────────────────────

    /// <summary>DungeonDifficultyScaler 에서 호출 — 난이도 배율 설정</summary>
    public void SetDifficultyScale(float _healthMult, float _attackMult)
    {
        healthMultiplier = _healthMult;
        attackMultiplier = _attackMult;
    }

    /// <summary>DungeonDifficultyScaler 에서 호출 — 층별 최대 스폰 수 설정</summary>
    public void SetMaxSpawnCount(int _nCount)
    {
        maxSpawnCount = Mathf.Max(1, _nCount);
        Debug.Log($"[EnemySpawner] 최대 스폰 수 설정: {maxSpawnCount}마리");
    }
}
