using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 무기별 공격 판정 시스템
///
/// [무기별 판정 방식]
/// - 단검 : 연속 다단히트 — 짧은 간격으로 hitCount 회 판정 (코루틴)
/// - 장검 : OverlapSphere + 각도 필터 — 앞쪽 부채꼴 (1회 판정)
/// - 창   : 관통 찌르기 — OverlapBox 직선상 모든 적 히트 (1회, 중복 없음)
///
/// [기본 수치]
/// - 단검: 리치 1.2, 너비 0.4, 3회 히트, 간격 0.08초
/// - 장검: 반경 2.0, 부채꼴 110도
/// - 창  : 리치 3.5, 너비 0.35 (관통)
///
/// [사용법]
/// PlayerCombat 에서 PerformAttack(weapon, onHit) 호출
/// onHit(GameObject target, int hitIndex) 콜백으로 피격 대상과 히트 회차 전달
/// </summary>
public class HitboxSystem : MonoBehaviour
{
    // ─────────────────────── 레이어 설정 ───────────────────────

    [Header("레이어 설정")]
    [Tooltip("적 감지 레이어")]
    [SerializeField] private LayerMask enemyLayer = ~0;

    [Tooltip("자원 노드 감지 레이어 (나무, 광석 등)")]
    [SerializeField] private LayerMask resourceLayer = ~0;

    /// <summary>공격 판정에 사용할 통합 레이어</summary>
    private LayerMask CombinedLayer => enemyLayer | resourceLayer;

    // ─────────────────────── 단검 설정 ───────────────────────

    [Header("단검 — 연속 다단히트")]
    [Tooltip("판정 박스 길이")]
    [SerializeField] private float daggerReach = 1.2f;

    [Tooltip("판정 박스 너비")]
    [SerializeField] private float daggerWidth = 0.4f;

    [Tooltip("총 히트 횟수")]
    [SerializeField][Range(1, 10)] private int daggerHitCount = 3;

    [Tooltip("히트 간격 (초)")]
    [SerializeField][Range(0.01f, 0.5f)] private float daggerHitInterval = 0.08f;

    [Tooltip("다단히트 시 회차별 데미지 배율 (1.0 = 100%). 배열 크기는 자동으로 hitCount에 맞춰짐")]
    [SerializeField] private float[] daggerHitMultipliers = { 1.0f, 0.8f, 1.2f };

    [Tooltip("최대 높이 차이 (이 값보다 높거나 낮은 적은 무시)")]
    [SerializeField] private float daggerMaxHeight = 2.0f;

    // ─────────────────────── 장검 설정 ───────────────────────

    [Header("장검 — 부채꼴")]
    [Tooltip("부채꼴 반경")]
    [SerializeField] private float swordReach = 2.0f;

    [Tooltip("부채꼴 전체 각도 (좌우 대칭). 110 = 앞 55도씩")]
    [SerializeField][Range(10f, 360f)] private float swordAngle = 110f;

    [Tooltip("최대 높이 차이")]
    [SerializeField] private float swordMaxHeight = 2.0f;

    // ─────────────────────── 창 설정 ───────────────────────

    [Header("창 — 관통 찌르기")]
    [Tooltip("관통 박스 길이 (긴 리치)")]
    [SerializeField] private float spearReach = 3.5f;

    [Tooltip("관통 박스 너비 (좁게 유지)")]
    [SerializeField] private float spearWidth = 0.35f;

    [Tooltip("최대 높이 차이")]
    [SerializeField] private float spearMaxHeight = 2.0f;

    // ─────────────────────── 디버그 정보 ───────────────────────

    [Header("디버그 (읽기 전용)")]
    [SerializeField] private int _lastHitCount;
    [SerializeField] private float _lastAttackTime;
    [SerializeField] private WeaponType _lastWeaponType;

    // ─────────────────────── 공격 실행 ───────────────────────

    /// <summary>
    /// 무기 타입에 따라 적절한 공격 판정 실행
    /// </summary>
    public void PerformAttack(WeaponInstance weapon, System.Action<GameObject, int> onHit)
    {
        if (weapon?.WeaponData == null || onHit == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[HitboxSystem] 무기 또는 콜백이 null입니다.");
#endif
            return;
        }

        _lastWeaponType = weapon.ResolvedWeaponType;
        _lastAttackTime = Time.time;

        switch (weapon.ResolvedWeaponType)
        {
            case WeaponType.Dagger:
                StartCoroutine(AttackDagger(onHit));
                break;
            case WeaponType.Sword:
                AttackSword(onHit);
                break;
            case WeaponType.Spear:
                AttackSpear(onHit);
                break;
        }
    }

    // ─────────────────────── 단검 공격 ───────────────────────

    /// <summary>
    /// 단검 다단히트 — 짧은 간격으로 여러 번 판정
    /// 같은 적이 여러 회차에 맞을 수 있음 (회차별 중복은 방지)
    /// </summary>
    private IEnumerator AttackDagger(System.Action<GameObject, int> onHit)
    {
        var hitTargetsPerWave = new HashSet<GameObject>();
        int totalHits = 0;

        for (int i = 0; i < daggerHitCount; i++)
        {
            hitTargetsPerWave.Clear();

            Vector3 center = GetBoxCenter(daggerReach);
            Vector3 halfExtents = new Vector3(daggerWidth * 0.5f, 0.5f, daggerReach * 0.5f);

            Collider[] hits = Physics.OverlapBox(
                center, halfExtents, transform.rotation, CombinedLayer);

            foreach (var col in hits)
            {
                GameObject target = col.gameObject;
                if (target == gameObject) continue;
                if (hitTargetsPerWave.Contains(target)) continue;

                // 높이 차이 체크
                float heightDiff = Mathf.Abs(col.transform.position.y - transform.position.y);
                if (heightDiff > daggerMaxHeight) continue;

                hitTargetsPerWave.Add(target);
                onHit(target, i);
                totalHits++;
            }

            if (i < daggerHitCount - 1)
                yield return new WaitForSeconds(daggerHitInterval);
        }

        _lastHitCount = totalHits;

#if UNITY_EDITOR
        Debug.Log($"[HitboxSystem] 단검 공격 완료 — 총 {totalHits}회 히트");
#endif
    }

    /// <summary>
    /// 단검 히트 회차별 데미지 배율 반환
    /// </summary>
    public float GetDaggerHitMultiplier(int hitIndex)
    {
        if (daggerHitMultipliers == null || daggerHitMultipliers.Length == 0) return 1f;
        hitIndex = Mathf.Clamp(hitIndex, 0, daggerHitMultipliers.Length - 1);
        return daggerHitMultipliers[hitIndex];
    }

    // ─────────────────────── 장검 공격 ───────────────────────

    /// <summary>
    /// 장검 부채꼴 공격 — 앞쪽 일정 각도 내 모든 적 히트
    /// </summary>
    private void AttackSword(System.Action<GameObject, int> onHit)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Collider[] hits = Physics.OverlapSphere(origin, swordReach, CombinedLayer);
        float halfAngle = swordAngle * 0.5f;
        int hitCount = 0;

        foreach (var col in hits)
        {
            if (col.gameObject == gameObject) continue;

            Vector3 targetPos = col.transform.position;
            Vector3 dir = targetPos - transform.position;

            // 높이 차이 체크
            if (Mathf.Abs(dir.y) > swordMaxHeight) continue;

            dir.y = 0f;

            // 각도 체크
            if (Vector3.Angle(transform.forward, dir) <= halfAngle)
            {
                onHit(col.gameObject, 0);
                hitCount++;
            }
        }

        _lastHitCount = hitCount;

#if UNITY_EDITOR
        Debug.Log($"[HitboxSystem] 장검 공격 완료 — {hitCount}개 타겟 히트");
#endif
    }

    // ─────────────────────── 창 공격 ───────────────────────

    /// <summary>
    /// 창 관통 공격 — 직선상 모든 적을 거리순으로 히트
    /// </summary>
    private void AttackSpear(System.Action<GameObject, int> onHit)
    {
        Vector3 center = GetBoxCenter(spearReach);
        Vector3 halfExtents = new Vector3(spearWidth * 0.5f, 0.5f, spearReach * 0.5f);

        Collider[] hits = Physics.OverlapBox(
            center, halfExtents, transform.rotation, CombinedLayer);

        // 거리순 정렬
        var sorted = new List<Collider>(hits);
        sorted.Sort((a, b) =>
        {
            float dA = Vector3.Distance(transform.position, a.transform.position);
            float dB = Vector3.Distance(transform.position, b.transform.position);
            return dA.CompareTo(dB);
        });

        int hitCount = 0;

        foreach (var col in sorted)
        {
            if (col.gameObject == gameObject) continue;

            // 높이 차이 체크
            float heightDiff = Mathf.Abs(col.transform.position.y - transform.position.y);
            if (heightDiff > spearMaxHeight) continue;

            onHit(col.gameObject, 0);
            hitCount++;
        }

        _lastHitCount = hitCount;

#if UNITY_EDITOR
        Debug.Log($"[HitboxSystem] 창 공격 완료 — {hitCount}개 타겟 관통");
#endif
    }

    // ─────────────────────── 유틸리티 ───────────────────────

    /// <summary>
    /// 공격 판정 박스의 중심점 계산
    /// </summary>
    private Vector3 GetBoxCenter(float reach) =>
        transform.position + transform.forward * (reach * 0.5f) + Vector3.up * 0.5f;

    // ─────────────────────── 에디터 검증 ───────────────────────

#if UNITY_EDITOR
    /// <summary>
    /// Inspector 값 변경 시 배열 크기 자동 조정
    /// </summary>
    private void OnValidate()
    {
        // 단검 히트 배율 배열 크기를 hitCount에 맞춤
        if (daggerHitMultipliers == null || daggerHitMultipliers.Length != daggerHitCount)
        {
            float[] newArray = new float[daggerHitCount];
            for (int i = 0; i < daggerHitCount; i++)
            {
                newArray[i] = (daggerHitMultipliers != null && i < daggerHitMultipliers.Length)
                    ? daggerHitMultipliers[i]
                    : 1.0f;
            }
            daggerHitMultipliers = newArray;
        }

        // 음수 방지
        daggerReach = Mathf.Max(0.1f, daggerReach);
        daggerWidth = Mathf.Max(0.1f, daggerWidth);
        swordReach = Mathf.Max(0.1f, swordReach);
        spearReach = Mathf.Max(0.1f, spearReach);
        spearWidth = Mathf.Max(0.1f, spearWidth);
        daggerMaxHeight = Mathf.Max(0.1f, daggerMaxHeight);
        swordMaxHeight = Mathf.Max(0.1f, swordMaxHeight);
        spearMaxHeight = Mathf.Max(0.1f, spearMaxHeight);
    }
#endif

    // ─────────────────────── 기즈모 ───────────────────────

#if UNITY_EDITOR
    [Header("기즈모 (에디터 전용)")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private WeaponType previewWeaponType = WeaponType.Sword;

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        switch (previewWeaponType)
        {
            case WeaponType.Dagger:
                DrawBoxGizmo(daggerReach, daggerWidth, new Color(1f, 0.5f, 0f, 0.4f));
                break;
            case WeaponType.Sword:
                DrawSwordGizmo();
                break;
            case WeaponType.Spear:
                DrawBoxGizmo(spearReach, spearWidth, new Color(0f, 1f, 0.3f, 0.4f));
                break;
        }
    }

    private void DrawBoxGizmo(float reach, float width, Color color)
    {
        Gizmos.color = color;
        Vector3 center = GetBoxCenter(reach);
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, new Vector3(width, 1f, reach));
               Gizmos.matrix = old;

        // 높이 제한 표시
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Vector3 playerPos = transform.position;
        float maxHeight = previewWeaponType == WeaponType.Dagger ? daggerMaxHeight : spearMaxHeight;
        
        // 상단 평면
        Gizmos.DrawWireCube(
            playerPos + Vector3.up * maxHeight,
            new Vector3(width * 2f, 0.1f, reach));
        
        // 하단 평면
        Gizmos.DrawWireCube(
            playerPos - Vector3.up * maxHeight,
            new Vector3(width * 2f, 0.1f, reach));
    }

    private void DrawSwordGizmo()
    {
        Gizmos.color = new Color(0f, 0.7f, 1f, 0.3f);
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        float halfAngle = swordAngle * 0.5f;
        int segments = 20;
        Vector3 prevPoint = Vector3.zero;

        // 부채꼴 외곽선
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            Vector3 cur = origin + dir * swordReach;
            if (i > 0) Gizmos.DrawLine(prevPoint, cur);
            prevPoint = cur;
        }

        // 부채꼴 양쪽 경계선
        Gizmos.DrawLine(origin, origin + Quaternion.AngleAxis(-halfAngle, Vector3.up) * transform.forward * swordReach);
        Gizmos.DrawLine(origin, origin + Quaternion.AngleAxis(halfAngle, Vector3.up) * transform.forward * swordReach);

        // 높이 제한 표시
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        
        // 상단 원
        DrawHeightCircle(origin + Vector3.up * swordMaxHeight, swordReach, halfAngle);
        
        // 하단 원
        DrawHeightCircle(origin - Vector3.up * swordMaxHeight, swordReach, halfAngle);
    }

    private void DrawHeightCircle(Vector3 center, float radius, float halfAngle)
    {
        int segments = 20;
        Vector3 prevPoint = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            Vector3 cur = center + dir * radius;
            if (i > 0) Gizmos.DrawLine(prevPoint, cur);
            prevPoint = cur;
        }
    }

    [ContextMenu("테스트: 단검 공격")]
    private void TestDaggerAttack()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[HitboxSystem] 플레이 모드에서만 테스트 가능합니다.");
            return;
        }

        var testWeapon = ScriptableObject.CreateInstance<WeaponData>();
        var weaponInstance = new WeaponInstance(testWeapon, WeaponType.Dagger);

        PerformAttack(weaponInstance, (target, hitIndex) =>
        {
            Debug.Log($"[테스트] {target.name} 히트 — 회차: {hitIndex}, 배율: {GetDaggerHitMultiplier(hitIndex)}");
        });
    }

    [ContextMenu("테스트: 장검 공격")]
    private void TestSwordAttack()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[HitboxSystem] 플레이 모드에서만 테스트 가능합니다.");
            return;
        }

        var testWeapon = ScriptableObject.CreateInstance<WeaponData>();
        var weaponInstance = new WeaponInstance(testWeapon, WeaponType.Sword);

        PerformAttack(weaponInstance, (target, hitIndex) =>
        {
            Debug.Log($"[테스트] {target.name} 히트");
        });
    }

    [ContextMenu("테스트: 창 공격")]
    private void TestSpearAttack()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[HitboxSystem] 플레이 모드에서만 테스트 가능합니다.");
            return;
        }

        var testWeapon = ScriptableObject.CreateInstance<WeaponData>();
        var weaponInstance = new WeaponInstance(testWeapon, WeaponType.Spear);

        PerformAttack(weaponInstance, (target, hitIndex) =>
        {
            Debug.Log($"[테스트] {target.name} 관통");
        });
    }
#endif
}
