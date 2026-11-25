// BossGroundField.cs
// 보스가 바닥에 남기는 장판 (위험 지대 / 이동 속도 감소 / 공격 속도 감소)
// 플레이어만 영향을 받고 보스는 영향 없음. 유지 시간이 끝나면 자동 제거
// 판정은 콜라이더 없이 수평 거리로 계산한다 (프리팹은 지름 1 유닛 원형 기준으로 크기 자동 조정)

using UnityEngine;

public class BossGroundField : MonoBehaviour
{
    public enum FieldType
    {
        Damage,      // 왼쪽 머리 착탄 위험 지대 (초당 피해)
        MoveSlow,    // 오른쪽 머리 이동 속도 감소
        AttackSlow   // 오른쪽 머리 공격 속도 감소 (보스 주위)
    }

    // 플레이어가 올라가 있는 감속 장판 수 (여러 장판이 겹쳐도 감소는 한 번만)
    private static int _nMoveSlowCount = 0;
    private static int _nAttackSlowCount = 0;

    private FieldType _type;
    private float _fRadius;
    private bool _bPlayerInside = false;
    private Transform _trPlayer;

    // 설정 (BossHead 에서 생성 직후 호출)
    public void Init(FieldType _fieldType, float _fFieldRadius, float _fDuration)
    {
        _type = _fieldType;
        _fRadius = _fFieldRadius;
        float fDiameter = _fFieldRadius * 2f;
        transform.localScale = new Vector3(fDiameter, transform.localScale.y, fDiameter);

        GameObject objPlayer = GameObject.FindWithTag("Player");
        if (objPlayer != null)
        {
            _trPlayer = objPlayer.transform;
        }
        Destroy(gameObject, _fDuration);
    }

    private void Update()
    {
        if (_trPlayer == null || BossNidhogg.Instance == null)
        {
            return;
        }

        // 보스 사망 시 남은 장판 즉시 제거 (모든 공격 중단)
        if (BossNidhogg.Instance.Phase == BossNidhogg.BossPhase.Dead || BossNidhogg.Instance.Phase == BossNidhogg.BossPhase.Ending)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 vDiff = _trPlayer.position - transform.position;
        vDiff.y = 0f;
        bool bInside = vDiff.magnitude <= _fRadius;

        if (bInside != _bPlayerInside)
        {
            _bPlayerInside = bInside;
            ChangeSlowCount(bInside == true ? 1 : -1);
        }

        if (bInside == true && _type == FieldType.Damage && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.TakeDamage(BossNidhogg.Instance.hazardDamagePerSecond * Time.deltaTime);
        }
    }

    private void OnDestroy()
    {
        // 위험: 플레이어가 장판 위에 있는 채로 사라지면 감속이 남으므로 해제
        if (_bPlayerInside == true)
        {
            ChangeSlowCount(-1);
        }
    }

    // 감속 장판 수 갱신 후 플레이어 이동/공격 속도 배율 적용
    private void ChangeSlowCount(int _nDelta)
    {
        BossNidhogg boss = BossNidhogg.Instance;
        if (_type == FieldType.MoveSlow)
        {
            _nMoveSlowCount = Mathf.Max(0, _nMoveSlowCount + _nDelta);
            float fMult = _nMoveSlowCount > 0 && boss != null ? 1f - boss.moveSlowPercent : 1f;
            PlayerController.Instance?.SetExternalMoveMultiplier(fMult);
        }
        else if (_type == FieldType.AttackSlow)
        {
            _nAttackSlowCount = Mathf.Max(0, _nAttackSlowCount + _nDelta);
            float fMult = _nAttackSlowCount > 0 && boss != null ? 1f - boss.attackSlowPercent : 1f;
            PlayerCombat combat = PlayerController.Instance != null ? PlayerController.Instance.GetComponent<PlayerCombat>() : null;
            combat?.SetExternalAttackSpeedMultiplier(fMult);
        }
    }
}
