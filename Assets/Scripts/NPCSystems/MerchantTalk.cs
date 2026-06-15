/*
 * MerchantTalk.cs
 * 상인(벨라) 메뉴창의 "대화하기" 버튼용 대사 순환
 * ShopMenuPanel 에 부착하고, MenuTalkButton 의 OnClick 에 OnTalkClicked 연결
 * 담당: 김보민
 */

using UnityEngine;
using TMPro;

public class MerchantTalk : MonoBehaviour
{
    [Header("대사 표시 텍스트")]
    [Tooltip("대화하기 누를 때 대사가 표시될 TMP 텍스트")]
    [SerializeField] private TMP_Text talkText;

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

    // 메뉴창이 열릴 때마다 첫 대사로 초기화하고 싶으면 OnEnable 사용
    private void OnEnable()
    {
        _lineIndex = 0;
        ShowCurrentLine();
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

    // 현재 번호의 대사를 텍스트에 표시
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

        talkText.text = sentences[_lineIndex];
    }
}