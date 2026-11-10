using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 멀티 접속 테스트용 런처 (MultiTest 씬 전용)
///
/// - 방 코드로 Host(방 만들기) 또는 Client(참가) 접속, 혼자 시작은 Single 모드
/// - 호스트가 접속한 플레이어마다 테스트 플레이어를 생성하고, 나가면 제거
/// - WASD 입력을 NetworkInputData 로 전송 (실제 게임은 NavMesh 경로 방향 입력으로 교체 예정)
///
/// [설정]
/// 1. 씬의 빈 오브젝트에 부착하고 playerPrefab 에 NetworkObject 프리팹 연결
/// 2. 씬이 빌드 목록에 있어야 함 (SceneRef 를 빌드 인덱스로 지정)
/// </summary>
public class NetworkLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    [Tooltip("접속한 플레이어마다 생성할 프리팹 (NetworkObject 포함)")]
    [SerializeField] private NetworkObject playerPrefab;

    [Tooltip("최대 인원")]
    [SerializeField] private int maxPlayers = 2;

    private NetworkRunner _runner;
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    private string _sRoomCode = "";
    private string _sStatus = "";
    private bool _bStarting = false;

    // ─────────────────────── 접속 UI (테스트용) ───────────────────────

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20f, 20f, 320f, 220f), GUI.skin.box);

        if (_runner == null)
        {
            GUILayout.Label("방 코드");
            _sRoomCode = GUILayout.TextField(_sRoomCode, 20);

            GUI.enabled = _bStarting == false;
            if (GUILayout.Button("방 만들기 (Host)"))
            {
                TryStart(GameMode.Host);
            }
            if (GUILayout.Button("참가 (Client)"))
            {
                TryStart(GameMode.Client);
            }
            if (GUILayout.Button("혼자 시작"))
            {
                StartGame(GameMode.Single);
            }
            GUI.enabled = true;
        }
        else
        {
            GUILayout.Label("모드: " + _runner.GameMode + " / 방 코드: " + _sRoomCode);
            GUILayout.Label("인원: " + _spawnedPlayers.Count + " (호스트 기준)");
            if (GUILayout.Button("나가기"))
            {
                _runner.Shutdown();
            }
        }

        GUILayout.Label(_sStatus);
        GUILayout.EndArea();
    }

    // 방 코드가 있어야 접속 (호스트와 참가자가 같은 코드를 입력)
    private void TryStart(GameMode _eMode)
    {
        if (string.IsNullOrWhiteSpace(_sRoomCode) == true)
        {
            _sStatus = "방 코드를 입력하세요";
            return;
        }
        StartGame(_eMode);
    }

    // ─────────────────────── 접속 ───────────────────────

    private async void StartGame(GameMode _eMode)
    {
        if (_bStarting == true)
        {
            return;
        }
        _bStarting = true;
        _sStatus = "접속 중...";

        // 위험: 러너와 콜백을 같은 오브젝트에 두면 콜백이 중복 등록되므로 별도 오브젝트에 생성
        GameObject objRunner = new GameObject("NetworkRunner");
        _runner = objRunner.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));

        StartGameArgs args = new StartGameArgs();
        args.GameMode = _eMode;
        args.SessionName = _sRoomCode.Trim();
        args.PlayerCount = maxPlayers;
        args.Scene = sceneInfo;
        args.SceneManager = objRunner.AddComponent<NetworkSceneManagerDefault>();

        StartGameResult result = await _runner.StartGame(args);

        if (result.Ok == false)
        {
            _sStatus = "접속 실패: " + result.ShutdownReason;
            Debug.LogWarning("[NetworkLauncher] 접속 실패: " + result.ShutdownReason);
            if (objRunner != null)
            {
                Destroy(objRunner);
            }
            _runner = null;
        }
        else
        {
            _sStatus = "접속 성공";
            Debug.Log("[NetworkLauncher] 접속 성공: " + _eMode + " / " + args.SessionName);
        }

        _bStarting = false;
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

    // 로컬 키보드 입력을 매 틱 전송
    public void OnInput(NetworkRunner _netRunner, NetworkInput _input)
    {
        NetworkInputData data = new NetworkInputData();
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            Vector3 vDir = Vector3.zero;
            if (keyboard.wKey.isPressed == true)
            {
                vDir.z += 1f;
            }
            if (keyboard.sKey.isPressed == true)
            {
                vDir.z -= 1f;
            }
            if (keyboard.dKey.isPressed == true)
            {
                vDir.x += 1f;
            }
            if (keyboard.aKey.isPressed == true)
            {
                vDir.x -= 1f;
            }
            data.vDirection = vDir;
        }
        // 씬 Player 가 있으면 위치/동작 상태를 함께 전송 (분신 동기화)
        NetworkPlayerAvatar.FillInput(ref data);
        _input.Set(data);
    }

    public void OnShutdown(NetworkRunner _netRunner, ShutdownReason _reason)
    {
        _spawnedPlayers.Clear();
        _runner = null;
        _sStatus = "연결 종료: " + _reason;
        Debug.Log("[NetworkLauncher] 연결 종료: " + _reason);
    }

    public void OnDisconnectedFromServer(NetworkRunner _netRunner, NetDisconnectReason _reason)
    {
        _sStatus = "서버 연결 끊김: " + _reason;
    }

    public void OnConnectFailed(NetworkRunner _netRunner, NetAddress _remoteAddress, NetConnectFailedReason _reason)
    {
        _sStatus = "접속 실패: " + _reason;
    }

    // 사용하지 않는 콜백
    public void OnObjectExitAOI(NetworkRunner _netRunner, NetworkObject _obj, PlayerRef _player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner _netRunner, NetworkObject _obj, PlayerRef _player)
    {
    }

    public void OnConnectRequest(NetworkRunner _netRunner, NetworkRunnerCallbackArgs.ConnectRequest _request, byte[] _token)
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
