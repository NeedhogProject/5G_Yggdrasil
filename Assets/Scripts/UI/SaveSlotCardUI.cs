/*
 * SaveSlotCardUI.cs
 * 저장 슬롯 카드 하나 — 이름/날짜/층 표시 + 덮어쓰기/이름변경/삭제
 * 동적 생성 방식: 저장된 슬롯만 카드로 만들어짐 (빈 슬롯 케이스 없음)
 * 담당: 김보민
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 슬롯 카드 하나 담당
///
/// [상태]
/// - 항상 저장된 슬롯 (동적 생성이라 빈 슬롯 카드는 안 만들어짐)
/// - 이름/날짜/층 표시, 덮어쓰기/이름변경/삭제 버튼
/// - 이름 편집 중: InputField 표시, 확인/취소
///
/// [씬 설정]
/// 카드 프리팹에 부착 후 SaveSlotPanelUI 가 Initialize(index, panel) 호출
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
    [SerializeField] private Button deleteButton;        // 삭제
    [SerializeField] private Button loadButton;          // 불러오기 (불러오기 모드 전용)

    [Header("이름 편집 영역 (평소엔 숨김)")]
    [SerializeField] private GameObject editArea;        // 편집 영역 루트
    [SerializeField] private TMP_InputField nameInput;   // 이름 입력창
    [SerializeField] private Button confirmButton;       // 확인
    [SerializeField] private Button cancelButton;        // 취소

    // 내부 상태
    private int _slotIndex = 0;
    private SaveSlotPanelUI _panel;

    /// <summary>초기화 — SaveSlotPanelUI 가 카드 생성 후 호출</summary>
    public void Initialize(int slotIndex, SaveSlotPanelUI panel)
    {
        _slotIndex = slotIndex;
        _panel = panel;

        // 버튼 이벤트 연결
        if (saveButton != null) { saveButton.onClick.AddListener(OnSaveClicked); }
        if (renameButton != null) { renameButton.onClick.AddListener(OnRenameClicked); }
        if (deleteButton != null) { deleteButton.onClick.AddListener(OnDeleteClicked); }
        if (loadButton != null) { loadButton.onClick.AddListener(OnLoadClicked); }
        if (confirmButton != null) { confirmButton.onClick.AddListener(OnConfirmClicked); }
        if (cancelButton != null) { cancelButton.onClick.AddListener(OnCancelClicked); }

        // 편집 영역 숨기기
        SetEditMode(false);

        Refresh();
    }

    /// <summary>슬롯 정보 갱신</summary>
    public void Refresh()
    {
        if (SaveSystem.Instance == null)
        {
            return;
        }

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

        SetEditMode(false);
    }

    // 저장(덮어쓰기) 버튼 — 기존 이름 유지하고 현재 게임 상태로 덮어쓰기
    private void OnSaveClicked()
    {
        SaveData meta = SaveSystem.Instance.GetSaveMeta(_slotIndex);
        string savedName = meta != null ? meta.saveName : "저장 " + (_slotIndex + 1);
        _panel.SaveToSlot(_slotIndex, savedName);
    }

    // 이름 변경 버튼 — 편집 모드 진입
    private void OnRenameClicked()
    {
        SaveData meta = SaveSystem.Instance.GetSaveMeta(_slotIndex);
        string currentName = meta != null ? meta.saveName : "";
        StartEditMode(currentName);
    }

    // 삭제 버튼
    private void OnDeleteClicked()
    {
        _panel.DeleteSlot(_slotIndex);
    }

    // 불러오기 버튼 — 이 슬롯을 불러오기
    private void OnLoadClicked()
    {
        Debug.Log("[SaveSlotCardUI] 불러오기 버튼 클릭 - 슬롯 " + _slotIndex.ToString());
        _panel.LoadSlot(_slotIndex);
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

    // 확인 버튼 — 이름 변경 적용
    private void OnConfirmClicked()
    {
        string inputName = nameInput != null ? nameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(inputName) == true)
        {
            inputName = "저장 " + (_slotIndex + 1);
        }

        _panel.RenameSlot(_slotIndex, inputName);
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
        if (editArea != null)
        {
            editArea.SetActive(editing);
        }

        // 편집 중이면 모든 일반 버튼 숨김
        if (editing == true)
        {
            if (saveButton != null) { saveButton.gameObject.SetActive(false); }
            if (renameButton != null) { renameButton.gameObject.SetActive(false); }
            if (deleteButton != null) { deleteButton.gameObject.SetActive(false); }
            if (loadButton != null) { loadButton.gameObject.SetActive(false); }

            // 편집 모드 진입 시 InputField 포커스
            if (nameInput != null)
            {
                nameInput.Select();
                nameInput.ActivateInputField();
            }
            return;
        }

        // 편집 아님: 모드에 따라 버튼 표시
        // 불러오기 모드면 불러오기 버튼만, 저장 모드면 저장/이름변경/삭제
        bool loadMode = _panel != null && _panel.IsLoadMode == true;

        if (loadButton != null) { loadButton.gameObject.SetActive(loadMode == true); }
        if (saveButton != null) { saveButton.gameObject.SetActive(loadMode == false); }
        if (renameButton != null) { renameButton.gameObject.SetActive(loadMode == false); }
        if (deleteButton != null) { deleteButton.gameObject.SetActive(loadMode == false); }
    }
}