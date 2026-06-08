/*
 * SaveSlotPanelUI.cs
 * 게임 저장 슬롯 선택 패널
 * 설정창의 "게임 저장" 버튼 클릭 시 열림
 * 담당: 김보민
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 저장 슬롯 패널 — 슬롯 3개를 보여주고 저장/이름변경 처리
///
/// [씬 설정]
/// Canvas 아래 SaveSlotPanel 오브젝트에 이 스크립트 부착
/// SlotCard0, SlotCard1, SlotCard2 자식 오브젝트 각각에 SaveSlotCardUI 부착
/// </summary>
public class SaveSlotPanelUI : MonoBehaviour
{
    // 싱글턴
    public static SaveSlotPanelUI Instance { get; private set; }

    [Header("패널 루트")]
    [SerializeField] private GameObject panelRoot;

    [Header("슬롯 카드 (3개)")]
    [SerializeField] private SaveSlotCardUI slot0;
    [SerializeField] private SaveSlotCardUI slot1;
    [SerializeField] private SaveSlotCardUI slot2;

    [Header("닫기 버튼")]
    [SerializeField] private Button closeButton;

    private SaveSlotCardUI[] _slots;

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
        _slots = new SaveSlotCardUI[] { slot0, slot1, slot2 };

        // 각 슬롯에 인덱스 할당
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null)
            {
                _slots[i].Initialize(i, this);
            }
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    /// <summary>패널 열기 — 슬롯 정보 갱신 후 표시</summary>
    public void Open()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        RefreshAllSlots();
    }

    /// <summary>패널 닫기</summary>
    public void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    /// <summary>모든 슬롯 카드 정보 갱신</summary>
    public void RefreshAllSlots()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null)
            {
                _slots[i].Refresh();
            }
        }
    }

    /// <summary>슬롯에 저장 — SaveSlotCardUI 에서 호출</summary>
    public void SaveToSlot(int slotIndex, string saveName)
    {
        if (SaveSystem.Instance == null)
        {
            return;
        }

        SaveSystem.Instance.Save(slotIndex, saveName);
        RefreshAllSlots();
    }

    /// <summary>슬롯 이름 변경 — SaveSlotCardUI 에서 호출</summary>
    public void RenameSlot(int slotIndex, string newName)
    {
        if (SaveSystem.Instance == null)
        {
            return;
        }

        SaveSystem.Instance.RenameSave(slotIndex, newName);
        RefreshAllSlots();
    }
}