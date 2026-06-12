/*
 * NPCDialogue.cs
 * NPC 대화창 공용 UI 제어 (마을 NPC 전원이 하나의 대화창을 공유)
 * 디자인 시안: 중앙 창 + 좌측 초상화 + 우측 선택지 + 하단 대사바
 * 타이핑 효과 / 선택지 / ESC 닫기 / AudioManager 연동
 * 담당: 김보민
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class NPCDialogue : MonoBehaviour
{
    // ── 싱글톤 ────────────────────────────────────
    public static NPCDialogue Instance { get; private set; }

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TMP_Text npcNameText;
    public TMP_Text dialogueText;
    public Image npcPortrait;
    public Button nextButton;
    public Button closeButton;

    [Header("Dialogue Settings")]
    public float textSpeed = 0.05f;
    public bool autoClose = false;
    public float autoCloseDelay = 3f;

    [Header("Choice System")]
    public GameObject choicePanel;
    public Button[] choiceButtons;

    private Queue<string> dialogueQueue = new Queue<string>();
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private DialogueData currentDialogue;
    private System.Action<int> onChoiceSelected;

    // 대화창이 열려있는지 외부에서 확인용
    public bool IsOpen
    {
        get
        {
            if (dialoguePanel == null)
            {
                return false;
            }
            return dialoguePanel.activeSelf;
        }
    }

    private void Awake()
    {
        // 중복 인스턴스 방지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        dialoguePanel.SetActive(false);
        choicePanel.SetActive(false);

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(DisplayNextSentence);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(EndDialogue);
        }

        // 선택지 버튼 설정
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i;
            choiceButtons[i].onClick.AddListener(() => OnChoiceClicked(index));
        }
    }

    private void Update()
    {
        // 대화창 열린 상태에서 ESC 누르면 닫기
        if (IsOpen == false)
        {
            return;
        }
        if (Keyboard.current == null)
        {
            return;
        }
        if (Keyboard.current.escapeKey.wasPressedThisFrame == true)
        {
            EndDialogue();
        }
    }

    public void StartDialogue(DialogueData dialogue)
    {
        currentDialogue = dialogue;
        dialoguePanel.SetActive(true);
        choicePanel.SetActive(false);

        // NPC 정보 설정
        if (npcNameText != null)
        {
            npcNameText.text = dialogue.npcName;
        }

        if (npcPortrait != null && dialogue.npcPortrait != null)
        {
            npcPortrait.sprite = dialogue.npcPortrait;
        }

        // 대화 큐 초기화
        dialogueQueue.Clear();

        foreach (string sentence in dialogue.sentences)
        {
            dialogueQueue.Enqueue(sentence);
        }

        DisplayNextSentence();

        // 시안 반영: 선택지를 대사와 동시에 표시하는 모드 (주요 NPC 메뉴형 대화)
        if (dialogue.showChoicesImmediately == true)
        {
            if (dialogue.choices != null && dialogue.choices.Length > 0)
            {
                ShowChoices();
            }
        }

        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
    }

    // 패널을 닫지 않고 하단 대사 한 줄만 교체 (대화하기 순환용)
    public void ShowLine(string sentence)
    {
        if (IsOpen == false)
        {
            return;
        }
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        dialogueQueue.Clear();
        isTyping = false;
        typingCoroutine = StartCoroutine(TypeSentence(sentence));
    }

    public void DisplayNextSentence()
    {
        // 타이핑 중이면 즉시 완성
        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            dialogueText.text = dialogueQueue.Peek();
            isTyping = false;
            return;
        }

        // 다음 문장이 없으면
        if (dialogueQueue.Count == 0)
        {
            // 선택지 동시 표시 모드는 이미 선택지가 떠 있으므로 아무것도 안 함
            if (currentDialogue != null && currentDialogue.showChoicesImmediately == true)
            {
                return;
            }

            // 선택지가 있으면 표시
            if (currentDialogue != null && currentDialogue.choices != null && currentDialogue.choices.Length > 0)
            {
                ShowChoices();
            }
            else
            {
                EndDialogue();
            }
            return;
        }

        string sentence = dialogueQueue.Dequeue();

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeSentence(sentence));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;

            // 타이핑 사운드
            if (letter != ' ')
            {
                AudioManager.Instance?.PlaySFX(SFXClip.UIClick);
            }

            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;

        // 자동 닫기
        if (autoClose && dialogueQueue.Count == 0)
        {
            yield return new WaitForSeconds(autoCloseDelay);
            EndDialogue();
        }
    }

    private void ShowChoices()
    {
        choicePanel.SetActive(true);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < currentDialogue.choices.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);
                choiceButtons[i].GetComponentInChildren<TMP_Text>().text = currentDialogue.choices[i];
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnChoiceClicked(int choiceIndex)
    {
        // 시안 반영: endOnChoice 가 꺼져 있으면 창과 선택지를 유지한 채 콜백만 실행
        // (대화하기 처럼 하단 대사만 바뀌는 선택지에 사용)
        bool endAfterChoice = true;
        if (currentDialogue != null)
        {
            endAfterChoice = currentDialogue.endOnChoice;
        }

        if (endAfterChoice == true)
        {
            choicePanel.SetActive(false);
        }

        // 선택지 콜백 실행
        currentDialogue.onChoiceSelected?.Invoke(choiceIndex);

        AudioManager.Instance?.PlaySFX(SFXClip.UIClick);

        if (endAfterChoice == true)
        {
            EndDialogue();
        }
    }

    public void EndDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        dialoguePanel.SetActive(false);
        choicePanel.SetActive(false);
        dialogueQueue.Clear();
        isTyping = false;

        currentDialogue?.onDialogueEnd?.Invoke();

        AudioManager.Instance?.PlaySFX(SFXClip.UIClose);
    }

    public void SkipDialogue()
    {
        dialogueQueue.Clear();
        EndDialogue();
    }
}

[System.Serializable]
public class DialogueData
{
    public string npcName;
    public Sprite npcPortrait;
    public string[] sentences;
    public string[] choices;
    public System.Action<int> onChoiceSelected;
    public System.Action onDialogueEnd;

    // 선택지를 대사와 동시에 표시 (주요 NPC 메뉴형 대화)
    public bool showChoicesImmediately = false;
    // 선택지를 눌러도 대화창을 닫지 않음 (대사 순환형 선택지)
    public bool endOnChoice = true;
}