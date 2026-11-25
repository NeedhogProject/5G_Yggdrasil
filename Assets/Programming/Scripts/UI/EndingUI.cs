// EndingUI.cs
// 보스 처치 후 엔딩 화면을 표시하고 타이틀로 돌아간다.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndingUI : MonoBehaviour
{
    // 인스펙터에서 연결
    public Button returnButton;

    private void Start()
    {
        if (returnButton == null)
        {
            return;
        }

        // 타이틀 복귀 버튼 연결
        returnButton.onClick.AddListener(ReturnToTitle);

        // 시작 시 꺼져있음
        Hide();
    }

    // 보스 처치 시 PlayerCombat 또는 BossNidhogg에서 호출
    public void Show()
    {
        gameObject.SetActive(true);

        // 게임 일시정지
        Time.timeScale = 0f;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // 타이틀 씬으로 이동
    private void ReturnToTitle()
    {
        // 일시정지 해제
        Time.timeScale = 1f;

        // 씬 이름은 정건희 팀원과 확인 후 맞춰야 함
        SceneManager.LoadScene("TitleScene");
    }
}