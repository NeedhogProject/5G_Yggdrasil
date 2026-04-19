using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 줄기 열쇠 분배 및 층 전환 관리
///
/// [기획 반영]
/// - 층 입장 시 생명체 4마리에게 열쇠(북/남/동/서) 1개씩 랜덤 분배
/// - 같은 열쇠 중복 불가 (층 내 고유)
/// - 어떤 생명체가 어떤 열쇠를 갖는지 플레이어는 모름 → 탐색 필요
/// - 3→4층: 줄기 1개 고정, 열쇠 1종만 분배
/// - 플레이어가 줄기 앞에서 E키 → 인벤에 맞는 열쇠 있으면 삽입 → 구멍 연출 → 입장
/// </summary>
public class StemManager : MonoBehaviour
{
    // ─────────────────────── 싱글턴 ───────────────────────

    public static StemManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        DistributeKeys();
    }

    // ─────────────────────── 설정 ───────────────────────

    [Header("현재 층 정보")]
    [SerializeField] private int currentFloor = 1;

    [Header("줄기 목록 (방향 순서대로 등록)")]
    [Tooltip("1~3층: North/South/East/West 4개\n3→4층: 1개만 등록")]
    [SerializeField] private List<StemConnector> stems = new List<StemConnector>();

    [Header("열쇠 ScriptableObject (방향별 4종)")]
    [SerializeField] private FloorKeyData northKey;
    [SerializeField] private FloorKeyData southKey;
    [SerializeField] private FloorKeyData eastKey;
    [SerializeField] private FloorKeyData westKey;

    [Header("다음 씬 이름")]
    [SerializeField] private string nextSceneName = "";

    // ─────────────────────── 런타임 상태 ───────────────────────

    /// <summary>생명체 → 열쇠 매핑 (EnemyBase 에서 참조)</summary>
    private readonly Dictionary<GameObject, FloorKeyData> _enemyKeyMap
        = new Dictionary<GameObject, FloorKeyData>();

    /// <summary>방향 → 열쇠 매핑 (어떤 줄기에 어떤 열쇠가 필요한지)</summary>
    private readonly Dictionary<KeyDirection, FloorKeyData> _directionKeyMap
        = new Dictionary<KeyDirection, FloorKeyData>();

    // ─────────────────────── 열쇠 분배 ───────────────────────

    /// <summary>
    /// 층 입장 시 호출 — 생명체에게 열쇠 랜덤 분배
    /// EnemySpawner 에서 생명체 생성 완료 후 RegisterEnemies() 호출 필요
    /// </summary>
    private void DistributeKeys()
    {
        _directionKeyMap.Clear();

        // 3→4층: 줄기 1개 고정 → 열쇠 1종만 (northKey 사용)
        if (stems.Count == 1)
        {
            var stem = stems[0];
            _directionKeyMap[ToKeyDirection(stem.Direction)] = GetKeyByDirection(ToKeyDirection(stem.Direction));
            Debug.Log($"[StemManager] {currentFloor}층 (고정 줄기) 열쇠: {stem.Direction}");
            return;
        }

        // 1~3층: 줄기 4개 → 열쇠 4종 1:1 랜덤 배정
        var directions = new List<KeyDirection>
            { KeyDirection.North, KeyDirection.South, KeyDirection.East, KeyDirection.West };

        // Fisher-Yates 셔플
        for (int i = directions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (directions[i], directions[j]) = (directions[j], directions[i]);
        }

        for (int i = 0; i < stems.Count && i < directions.Count; i++)
            _directionKeyMap[ToKeyDirection(stems[i].Direction)] = GetKeyByDirection(directions[i]);

        Debug.Log($"[StemManager] {currentFloor}층 열쇠 분배 완료");
    }

    /// <summary>
    /// EnemySpawner 에서 이 층의 열쇠 소유 생명체 등록
    /// 열쇠 소유 생명체 목록을 받아 랜덤 분배 (중복 없음)
    /// </summary>
    public void RegisterEnemies(List<GameObject> keyEnemies)
    {
        _enemyKeyMap.Clear();

        if (keyEnemies == null || keyEnemies.Count == 0) return;

        // 분배할 열쇠 목록 수집
        var keys = new List<FloorKeyData>(_directionKeyMap.Values);

        // 생명체 수와 열쇠 수 중 작은 쪽 기준으로 분배
        int count = Mathf.Min(keyEnemies.Count, keys.Count);

        // 열쇠 셔플
        for (int i = keys.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (keys[i], keys[j]) = (keys[j], keys[i]);
        }

        for (int i = 0; i < count; i++)
        {
            _enemyKeyMap[keyEnemies[i]] = keys[i];
            Debug.Log($"[StemManager] {keyEnemies[i].name} → {keys[i].KeyDirection} 열쇠 소유");
        }
    }

    /// <summary>
    /// 생명체 사망 시 EnemyBase 에서 호출 — 열쇠 드롭
    /// 반환값: 드롭할 FloorKeyData (없으면 null)
    /// </summary>
    public FloorKeyData OnEnemyDied(GameObject enemy)
    {
        if (!_enemyKeyMap.TryGetValue(enemy, out var key)) return null;
        _enemyKeyMap.Remove(enemy);
        Debug.Log($"[StemManager] {enemy.name} 사망 → {key.KeyDirection} 열쇠 드롭");
        return key;
    }

    // ─────────────────────── 열쇠 삽입 시도 ───────────────────────

    /// <summary>
    /// StemConnector 에서 E키 입력 시 호출
    /// 플레이어 인벤토리에서 해당 방향 열쇠 확인 후 삽입
    /// </summary>
    public void TryInsertKey(GameObject playerObj, StemConnector stem)
    {
        if (stem.IsUnlocked) return;

        // 이 줄기에 필요한 열쇠 확인
        if (!_directionKeyMap.TryGetValue(ToKeyDirection(stem.Direction), out var requiredKey))
        {
            Debug.Log($"[StemManager] {stem.Direction} 줄기에 대응하는 열쇠 없음");
            return;
        }

        // ── 인벤토리 연동 (InventorySystem 완성 후 주석 해제) ──────────────
        /*
        var inventory = playerObj.GetComponent<InventorySystem>();
        if (inventory == null) return;

        bool hasKey = inventory.TryConsumeItem(requiredKey);
        if (!hasKey)
        {
            Debug.Log($"[StemManager] {stem.Direction} 열쇠 없음 — 삽입 불가");
            // TODO: UI 메시지 ("맞는 열쇠가 없습니다") 표시
            return;
        }
        */

        // ── 임시: 열쇠 체크 없이 바로 성공 (InventorySystem 연동 전) ────────
        Debug.Log($"[StemManager] {stem.Direction} 열쇠 삽입 성공 → 구멍 연출 시작");
        stem.OnKeyInserted();
    }

    /// <summary>구멍 연출 완료 후 StemConnector 에서 호출 — 씬 전환</summary>
    public void EnterNextFloor()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("[StemManager] nextSceneName 미설정");
            return;
        }
        FloorManager.Instance?.LoadFloor(nextSceneName);
    }

    // ─────────────────────── 유틸 ───────────────────────

    /// <summary>StemDirection → KeyDirection 변환 — StemConnector.Direction 참조 시 사용</summary>
    private static KeyDirection ToKeyDirection(StemDirection d) => d switch
    {
        StemDirection.North => KeyDirection.North,
        StemDirection.South => KeyDirection.South,
        StemDirection.East  => KeyDirection.East,
        StemDirection.West  => KeyDirection.West,
        _                   => KeyDirection.North
    };

    private FloorKeyData GetKeyByDirection(KeyDirection dir)
    {
        return dir switch
        {
            KeyDirection.North => northKey,
            KeyDirection.South => southKey,
            KeyDirection.East  => eastKey,
            KeyDirection.West  => westKey,
            _                  => null
        };
    }

    /// <summary>현재 층 번호</summary>
    public int CurrentFloor => currentFloor;

    /// <summary>특정 줄기에 필요한 열쇠 반환 (UI 힌트용)</summary>
    public FloorKeyData GetRequiredKey(KeyDirection dir) =>
        _directionKeyMap.TryGetValue(dir, out var k) ? k : null;

    /// <summary>디버그: 열쇠 분배 재추첨</summary>
    [ContextMenu("열쇠 재분배")]
    public void RerollKeys() => DistributeKeys();
}
