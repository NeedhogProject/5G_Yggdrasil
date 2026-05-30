// KeySettingUI.cs
// 키 설정 팝업 UI
// 각 액션 슬롯 버튼을 누르면 다음에 입력되는 키로 재설정한다.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeySettingUI : MonoBehaviour
{
    // 액션 한 줄에 대응하는 UI 묶음
    [System.Serializable]
    public class KeySlot
    {
        public string actionName;
        public TMP_Text keyText;
        public Button changeButton;
    }

    [SerializeField] private KeySlot[] keySlots;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;

    // 키 입력 대기 중인지
    private bool waitingForKey = false;

    // 현재 재설정 중인 액션 이름
    private string currentAction;

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

                string action = slot.actionName;
                slot.changeButton.onClick.AddListener(() => StartRebind(action));
            }
        }

        if (applyButton != null)
        {
            applyButton.onClick.AddListener(Apply);
        }
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetKeys);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        RefreshUI();
    }

    private void Update()
    {
        if (waitingForKey == false)
        {
            return;
        }

        // 다음으로 눌린 키를 현재 액션에 할당
        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(key) == true)
            {
                if (KeyBindingManager.Instance != null)
                {
                    KeyBindingManager.Instance.SetKey(currentAction, key);
                }

                waitingForKey = false;
                RefreshUI();
                break;
            }
        }
    }

    // 재설정 시작, 다음 입력 대기
    private void StartRebind(string actionName)
    {
        waitingForKey = true;
        currentAction = actionName;
        RefreshUI();
    }

    // 적용, PlayerPrefs 저장
    private void Apply()
    {
        if (KeyBindingManager.Instance != null)
        {
            KeyBindingManager.Instance.SaveKeys();
        }
    }

    // 기본값으로 초기화
    private void ResetKeys()
    {
        if (KeyBindingManager.Instance != null)
        {
            KeyBindingManager.Instance.ResetDefaults();
        }
        RefreshUI();
    }

    // 팝업 닫기
    private void Close()
    {
        gameObject.SetActive(false);
    }

    // 슬롯 텍스트 갱신
    private void RefreshUI()
    {
        if (keySlots == null)
        {
            return;
        }

        foreach (KeySlot slot in keySlots)
        {
            if (slot.keyText == null)
            {
                continue;
            }

            if (waitingForKey == true && currentAction == slot.actionName)
            {
                slot.keyText.text = "Press Key...";
            }
            else if (KeyBindingManager.Instance != null)
            {
                slot.keyText.text = KeyBindingManager.Instance.GetKey(slot.actionName).ToString();
            }
        }
    }
}