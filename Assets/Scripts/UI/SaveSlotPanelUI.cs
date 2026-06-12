/*
 * SaveSlotPanelUI.cs
 * 게임 저장 슬롯 선택 패널 (동적 생성 방식)
 * "새 저장" 버튼 → 빈 슬롯에 카드 생성 (최대 5개)
 * 저장된 슬롯만 카드로 표시
 * 담당: 김보민
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 저장 슬롯 패널 — 저장된 슬롯을 카드로 동적 생성
///
/// [동작]
/// - 패널 열릴 때 저장된 슬롯들을 스캔해서 카드 생성
/// - "새 저장" 버튼 → 빈 슬롯 찾아 카드 생성 후 저장 (최대 SLOT_COUNT 개)
/// - 카드: 덮어쓰기 / 이름변경 / 삭제
///
/// [씬 설정]
/// SaveSlotPanel 에 이 스크립트 부착
/// slotCardPrefab = SaveSlotCardUI 가 붙은 카드 프리팹
/// cardContainer = 카드들이 생성될 부모 (Scroll View > Viewport > Content)
/// newSaveButton = "새 저장" 버튼
/// </summary>
public class SaveSlotPanelUI : MonoBehaviour
{
    // 싱글턴
    public static SaveSlotPanelUI Instance { get; private set; }

    [Header("패널 루트")]
    [SerializeField] private GameObject panelRoot;

    [Header("카드 동적 생성")]
    [SerializeField] private GameObject slotCardPrefab;   // SaveSlotCardUI 붙은 프리팹
    [SerializeField] private Transform cardContainer;     // 카드 부모 (Content)

    [Header("버튼")]
    [SerializeField] private Button newSaveButton;        // 새 저장
    [SerializeField] private Button closeButton;          // 닫기

    [Header("안내 텍스트 (선택)")]
    [SerializeField] private TMP_Text noticeText;         // "슬롯이 가득 찼습니다" 등

    // 현재 생성된 카드 목록
    private List<SaveSlotCardUI> _cards = new List<SaveSlotCardUI>();
    private bool _initialized = false;

    // 불러오기 모드 여부 (true 면 카드 클릭 시 해당 슬롯 불러오기)
    private bool _isLoadMode = false;

    /// <summary>현재 불러오기 모드인지 (카드에서 확인용)</summary>
    public bool IsLoadMode
    {
        get { return _isLoadMode; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        EnsureInitialized();

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void Update()
    {
        // 저장 슬롯 창이 열려있을 때 ESC 누르면 닫기 (설정창으로 복귀)
        if (panelRoot == null)
        {
            return;
        }

        if (panelRoot.activeSelf == false)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame == true)
        {
            Close();
        }
    }

    // 버튼 연결 (Start 또는 Open 에서 1회 실행)
    // 패널이 꺼진 채 시작하면 Start 가 안 불리므로 Open 에서도 보장
    private void EnsureInitialized()
    {
        if (_initialized == true)
        {
            return;
        }

        if (newSaveButton != null)
        {
            newSaveButton.onClick.AddListener(OnNewSaveClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        _initialized = true;
    }

    /// <summary>패널 열기 (저장 모드) — 저장된 슬롯 스캔 후 카드 생성</summary>
    public void Open()
    {
        _isLoadMode = false;
        OpenInternal();
    }

    /// <summary>패널 열기 (불러오기 모드) — 카드 클릭 시 그 슬롯 불러오기</summary>
    public void OpenForLoad()
    {
        _isLoadMode = true;
        OpenInternal();
    }

    // 실제 열기 처리 (공통)
    private void OpenInternal()
    {
        EnsureInitialized();

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        // 불러오기 모드면 "새 저장" 버튼 숨기기
        if (newSaveButton != null)
        {
            newSaveButton.gameObject.SetActive(_isLoadMode == false);
        }

        RebuildCards();
        HideNotice();
    }

    /// <summary>패널 닫기</summary>
    public void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    // 저장된 슬롯을 스캔해서 카드 다시 생성
    public void RebuildCards()
    {
        ClearCards();

        if (SaveSystem.Instance == null)
        {
            return;
        }

        // 저장된 슬롯만 카드 생성
        for (int i = 0; i < SaveSystem.SLOT_COUNT; i++)
        {
            if (SaveSystem.Instance.HasSave(i) == true)
            {
                CreateCard(i);
            }
        }

        // 새 저장 버튼 활성/비활성 (꽉 찼으면 끔)
        UpdateNewSaveButton();
    }

    // 카드 하나 생성
    private void CreateCard(int slotIndex)
    {
        if (slotCardPrefab == null || cardContainer == null)
        {
            Debug.LogWarning("[SaveSlotPanelUI] slotCardPrefab 또는 cardContainer 가 연결 안 됨");
            return;
        }

        GameObject cardObj = Instantiate(slotCardPrefab, cardContainer);
        SaveSlotCardUI card = cardObj.GetComponent<SaveSlotCardUI>();

        if (card != null)
        {
            card.Initialize(slotIndex, this);
            _cards.Add(card);
        }
    }

    // 생성된 카드 전부 제거
    private void ClearCards()
    {
        for (int i = 0; i < _cards.Count; i++)
        {
            if (_cards[i] != null)
            {
                Destroy(_cards[i].gameObject);
            }
        }

        _cards.Clear();
    }

    // 새 저장 버튼 클릭 — 빈 슬롯 찾아 저장
    private void OnNewSaveClicked()
    {
        int emptySlot = FindEmptySlot();

        if (emptySlot < 0)
        {
            ShowNotice("저장 슬롯이 가득 찼습니다. (최대 " + SaveSystem.SLOT_COUNT + "개)");
            return;
        }

        // 기본 이름으로 저장
        string defaultName = "저장 " + (emptySlot + 1);
        SaveToSlot(emptySlot, defaultName);
    }

    // 빈 슬롯 인덱스 찾기 (없으면 -1)
    private int FindEmptySlot()
    {
        if (SaveSystem.Instance == null)
        {
            return -1;
        }

        for (int i = 0; i < SaveSystem.SLOT_COUNT; i++)
        {
            if (SaveSystem.Instance.HasSave(i) == false)
            {
                return i;
            }
        }

        return -1;
    }

    // 새 저장 버튼 상태 갱신 (꽉 차면 비활성화)
    private void UpdateNewSaveButton()
    {
        if (newSaveButton == null)
        {
            return;
        }

        bool hasEmpty = FindEmptySlot() >= 0;
        newSaveButton.interactable = hasEmpty;
    }

    /// <summary>슬롯에 저장 — 카드에서 호출</summary>
    public void SaveToSlot(int slotIndex, string saveName)
    {
        if (SaveSystem.Instance == null)
        {
            return;
        }

        SaveSystem.Instance.Save(slotIndex, saveName);
        RebuildCards();
    }

    /// <summary>슬롯 이름 변경 — 카드에서 호출</summary>
    public void RenameSlot(int slotIndex, string newName)
    {
        if (SaveSystem.Instance == null)
        {
            return;
        }

        SaveSystem.Instance.RenameSave(slotIndex, newName);
        RebuildCards();
    }

    /// <summary>슬롯 삭제 — 카드에서 호출</summary>
    public void DeleteSlot(int slotIndex)
    {
        if (SaveSystem.Instance == null)
        {
            return;
        }

        SaveSystem.Instance.DeleteSave(slotIndex);
        RebuildCards();
    }

    /// <summary>슬롯 불러오기 — 불러오기 모드에서 카드 클릭 시 호출</summary>
    public void LoadSlot(int slotIndex)
    {
        Debug.Log("[SaveSlotPanelUI] LoadSlot 호출 - 슬롯 " + slotIndex.ToString());

        if (SaveSystem.Instance == null)
        {
            Debug.LogWarning("[SaveSlotPanelUI] SaveSystem 이 null");
            return;
        }

        if (SaveSystem.Instance.HasSave(slotIndex) == false)
        {
            Debug.LogWarning("[SaveSlotPanelUI] 슬롯 " + slotIndex.ToString() + " 에 저장 없음");
            return;
        }

        // 패널 닫고 게임 불러오기
        Close();

        if (GameManager.Instance != null)
        {
            Debug.Log("[SaveSlotPanelUI] GameManager.ContinueGame 호출");
            GameManager.Instance.ContinueGame(slotIndex);
        }
        else
        {
            Debug.LogWarning("[SaveSlotPanelUI] GameManager 가 null");
        }
    }

    // 안내 메시지 표시
    private void ShowNotice(string message)
    {
        if (noticeText == null)
        {
            return;
        }

        noticeText.gameObject.SetActive(true);
        noticeText.text = message;
    }

    // 안내 메시지 숨기기
    private void HideNotice()
    {
        if (noticeText != null)
        {
            noticeText.gameObject.SetActive(false);
        }
    }
}