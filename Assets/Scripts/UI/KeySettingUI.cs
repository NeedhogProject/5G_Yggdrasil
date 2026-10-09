// KeySettingUI.cs
// Ű ���� �˾� UI (New Input System �����ε� ���)
// �� ������ ��ư�� ������ ���� �Է� Ű�� �ش� ���ε��� �缳���Ѵ�.

using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeySettingUI : MonoBehaviour
{
    // �� ��(�׼� �ϳ�)�� �����ϴ� UI ����
    [System.Serializable]
    public class KeySlot
    {
        [Tooltip("InputActionAsset�� ��ϵ� �׼� �̸�")]
        public string actionName;

        [Tooltip("���ε� �ε���. ���� Ű�� 0, 2D Vector ������(Move)�� 1=Up, 2=Down, 3=Left, 4=Right")]
        public int bindingIndex;

        public TMP_Text keyText;
        public Button changeButton;
    }


    [Header("�� �б� (���� �ʱ�ȭ ��ư�� ��� ���� �ʱ�ȭ���� �Ǵ�)")]
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private SettingUI audioSettingUI;
    [SerializeField] private GameObject keyPanel;
    [SerializeField] private KeySlot[] keySlots;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject popupRoot;  // ����â ��ü, ���� �ڱ� �ڽ��� ����


    // ���� ���� �����ε� �۾�
    private InputActionRebindingExtensions.RebindingOperation currentRebind;

    // ���� �缳�� ���� ����
    private KeySlot rebindingSlot;
    // �̹� ���ǿ��� ���� ��ư�� ��������
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

    // ������ Ű �缳�� ����
    private void StartRebind(KeySlot slot)
    {
        if (KeyBindingManager.Instance == null)
        {
            return;
        }

        // �̹� �ٸ� �����ε��� ���� ���̸� ���
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

    // �����ε� ����(�Ϸ� �Ǵ� ���) ���� ó��
    private void OnRebindFinished()
    {
        currentRebind = null;
        rebindingSlot = null;
        RefreshUI();
    }

    // ������� ���� (���� ��ư)
    // ������� ���� (���� ��ư)
    private void Apply()
    {
        if (KeyBindingManager.Instance != null)
        {
            KeyBindingManager.Instance.SaveBindings();
        }

        // �� �� �κ� �߰� (������� ����)
        if (audioSettingUI != null)
        {
            audioSettingUI.ApplyAudio();
        }

        _changesApplied = true;
    }

    // �⺻������ ���� (���� �ʱ�ȭ ��ư)
    public void ResetKeys()   // private �� public
    {
        if (KeyBindingManager.Instance != null)
        {
            KeyBindingManager.Instance.ResetBindings();
        }
        RefreshUI();
    }

    // ���� Ȱ��ȭ�� �ǿ� ���� ����� �Ǵ� Ű�� �ʱ�ȭ
    private void ResetCurrent()
    {
        Debug.Log($"[Reset] audioPanel={audioPanel}, activeSelf={audioPanel?.activeSelf}, audioSettingUI={audioSettingUI}");
        Debug.Log($"[Reset] keyPanel={keyPanel}, activeSelf={keyPanel?.activeSelf}");

        if (audioPanel != null && audioPanel.activeSelf == true)
        {
            if (audioSettingUI != null)
            {
                Debug.Log("[Reset] ����� �ʱ�ȭ ����");
                audioSettingUI.ResetAudio();
            }
            else
            {
                Debug.Log("[Reset] audioSettingUI�� null �̶� �ʱ�ȭ �� ��");
            }
            return;
        }

        if (keyPanel != null && keyPanel.activeSelf == true)
        {
            Debug.Log("[Reset] Ű �ʱ�ȭ ����");
            ResetKeys();
        }
    }

    // �˾� �ݱ� (������ ��ư)
    // �˾� �ݱ� (������ ��ư)
    private void Close()
    {
        CancelPendingRebind();

        if (_changesApplied == false)
        {
            if (KeyBindingManager.Instance != null)
            {
                KeyBindingManager.Instance.RestoreSnapshot();
                RefreshUI();
            }

            // �� �� �κ� �߰� (������� �ǵ�����)
            if (audioSettingUI != null)
            {
                audioSettingUI.RestoreAudioSnapshot();
            }
        }

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        // ����â�� �Ͻ����� ���·� �����Ƿ� ���� �� ���� �簳 (ESC �� ���� ���� ����)
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Paused)
        {
            GameManager.Instance.Resume();
        }
    }

    // ���� ���� �����ε��� ������ ���
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

    // ��� ������ ǥ�� ���� ����
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

    // ��Ȱ��ȭ �� �����ε� ���� ����
    private void OnEnable()
    {
        _changesApplied = false;

        if (KeyBindingManager.Instance != null)
        {
            KeyBindingManager.Instance.TakeSnapshot();
        }

        // �� �� �κ� �߰� (������� ���� ���)
        if (audioSettingUI != null)
        {
            audioSettingUI.TakeAudioSnapshot();
        }
    }
}