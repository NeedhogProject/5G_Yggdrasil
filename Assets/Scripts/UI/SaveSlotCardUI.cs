/*
 * SaveSlotCardUI.cs
 * 저장 슬롯 카드 하나 — 이름/날짜/저장 버튼/이름 편집 입력창
 * 담당: 김보민
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 슬롯 카드 하나 담당
///
/// [상태]
/// - 빈 슬롯: "빈 저장 슬롯" 표시, 저장 버튼만 활성
/// - 있는 슬롯: 이름/날짜 표시, 저장(덮어쓰기)/이름변경 버튼 활성
/// - 이름 편집 중: InputField 표시, 확인/취소 버튼
///
/// [씬 설정]
/// 슬롯 카드 오브젝트에 부착 후 SaveSlotPanelUI 에서 Initialize(index, panel) 호출
/// </summary>
public class SaveSlotCardUI : MonoBehaviour
{
    [Header("슬롯 정보 표시")]
    [SerializeField] private TMP_Text slotNameText;      // 저장 파일 이름
    [SerializeField] private TMP_Text slotDateText;      // 저장 일시
    [SerializeField] private TMP_Text slotFloorText;     // 저장 층

    [Header("기본 버튼")]
    [SerializeField] private Button saveButton;          // 저장(덮어쓰기)
    [SerializeField] private Button renameButton;        // 이름 변경

    [Header("이름 편집 영역 (평소엔 숨김)")]
    [SerializeField] private GameObject editArea;        // 편집 영역 루트
    [SerializeField] private TMP_InputField nameInput;   // 이름 입력창
    [SerializeField] private Button confirmButton;       // 확인
    [SerializeField] private Button cancelButton;        // 취소

    [Header("빈 슬롯 표시 오브젝트 (있으면 비었을 때 표시)")]
    [SerializeField] private GameObject emptyLabel;

    // 내부 상태
    private int _slotIndex = 0;
    private SaveSlotPanelUI _panel;
    private bool _isEditing = false;
    private bool _isEmpty = true;

    /// <summary>초기화 — SaveSlotPanelUI.Start 에서 호출</summary>
    public void Initialize(int slotIndex, SaveSlotPanelUI panel)
    {
        _slotIndex = slotIndex;
        _panel = panel;

        // 버튼 이벤트 연결
        if (saveButton != null) { saveButton.onClick.AddListener(OnSaveClicked); }
        if (renameButton != null) { renameButton.onClick.AddListener(OnRenameClicked); }
        if (confirmButton != null) { confirmButton.onClick.AddListener(OnConfirmClicked); }
        if (cancelButton != null) { cancelButton.onClick.AddListener(OnCancelClicked); }

        // 편집 영역 숨기기
        SetEditMode(false);

        Refresh();
    }

    /// <summary>슬롯 정보 갱신 (저장 후, 패널 열릴 때 호출)</summary>
    public void Refresh()
    {
        if (SaveSystem.Instance == null)
        {
            return;
        }

        _isEmpty = SaveSystem.Instance.HasSave(_slotIndex) == false;

        if (_isEmpty == true)
        {
            // 빈 슬롯
            if (slotNameText != null) { slotNameText.text = "빈 저장 슬롯"; }
            if (slotDateText != null) { slotDateText.text = ""; }
            if (slotFloorText != null) { slotFloorText.text = ""; }
            if (emptyLabel != null) { emptyLabel.SetActive(true); }
            if (renameButton != null) { renameButton.interactable = false; }
        }
        else
        {
            SaveData meta = SaveSystem.Instance.GetSaveMeta(_slotIndex);

            if (meta != null)
            {
                string displayName = string.IsNullOrEmpty(meta.saveName) == false
                    ? meta.saveName
                    : "저장 " + (_slotIndex + 1);

                if (slotNameText != null) { slotNameText.text = displayName; }
                if (slotDateText != null) { slotDateText.text = meta.saveDateTime; }
                if (slotFloorText != null)
                {
                    string floorStr = meta.currentFloor == 0
                        ? "마을"
                        : meta.currentFloor + "층";
                    slotFloorText.text = floorStr;
                }
            }

            if (emptyLabel != null) { emptyLabel.SetActive(false); }
            if (renameButton != null) { renameButton.interactable = true; }
        }

        // 편집 모드 종료
        SetEditMode(false);
    }

    // 저장 버튼 클릭 — 빈 슬롯이면 바로 이름 편집 모드, 있으면 덮어쓰기 확인
    private void OnSaveClicked()
    {
        if (_isEmpty == true)
        {
            // 빈 슬롯: 이름 입력 후 저장
            StartEditMode("저장 " + (_slotIndex + 1));
        }
        else
        {
            // 있는 슬롯: 기존 이름으로 바로 덮어쓰기
            SaveData meta = SaveSystem.Instance.GetSaveMeta(_slotIndex);
            string savedName = meta != null ? meta.saveName : "저장 " + (_slotIndex + 1);
            _panel.SaveToSlot(_slotIndex, savedName);
        }
    }

    // 이름 변경 버튼 클릭
    private void OnRenameClicked()
    {
        SaveData meta = SaveSystem.Instance.GetSaveMeta(_slotIndex);
        string currentName = meta != null ? meta.saveName : "";
        StartEditMode(currentName);
    }

    // 편집 모드 시작
    private void StartEditMode(string initialName)
    {
        if (nameInput != null)
        {
            nameInput.text = initialName;
        }

        SetEditMode(true);
    }

    // 확인 버튼 — 이름 저장
    private void OnConfirmClicked()
    {
        string inputName = nameInput != null ? nameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(inputName) == true)
        {
            inputName = "저장 " + (_slotIndex + 1);
        }

        if (_isEmpty == true)
        {
            // 빈 슬롯: 이름과 함께 새로 저장
            _panel.SaveToSlot(_slotIndex, inputName);
        }
        else
        {
            // 있는 슬롯: 이름만 변경
            _panel.RenameSlot(_slotIndex, inputName);
        }

        SetEditMode(false);
    }

    // 취소 버튼
    private void OnCancelClicked()
    {
        SetEditMode(false);
    }

    // 편집 모드 전환
    private void SetEditMode(bool editing)
    {
        _isEditing = editing;

        if (editArea != null) { editArea.SetActive(editing); }
        if (saveButton != null) { saveButton.gameObject.SetActive(editing == false); }
        if (renameButton != null) { renameButton.gameObject.SetActive(editing == false); }

        // 편집 모드 진입 시 InputField 포커스
        if (editing == true && nameInput != null)
        {
            nameInput.Select();
            nameInput.ActivateInputField();
        }
    }
}