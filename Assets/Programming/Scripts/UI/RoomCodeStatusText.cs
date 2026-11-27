// RoomCodeStatusText.cs
// ESC 화면(KeySettingPanel)에 방 코드 상태를 표시한다.
// 화면이 열릴 때마다(OnEnable) NetworkLauncher.StatusText 를 읽어 그대로 보여준다.
// 값 예시: "방 코드: XXXXXX" / "오프라인(참가 불가)" / 세션이 없으면 빈 문자열(텍스트 숨김)
// 사용법: KeySettingPanel 안의 TMP_Text 오브젝트에 이 컴포넌트를 붙이면 끝.
// 담당: 김보민

using UnityEngine;
using TMPro;

public class RoomCodeStatusText : MonoBehaviour
{
    [Tooltip("방 코드를 표시할 텍스트 (비우면 같은 오브젝트의 TMP_Text 사용)")]
    [SerializeField] private TMP_Text statusText;

    private void Awake()
    {
        if (statusText == null)
        {
            statusText = GetComponent<TMP_Text>();
        }
    }

    // 화면이 열릴 때마다 최신 상태를 읽는다
    private void OnEnable()
    {
        Refresh();
    }

    // NetworkLauncher 의 상태 문구를 읽어 표시 (빈 문자열이면 텍스트를 숨김)
    public void Refresh()
    {
        if (statusText == null)
        {
            return;
        }

        string text = NetworkLauncher.StatusText;
        if (string.IsNullOrEmpty(text) == true)
        {
            statusText.text = "";
            statusText.enabled = false;
            return;
        }

        statusText.text = text;
        statusText.enabled = true;
    }
}
