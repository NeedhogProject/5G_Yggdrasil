// BossProjectile.cs
// 보스 왼쪽 머리 투사체 — 목표 지점까지 직선 비행 후 착탄 콜백 호출, 자신은 제거
// 착탄 피해와 위험 지대 생성은 BossHead 가 콜백에서 처리한다 (플레이어를 직접 따라가지 않음)

using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    private Vector3 _vTarget;
    private float _fSpeed;
    private System.Action _onArrive;
    private bool _bLaunched = false;

    // 발사 (BossHead 에서 호출)
    public void Launch(Vector3 _vTargetPos, float _fMoveSpeed, System.Action _onArrived)
    {
        _vTarget = _vTargetPos;
        _fSpeed = _fMoveSpeed;
        _onArrive = _onArrived;
        _bLaunched = true;
        transform.LookAt(_vTargetPos);
    }

    private void Update()
    {
        if (_bLaunched == false)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, _vTarget, _fSpeed * Time.deltaTime);

        if ((transform.position - _vTarget).sqrMagnitude <= 0.0001f)
        {
            _bLaunched = false;
            _onArrive?.Invoke();
            Destroy(gameObject);
        }
    }
}
