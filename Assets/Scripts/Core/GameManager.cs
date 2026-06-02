using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전체 상태 및 씬 전환 관리 싱글턴
///
/// [기획 반영]
/// - 게임 흐름: 타이틀 → 마을 → 던전(1~4층) → 엔딩
/// - 게임 상태: Playing / Paused / GameOver / Ending
/// - 플레이어 사망 시 인벤 드롭 → 마을 복귀
/// - DontDestroyOnLoad 로 씬 전환에도 유지
///
/// [씬 이름 설정]
/// 인스펙터에서 각 씬 이름을 프로젝트에 맞게 설정
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─────────────────────── 싱글턴 ───────────────────────

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────── 게임 상태 ───────────────────────

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

    // ─────────────────────── 씬 이름 ───────────────────────

    [Header("씬 이름 설정")]
    [SerializeField] private string titleSceneName  = "Title";
    [SerializeField] private string townSceneName   = "Town";
    [SerializeField] private string floor1SceneName = "Floor_1";
    [SerializeField] private string floor2SceneName = "Floor_2";
    [SerializeField] private string floor3SceneName = "Floor_3";
    [SerializeField] private string floor4SceneName = "Floor_4_Boss";
    [SerializeField] private string endingSceneName = "Ending";

    // ─────────────────────── 런타임 상태 ───────────────────────

    /// <summary>현재 층 (0=마을, 1~4=던전)</summary>
    public int CurrentFloor { get; private set; } = 0;

    /// <summary>일시정지 이전 상태 (Resume 시 복귀용)</summary>
    private GameState _stateBeforePause;

    // ─────────────────────── 게임 시작 흐름 ───────────────────────

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
        IsNewGame   = true;
        CurrentSlot = slotIndex;
        CurrentFloor = 0;

        // 해당 슬롯에 기존 세이브가 있으면 삭제
        if (SaveSystem.Instance != null && SaveSystem.Instance.HasSave(slotIndex))
            SaveSystem.Instance.DeleteSave(slotIndex);

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
            Debug.LogWarning($"[GameManager] 슬롯 {slotIndex} 에 세이브 없음");
            return;
        }

        IsNewGame   = false;
        CurrentSlot = slotIndex;

        // 저장된 층 정보 읽기
        SaveData meta = SaveSystem.Instance.GetSaveMeta(slotIndex);
        int savedFloor = meta != null ? meta.currentFloor : 0;
        CurrentFloor = savedFloor;

        string sceneName = savedFloor switch
        {
            0 => townSceneName,
            1 => floor1SceneName,
            2 => floor2SceneName,
            3 => floor3SceneName,
            4 => floor4SceneName,
            _ => townSceneName
        };

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
        SaveSystem.Instance?.Load(slotIndex);
    }

    /// <summary>현재 슬롯에 수동 저장 (집/특정 NPC 에서 호출)</summary>
    public void SaveCurrentGame()
    {
        if (CurrentSlot < 0)
        {
            Debug.LogWarning("[GameManager] 저장할 슬롯이 지정되지 않음");
            return;
        }
        SaveSystem.Instance?.Save(CurrentSlot);
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
        string sceneName = CurrentFloor switch
        {
            1 => floor1SceneName,
            2 => floor2SceneName,
            3 => floor3SceneName,
            4 => floor4SceneName,
            _ => townSceneName
        };
        LoadScene(sceneName, GameState.Dungeon);
    }

    /// <summary>
    /// 씬 이름으로 현재 층 동기화 — YggdrasilPortal 에서 호출
    /// </summary>
    public void SyncFloor(string sceneName)
    {
        if      (sceneName == floor1SceneName) CurrentFloor = 1;
        else if (sceneName == floor2SceneName) CurrentFloor = 2;
        else if (sceneName == floor3SceneName) CurrentFloor = 3;
        else if (sceneName == floor4SceneName) CurrentFloor = 4;
        else                                   CurrentFloor = 0;
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
        string sceneName = CurrentFloor switch
        {
            0 => townSceneName,
            1 => floor1SceneName,
            2 => floor2SceneName,
            3 => floor3SceneName,
            4 => floor4SceneName,
            _ => townSceneName
        };
        LoadScene(sceneName, CurrentFloor == 0 ? GameState.Town : GameState.Dungeon);
    }

    public void ReturnToTown()
    {
        CurrentFloor = 0;
        LoadScene(townSceneName, GameState.Town);
    }

    /// <summary>타이틀로 복귀</summary>
    public void GoToTitle()
    {
        CurrentFloor = 0;
        LoadScene(titleSceneName, GameState.Title);
    }

    // ─────────────────────── 일시정지 ───────────────────────

    /// <summary>일시정지 토글</summary>
    public void TogglePause()
    {
        if (CurrentState == GameState.Paused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (CurrentState == GameState.Paused) return;
        _stateBeforePause = CurrentState;
        Time.timeScale    = 0f;
        ChangeState(GameState.Paused);
    }

    public void Resume()
    {
        if (CurrentState != GameState.Paused) return;
        Time.timeScale = 1f;
        ChangeState(_stateBeforePause);
    }

    // ─────────────────────── 플레이어 사망 ───────────────────────

    /// <summary>
    /// 플레이어 사망 처리 — PlayerDeath 에서 호출
    /// 인벤토리 드롭 → GameOver 상태 → 마을 복귀
    /// </summary>
    public void OnPlayerDeath()
    {
        if (CurrentState == GameState.GameOver) return;

        ChangeState(GameState.GameOver);
        Time.timeScale = 0f;

        Debug.Log("[GameManager] 플레이어 사망 → GameOver");

        // TODO: GameOver UI 표시 후 플레이어 입력으로 마을 복귀
        // 임시: 3초 후 자동 복귀
        Invoke(nameof(GameOverToTown), 3f);
    }

    private void GameOverToTown()
    {
        Time.timeScale = 1f;
        ReturnToTown();
    }

    // ─────────────────────── 엔딩 ───────────────────────

    /// <summary>
    /// 니드호그 처치 → 엔딩
    /// BossNidhogg 에서 호출
    /// </summary>
    public void TriggerEnding()
    {
        ChangeState(GameState.Ending);
        LoadScene(endingSceneName, GameState.Ending);
    }

    // ─────────────────────── 씬 전환 ───────────────────────

    private void LoadScene(string sceneName, GameState nextState)
    {
        ChangeState(nextState);
        SceneManager.LoadScene(sceneName);
        Debug.Log($"[GameManager] 씬 전환 → {sceneName} ({nextState})");
    }

    private void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }

    // ─────────────────────── 유틸 ───────────────────────

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
    [ContextMenu("테스트: 마을로")] private void TestTown()   => ReturnToTown();
    [ContextMenu("테스트: 던전으로")] private void TestDungeon() => EnterDungeon();
    [ContextMenu("테스트: 사망")] private void TestDeath()  => OnPlayerDeath();
#endif
}
