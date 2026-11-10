using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 멀티 몬스터 동기화
///
/// - 몬스터는 호스트만 생성하고 움직인다 (스폰, AI, 체력 계산). 위치는 NetworkTransform 이 전달한다
/// - 클라이언트에서는 AI 와 NavMeshAgent 를 끄고, 호스트가 보낸 체력만 반영한다
/// - 피격: 때린 PC 가 피해량을 호스트로 보낸다 (EnemyBase.TakeDamage 에서 RouteDamage 호출)
/// - 사망: 호스트가 막타 친 플레이어와 함께 모든 PC 에 알리고, 각 PC 가 EnemyBase.DieLocal 로 처리한다
///   (드롭/달란은 막타 친 PC 에서만 생성되어 그 플레이어만 보고 주울 수 있다)
/// - 세션 없이 Instantiate 된 경우에는 아무 일도 하지 않는다 (기존 싱글 동작 유지)
///
/// [설정]
/// 적 프리팹 루트에 NetworkObject, NetworkTransform 과 함께 부착
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class NetworkEnemy : NetworkBehaviour
{
    [Networked] public float Health { get; set; }
    [Networked] public float MaxHealth { get; set; }

    private EnemyBase _base;
    private bool _bSpawned = false;      // 세션에서 생성되어 동기화 중인지
    private bool _bDeathSent = false;    // 호스트: 사망을 이미 알렸는지
    private bool _bRemoteHit = false;    // 호스트: 클라이언트가 보낸 피해를 적용하는 중인지
    private PlayerRef _lastAttacker;     // 호스트: 마지막으로 때린 플레이어
    private float _fShownHealth = -1f;   // 클라이언트: 마지막으로 반영한 체력

    // 호스트가 생성한 몬스터 목록 (층 이동 시 일괄 제거용)
    private static readonly List<NetworkEnemy> _hostEnemies = new List<NetworkEnemy>();

    /// <summary>세션에서 생성된 몬스터이고 내가 호스트가 아닌지 (호스트만 하는 처리를 건너뛸 때 사용)</summary>
    public bool IsClientProxy => _bSpawned == true && HasStateAuthority == false;

    private void Awake()
    {
        _base = GetComponent<EnemyBase>();
    }

    public override void Spawned()
    {
        _bSpawned = true;

        if (HasStateAuthority == true)
        {
            _hostEnemies.Add(this);
            return;
        }

        // 위험: 클라이언트가 먼저 씬을 옮겨도 호스트가 제거하기 전에 스스로 파괴되지 않도록 유지
        Runner.MakeDontDestroyOnLoad(gameObject);

        // 클라이언트에서는 AI 를 돌리지 않음 (Start 전에 꺼서 경로 탐색이 시작되지 않게 함)
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.enabled = false;
        }
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }
    }

    public override void Despawned(NetworkRunner _runner, bool _bHasState)
    {
        _bSpawned = false;
        _hostEnemies.Remove(this);
    }

    // 호스트: 난이도 배율이 적용된 체력을 매 틱 기록 (값이 바뀔 때만 전송됨)
    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        Health = _base.Health;
        MaxHealth = _base.MaxHealth;
    }

    // 클라이언트: 호스트가 보낸 체력을 반영 (줄어들었을 때만 HP바 표시)
    public override void Render()
    {
        if (HasStateAuthority == true || Health == _fShownHealth)
        {
            return;
        }

        bool bDamaged = _fShownHealth >= 0f && Health < _fShownHealth;
        _fShownHealth = Health;
        _base.ApplyNetworkHealth(Health, MaxHealth, bDamaged);
    }

    // ─────────────────────── 피격 ───────────────────────

    /// <summary>
    /// EnemyBase.TakeDamage 에서 호출
    /// 클라이언트면 피해를 호스트로 보내고 true (로컬 체력 계산 생략), 그 외에는 false
    /// </summary>
    public bool RouteDamage(float _fRawDamage)
    {
        if (_bSpawned == false)
        {
            return false;
        }

        if (HasStateAuthority == true)
        {
            // 호스트 본인이 때린 경우 막타 후보로 기록
            if (_bRemoteHit == false)
            {
                _lastAttacker = Runner.LocalPlayer;
            }
            return false;
        }

        RPC_Damage(_fRawDamage);
        return true;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Damage(float _fRawDamage, RpcInfo _info = default)
    {
        _lastAttacker = _info.Source;

        _bRemoteHit = true;
        _base.TakeDamage(_fRawDamage);
        _bRemoteHit = false;
    }

    // ─────────────────────── 사망 ───────────────────────

    /// <summary>
    /// EnemyBase.Die 에서 호출
    /// 세션 몬스터면 모든 PC 에 사망을 알리고 true, 그 외에는 false (기존 방식으로 처리)
    /// </summary>
    public bool RouteDeath()
    {
        if (_bSpawned == false)
        {
            return false;
        }

        if (HasStateAuthority == true && _bDeathSent == false)
        {
            _bDeathSent = true;
            RPC_Die(_lastAttacker);
        }
        return true;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Die(PlayerRef _killer)
    {
        _base.DieLocal(_killer == Runner.LocalPlayer);
    }

    /// <summary>
    /// 사망 연출이 끝난 뒤 제거 예약 (EnemyBase.DieLocal 에서 호출)
    /// 세션 몬스터면 true (호스트만 실제로 예약), 그 외에는 false (기존 방식으로 파괴)
    /// </summary>
    public bool ScheduleDespawn(float _fDelay)
    {
        if (_bSpawned == false)
        {
            return false;
        }

        if (HasStateAuthority == true)
        {
            Invoke(nameof(DespawnSelf), _fDelay);
        }
        return true;
    }

    private void DespawnSelf()
    {
        if (_bSpawned == true)
        {
            Runner.Despawn(Object);
        }
    }

    // ─────────────────────── 스폰 / 일괄 제거 ───────────────────────

    /// <summary>
    /// 지금 이 PC 가 몬스터를 생성해도 되는지 (EnemySpawner 에서 호출)
    /// 세션 중에는 호스트만, 그리고 모든 플레이어가 호스트와 같은 층에 도착한 뒤에만 생성한다
    /// </summary>
    public static bool CanSpawn()
    {
        if (NetworkPlayerAvatar.InSession == false)
        {
            return true;
        }

        return NetworkPlayerAvatar.SessionRunner.IsServer == true
            && NetworkPlayerAvatar.AreAllPlayersOnMyFloor() == true;
    }

    /// <summary>적 생성 (세션 중이면 네트워크 생성, 아니면 기존처럼 Instantiate)</summary>
    public static GameObject SpawnEnemyObject(GameObject _prefab, Vector3 _vPosition, Quaternion _qRotation)
    {
        if (NetworkPlayerAvatar.InSession == true)
        {
            NetworkObject netPrefab = _prefab.GetComponent<NetworkObject>();
            if (netPrefab != null)
            {
                return NetworkPlayerAvatar.SessionRunner.Spawn(netPrefab, _vPosition, _qRotation).gameObject;
            }
        }

        return Instantiate(_prefab, _vPosition, _qRotation);
    }

    /// <summary>호스트가 생성한 몬스터를 모두 제거 (층 이동 승인 시, 호스트의 씬 전환 직전에 호출)</summary>
    public static void DespawnAll()
    {
        if (NetworkPlayerAvatar.InSession == false || NetworkPlayerAvatar.SessionRunner.IsServer == false)
        {
            return;
        }

        NetworkEnemy[] enemies = _hostEnemies.ToArray();
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i]._bSpawned == true)
            {
                NetworkPlayerAvatar.SessionRunner.Despawn(enemies[i].Object);
            }
        }
        _hostEnemies.Clear();
    }
}
