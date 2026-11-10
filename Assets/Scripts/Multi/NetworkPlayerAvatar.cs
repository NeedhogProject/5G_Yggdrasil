using Fusion;
using UnityEngine;

/// <summary>
/// 멀티 플레이어 분신 (상대 화면에 보이는 내 캐릭터)
///
/// - 실제 캐릭터는 각자 PC 의 씬 Player 이고, 이 분신은 그 위치/회전/동작을 따라간다
/// - 내 분신은 숨기고(씬 Player 가 보이므로), 상대 분신만 모델/애니메이션/무기를 표시한다
/// - 위치/회전/이동/공격은 NetworkInputData 로, 장착 무기는 RPC 로 호스트에 전달한다
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

    // 이 거리 이상 한 번에 움직이면 보간 없이 순간이동 (집 문, 낙하 복귀 등)
    private const float TELEPORT_DISTANCE = 3f;

    private Animator _animator;
    private NetworkTransform _netTransform;
    private ChangeDetector _changes;
    private GameObject _objWeaponModel;

    // 내 분신 전용: 씬 Player 의 장비와 마지막으로 보낸 무기 ID
    private PlayerEquipment _localEquipment;
    private string _sSentWeaponId = null;

    // 입력 수집용 (씬 Player 기준, 내 PC 에 하나)
    private static PlayerController _player;
    private static PlayerCombat _combat;
    private static Animator _playerAnimator;
    private static bool _bWasAttacking = false;
    private static byte _nAttackCount = 0;

    public override void Spawned()
    {
        _animator = GetComponentInChildren<Animator>();
        _netTransform = GetComponent<NetworkTransform>();
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasInputAuthority == true)
        {
            // 내 분신은 숨김 (화면에는 씬 Player 가 보임)
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = false;
            }
        }
        else
        {
            RefreshWeaponModel();
        }

        gameObject.name = "PlayerAvatar_" + Object.InputAuthority.PlayerId;
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
    }

    public override void Render()
    {
        if (HasInputAuthority == true)
        {
            SyncLocalWeapon();
            return;
        }

        foreach (string sChange in _changes.DetectChanges(this))
        {
            if (sChange == nameof(AttackCount))
            {
                _animator.SetTrigger("Attack");
            }
            else if (sChange == nameof(WeaponId))
            {
                RefreshWeaponModel();
            }
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
    }
}
