// BossNidhogg.cs
// 최종 보스 니드호그 — 머리 3개(왼/오/가운데)를 총괄하는 컨트롤러.
// 좌우 머리가 모두 죽어야 가운데 머리의 무적이 풀리고, 가운데 머리까지 죽으면 보스 사망 처리.
// 몸통 이동/박치기 패턴은 기획에서 삭제됨 — 머리 3개만 고정 위치에서 전투.
// 전투 단계(전투 시작, 한쪽 머리 파괴, 그로기, 중앙 최종전, 보스 사망, 엔딩)와 보스전 수치를 한곳에서 관리한다.
// 담당: 김보민 (보스 패턴 구현: 정건)

using System.Collections;
using UnityEngine;

public class BossNidhogg : MonoBehaviour
{
    public static BossNidhogg Instance { get; private set; }

    // 보스전 전투 단계
    public enum BossPhase
    {
        Start,        // 전투 시작 (세 머리 공격, 가운데 무적)
        OneHeadDown,  // 한쪽 머리 파괴
        Groggy,       // 양쪽 머리 파괴 후 그로기 (가운데 무적 해제, 받는 피해 증가)
        CenterFinal,  // 가운데 머리 단독 최종전 (공격 빨라짐)
        Dead,         // 보스 사망 (공격 중단, Die 애니메이션만 재생)
        Ending        // 엔딩 전환
    }

    [Header("머리 3개 연결")]
    [SerializeField] private BossHead leftHead;
    [SerializeField] private BossHead rightHead;
    [SerializeField] private BossHead centerHead;

    [Header("보스 전체 HP바 (선택)")]
    [SerializeField] private GameObject bossHpBarObject;

    [Header("체력")]
    public float leftHeadHealth = 1500f;
    public float rightHeadHealth = 1500f;
    public float centerHeadHealth = 1200f;
    [Tooltip("머리가 받는 추가 피해 (0.25 = 25%)")]
    public float headDamageBonus = 0.25f;

    [Header("머리 파괴 시 경직")]
    public bool useStaggerOnHeadDeath = true;
    [Tooltip("좌우 머리 파괴 시 남은 머리들이 멈추는 시간 (초)")]
    public float staggerTime = 2f;

    [Header("그로기 (양쪽 머리 파괴 후)")]
    [Tooltip("가운데 머리 공격이 멈추고 받는 피해가 늘어나는 시간 (초)")]
    public float groggyTime = 6f;
    [Tooltip("그로기 중 받는 피해 증가 (0.5 = 50%)")]
    public float groggyDamageBonus = 0.5f;

    [Header("공통 공격")]
    [Tooltip("머리마다 패턴 사이 대기 시간 (초)")]
    public float attackDelay = 5f;
    [Tooltip("공격 예고 존 프리팹 (지름 1 유닛 원형, 색은 노란색에서 빨간색으로 바뀜)")]
    public GameObject telegraphZonePrefab;
    [Tooltip("예고 존이 빨간색으로 바뀌는 시점 (공격 몇 초 전)")]
    public float redWarningTime = 0.3f;

    [Header("[왼쪽] 투사체")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 7f;
    [Tooltip("착탄 범위 반경")]
    public float projectileImpactRadius = 1.5f;
    [Tooltip("한 번에 나올 수 있는 최대 연속 발사 수")]
    public int maxConsecutiveShots = 2;
    [Tooltip("추가 발사가 나올 확률 (%)")]
    [Range(0f, 100f)] public float extraShotChance = 30f;
    [Tooltip("추가 투사체가 떨어지는 플레이어 주변 범위")]
    public float extraShotSpread = 3f;
    [Tooltip("착탄 자리에 위험 지대를 남길지")]
    public bool leaveHazardZone = true;
    public GameObject hazardZonePrefab;
    public float hazardDuration = 3f;
    [Tooltip("위험 지대 초당 피해")]
    public float hazardDamagePerSecond = 10f;

    [Header("[오른쪽] 장판")]
    public GameObject moveSlowFieldPrefab;
    public GameObject attackSlowFieldPrefab;
    [Tooltip("장판 종류별 최대 유지 개수")]
    public int maxFieldCount = 2;
    public float fieldDuration = 4f;
    [Tooltip("이동 속도 저하 장판이 플레이어에게서 떨어지는 최대 거리")]
    public float fieldDistance = 5f;
    [Tooltip("이동 속도 감소 (0.25 = 25%)")]
    [Range(0f, 1f)] public float moveSlowPercent = 0.25f;
    [Tooltip("공격 속도 감소 (0.15 = 15%)")]
    [Range(0f, 1f)] public float attackSlowPercent = 0.15f;
    public float fieldRadius = 2.5f;

    [Header("[가운데] 근접")]
    [Tooltip("근접 공격 예고 시간 (예고 존 표시 후 공격까지)")]
    public float meleeTelegraphTime = 1.5f;
    [Tooltip("근접 공격 범위 반경")]
    public float meleeRange = 5f;

    [Header("[가운데] 최종전 (가운데 머리만 남았을 때)")]
    [Tooltip("최종전 패턴 사이 대기 시간 (초) - 초기값 미정")]
    public float finalAttackDelay = 3f;
    [Tooltip("최종전 근접 예고 시간 (초) - 초기값 미정")]
    public float finalMeleeTelegraphTime = 1f;

    [Header("사망 후 엔딩까지 대기 (초)")]
    public float endingDelay = 3f;

    private bool _leftDead = false;
    private bool _rightDead = false;
    private float _staggerEndTime = 0f;
    private float _groggyEndTime = 0f;

    /// <summary>현재 전투 단계</summary>
    public BossPhase Phase { get; private set; } = BossPhase.Start;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        SetupHead(leftHead, leftHeadHealth);
        SetupHead(rightHead, rightHeadHealth);
        SetupHead(centerHead, centerHeadHealth);

        // 가운데 머리는 좌우 머리가 모두 죽기 전까지 무적
        if (centerHead != null)
        {
            centerHead.SetVulnerable(false);
        }
    }

    // 머리 체력 설정 및 사망 이벤트 구독
    private void SetupHead(BossHead _head, float _fHealth)
    {
        if (_head == null)
        {
            return;
        }
        _head.InitHealth(_fHealth);
        _head.OnDied += OnHeadDied;
    }

    private void Update()
    {
        // 그로기 시간이 끝나면 최종전으로
        if (Phase == BossPhase.Groggy && Time.time >= _groggyEndTime)
        {
            Phase = BossPhase.CenterFinal;
            Debug.Log("[BossNidhogg] 중앙 머리 최종전");
        }
    }

    // 머리 하나가 죽었을 때 호출
    private void OnHeadDied(EnemyBase _head)
    {
        BossHead bossHead = _head as BossHead;
        if (bossHead == null)
        {
            return;
        }

        if (bossHead.Role == BossHead.HeadRole.Center)
        {
            OnBossDefeated();
            return;
        }

        if (bossHead.Role == BossHead.HeadRole.Left)
        {
            _leftDead = true;
        }
        else
        {
            _rightDead = true;
        }

        // 남은 머리 경직
        if (useStaggerOnHeadDeath == true)
        {
            _staggerEndTime = Time.time + staggerTime;
        }

        if (_leftDead == true && _rightDead == true)
        {
            // 양쪽 파괴: 가운데 무적 해제 후 그로기
            Phase = BossPhase.Groggy;
            _groggyEndTime = Time.time + groggyTime;
            if (centerHead != null)
            {
                centerHead.SetVulnerable(true);
            }
            Debug.Log("[BossNidhogg] 양쪽 머리 처치 — 그로기, 가운데 머리 공격 가능");
        }
        else
        {
            Phase = BossPhase.OneHeadDown;
            Debug.Log("[BossNidhogg] 한쪽 머리 처치 — 반대편 머리 공략");
        }
    }

    // 보스 처치 처리 (가운데 머리 사망)
    private void OnBossDefeated()
    {
        if (Phase == BossPhase.Dead || Phase == BossPhase.Ending)
        {
            return;
        }
        Phase = BossPhase.Dead;
        Debug.Log("[BossNidhogg] 보스 니드호그 처치");

        // 모든 머리 공격 중단 (가운데 머리 Die 애니메이션은 BossHead.Die 에서 재생)
        StopHead(leftHead);
        StopHead(rightHead);
        StopHead(centerHead);

        if (bossHpBarObject != null)
        {
            bossHpBarObject.SetActive(false);
        }

        StartCoroutine(GoToEnding());
    }

    private void StopHead(BossHead _head)
    {
        if (_head != null)
        {
            _head.StopAll();
        }
    }

    private IEnumerator GoToEnding()
    {
        yield return new WaitForSeconds(endingDelay);
        Phase = BossPhase.Ending;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerEnding();
        }
    }

    // ─────────────────────── 머리에서 조회 ───────────────────────

    /// <summary>머리가 받는 피해 배율 (기본 추가 피해 + 그로기 증가)</summary>
    public float DamageMultiplier
    {
        get
        {
            float fMult = 1f + headDamageBonus;
            if (Phase == BossPhase.Groggy)
            {
                fMult = fMult * (1f + groggyDamageBonus);
            }
            return fMult;
        }
    }

    /// <summary>패턴 대기 시간 (최종전이면 빨라짐)</summary>
    public float CurrentAttackDelay => Phase == BossPhase.CenterFinal ? finalAttackDelay : attackDelay;

    /// <summary>근접 예고 시간 (최종전이면 빨라짐)</summary>
    public float CurrentMeleeTelegraphTime => Phase == BossPhase.CenterFinal ? finalMeleeTelegraphTime : meleeTelegraphTime;

    // 머리가 공격을 시작하기 전에 호출 — 지금 공격해도 되는지 판단한다.
    // 가운데 머리: 좌우 중 하나라도 공격 중이면 불가 (항상 단독 공격)
    // 좌우 머리  : 가운데가 공격 중이면 불가 (좌우끼리는 동시 공격 가능)
    public bool CanHeadAttack(BossHead _head)
    {
        if (_head == null || Phase == BossPhase.Dead || Phase == BossPhase.Ending)
        {
            return false;
        }

        // 머리 파괴 경직 중이면 모두 대기
        if (Time.time < _staggerEndTime)
        {
            return false;
        }

        if (_head.Role == BossHead.HeadRole.Center)
        {
            // 그로기 중에는 가운데 공격 일시 중단
            if (Phase == BossPhase.Groggy)
            {
                return false;
            }
            return IsSideAttacking() == false;
        }

        return centerHead == null || centerHead.IsAttacking == false;
    }

    // 좌우 머리 중 하나라도 공격 중인지
    private bool IsSideAttacking()
    {
        if (leftHead != null && leftHead.IsAttacking == true)
        {
            return true;
        }
        if (rightHead != null && rightHead.IsAttacking == true)
        {
            return true;
        }
        return false;
    }

    // 보스 머리 위치 (오른쪽 머리의 공격 속도 장판 설치용)
    public Vector3 BossCenterPosition => centerHead != null ? centerHead.transform.position : transform.position;
}
