// DevTestCamera.cs
// 개발용 임시 카메라 스크립트이다.
// Floor 씬을 단독 실행할 때만 카메라가 살아남고, 마을에서 진입하면 스스로 제거한다.
// GameCore 카메라(AudioListener 보유)가 이미 있으면 중복을 막기 위해 자신을 제거한다.

using UnityEngine;

public class DevTestCamera : MonoBehaviour
{
    private void Awake()
    {
        // 씬에 존재하는 모든 AudioListener를 수집
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

        // GameCore 카메라가 이미 있으면 본인 포함 2개 이상이 됨
        bool hasGameCoreCamera = listeners.Length > 1;

        if (hasGameCoreCamera == true)
        {
            // 마을에서 진입한 경우: GameCore 카메라가 처리하므로 임시 카메라 제거
            Destroy(gameObject);
        }
    }
}
