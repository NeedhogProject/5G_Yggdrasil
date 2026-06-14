using UnityEngine;
using System;

/// <summary>
/// 적 베이스 클래스
///
/// [기획 반영]
/// - 체력/공격력/이동속도/공격범위 스탯
/// - 피격 시 HP바 표시
/// - 사망 시 StemManager 에 열쇠 드롭 알림
/// - 층이 깊어질수록 스탯 강화 (DungeonDifficultyScaler 연동)
/// - EnemyAI 에서 상속하여 사용
/// </summary>
public class EnemyBase : MonoBehaviour
{
    // ─────────────────────── 기본 스탯 ───────────────────────

    [Header("기본 스탯")]
    [SerializeField] protected float maxHealth    = 100f;
    [SerializeField] protected float attackPower  = 10f;
    [SerializeField] protected float moveSpeed    = 3f;
    [SerializeField] protected float attackRange  = 1.5f;

    [Header("방어력")]
    [Tooltip("0~100. PlayerStats 의 방어력 공식과 동일하게 적용")]
    [SerializeField] protected float defense = 0f;

    [Header("달란 드롭 (이번 학기: 즉시 지급)")]
    [Tooltip("처치 시 달란을 지급할 확률 0~1. 층/종류별 차등은 프리팹마다 값을 다르게 설정")]
    [SerializeField] [Range(0f, 1f)] protected float goldDropChance = 1f;
    [Tooltip("지급 달란 최소값 (포함)")]
    [SerializeField] protected int goldMin = 0;
    [Tooltip("지급 달란 최대값 (포함)")]
    [SerializeField] protected int goldMax = 0;

    // ─────────────────────── 런타임 상태 ───────────────────────

    protected float _currentHealth;
    protected bool  _isDead = false;

    public bool IsDead => _isDead;

    // ─────────────────────── HP바 ───────────────────────

    [Header("HP바")]
    [Tooltip("피격 시 표시할 HP바 UI 오브젝트 (World Space Canvas 권장)")]
    [SerializeField] private GameObject hpBarObject;

    [Tooltip("HP바 Fill 이미지 (Image 컴포넌트)")]
    [SerializeField] private UnityEngine.UI.Image hpBarFill;

    [Tooltip("HP바 자동 숨김 시간 (초). 0이면 항상 표시")]
    [SerializeField] private float hpBarHideDelay = 3f;

    private float _hpBarTimer = 0f;
    private bool  _hpBarVisible = false;

    // ─────────────────────── 이벤트 ───────────────────────

    public event Action<float, float> OnHealthChanged; // (current, max)
    public event Action<EnemyBase>    OnDied;

    // ─────────────────────── 프로퍼티 ───────────────────────

    public float MaxHealth   => maxHealth;
    public float Health      => _currentHealth;
    public float AttackPower => attackPower;
    public float MoveSpeed   => moveSpeed;
    public float AttackRange => attackRange;

    // ─────────────────────── 초기화 ───────────────────────

    protected virtual void Awake()
    {
        _currentHealth = maxHealth;
        if (hpBarObject != null) hpBarObject.SetActive(false);
    }

    protected virtual void Update()
    {
        UpdateHPBar();
    }

    // ─────────────────────── 피격 / 데미지 ───────────────────────

    /// <summary>
    /// 데미지 수신 — PlayerCombat.OnHit 에서 호출
    /// PlayerStats.TakeDamage 와 동일한 방어력 공식 적용
    /// </summary>
    public virtual void TakeDamage(float rawDamage)
    {
        if (_isDead) return;

        float actualDamage = CalculateDamage(rawDamage);
        _currentHealth = Mathf.Max(0f, _currentHealth - actualDamage);

        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        ShowHPBar();

        Debug.Log($"[{gameObject.name}] 피격 — 데미지: {actualDamage:F1} / 체력: {_currentHealth:F1}/{maxHealth}");

        if (_currentHealth <= 0f) Die();
    }

    /// <summary>
    /// 방어력 기반 데미지 계산 (PlayerStats.TakeDamage 와 동일한 공식)
    /// </summary>
    private float CalculateDamage(float rawDamage)
    {
        // 방어력 100 당 5% 경감 (PlayerStats 와 동일 공식)
        float dd = defense * 0.05f;
        dd = Mathf.Clamp(dd, 0f, 95f);
        float damageRatio = Mathf.Max(1f - (dd / 100f), 0.05f);
        return rawDamage * damageRatio;
    }

    // ─────────────────────── 사망 ───────────────────────

    protected virtual void Die()
    {
        if (_isDead) return;
        _isDead = true;

        Debug.Log($"[{gameObject.name}] 사망");

        // 열쇠 드롭 — StemManager 에 알림
        FloorKeyData droppedKey = StemManager.Instance?.OnEnemyDied(gameObject);
        if (droppedKey != null)
            DropKey(droppedKey);

        // 아이템 드롭 — LootTable 연동
        GetComponent<LootTable>()?.RollDrop(transform.position);

        // 달란 지급
        GrantGold();

        OnDied?.Invoke(this);

        // 사망 연출 후 오브젝트 제거
        // 애니메이션 있을 경우 Invoke 딜레이 조정
        Invoke(nameof(DestroyEnemy), 0.5f);
    }

    private void DropKey(FloorKeyData key)
    {
        // 열쇠 아이템을 월드에 스폰
        // InventorySystem 완성 전 임시: 로그만 출력
        Debug.Log($"[{gameObject.name}] {key.KeyDirection} 열쇠 드롭");
        // TODO: 열쇠 아이템 프리팹 스폰 → 플레이어 획득 시 인벤토리 추가
    }

    // 달란 지급 — 이번 학기는 즉시 지급(인스펙터 확률/범위 기반)
    // 다음 학기 데이터 테이블 기반(층/종류별)으로 전환 시 이 메서드 내부만 교체
    protected virtual void GrantGold()
    {
        if (PlayerStats.Instance == null)
        {
            return;
        }
        if (goldMax <= 0)
        {
            return;
        }
        // 확률 판정
        if (UnityEngine.Random.value > goldDropChance)
        {
            return;
        }

        // 최소~최대 범위 랜덤 (Range 의 max 는 배타적이라 +1)
        int nLow = Mathf.Min(goldMin, goldMax);
        int nHigh = Mathf.Max(goldMin, goldMax);
        int nAmount = UnityEngine.Random.Range(nLow, nHigh + 1);
        if (nAmount <= 0)
        {
            return;
        }

        // gold setter 가 상한 999999 로 클램프
        PlayerStats.Instance.gold += nAmount;
        Debug.Log($"[{gameObject.name}] 달란 +{nAmount}");
    }

    private void DestroyEnemy() => Destroy(gameObject);

    // ─────────────────────── HP바 ───────────────────────

    private void ShowHPBar()
    {
        if (hpBarObject == null) return;
        hpBarObject.SetActive(true);
        _hpBarVisible = true;
        _hpBarTimer   = hpBarHideDelay;
        UpdateHPBarFill();
    }

    private void UpdateHPBar()
    {
        if (_hpBarVisible == false || hpBarHideDelay <= 0f) return;

        _hpBarTimer -= Time.deltaTime;
        if (_hpBarTimer <= 0f)
        {
            _hpBarVisible = false;
            if (hpBarObject != null) hpBarObject.SetActive(false);
        }
    }

    private void UpdateHPBarFill()
    {
        if (hpBarFill == null) return;
        hpBarFill.fillAmount = maxHealth > 0f ? _currentHealth / maxHealth : 0f;
    }

    // ─────────────────────── 스탯 조정 (DungeonDifficultyScaler 연동) ───────────────────────

    /// <summary>
    /// 층/거리 기반 스탯 배율 적용
    /// DungeonDifficultyScaler 에서 호출
    /// </summary>
    public void ApplyDifficultyScale(float healthMult, float attackMult)
    {
        maxHealth    *= healthMult;
        attackPower  *= attackMult;
        _currentHealth = maxHealth;
    }

    // ─────────────────────── 에디터 기즈모 ───────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 공격 범위 시각화
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}
