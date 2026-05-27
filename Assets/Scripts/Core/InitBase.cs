using UnityEngine;

// 파생 클래스에서 Init 만 재정의해 초기화 로직을 두기 위한 베이스
// MonoBehaviour 를 상속해야 Unity 가 Awake 를 호출함
public class InitBase : MonoBehaviour
{
    private void Awake()
    {
        Init();
    }

    protected virtual void Init()
    {
    }
}
