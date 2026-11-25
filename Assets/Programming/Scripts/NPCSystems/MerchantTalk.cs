/*
 * MerchantTalk.cs
 * 상인(벨라) 메뉴창의 "대화하기" 버튼용 대사 순환
 * ShopMenuPanel 에 부착하고, MenuTalkButton 의 OnClick 에 OnTalkClicked 연결
 * 담당: 김보민
 */

using UnityEngine;
using TMPro;
using System.Collections;

public class MerchantTalk : MonoBehaviour
{
    [Header("대사 표시 텍스트")]
    [Tooltip("대화하기 누를 때 대사가 표시될 TMP 텍스트")]
    [SerializeField] private TMP_Text talkText;

    [Header("타이핑 효과 (초당 간격, 0 이하면 기본값 0.03 사용)")]
    [Tooltip("한 글자씩 찍히는 간격. 작을수록 빠름")]
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("대사 목록")]
    [Tooltip("대화하기를 누를 때마다 순서대로 순환하며 표시")]
    [TextArea]
    [SerializeField]
    private string[] sentences = new string[]
    {
        "정직한 가격으로 모십니다~! 어서오세요!",
        "필요한 게 있으신가요? 천천히 둘러보세요~",
        "헤헤, 찾아와 주셔서 감사합니다!"
    };

    // 현재 표시 중인 대사 번호
    private int _lineIndex = 0;

    // 진행 중인 타이핑 코루틴 (새 대사가 나오면 멈추고 새로 시작)
    private Coroutine _typingCoroutine = null;

    // 메뉴창이 열릴 때마다 첫 대사부터 타이핑으로 표시
    private void OnEnable()
    {
        _lineIndex = 0;
        ShowCurrentLine();
    }

    // 창이 꺼질 때 진행 중이던 타이핑을 멈춘다
    private void OnDisable()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
    }

    // 대화하기 버튼 OnClick 에 연결
    public void OnTalkClicked()
    {
        if (sentences == null || sentences.Length == 0)
        {
            return;
        }

        _lineIndex = _lineIndex + 1;
        if (_lineIndex >= sentences.Length)
        {
            _lineIndex = 0;
        }

        ShowCurrentLine();
    }

    // 현재 번호의 대사를 타이핑 효과로 표시
    private void ShowCurrentLine()
    {
        if (talkText == null)
        {
            return;
        }
        if (sentences == null || sentences.Length == 0)
        {
            return;
        }

        if (_lineIndex < 0 || _lineIndex >= sentences.Length)
        {
            _lineIndex = 0;
        }

        StartTyping(sentences[_lineIndex]);
    }

    // 이전 타이핑을 멈추고 새 대사 타이핑을 시작
    private void StartTyping(string line)
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        // 오브젝트가 비활성이면 코루틴을 못 돌리므로 즉시 표시
        if (gameObject.activeInHierarchy == false)
        {
            talkText.text = line;
            return;
        }

        _typingCoroutine = StartCoroutine(TypeLine(line));
    }

    // 한 글자씩 출력하는 코루틴 (WaitForSecondsRealtime 라 일시정지 중에도 동작)
    private IEnumerator TypeLine(string line)
    {
        talkText.text = "";

        // 속도 보정 — 0 이하이면 기본값 사용 (인스펙터 0 실수 방지)
        float speed = typingSpeed;
        if (speed <= 0f)
        {
            speed = 0.03f;
        }

        int i = 0;
        int length = line.Length;

        while (i < length)
        {
            talkText.text = talkText.text + line[i];
            i = i + 1;
            yield return new WaitForSecondsRealtime(speed);
        }

        _typingCoroutine = null;
    }
}