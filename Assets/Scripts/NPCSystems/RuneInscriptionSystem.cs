// RuneInscriptionSystem.cs
// 각인 조합 로직을 담당한다.
// 자원을 소모하여 장비에 각인 태그를 부여하고 초기화권으로 초기화한다.
// ArmorInstance 연동은 정건희 팀원 작업 완료 후 추가한다.

using UnityEngine;

public class RuneInscriptionSystem : MonoBehaviour
{
    // 싱글턴
    public static RuneInscriptionSystem Instance { get; private set; }

    // 각인 부여에 필요한 자원 개수
    private int requiredResourceCount = 3;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // 자원 개수 확인
    public bool HasEnoughResource(int resourceCount)
    {
        if (resourceCount < requiredResourceCount)
        {
            Debug.Log("자원이 부족합니다. 필요 수량: " + requiredResourceCount);
            return false;
        }

        return true;
    }

    // 초기화권 개수 확인
    public bool HasResetScroll(int scrollCount)
    {
        if (scrollCount < 1)
        {
            Debug.Log("각인 초기화권이 없습니다.");
            return false;
        }

        return true;
    }

    // 각인 부여 결과 로그 (ArmorInstance 연동 전 임시)
    public void LogInscribeResult(string equipName, string runeType)
    {
        Debug.Log("각인 부여 완료: " + runeType + " → " + equipName);
    }

    // 각인 초기화 결과 로그 (ArmorInstance 연동 전 임시)
    public void LogResetResult(string equipName)
    {
        Debug.Log("각인 초기화 완료: " + equipName);
    }
}