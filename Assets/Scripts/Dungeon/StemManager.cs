using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 줄기 열쇠 분배 및 층 전환 관리
///
/// [기획 반영]
/// - 층 입장 시 생명체 4마리에게 열쇠(북/남/동/서) 1개씩 랜덤 분배
/// - 열쇠는 층에 들어온 뒤 나갈 때까지 방향별 1개씩, 총 줄기 수만큼만 존재 (재배정 없음)
/// - 같은 열쇠 중복 불가 (층 내 고유)
/// - 어떤 생명체가 어떤 열쇠를 갖는지 플레이어는 모름 → 탐색 필요
/// - 올라가기 전용 줄기(3층)는 열쇠 불필요, 열쇠 분배 대상에서 제외
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
    [Tooltip("1~2층: North/South/East/West 4개\n3층: 1개만 등록")]
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

    // 아직 아무 생명체에게도 주지 않은 열쇠 (한 번 꺼내면 다시 들어오지 않음)
    private readonly Queue<FloorKeyData> _pendingKeys = new Queue<FloorKeyData>();

    // ─────────────────────── 열쇠 분배 ───────────────────────

    /// <summary>
    /// 층 입장 시 호출 — 생명체에게 열쇠 랜덤 분배
    /// 열쇠 소유 생명체가 생성될 때마다 EnemySpawner 에서 AssignKey() 호출
    /// </summary>
    private void DistributeKeys()
    {
        _directionKeyMap.Clear();

        // 올라가기 전용 줄기는 위층에서 이미 연 줄기로 돌아가는 것이라 열쇠 불필요
        List<StemConnector> keyStems = stems.FindAll(stem => stem != null && stem.Mode != StemMode.UpOnly);

        if (keyStems.Count == 0)
        {
            _pendingKeys.Clear();
            Debug.Log($"[StemManager] {currentFloor}층 열쇠 없음 (올라가기 전용 줄기만 존재)");
            return;
        }

        // 열쇠가 필요한 줄기 1개: 열쇠 1종만
        if (keyStems.Count == 1)
        {
            StemConnector stem = keyStems[0];
            _directionKeyMap[ToKeyDirection(stem.Direction)] = GetKeyByDirection(ToKeyDirection(stem.Direction));
            Debug.Log($"[StemManager] {currentFloor}층 (고정 줄기) 열쇠: {stem.Direction}");
            FillPendingKeys();
            return;
        }

        // 1~2층: 줄기 4개, 열쇠 4종 1:1 랜덤 배정
        List<KeyDirection> directions = new List<KeyDirection>
            { KeyDirection.North, KeyDirection.South, KeyDirection.East, KeyDirection.West };

        // Fisher-Yates 셔플
        for (int i = directions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (directions[i], directions[j]) = (directions[j], directions[i]);
        }

        for (int i = 0; i < keyStems.Count && i < directions.Count; i++)
            _directionKeyMap[ToKeyDirection(keyStems[i].Direction)] = GetKeyByDirection(directions[i]);

        FillPendingKeys();
        Debug.Log($"[StemManager] {currentFloor}층 열쇠 분배 완료");
    }

    // 줄기별 열쇠를 섞어서 배정 대기열에 넣음 (층 입장 시 1회)
    private void FillPendingKeys()
    {
        List<FloorKeyData> keys = new List<FloorKeyData>(_directionKeyMap.Values);

        for (int i = keys.Count - 1; i > 0; i--)
        {
            int nJ = Random.Range(0, i + 1);
            (keys[i], keys[nJ]) = (keys[nJ], keys[i]);
        }

        _pendingKeys.Clear();
        foreach (FloorKeyData key in keys)
        {
            _pendingKeys.Enqueue(key);
        }
    }

    /// <summary>
    /// 열쇠 소유 생명체 1마리 등록, 남은 열쇠 중 하나를 배정
    /// 이미 배정한 열쇠는 다시 섞거나 다른 생명체에게 옮기지 않음
    /// </summary>
    public void AssignKey(GameObject _enemy)
    {
        if (_enemy == null || _pendingKeys.Count == 0)
        {
            return;
        }

        FloorKeyData key = _pendingKeys.Dequeue();
        _enemyKeyMap[_enemy] = key;
        Debug.Log($"[StemManager] {_enemy.name} 에게 {key.KeyDirection} 열쇠 배정 (남은 열쇠 {_pendingKeys.Count}개)");
    }

    /// <summary>
    /// 생명체 사망 시 EnemyBase 에서 호출 — 열쇠 드롭
    /// 반환값: 드롭할 FloorKeyData (없으면 null)
    /// </summary>
    public FloorKeyData OnEnemyDied(GameObject enemy)
    {
        if (_enemyKeyMap.TryGetValue(enemy, out FloorKeyData key) == false) return null;
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
        if (_directionKeyMap.TryGetValue(ToKeyDirection(stem.Direction), out FloorKeyData requiredKey) == false)
        {
            Debug.Log($"[StemManager] {stem.Direction} 줄기에 대응하는 열쇠 없음");
            return;
        }

        // ── 인벤토리 연동 (InventorySystem 완성 후 주석 해제) ──────────────
        /*
        InventorySystem inventory = playerObj.GetComponent<InventorySystem>();
        if (inventory == null) return;

        bool hasKey = inventory.TryConsumeItem(requiredKey);
        if (hasKey == false)
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
        _directionKeyMap.TryGetValue(dir, out FloorKeyData k) ? k : null;

    /// <summary>디버그: 열쇠 분배 재추첨</summary>
    [ContextMenu("열쇠 재분배")]
    public void RerollKeys() => DistributeKeys();
}
