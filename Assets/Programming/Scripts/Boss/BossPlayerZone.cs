// BossPlayerZone.cs
// 보스 머리가 담당하는 플레이어 이동 구역 (트리거 콜라이더)
// 플레이어가 구역 안에 있으면 연결된 머리가 공격을 시도한다.
// 씬 설정: 빈 오브젝트에 Box Collider(Is Trigger 켬) 와 함께 부착, BossHead 의 Player Zone 칸에 연결

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossPlayerZone : MonoBehaviour
{
    private int _nInside = 0;
    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    /// <summary>플레이어가 구역 안에 있는지 — BossHead 참조</summary>
    public bool IsPlayerInside => _nInside > 0;

    /// <summary>지정 위치가 구역 안인지 (멀티에서 상대 플레이어 분신 확인용, 분신은 트리거에 잡히지 않음)</summary>
    public bool Contains(Vector3 _vPosition)
    {
        return (_collider.ClosestPoint(_vPosition) - _vPosition).sqrMagnitude < 0.0001f;
    }

    private void OnTriggerEnter(Collider _other)
    {
        if (_other.CompareTag("Player") == true)
        {
            _nInside++;
        }
    }

    private void OnTriggerExit(Collider _other)
    {
        if (_other.CompareTag("Player") == true)
        {
            _nInside = Mathf.Max(0, _nInside - 1);
        }
    }
}
