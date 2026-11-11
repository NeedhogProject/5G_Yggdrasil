using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// 멀티 플레이어 분신 (상대 화면에 보이는 내 캐릭터)
///
/// - 실제 캐릭터는 각자 PC 의 씬 Player 이고, 이 분신은 그 위치/회전/동작을 따라간다
/// - 내 분신은 숨기고(씬 Player 가 보이므로), 상대 분신만 모델/애니메이션/무기를 표시한다
/// - 위치/회전/이동/공격은 NetworkInputData 로, 장착 무기는 RPC 로 호스트에 전달한다
/// - 층 이동은 호스트가 승인한 뒤 모든 PC 가 각자 GameManager.GoToFloor 로 씬을 옮긴다 (파티 전원 이동)
/// - 동료가 있는 동안에는 시간을 멈추지 않는다. 죽으면 살아 있는 동료를 관전하고, 파티가 마을로 돌아오면 집에서 부활한다
///
/// [설정]
/// 1. Player 프리팹 변형에서 게임플레이 컴포넌트를 제거한 프리팹(PlayerAvatar)에 부착
/// 2. NetworkObject, NetworkTransform 과 함께 사용
/// 3. weaponHolder 에 오른손의 WeaponHolder 연결
/// </summary>
public class NetworkPlayerAvatar : NetworkBehaviour
{
    [Tooltip("무기 모델을 붙일 위치 (오른손 WeaponHolder)")]
    [SerializeField] private Transform weaponHolder;

    [Tooltip("Animator 의 UpperBody 레이어 인덱스 (PlayerCombat 과 동일)")]
    [SerializeField] private int upperBodyLayerIndex = 1;
    [SerializeField] private float upperBodyBlendSpeed = 10f;

    [Networked] public byte MoveState { get; set; }               // 0 정지, 1 걷기, 2 달리기
    [Networked] public byte AttackCount { get; set; }             // 공격할 때마다 1 증가
    [Networked] public NetworkString<_64> WeaponId { get; set; }  // 장착 무기 아이템 ID (없으면 빈 문자열)
    [Networked] public byte Floor { get; set; }                   // 주인이 있는 층 (0 마을)
    [Networked] public NetworkBool IsDead { get; set; }           // 주인의 사망 여부

    // 이 거리 이상 한 번에 움직이면 보간 없이 순간이동 (집 문, 낙하 복귀 등)
    private const float TELEPORT_DISTANCE = 3f;

    // 파티 층 이동
    private const float PARTY_MOVE_DELAY = 2.5f;   // 다른 플레이어가 안내를 본 뒤 이동하기까지 시간
    private const float PARTY_MOVE_LOCK  = 5f;     // 호스트가 요청을 승인한 뒤 다음 요청을 받지 않는 시간
    private const float WIPE_RETURN_DELAY = 3f;    // 전원 사망 후 마을로 귀환하기까지 시간

    private Animator _animator;
    private NetworkTransform _netTransform;
    private CapsuleCollider _collider;      // 상대 플레이어와 겹치지 않게 막는 충돌체
    private ChangeDetector _changes;
    private GameObject _objWeaponModel;
    private bool _bModelVisible = true;
    private byte _nPendingFloor = 0;
    private bool _bPartyDeath = false;      // 내 분신: 동료가 있는 상태에서 죽어 관전/귀환 처리 중인지
    private bool _bWipeScheduled = false;   // 내 분신: 전원 사망 귀환을 예약했는지

    // 내 분신 (세션이 없으면 null)
    private static NetworkPlayerAvatar _local;
    // 상대 플레이어 분신 목록
    private static readonly List<NetworkPlayerAvatar> _remotes = new List<NetworkPlayerAvatar>();

    /// <summary>같은 세션에 다른 플레이어가 있는지</summary>
    public static bool HasTeammates => _remotes.Count > 0;

    /// <summary>멀티 세션에 접속 중인지 (혼자 시작한 세션 포함)</summary>
    public static bool InSession => _local != null;

    /// <summary>현재 세션의 러너 (세션 밖이면 null)</summary>
    public static NetworkRunner SessionRunner => _local != null ? _local.Runner : null;
    // 호스트 전용: 이 시각까지 층 이동 요청을 받지 않음
    private static float _fPartyMoveLockUntil = 0f;
    // 층 이동 안내 문구와 표시 종료 시각
    private static string _sNotice = "";
    private static float _fNoticeUntil = 0f;

    // 내 분신 전용: 씬 Player 의 장비와 마지막으로 보낸 무기 ID
    private PlayerEquipment _localEquipment;
    private string _sSentWeaponId = null;

    // 입력 수집용 (씬 Player 기준, 내 PC 에 하나)
    private static PlayerController _player;
    private static PlayerCombat _combat;
    private static PlayerDeath _playerDeath;
    private static Animator _playerAnimator;
    private static bool _bWasAttacking = false;
    private static byte _nAttackCount = 0;

    public override void Spawned()
    {
        _animator = GetComponentInChildren<Animator>();
        _netTransform = GetComponent<NetworkTransform>();
        _collider = GetComponent<CapsuleCollider>();
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

        // 층 이동으로 씬이 바뀌어도 분신이 유지되도록 함
        Runner.MakeDontDestroyOnLoad(gameObject);

        if (HasInputAuthority == true)
        {
            // 내 분신은 숨김 (화면에는 씬 Player 가 보임)
            _local = this;
            SetModelVisible(false);
        }
        else
        {
            _remotes.Add(this);
            RefreshWeaponModel();
        }

        gameObject.name = "PlayerAvatar_" + Object.InputAuthority.PlayerId;
    }

    public override void Despawned(NetworkRunner _runner, bool _bHasState)
    {
        if (_local == this)
        {
            _local = null;
        }
        _remotes.Remove(this);
    }

    // 모델(무기 포함) 표시 여부 전환
    private void SetModelVisible(bool _bVisible)
    {
        _bModelVisible = _bVisible;
        _animator.gameObject.SetActive(_bVisible);
        // 보이는 분신만 길을 막음 (내 분신은 항상 숨김이라 내 캐릭터와 충돌하지 않음)
        _collider.enabled = _bVisible;
    }

    // 호스트(와 예측 중인 본인)가 입력으로 받은 상태를 분신에 반영
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data) == false)
        {
            return;
        }
        if (data.bHasPlayer == false)
        {
            return;
        }

        Quaternion qRotation = Quaternion.Euler(0f, data.fYaw, 0f);
        if ((data.vPosition - transform.position).sqrMagnitude > TELEPORT_DISTANCE * TELEPORT_DISTANCE)
        {
            _netTransform.Teleport(data.vPosition, qRotation);
        }
        else
        {
            transform.SetPositionAndRotation(data.vPosition, qRotation);
        }

        MoveState = data.nMoveState;
        AttackCount = data.nAttackCount;
        Floor = data.nFloor;
        IsDead = data.bDead;
    }

    // 동료가 있는 동안에는 UI(인벤토리, 창고, 지도, 일시정지)가 시간을 멈추지 못하게 함
    private void Update()
    {
        if (_local == this && _remotes.Count > 0 && Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }

    public override void Render()
    {
        if (HasInputAuthority == true)
        {
            SyncLocalWeapon();
            UpdateLocalDeath();
            return;
        }

        // 죽었거나 나와 다른 층에 있는 분신은 숨김 (층 이동 도중 잠깐 갈라지는 구간 포함)
        bool bVisible = IsDead == false && IsOnMyFloor(this) == true;
        if (bVisible != _bModelVisible)
        {
            SetModelVisible(bVisible);
        }

        foreach (string sChange in _changes.DetectChanges(this))
        {
            if (sChange == nameof(AttackCount) && _bModelVisible == true)
            {
                _animator.SetTrigger("Attack");
            }
            else if (sChange == nameof(WeaponId))
            {
                RefreshWeaponModel();
            }
        }

        if (_bModelVisible == false)
        {
            return;
        }

        _animator.SetFloat("Speed", MoveState > 0 ? 1f : 0f);
        _animator.SetBool("IsSprinting", MoveState == 2);
        UpdateUpperBodyLayer();
    }

    // 공격 모션 동안만 상체 레이어를 켬 (PlayerCombat 과 같은 방식)
    private void UpdateUpperBodyLayer()
    {
        if (_animator.layerCount <= upperBodyLayerIndex)
        {
            return;
        }

        bool bAttacking = _animator.GetCurrentAnimatorStateInfo(upperBodyLayerIndex).IsName("Attack");
        if (bAttacking == false && _animator.IsInTransition(upperBodyLayerIndex) == true)
        {
            bAttacking = _animator.GetNextAnimatorStateInfo(upperBodyLayerIndex).IsName("Attack");
        }

        float fTarget = bAttacking ? 1f : 0f;
        float fCurrent = _animator.GetLayerWeight(upperBodyLayerIndex);
        _animator.SetLayerWeight(upperBodyLayerIndex, Mathf.MoveTowards(fCurrent, fTarget, upperBodyBlendSpeed * Time.deltaTime));
    }

    // ─────────────────────── 무기 동기화 ───────────────────────

    // 씬 Player 의 장착 무기가 바뀌면 호스트에 알림 (내 분신에서만 호출)
    private void SyncLocalWeapon()
    {
        if (_localEquipment == null)
        {
            if (PlayerController.Instance == null)
            {
                return;
            }
            _localEquipment = PlayerController.Instance.GetComponent<PlayerEquipment>();
        }

        string sWeaponId = "";
        if (_localEquipment.EquippedWeapon != null && ItemDataRegistry.Instance != null)
        {
            sWeaponId = ItemDataRegistry.Instance.GetId(_localEquipment.EquippedWeapon.Data);
            if (sWeaponId == null)
            {
                sWeaponId = "";
            }
        }

        if (sWeaponId == _sSentWeaponId)
        {
            return;
        }
        _sSentWeaponId = sWeaponId;
        RPC_SetWeapon(sWeaponId);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetWeapon(NetworkString<_64> _sWeaponId)
    {
        WeaponId = _sWeaponId;
    }

    // WeaponId 에 맞는 무기 모델을 손에 부착 (PlayerEquipment.AttachWeaponModel 과 같은 방식)
    private void RefreshWeaponModel()
    {
        if (_objWeaponModel != null)
        {
            Destroy(_objWeaponModel);
            _objWeaponModel = null;
        }

        string sWeaponId = WeaponId.ToString();
        if (string.IsNullOrEmpty(sWeaponId) == true || weaponHolder == null || ItemDataRegistry.Instance == null)
        {
            return;
        }

        WeaponData weaponData = ItemDataRegistry.Instance.FindById(sWeaponId) as WeaponData;
        if (weaponData == null || weaponData.WeaponModelPrefab == null)
        {
            return;
        }

        _objWeaponModel = Instantiate(weaponData.WeaponModelPrefab, weaponHolder);
        _objWeaponModel.transform.localPosition = Vector3.zero;
        _objWeaponModel.transform.localRotation = Quaternion.identity;
    }

    // ─────────────────────── 파티 층 이동 ───────────────────────

    /// <summary>
    /// 파티 전원 층 이동 요청 (FloorManager, YggdrasilPortal 에서 호출)
    /// 멀티 세션 중이 아니면 false 를 돌려주며, 호출한 쪽이 기존 방식으로 직접 이동한다
    /// </summary>
    public static bool RequestPartyMove(int _nFloor)
    {
        if (_local == null)
        {
            return false;
        }

        _local.RPC_RequestPartyMove((byte)_nFloor);
        return true;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestPartyMove(byte _nFloor)
    {
        // 위험: 두 사람이 거의 동시에 다른 층을 요청하면 파티가 갈라지므로 호스트가 먼저 온 요청만 승인
        if (Time.time < _fPartyMoveLockUntil)
        {
            return;
        }
        _fPartyMoveLockUntil = Time.time + PARTY_MOVE_LOCK;

        // 위험: 요청한 클라이언트가 먼저 떠나므로, 이 층의 몬스터를 승인 시점에 바로 제거
        NetworkEnemy.DespawnAll();

        RPC_PartyMove(_nFloor);
    }

    // 요청한 플레이어의 분신에서 모든 PC 로 전달: 요청한 본인은 바로, 다른 플레이어는 안내 후 이동
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PartyMove(byte _nFloor)
    {
        if (HasInputAuthority == true)
        {
            MoveToFloor(_nFloor);
            return;
        }

        string sPlace = _nFloor == 0 ? "마을로" : _nFloor + "층으로";
        ShowNotice("플레이어 " + Object.InputAuthority.PlayerId + "님이 " + sPlace + " 이동합니다", PARTY_MOVE_DELAY);

        _nPendingFloor = _nFloor;
        Invoke(nameof(MoveToPendingFloor), PARTY_MOVE_DELAY);
    }

    private void MoveToPendingFloor()
    {
        MoveToFloor(_nPendingFloor);
    }

    private static void MoveToFloor(byte _nFloor)
    {
        // 죽은 상태로 파티가 마을에 돌아오면 집에서 부활 (다른 층으로는 죽은 채 따라감)
        if (_nFloor == 0 && _playerDeath != null && _playerDeath.IsDead == true)
        {
            _playerDeath.OnRetryClicked();
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToFloor(_nFloor);
        }
    }

    private static void ShowNotice(string _sText, float _fDuration)
    {
        _sNotice = _sText;
        _fNoticeUntil = Time.time + _fDuration;
    }

    // 안내 문구 (임시 표시, 정식 UI 로 교체 예정)
    private void OnGUI()
    {
        if (_local != this || Time.time >= _fNoticeUntil)
        {
            return;
        }

        GUI.Box(new Rect(Screen.width * 0.5f - 220f, 60f, 440f, 36f), _sNotice);
    }

    // ─────────────────────── 사망 / 관전 ───────────────────────

    // 내 캐릭터가 동료와 함께 있다가 죽은 동안: 살아 있는 동료가 있으면 관전, 전원 사망이면 잠시 뒤 마을로 귀환
    private void UpdateLocalDeath()
    {
        if (_playerDeath == null || _playerDeath.IsDead == false)
        {
            _bPartyDeath = false;
            _bWipeScheduled = false;
            return;
        }

        // 혼자 있을 때 죽은 경우는 기존 사망 화면(PlayerDeath)이 처리
        if (_bPartyDeath == false)
        {
            if (_remotes.Count == 0)
            {
                return;
            }
            _bPartyDeath = true;
        }

        NetworkPlayerAvatar teammate = FindAliveTeammate();
        if (teammate != null)
        {
            // 씬이 바뀌면 카메라가 내 캐릭터로 돌아가므로 매 프레임 지정
            CameraFollow follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (follow != null)
            {
                follow.SetTarget(teammate.transform);
            }
            ShowNotice("관전 중 - 파티가 마을로 돌아가면 부활합니다", 0.5f);
            return;
        }

        if (_bWipeScheduled == false)
        {
            _bWipeScheduled = true;
            ShowNotice("전원 사망 - 잠시 뒤 마을로 귀환합니다", WIPE_RETURN_DELAY);
            Invoke(nameof(ReviveAtHome), WIPE_RETURN_DELAY);
        }
    }

    private static NetworkPlayerAvatar FindAliveTeammate()
    {
        for (int i = 0; i < _remotes.Count; i++)
        {
            if (_remotes[i].IsDead == false)
            {
                return _remotes[i];
            }
        }
        return null;
    }

    // 전원 사망 후 귀환: 기존 다시하기와 같은 처리 (회복, 기본 무기 지급, 집에서 부활)
    private void ReviveAtHome()
    {
        if (_playerDeath != null && _playerDeath.IsDead == true)
        {
            _playerDeath.OnRetryClicked();
        }
    }

    // ─────────────────────── 몬스터 연동 ───────────────────────

    /// <summary>
    /// 지정 위치에서 가장 가까운 플레이어 (EnemyAI, EnemySpawner 에서 호출)
    /// 살아 있는 내 캐릭터와, 나와 같은 층에 있는 살아 있는 상대 플레이어 분신 중에서 고른다. 세션 밖이면 내 캐릭터
    /// </summary>
    public static Transform FindNearestPlayer(Vector3 _vPosition)
    {
        Transform trNearest = null;
        float fBestSqr = float.MaxValue;

        // 죽은 플레이어는 대상에서 제외 (모두 죽었으면 null)
        bool bLocalDead = _playerDeath != null && _playerDeath.IsDead == true;
        if (PlayerController.Instance != null && bLocalDead == false)
        {
            trNearest = PlayerController.Instance.transform;
            fBestSqr = (trNearest.position - _vPosition).sqrMagnitude;
        }

        for (int i = 0; i < _remotes.Count; i++)
        {
            if (_remotes[i].IsDead == true || IsOnMyFloor(_remotes[i]) == false)
            {
                continue;
            }

            float fSqr = (_remotes[i].transform.position - _vPosition).sqrMagnitude;
            if (fSqr < fBestSqr)
            {
                fBestSqr = fSqr;
                trNearest = _remotes[i].transform;
            }
        }

        return trNearest;
    }

    /// <summary>모든 상대 플레이어가 나와 같은 층에 있는지 (호스트의 몬스터 생성 시작 조건)</summary>
    public static bool AreAllPlayersOnMyFloor()
    {
        for (int i = 0; i < _remotes.Count; i++)
        {
            if (IsOnMyFloor(_remotes[i]) == false)
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsOnMyFloor(NetworkPlayerAvatar _avatar)
    {
        return GameManager.Instance == null || _avatar.Floor == GameManager.Instance.CurrentFloor;
    }

    /// <summary>몬스터가 이 분신의 주인을 공격 (호스트의 EnemyAI 에서 호출, 주인 PC 에서 자기 방어력으로 피해 적용)</summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_TakeDamage(float _fRawDamage)
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.TakeDamage(_fRawDamage);
        }
    }

    // ─────────────────────── 입력 수집 ───────────────────────

    /// <summary>씬 Player 의 현재 상태를 입력 데이터에 채움 (NetworkLauncher.OnInput 에서 호출)</summary>
    public static void FillInput(ref NetworkInputData _data)
    {
        PlayerController player = PlayerController.Instance;
        if (player == null)
        {
            return;
        }

        if (player != _player)
        {
            _player = player;
            _combat = player.GetComponent<PlayerCombat>();
            _playerDeath = player.GetComponent<PlayerDeath>();
            _playerAnimator = player.GetComponentInChildren<Animator>();
            _bWasAttacking = false;
        }

        // 공격 시작 순간마다 횟수 증가 (상대 화면에서 공격 모션 재생용)
        bool bAttacking = _combat != null && _combat.IsAttacking == true;
        if (bAttacking == true && _bWasAttacking == false)
        {
            _nAttackCount++;
        }
        _bWasAttacking = bAttacking;

        bool bMoving = _playerAnimator != null && _playerAnimator.GetFloat("Speed") > 0.5f;
        byte nMoveState = 0;
        if (bMoving == true)
        {
            nMoveState = player.IsSprinting == true ? (byte)2 : (byte)1;
        }

        _data.bHasPlayer = true;
        _data.vPosition = player.transform.position;
        _data.fYaw = player.transform.eulerAngles.y;
        _data.nMoveState = nMoveState;
        _data.nAttackCount = _nAttackCount;
        _data.bDead = _playerDeath != null && _playerDeath.IsDead == true;
        _data.nFloor = GameManager.Instance != null ? (byte)GameManager.Instance.CurrentFloor : (byte)0;
    }
}
