using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적 상태 이상 (세트 효과용)
///
/// [기획 반영]
/// - 점화 : 지속 시간 동안 초당 피해, 타오르는 이펙트 부착 (불 3세트)
/// - 둔화 : 이동 속도 % 감소 (물 2세트)
/// - 기절 : 이동/공격 정지 (땅 4세트, 보스 제외는 호출 측에서 판정)
///
/// [사용법]
/// - 프리팹에 붙일 필요 없음. SetEffectController 가 GetOrAdd 로 런타임 부착
/// </summary>
public class EnemyStatusEffect : MonoBehaviour
{
    private EnemyBase _base;
    private EnemyAI _ai;
    private NavMeshAgent _agent;
    private NetworkEnemy _netEnemy;

    // 점화
    private float _fIgniteTimer = 0f;
    private float _fIgniteDps = 0f;
    private float _fIgniteTickTimer = 0f;
    private GameObject _objIgniteVFX;

    // 둔화
    private float _fSlowTimer = 0f;
    private float _fSlowPercent = 0f;

    // 기절
    private float _fStunTimer = 0f;
    private bool _bAgentWasStopped = false;

    /// <summary>점화 상태 여부 (불 4세트 폭발 판정)</summary>
    public bool IsIgnited => _fIgniteTimer > 0f;

    /// <summary>적에 붙은 상태 이상 컴포넌트 반환, 없으면 부착</summary>
    public static EnemyStatusEffect GetOrAdd(EnemyBase _enemy)
    {
        EnemyStatusEffect status = _enemy.GetComponent<EnemyStatusEffect>();
        if (status == null)
        {
            status = _enemy.gameObject.AddComponent<EnemyStatusEffect>();
        }
        return status;
    }

    private void Awake()
    {
        _base = GetComponent<EnemyBase>();
        _ai = GetComponent<EnemyAI>();
        _agent = GetComponent<NavMeshAgent>();
        _netEnemy = GetComponent<NetworkEnemy>();
    }

    private void Update()
    {
        if (_base.IsDead == true)
        {
            ClearIgniteVFX();
            return;
        }

        UpdateIgnite();
        UpdateSlow();
        UpdateStun();
    }

    // ─────────────────────── 점화 ───────────────────────

    /// <summary>점화 적용 (재적용 시 지속 시간 갱신)</summary>
    public void ApplyIgnite(float _fDuration, float _fDps, GameObject _prefabVFX)
    {
        _fIgniteTimer = _fDuration;
        _fIgniteDps = _fDps;
        _fIgniteTickTimer = 1f;

        if (_objIgniteVFX == null && _prefabVFX != null)
        {
            _objIgniteVFX = Instantiate(_prefabVFX, transform.position, Quaternion.identity, transform);
        }
    }

    private void UpdateIgnite()
    {
        if (_fIgniteTimer <= 0f)
        {
            return;
        }

        _fIgniteTimer -= Time.deltaTime;
        _fIgniteTickTimer -= Time.deltaTime;

        // 1초마다 초당 피해 적용
        if (_fIgniteTickTimer <= 0f)
        {
            _fIgniteTickTimer += 1f;
            _base.TakeDamage(_fIgniteDps);
        }

        if (_fIgniteTimer <= 0f)
        {
            ClearIgniteVFX();
        }
    }

    private void ClearIgniteVFX()
    {
        if (_objIgniteVFX != null)
        {
            Destroy(_objIgniteVFX);
            _objIgniteVFX = null;
        }
    }

    // ─────────────────────── 둔화 ───────────────────────

    /// <summary>이동 속도 감소 적용 (_fPercent 0.05 = 5%)</summary>
    public void ApplySlow(float _fPercent, float _fDuration)
    {
        // 멀티 클라이언트는 호스트로 전달 (몬스터 이동은 호스트가 처리)
        if (_netEnemy != null && _netEnemy.IsClientProxy == true)
        {
            _netEnemy.RPC_Slow(_fPercent, _fDuration);
            return;
        }

        _fSlowPercent = _fPercent;
        _fSlowTimer = _fDuration;
    }

    private void UpdateSlow()
    {
        if (_agent == null || _fSlowTimer <= 0f)
        {
            return;
        }

        // 끝나는 프레임에 원래 속도로 복구
        _fSlowTimer -= Time.deltaTime;
        _agent.speed = _fSlowTimer > 0f ? _base.MoveSpeed * (1f - _fSlowPercent) : _base.MoveSpeed;
    }

    // ─────────────────────── 기절 ───────────────────────

    /// <summary>기절 적용 (AI 정지)</summary>
    public void ApplyStun(float _fDuration)
    {
        // 멀티 클라이언트는 호스트로 전달 (몬스터 AI 는 호스트가 처리)
        if (_netEnemy != null && _netEnemy.IsClientProxy == true)
        {
            _netEnemy.RPC_Stun(_fDuration);
            return;
        }

        if (_fStunTimer <= 0f)
        {
            if (_ai != null)
            {
                _ai.enabled = false;
            }
            if (_agent != null && _agent.enabled == true)
            {
                _bAgentWasStopped = _agent.isStopped;
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
            }
        }
        _fStunTimer = Mathf.Max(_fStunTimer, _fDuration);
    }

    private void UpdateStun()
    {
        if (_fStunTimer <= 0f)
        {
            return;
        }

        _fStunTimer -= Time.deltaTime;
        if (_fStunTimer > 0f)
        {
            return;
        }

        // 위험: 기절 중 사망하면 EnemyAI 가 스스로 꺼지므로 다시 켜지 않음 (상단 IsDead 가드)
        if (_ai != null)
        {
            _ai.enabled = true;
        }
        if (_agent != null && _agent.enabled == true)
        {
            _agent.isStopped = _bAgentWasStopped;
        }
    }
}
