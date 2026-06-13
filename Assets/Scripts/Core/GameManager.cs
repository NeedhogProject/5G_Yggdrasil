using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전체 상태 및 씬 전환 관리 싱글턴
///
/// [기획 반영]
/// - 게임 흐름: 타이틀 → 마을 → 던전(1~4층) → 엔딩
/// - 게임 상태: Playing / Paused / GameOver / Ending
/// - 플레이어 사망 시 인벤 드롭 → 집에서 부활
/// - DontDestroyOnLoad 로 씬 전환에도 유지
///
/// [씬 이름 설정]
/// 인스펙터에서 각 씬 이름을 프로젝트에 맞게 설정
/// </summary>
public class GameManager : MonoBehaviour
{
    // 싱글턴

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // 게임 상태

    public enum GameState
    {
        Title,      // 타이틀 화면
        Town,       // 마을
        Dungeon,    // 던전 (1~4층)
        Paused,     // 일시정지
        GameOver,   // 플레이어 사망
        Ending      // 엔딩 (니드호그 처치)
    }

    public GameState CurrentState { get; private set; } = GameState.Title;

    /// <summary>게임 상태 변경 이벤트 (UI 등에서 구독)</summary>
    public event System.Action<GameState> OnGameStateChanged;

    // 씬 이름

    [Header("씬 이름 설정")]
    [SerializeField] private string titleSceneName = "Title";
    [SerializeField] private string townSceneName = "Town";
    [SerializeField] private string floor1SceneName = "Floor_1";
    [SerializeField] private string floor2SceneName = "Floor_2";
    [SerializeField] private string floor3SceneName = "Floor_3";
    [SerializeField] private string floor4SceneName = "Floor_4_Boss";
    [SerializeField] private string endingSceneName = "Ending";

    [Header("사망 후 부활 위치 (마을 집 좌표)")]
    [SerializeField] private Vector3 homeRespawnPosition = new Vector3(-33.61f, -43.46f, -1.83f);

    // 런타임 상태

    /// <summary>현재 층 (0=마을, 1~4=던전)</summary>
    public int CurrentFloor { get; private set; } = 0;

    // 마을 진입 종류 (스폰 위치 분기용)
    public enum TownEntry
    {
        NewGame,
        DungeonReturn,
        PlayerRespawn
    }

    private TownEntry _townEntry = TownEntry.NewGame;

    /// <summary>일시정지 이전 상태 (Resume 시 복귀용)</summary>
    private GameState _stateBeforePause;

    // 게임 시작 흐름

    /// <summary>새 게임 여부 — StartingEquipment 가 참조</summary>
    public bool IsNewGame { get; private set; }

    /// <summary>현재 플레이 중인 세이브 슬롯 (수동 저장 시 사용)</summary>
    public int CurrentSlot { get; private set; } = -1;

    /// <summary>
    /// 새 게임 시작 — 지정 슬롯을 비우고 마을부터 시작
    /// 타이틀의 "새게임" 버튼에서 슬롯 선택 후 호출
    /// </summary>
    public void StartNewGame(int slotIndex)
    {
        _townEntry = TownEntry.NewGame;
        IsNewGame = true;
        CurrentSlot = slotIndex;
        CurrentFloor = 0;

        // 이전 플레이의 영속 상태가 남지 않도록 정리 (씬에 배치된 새 오브젝트가 깨끗하게 시작)
        DestroyPersistentPlayerObjects();

        // 해당 슬롯에 기존 세이브가 있으면 삭제
        if (SaveSystem.Instance != null && SaveSystem.Instance.HasSave(slotIndex))
        {
            SaveSystem.Instance.DeleteSave(slotIndex);
        }

        LoadScene(townSceneName, GameState.Town);
    }

    /// <summary>
    /// 이어하기 — 지정 슬롯의 세이브를 로드
    /// 저장된 층의 씬을 먼저 로드한 뒤, 씬 로드 완료 시점에 데이터 복원
    /// </summary>
    public void ContinueGame(int slotIndex)
    {
        if (SaveSystem.Instance == null || SaveSystem.Instance.HasSave(slotIndex) == false)
        {
            Debug.LogWarning("[GameManager] 슬롯 " + slotIndex + " 에 세이브 없음");
            return;
        }

        IsNewGame = false;
        CurrentSlot = slotIndex;

        // 이전 플레이의 영속 상태가 남지 않도록 정리 (세이브는 새 인스턴스에 복원)
        DestroyPersistentPlayerObjects();

        // 저장된 층 정보 읽기
        SaveData meta = SaveSystem.Instance.GetSaveMeta(slotIndex);
        int savedFloor = meta != null ? meta.currentFloor : 0;
        CurrentFloor = savedFloor;

        string sceneName;

        if (savedFloor == 1)
        {
            sceneName = floor1SceneName;
        }
        else if (savedFloor == 2)
        {
            sceneName = floor2SceneName;
        }
        else if (savedFloor == 3)
        {
            sceneName = floor3SceneName;
        }
        else if (savedFloor == 4)
        {
            sceneName = floor4SceneName;
        }
        else
        {
            sceneName = townSceneName;
        }

        // 씬 로드 완료 후 데이터 복원하도록 예약
        _pendingLoadSlot = slotIndex;
        SceneManager.sceneLoaded += OnContinueSceneLoaded;

        LoadScene(sceneName, savedFloor == 0 ? GameState.Town : GameState.Dungeon);
    }

    private int _pendingLoadSlot = -1;

    private void OnContinueSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnContinueSceneLoaded;

        if (_pendingLoadSlot >= 0)
        {
            // 씬의 Player/Inventory 가 준비된 뒤 복원
            StartCoroutine(LoadDataNextFrame(_pendingLoadSlot));
            _pendingLoadSlot = -1;
        }
    }

    private System.Collections.IEnumerator LoadDataNextFrame(int slotIndex)
    {
        yield return null;

        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.Load(slotIndex);
        }
    }

    /// <summary>현재 슬롯에 수동 저장 (PauseMenu 에서 호출)</summary>
    public void SaveCurrentGame()
    {
        if (CurrentSlot < 0)
        {
            Debug.LogWarning("[GameManager] 저장할 슬롯이 지정되지 않음");
            return;
        }

        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.Save(CurrentSlot);
        }
    }

    /// <summary>타이틀 → 마을 (새 게임 시작, 기본 슬롯 0) — 하위호환</summary>
    public void StartNewGame()
    {
        StartNewGame(0);
    }

    /// <summary>마을 → 던전 1층</summary>
    public void EnterDungeon()
    {
        LoadScene(floor1SceneName, GameState.Dungeon);
        CurrentFloor = 1;
    }

    /// <summary>다음 층으로 이동 (FloorManager 에서 호출)</summary>
    public void GoToNextFloor()
    {
        CurrentFloor++;

        string sceneName;

        if (CurrentFloor == 1)
        {
            sceneName = floor1SceneName;
        }
        else if (CurrentFloor == 2)
        {
            sceneName = floor2SceneName;
        }
        else if (CurrentFloor == 3)
        {
            sceneName = floor3SceneName;
        }
        else if (CurrentFloor == 4)
        {
            sceneName = floor4SceneName;
        }
        else
        {
            sceneName = townSceneName;
        }

        LoadScene(sceneName, GameState.Dungeon);
    }

    /// <summary>씬 이름으로 현재 층 동기화 — YggdrasilPortal 에서 호출</summary>
    public void SyncFloor(string sceneName)
    {
        if (sceneName == floor1SceneName)
        {
            CurrentFloor = 1;
        }
        else if (sceneName == floor2SceneName)
        {
            CurrentFloor = 2;
        }
        else if (sceneName == floor3SceneName)
        {
            CurrentFloor = 3;
        }
        else if (sceneName == floor4SceneName)
        {
            CurrentFloor = 4;
        }
        else
        {
            CurrentFloor = 0;
        }
    }

    /// <summary>층 번호로 동기화 — YggdrasilPortal int 호출 호환용</summary>
    public void SyncFloor(int floor)
    {
        CurrentFloor = Mathf.Clamp(floor, 0, 4);
    }

    /// <summary>층 번호로 직접 씬 이동 — FloorManager.LoadFloor(int) 에서 호출</summary>
    public void GoToFloor(int floor)
    {
        CurrentFloor = Mathf.Clamp(floor, 0, 4);

        string sceneName;

        if (CurrentFloor == 0)
        {
            sceneName = townSceneName;
        }
        else if (CurrentFloor == 1)
        {
            sceneName = floor1SceneName;
        }
        else if (CurrentFloor == 2)
        {
            sceneName = floor2SceneName;
        }
        else if (CurrentFloor == 3)
        {
            sceneName = floor3SceneName;
        }
        else if (CurrentFloor == 4)
        {
            sceneName = floor4SceneName;
        }
        else
        {
            sceneName = townSceneName;
        }

        LoadScene(sceneName, CurrentFloor == 0 ? GameState.Town : GameState.Dungeon);

        if (CurrentFloor == 0)
        {
            _townEntry = TownEntry.DungeonReturn;
        }
    }

    public void ReturnToTown()
    {
        _townEntry = TownEntry.DungeonReturn;
        CurrentFloor = 0;
        LoadScene(townSceneName, GameState.Town);
    }

    /// <summary>사망 후 집에서 부활하며 마을로 복귀 — PlayerDeath 의 다시하기 버튼에서 호출</summary>
    public void RespawnAtHome()
    {
        _townEntry = TownEntry.PlayerRespawn;
        CurrentFloor = 0;
        LoadScene(townSceneName, GameState.Town);
    }

    // 씬 로드 완료 시 스폰 위치 조정 + 자동 저장
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 어떤 경로로 진입해도 층 번호를 씬 이름 기준으로 동기화
        SyncFloor(scene.name);

        if (scene.name != townSceneName)
        {
            // 던전 씬: 영속 플레이어가 이전 씬 위치를 들고 오므로 시작 지점으로 이동
            if (CurrentState == GameState.Dungeon)
            {
                MovePlayerToSpawn("Spawn_DungeonStart");
            }
            return;
        }

        // 마을 진입 종류에 따라 스폰 위치 분기
        if (_townEntry == TownEntry.PlayerRespawn)
        {
            // 사망 후 부활: 집 좌표로 이동하며 안전 위치도 함께 갱신
            // (지하 집은 낙하 임계값보다 낮으므로 SetSafePosition 으로 낙하 복귀를 막는다)
            RespawnPlayerToPosition(homeRespawnPosition);
        }
        else if (_townEntry == TownEntry.DungeonReturn)
        {
            // 던전 복귀 시 마을 중앙으로 이동
            MovePlayerToSpawn("Spawn_TownCenter");
        }

        // 마을 도착 시 자동 저장 (던전 복귀 또는 부활 + 슬롯 지정된 경우)
        bool shouldAutoSave = _townEntry == TownEntry.DungeonReturn || _townEntry == TownEntry.PlayerRespawn;
        if (shouldAutoSave == true && CurrentSlot >= 0)
        {
            StartCoroutine(AutoSaveNextFrame());
        }
    }

    // 한 프레임 뒤 저장 (씬 완전히 로드된 후)
    private System.Collections.IEnumerator AutoSaveNextFrame()
    {
        yield return null;

        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.Save(CurrentSlot);
            Debug.Log("[GameManager] 마을 도착 자동 저장 완료");
        }
    }

    // 플레이어를 지정한 이름의 스폰 지점으로 이동 (Rigidbody 기반)
    private void MovePlayerToSpawn(string spawnName)
    {
        GameObject spawn = GameObject.Find(spawnName);

        if (spawn == null)
        {
            Debug.LogWarning("[GameManager] " + spawnName + " 를 찾을 수 없습니다.");
            return;
        }

        MovePlayerToPosition(spawn.transform.position);
    }

    // 플레이어를 지정한 좌표로 이동 (Rigidbody 기반)
    private void MovePlayerToPosition(Vector3 targetPosition)
    {
        if (PlayerController.Instance == null)
        {
            return;
        }

        // 위치 직접 설정 후 속도 0 으로 (옮긴 뒤 미끄러짐 방지)
        Rigidbody body = PlayerController.Instance.GetComponent<Rigidbody>();

        if (body != null)
        {
            body.position = targetPosition;
            body.linearVelocity = Vector3.zero;
        }
        else
        {
            PlayerController.Instance.transform.position = targetPosition;
        }
    }

    // 사망 부활 전용: 위치 이동 + 안전 위치 갱신 (지하 집 낙하 복귀 방지)
    private void RespawnPlayerToPosition(Vector3 targetPosition)
    {
        if (PlayerController.Instance == null)
        {
            return;
        }

        // PlayerController 가 위치 이동과 안전 위치 기록을 함께 처리한다.
        PlayerController.Instance.SetSafePosition(targetPosition);
    }

    /// <summary>타이틀로 복귀</summary>
    public void GoToTitle()
    {
        CurrentFloor = 0;

        // 타이틀 씬에 플레이어/인벤토리가 남지 않도록 영속 오브젝트 정리
        DestroyPersistentPlayerObjects();

        LoadScene(titleSceneName, GameState.Title);
    }

    /// <summary>영속(DontDestroyOnLoad) 플레이어 단위 오브젝트 파괴</summary>
    // 새 게임/이어하기/타이틀 복귀 시 호출. 다음 씬에 배치된 오브젝트가 새 인스턴스가 된다
    // 주의: 지연 파괴(Destroy)를 쓰면 새 씬의 싱글턴 Awake 가 아직 살아있는 이전 인스턴스를 보고
    // 자기 자신을 파괴해 인벤토리/플레이어가 사라진다. 즉시 파괴(DestroyImmediate)가 필수
    private void DestroyPersistentPlayerObjects()
    {
        if (PlayerController.Instance != null)
        {
            DestroyImmediate(PlayerController.Instance.gameObject);
        }

        if (InventorySystem.Instance != null)
        {
            DestroyImmediate(InventorySystem.Instance.gameObject);
        }

        if (ResourceInventory.Instance != null)
        {
            DestroyImmediate(ResourceInventory.Instance.gameObject);
        }
    }

    // 일시정지

    /// <summary>일시정지 토글</summary>
    public void TogglePause()
    {
        if (CurrentState == GameState.Paused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        if (CurrentState == GameState.Paused)
        {
            return;
        }

        _stateBeforePause = CurrentState;
        Time.timeScale = 0f;
        ChangeState(GameState.Paused);
    }

    public void Resume()
    {
        if (CurrentState != GameState.Paused)
        {
            return;
        }

        Time.timeScale = 1f;
        ChangeState(_stateBeforePause);
    }

    // 플레이어 사망

    /// <summary>
    /// 플레이어 사망 처리 — PlayerDeath 에서 호출
    /// GameOver 상태로 전환만 한다. 실제 복귀/타이틀 이동은 GameOverPanel 버튼이 처리한다.
    /// </summary>
    public void OnPlayerDeath()
    {
        if (CurrentState == GameState.GameOver)
        {
            return;
        }

        ChangeState(GameState.GameOver);
        Time.timeScale = 0f;

        Debug.Log("[GameManager] 플레이어 사망 → GameOver");

        // 자동 복귀 제거: GameOverPanel 의 다시하기/타이틀 버튼이 직접 처리한다.
    }

    // 엔딩

    /// <summary>니드호그 처치 → 엔딩. BossNidhogg 에서 호출</summary>
    public void TriggerEnding()
    {
        ChangeState(GameState.Ending);
        LoadScene(endingSceneName, GameState.Ending);
    }

    // 씬 전환

    private void LoadScene(string sceneName, GameState nextState)
    {
        ChangeState(nextState);
        SceneManager.LoadScene(sceneName);
        Debug.Log("[GameManager] 씬 전환 → " + sceneName + " (" + nextState + ")");
    }

    private void ChangeState(GameState newState)
    {
        CurrentState = newState;

        if (OnGameStateChanged != null)
        {
            OnGameStateChanged.Invoke(newState);
        }
    }

    // 유틸

    /// <summary>현재 던전 안인지</summary>
    public bool IsInDungeon => CurrentState == GameState.Dungeon;

    /// <summary>현재 마을인지</summary>
    public bool IsInTown => CurrentState == GameState.Town;

    /// <summary>게임 플레이 가능한 상태인지 (일시정지/사망/엔딩 제외)</summary>
    public bool IsPlaying => CurrentState == GameState.Town
                          || CurrentState == GameState.Dungeon;

    private void OnApplicationQuit()
    {
        Time.timeScale = 1f;
    }

#if UNITY_EDITOR
    [ContextMenu("테스트: 마을로")] private void TestTown() { ReturnToTown(); }
    [ContextMenu("테스트: 던전으로")] private void TestDungeon() { EnterDungeon(); }
    [ContextMenu("테스트: 사망")] private void TestDeath() { OnPlayerDeath(); }
#endif
}