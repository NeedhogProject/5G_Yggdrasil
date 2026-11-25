// ConfirmDialogUI.cs
// 예/아니오 확인 팝업 (공용 재사용)
// Show(메시지, 예콜백, 아니오콜백) 으로 띄운다. 던전 입장 확인 등에 사용.
// 싱글톤. 예 누르면 예콜백 실행, 아니오 누르면 아니오콜백 실행 후 닫힘.

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmDialogUI : MonoBehaviour
{
    public static ConfirmDialogUI Instance { get; private set; }

    [Header("패널 루트 (확인창 전체)")]
    [SerializeField] private GameObject panelRoot;

    [Header("메시지 텍스트")]
    [SerializeField] private TMP_Text messageText;

    [Header("버튼")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action _onYes;
    private Action _onNo;
    private bool _isOpen = false;

    public bool IsOpen
    {
        get { return _isOpen; }
    }

    private void Awake()
    {
        // 싱글톤 등록 (씬마다 하나)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 버튼 콜백 1회 등록
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(OnYesClicked);
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(OnNoClicked);
        }

        // 시작할 땐 닫아둔다
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        _isOpen = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // 확인창 띄우기
    // message: 보여줄 문구 / onYes: 예 콜백 / onNo: 아니오 콜백 (없으면 null)
    public void Show(string message, Action onYes, Action onNo)
    {
        _onYes = onYes;
        _onNo = onNo;

        if (messageText != null)
        {
            messageText.text = message;
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        _isOpen = true;
    }

    // 예 클릭
    private void OnYesClicked()
    {
        Action callback = _onYes;
        Close();

        if (callback != null)
        {
            callback.Invoke();
        }
    }

    // 아니오 클릭
    private void OnNoClicked()
    {
        Action callback = _onNo;
        Close();

        if (callback != null)
        {
            callback.Invoke();
        }
    }

    // 패널 닫기
    private void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        _onYes = null;
        _onNo = null;
        _isOpen = false;
    }
}