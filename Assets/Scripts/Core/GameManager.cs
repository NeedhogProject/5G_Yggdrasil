using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전체 상태 및 씬 전환 관리 싱글턴
///
/// [기획 반영]
/// - 게임 흐름: 타이틀에서 마을, 던전(1~4층), 엔딩 순서로 진행
/// - 게임 상태: Playing / Paused / GameOver / Ending
/// - 플레이어 사망 시 인벤 드롭 후 마을 복귀
/// - DontDestroyOnLoad 로 씬 전환에도 유지
///
/// [저장 시스템 제거]
/// - 이어하기 및 저장/불러오기 기능을 완전히 제거함
/// - IsNewGame, CurrentSlot 프로퍼티는 다른 파일 호환을 위해 남겨둠
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

    // 런타임 상태

    /// <summary>현재 층 (0=마을, 1~4=던전)</summary>
    public int CurrentFloor { get; private set; } = 0;

    // 마을 진입 종류 (스폰 위치 분기용)
    public enum TownEntry
    {
        NewGame,
        DungeonReturn,
        Respawn
    }

    private TownEntry _townEntry = TownEntry.NewGame;

    /// <summary>일시정지 이전 상태 (Resume 시 복귀용)</summary>
    private GameState _stateBeforePause;

    // 게임 시작 흐름

    /// <summary>새 게임 여부 — StartingEquipment 가 참조</summary>
    public bool IsNewGame { get; private set; }

    /// <summary>
    /// 저장 슬롯 개념은 제거되었으나 다른 파일 호환을 위해 프로퍼티만 남겨둔다.
    /// 항상 -1 을 유지한다.
    /// </summary>
    public int CurrentSlot { get; private set; } = -1;

    /// <summary>
    /// 새 게임 시작 — 마을부터 시작
    /// 타이틀의 "게임 시작" 버튼에서 호출
    /// </summary>
    public void StartNewGame()
    {
        _townEntry = TownEntry.NewGame;
        IsNewGame = true;
        CurrentFloor = 0;

        // 이전 플레이의 영속 상태가 남지 않도록 정리 (씬에 배치된 새 오브젝트가 깨끗하게 시작)
        DestroyPersistentPlayerObjects();

        LoadScene(townSceneName, GameState.Town);
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

    /// <summary>사망 후 다시하기 — 새 게임과 동일하게 집 안에서 부활</summary>
    public void RespawnAtHome()
    {
        _townEntry = TownEntry.Respawn;
        CurrentFloor = 0;
        LoadScene(townSceneName, GameState.Town);
    }

    // 씬 로드 완료 시 스폰 위치 조정
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

        // 던전 복귀 시 마을 중앙으로 이동
        if (_townEntry == TownEntry.DungeonReturn)
        {
            MovePlayerToSpawn("Spawn_TownCenter");
        }

        // 새 게임은 집 안에서 시작 — 카메라를 집 고정 위치로 (한 프레임 뒤: CameraFollow Start 완료 후)
        if (_townEntry == TownEntry.NewGame)
        {
            StartCoroutine(MoveCameraToHouseNextFrame());
        }

        // 사망 부활은 던전에서 마을로 오므로 플레이어를 집 위치로 옮긴 뒤 카메라 고정
        if (_townEntry == TownEntry.Respawn)
        {
            MovePlayerToSpawn("Spawn_House");
            StartCoroutine(MoveCameraToHouseNextFrame());
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

        if (PlayerController.Instance == null)
        {
            return;
        }

        // 위치 직접 설정 후 속도 0 으로 (옮긴 뒤 미끄러짐 방지)
        Rigidbody body = PlayerController.Instance.GetComponent<Rigidbody>();

        if (body != null)
        {
            body.position = spawn.transform.position;
            body.linearVelocity = Vector3.zero;
        }
        else
        {
            PlayerController.Instance.transform.position = spawn.transform.position;
        }
    }

    // 한 프레임 뒤 카메라 고정 (씬의 CameraFollow Awake/Start 완료를 보장)
    private System.Collections.IEnumerator MoveCameraToHouseNextFrame()
    {
        yield return null;
        MoveCameraToHousePoint();
    }

    // 새 게임 시작 시 카메라를 집 고정 위치(HouseCameraPoint)로 이동하고 추적 정지
    private void MoveCameraToHousePoint()
    {
        GameObject objPoint = GameObject.Find("HouseCameraPoint");
        if (objPoint == null)
        {
            Debug.LogWarning("[GameManager] HouseCameraPoint 를 찾을 수 없습니다. 카메라 고정 생략.");
            return;
        }

        CameraFollow camFollow = FindFirstObjectByType<CameraFollow>();
        if (camFollow == null)
        {
            return;
        }

        camFollow.MoveToFixedPoint(objPoint.transform);
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
    // 새 게임/타이틀 복귀 시 호출. 다음 씬에 배치된 오브젝트가 새 인스턴스가 된다
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
    /// 인벤토리 드롭 후 GameOver 상태로 전환하고 마을 복귀
    /// </summary>
    public void OnPlayerDeath()
    {
        if (CurrentState == GameState.GameOver)
        {
            return;
        }

        ChangeState(GameState.GameOver);
        Time.timeScale = 0f;

        Debug.Log("[GameManager] 플레이어 사망 후 GameOver 로 전환");

        // 임시: 3초 후 자동 복귀
        Invoke(nameof(GameOverToTown), 3f);
    }

    private void GameOverToTown()
    {
        Time.timeScale = 1f;
        ReturnToTown();
    }

    // 엔딩

    /// <summary>니드호그 처치 후 엔딩으로 전환. BossNidhogg 에서 호출</summary>
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
        Debug.Log("[GameManager] 씬 전환 후 로드: " + sceneName + " (" + nextState + ")");
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