// BossNidhogg.cs
// 최종 보스 니드호그 — 머리 3개(왼/오/가운데)를 총괄하는 컨트롤러.
// 좌우 머리가 모두 죽어야 가운데 머리의 무적이 풀리고, 가운데 머리까지 죽으면 보스 사망 처리.
// 몸통 이동/박치기 패턴은 기획에서 삭제됨 — 머리 3개만 고정 위치에서 전투.
// 담당: 김보민

using UnityEngine;

public class BossNidhogg : MonoBehaviour
{
    public static BossNidhogg Instance { get; private set; }

    [Header("머리 3개 연결")]
    [SerializeField] private BossHead leftHead;
    [SerializeField] private BossHead rightHead;
    [SerializeField] private BossHead centerHead;

    [Header("보스 전체 HP바 (선택)")]
    [SerializeField] private GameObject bossHpBarObject;

    // 좌우 머리 사망 여부
    private bool _leftDead = false;
    private bool _rightDead = false;
    private bool _bossDefeated = false;

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
        // 각 머리의 사망 이벤트 구독 (EnemyBase.OnDied)
        if (leftHead != null)
        {
            leftHead.OnDied += OnHeadDied;
        }
        if (rightHead != null)
        {
            rightHead.OnDied += OnHeadDied;
        }
        if (centerHead != null)
        {
            centerHead.OnDied += OnHeadDied;

            // 가운데 머리는 시작 시 무적 (BossHead.Awake 에서도 설정하지만 안전하게 한 번 더)
            centerHead.SetVulnerable(false);
        }
    }

    // 머리 하나가 죽었을 때 호출
    private void OnHeadDied(EnemyBase head)
    {
        BossHead bossHead = head as BossHead;
        if (bossHead == null)
        {
            return;
        }

        switch (bossHead.Role)
        {
            case BossHead.HeadRole.Left:
                _leftDead = true;
                Debug.Log("[BossNidhogg] 왼쪽 머리 처치");
                break;
            case BossHead.HeadRole.Right:
                _rightDead = true;
                Debug.Log("[BossNidhogg] 오른쪽 머리 처치");
                break;
            case BossHead.HeadRole.Center:
                OnBossDefeated();
                return;
        }

        // 좌우 머리가 모두 죽으면 가운데 머리 무적 해제
        if (_leftDead == true && _rightDead == true)
        {
            if (centerHead != null)
            {
                centerHead.SetVulnerable(true);
                Debug.Log("[BossNidhogg] 좌우 머리 모두 처치 — 가운데 머리 공격 가능");
            }
        }
    }

    // 보스 처치 처리 (가운데 머리 사망)
    private void OnBossDefeated()
    {
        if (_bossDefeated == true)
        {
            return;
        }
        _bossDefeated = true;

        Debug.Log("[BossNidhogg] 보스 니드호그 처치 — 엔딩");

        if (bossHpBarObject != null)
        {
            bossHpBarObject.SetActive(false);
        }

        // 엔딩 트리거 (GameManager)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerEnding();
        }
    }

    // ─────────────────────── 공격 조율 (가운데 단독 규칙) ───────────────────────

    // 머리가 공격을 시작하기 전에 호출 — 지금 공격해도 되는지 판단한다.
    // 가운데 머리: 좌우 중 하나라도 공격 중이면 불가 (항상 단독 공격)
    // 좌우 머리  : 가운데가 공격 중이면 불가 (좌우끼리는 동시 공격 가능)
    public bool CanHeadAttack(BossHead head)
    {
        if (head == null)
        {
            return false;
        }

        if (head.Role == BossHead.HeadRole.Center)
        {
            // 가운데는 단독 공격 — 좌우 머리가 공격 중이면 대기
            if (IsSideAttacking() == true)
            {
                return false;
            }
            return true;
        }

        // 좌우 머리는 가운데가 공격 중이면 대기
        if (centerHead != null && centerHead.IsAttacking == true)
        {
            return false;
        }
        return true;
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

    // 좌우 머리가 모두 죽었는지 (외부 확인용)
    public bool AreSideHeadsDead
    {
        get { return _leftDead == true && _rightDead == true; }
    }

    // 보스가 처치되었는지 (외부 확인용)
    public bool IsDefeated
    {
        get { return _bossDefeated; }
    }
}