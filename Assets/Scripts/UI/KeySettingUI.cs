// KeySettingUI.cs
// 키 설정 팝업 UI (New Input System 리바인딩 기반)
// 각 슬롯의 버튼을 누르면 다음 입력 키로 해당 바인딩을 재설정한다.

using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeySettingUI : MonoBehaviour
{
    // 한 줄(액션 하나)에 대응하는 UI 묶음
    [System.Serializable]
    public class KeySlot
    {
        [Tooltip("InputActionAsset에 등록된 액션 이름")]
        public string actionName;

        [Tooltip("바인딩 인덱스. 단일 키는 0, 2D Vector 컴포짓(Move)은 1=Up, 2=Down, 3=Left, 4=Right")]
        public int bindingIndex;

        public TMP_Text keyText;
        public Button changeButton;
    }


    [Header("탭 분기 (설정 초기화 버튼이 어느 쪽을 초기화할지 판단)")]
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private SettingsUI audioSettingsUI;
    [SerializeField] private GameObject keyPanel;
    [SerializeField] private KeySlot[] keySlots;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject popupRoot;  // 설정창 전체, 비우면 자기 자신을 닫음

    // 진행 중인 리바인딩 작업
    private InputActionRebindingExtensions.RebindingOperation currentRebind;

    // 현재 재설정 중인 슬롯
    private KeySlot rebindingSlot;
    // 이번 세션에서 적용 버튼을 눌렀는지
    private bool _changesApplied = false;

    private void Start()
    {
        if (keySlots != null)
        {
            foreach (KeySlot slot in keySlots)
            {
                if (slot.changeButton == null)
                {
                    continue;
                }
                KeySlot captured = slot;
                slot.changeButton.onClick.AddListener(() => StartRebind(captured));
            }
        }

        if (applyButton != null)
        {
            applyButton.onClick.AddListener(Apply);
        }
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetCurrent);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        RefreshUI();
    }

    // 슬롯의 키 재설정 시작
    private void StartRebind(KeySlot slot)
    {
        if (KeyBindingManager.Instance == null)
        {
            return;
        }

        // 이미 다른 리바인딩이 진행 중이면 취소
        if (currentRebind != null)
        {
            currentRebind.Cancel();
            currentRebind.Dispose();
            currentRebind = null;
        }

        rebindingSlot = slot;
        if (slot.keyText != null)
        {
            slot.keyText.text = "Press Key...";
        }

        currentRebind = KeyBindingManager.Instance.StartRebind(
            slot.actionName,
            slot.bindingIndex,
            onComplete: OnRebindFinished,
            onCancel: OnRebindFinished);
    }

    // 리바인딩 종료(완료 또는 취소) 공통 처리
    private void OnRebindFinished()
    {
        currentRebind = null;
        rebindingSlot = null;
        RefreshUI();
    }

    // 변경사항 저장 (적용 버튼)
    // 변경사항 저장 (적용 버튼)
    private void Apply()
    {
        if (KeyBindingManager.Instance != null)
        {
            KeyBindingManager.Instance.SaveBindings();
        }
        _changesApplied = true;   // 추가
    }

    // 기본값으로 복원 (설정 초기화 버튼)
    public void ResetKeys()   // private → public
    {
        if (KeyBindingManager.Instance != null)
        {
            KeyBindingManager.Instance.ResetBindings();
        }
        RefreshUI();
    }

    // 현재 활성화된 탭에 맞춰 오디오 또는 키를 초기화
    private void ResetCurrent()
    {
        Debug.Log($"[Reset] audioPanel={audioPanel}, activeSelf={audioPanel?.activeSelf}, audioSettingsUI={audioSettingsUI}");
        Debug.Log($"[Reset] keyPanel={keyPanel}, activeSelf={keyPanel?.activeSelf}");

        if (audioPanel != null && audioPanel.activeSelf == true)
        {
            if (audioSettingsUI != null)
            {
                Debug.Log("[Reset] 오디오 초기화 실행");
                audioSettingsUI.ResetAudio();
            }
            else
            {
                Debug.Log("[Reset] audioSettingsUI가 null 이라 초기화 못 함");
            }
            return;
        }

        if (keyPanel != null && keyPanel.activeSelf == true)
        {
            Debug.Log("[Reset] 키 초기화 실행");
            ResetKeys();
        }
    }

    // 팝업 닫기 (나가기 버튼)
    // 팝업 닫기 (나가기 버튼)
    private void Close()
    {
        CancelPendingRebind();

        // 적용 안 누른 변경사항이 있으면 스냅샷으로 복원
        if (_changesApplied == false && KeyBindingManager.Instance != null)
        {
            KeyBindingManager.Instance.RestoreSnapshot();
            RefreshUI();
        }

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // 진행 중인 리바인딩이 있으면 취소
    private void CancelPendingRebind()
    {
        if (currentRebind == null)
        {
            return;
        }
        currentRebind.Cancel();
        currentRebind.Dispose();
        currentRebind = null;
        rebindingSlot = null;
    }

    // 모든 슬롯의 표시 글자 갱신
    private void RefreshUI()
    {
        if (keySlots == null)
        {
            return;
        }
        if (KeyBindingManager.Instance == null)
        {
            return;
        }

        foreach (KeySlot slot in keySlots)
        {
            if (slot.keyText == null)
            {
                continue;
            }

            if (rebindingSlot == slot)
            {
                slot.keyText.text = "Press Key...";
            }
            else
            {
                slot.keyText.text = KeyBindingManager.Instance.GetBindingDisplay(
                    slot.actionName, slot.bindingIndex);
            }
        }
    }

    // 비활성화 시 리바인딩 안전 정리
    private void OnDisable()
    {
        CancelPendingRebind();
    }

    // 팝업 켜질 때 현재 바인딩 스냅샷 저장
    private void OnEnable()
    {
        _changesApplied = false;

        if (KeyBindingManager.Instance != null)
        {
            KeyBindingManager.Instance.TakeSnapshot();
        }
    }
}