using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using Fusion.Photon.Realtime;
using UnityEngine;

/// <summary>
/// 멀티 세션 관리 (GameCore 에 부착, 씬 전환에도 유지)
///
/// - 싱글: 방 코드를 자동으로 만들어 Host 로 접속 (다른 플레이어 참가 가능). 실패하면 오프라인으로 진행
/// - 멀티: 입력한 방 코드로 Client 접속. 호스트가 마을에 없으면 호스트가 거절
/// - 씬은 각 PC 의 GameManager 가 불러오고, 러너는 씬을 불러오지 않는다
/// - 호스트가 접속한 플레이어마다 분신을 생성하고, 나가면 제거
/// - Photon 서버 지역은 kr 고정
///
/// [설정]
/// 1. GameCore 에 부착하고 playerPrefab 에 PlayerAvatar 프리팹 연결
/// </summary>
public class NetworkLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkLauncher Instance { get; private set; }

    [Tooltip("접속한 플레이어마다 생성할 분신 프리팹 (NetworkObject 포함)")]
    [SerializeField] private NetworkObject playerPrefab;

    [Tooltip("최대 인원")]
    [SerializeField] private int maxPlayers = 2;

    private const string REGION = "kr";
    private const int ROOM_CODE_LENGTH = 6;
    // 헷갈리는 글자(0, O, 1, I) 제외
    private const string ROOM_CODE_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    // 이전 세션이 닫히기를 기다리는 최대 시간 (게임 나가기 직후 다시 접속할 때)
    private const float SHUTDOWN_WAIT_SECONDS = 5f;

    private NetworkRunner _runner;
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    private bool _bStarting = false;
    private bool _bJoinedAsClient = false;   // 참가자로 접속했는지 (호스트가 나가면 타이틀로 복귀)

    /// <summary>현재 방 코드 (세션 밖이면 빈 문자열)</summary>
    public static string RoomCode { get; private set; } = "";

    /// <summary>Host 생성에 실패해 오프라인으로 진행 중인지</summary>
    public static bool IsOffline { get; private set; } = false;

    /// <summary>ESC 화면 표시용 문구</summary>
    public static string StatusText
    {
        get
        {
            if (IsOffline == true)
            {
                return "오프라인(참가 불가)";
            }
            return RoomCode.Length > 0 ? "방 코드: " + RoomCode : "";
        }
    }

    /// <summary>접속을 시도하는 중인지 (타이틀 버튼 잠금용)</summary>
    public bool IsStarting => _bStarting;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ─────────────────────── 접속 ───────────────────────

    /// <summary>방 코드를 만들어 Host 로 접속. 실패하면 오프라인으로 전환. 결과는 _onDone 으로 (true 온라인)</summary>
    public async void StartHost(Action<bool> _onDone)
    {
        string sCode = CreateRoomCode();
        ShutdownReason eResult = await StartSession(GameMode.Host, sCode);

        IsOffline = eResult != ShutdownReason.Ok;
        if (IsOffline == true)
        {
            Debug.LogWarning("[NetworkLauncher] Host 생성 실패, 오프라인으로 진행: " + eResult);
        }
        if (_onDone != null)
        {
            _onDone.Invoke(IsOffline == false);
        }
    }

    /// <summary>방 코드로 Client 접속. 결과는 _onDone 으로 (실패 시 안내 문구, 성공 시 null)</summary>
    public async void JoinRoom(string _sCode, Action<string> _onDone)
    {
        string sCode = _sCode == null ? "" : _sCode.Trim().ToUpperInvariant();
        string sError = null;
        if (sCode.Length == 0)
        {
            sError = "방 코드를 입력하세요";
        }
        else
        {
            ShutdownReason eResult = await StartSession(GameMode.Client, sCode);
            sError = ToJoinErrorText(eResult);
            _bJoinedAsClient = eResult == ShutdownReason.Ok;
        }
        if (_onDone != null)
        {
            _onDone.Invoke(sError);
        }
    }

    // 세션 시작 (결과 이유 반환, 성공은 Ok)
    private async System.Threading.Tasks.Task<ShutdownReason> StartSession(GameMode _eMode, string _sCode)
    {
        if (_bStarting == true)
        {
            return ShutdownReason.AlreadyRunning;
        }
        _bStarting = true;
        IsOffline = false;

        // 이전 세션이 닫히는 중이면 OnShutdown 까지 대기
        float fWaitUntil = Time.realtimeSinceStartup + SHUTDOWN_WAIT_SECONDS;
        while (_runner != null && Time.realtimeSinceStartup < fWaitUntil)
        {
            await System.Threading.Tasks.Task.Yield();
        }
        if (_runner != null)
        {
            _bStarting = false;
            return ShutdownReason.AlreadyRunning;
        }

        // 위험: 러너와 콜백을 같은 오브젝트에 두면 콜백이 중복 등록되므로 별도 오브젝트에 생성
        GameObject objRunner = new GameObject("NetworkRunner");
        DontDestroyOnLoad(objRunner);
        _runner = objRunner.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        FusionAppSettings appSettings = PhotonAppSettings.Global.AppSettings.GetCopy();
        appSettings.FixedRegion = REGION;

        StartGameArgs args = new StartGameArgs();
        args.GameMode = _eMode;
        args.SessionName = _sCode;
        args.PlayerCount = maxPlayers;
        args.CustomPhotonAppSettings = appSettings;
        // 씬은 GameManager 가 불러오므로 Scene 은 지정하지 않음
        args.SceneManager = objRunner.AddComponent<NetworkSceneManagerDefault>();

        StartGameResult result = await _runner.StartGame(args);
        _bStarting = false;

        if (result.Ok == false)
        {
            Debug.LogWarning("[NetworkLauncher] 접속 실패: " + _eMode + " / " + result.ShutdownReason);
            if (objRunner != null)
            {
                Destroy(objRunner);
            }
            _runner = null;
            return result.ShutdownReason == ShutdownReason.Ok ? ShutdownReason.Error : result.ShutdownReason;
        }

        RoomCode = _sCode;
        Debug.Log("[NetworkLauncher] 접속 성공: " + _eMode + " / " + _sCode);
        return ShutdownReason.Ok;
    }

    // 참가 실패 안내 문구
    private static string ToJoinErrorText(ShutdownReason _eReason)
    {
        switch (_eReason)
        {
            case ShutdownReason.Ok:
                return null;
            case ShutdownReason.ConnectionRefused:
                return "호스트가 마을에 있지 않습니다";
            case ShutdownReason.GameNotFound:
            case ShutdownReason.GameClosed:
                return "방을 찾을 수 없습니다";
            case ShutdownReason.GameIsFull:
                return "방이 가득 찼습니다";
            default:
                return "접속에 실패했습니다 (" + _eReason + ")";
        }
    }

    private static string CreateRoomCode()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder(ROOM_CODE_LENGTH);
        for (int i = 0; i < ROOM_CODE_LENGTH; i++)
        {
            builder.Append(ROOM_CODE_CHARS[UnityEngine.Random.Range(0, ROOM_CODE_CHARS.Length)]);
        }
        return builder.ToString();
    }

    // ─────────────────────── 러너 콜백 ───────────────────────

    // 호스트만 플레이어를 생성 (입력 권한은 접속한 플레이어에게)
    public void OnPlayerJoined(NetworkRunner _netRunner, PlayerRef _player)
    {
        if (_netRunner.IsServer == false)
        {
            return;
        }

        Vector3 vSpawn = new Vector3(_spawnedPlayers.Count * 2f, 1f, 0f);
        NetworkObject spawned = _netRunner.Spawn(playerPrefab, vSpawn, Quaternion.identity, _player);
        _spawnedPlayers[_player] = spawned;
        Debug.Log("[NetworkLauncher] 플레이어 입장: " + _player);
    }

    public void OnPlayerLeft(NetworkRunner _netRunner, PlayerRef _player)
    {
        if (_spawnedPlayers.TryGetValue(_player, out NetworkObject spawned) == true)
        {
            _netRunner.Despawn(spawned);
            _spawnedPlayers.Remove(_player);
        }
        Debug.Log("[NetworkLauncher] 플레이어 퇴장: " + _player);
    }

    // 씬 Player 의 위치/동작 상태를 매 틱 전송 (분신 동기화)
    public void OnInput(NetworkRunner _netRunner, NetworkInput _input)
    {
        NetworkInputData data = new NetworkInputData();
        NetworkPlayerAvatar.FillInput(ref data);
        _input.Set(data);
    }

    public void OnShutdown(NetworkRunner _netRunner, ShutdownReason _reason)
    {
        _spawnedPlayers.Clear();
        _runner = null;
        RoomCode = "";
        Debug.Log("[NetworkLauncher] 연결 종료: " + _reason);

        // 호스트가 나가서 끊긴 참가자는 저장 후 타이틀로 복귀 (내가 직접 나간 경우는 Ok)
        bool bHostLeft = _bJoinedAsClient == true && _reason != ShutdownReason.Ok;
        _bJoinedAsClient = false;
        if (bHostLeft == true && GameManager.Instance != null)
        {
            GameManager.Instance.GoToTitle();
        }
    }

    public void OnDisconnectedFromServer(NetworkRunner _netRunner, NetDisconnectReason _reason)
    {
        Debug.Log("[NetworkLauncher] 서버 연결 끊김: " + _reason);
    }

    public void OnConnectFailed(NetworkRunner _netRunner, NetAddress _remoteAddress, NetConnectFailedReason _reason)
    {
        Debug.Log("[NetworkLauncher] 접속 실패: " + _reason);
    }

    // 호스트: 마을에 있을 때만 참가 허용
    public void OnConnectRequest(NetworkRunner _netRunner, NetworkRunnerCallbackArgs.ConnectRequest _request, byte[] _token)
    {
        bool bInTown = GameManager.Instance != null && GameManager.Instance.CurrentFloor == 0;
        if (bInTown == true)
        {
            _request.Accept();
        }
        else
        {
            _request.Refuse();
        }
    }

    // 사용하지 않는 콜백
    public void OnObjectExitAOI(NetworkRunner _netRunner, NetworkObject _obj, PlayerRef _player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner _netRunner, NetworkObject _obj, PlayerRef _player)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner _netRunner, SimulationMessagePtr _message)
    {
    }

    public void OnReliableDataReceived(NetworkRunner _netRunner, PlayerRef _player, ReliableKey _key, ReadOnlySpan<byte> _data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner _netRunner, PlayerRef _player, ReliableKey _key, float _fProgress)
    {
    }

    public void OnInputMissing(NetworkRunner _netRunner, PlayerRef _player, NetworkInput _input)
    {
    }

    public void OnConnectedToServer(NetworkRunner _netRunner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner _netRunner, List<SessionInfo> _sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner _netRunner, Dictionary<string, object> _data)
    {
    }

    public void OnHostMigration(NetworkRunner _netRunner, HostMigrationToken _hostMigrationToken)
    {
    }

    public void OnSceneLoadDone(NetworkRunner _netRunner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner _netRunner)
    {
    }
}
