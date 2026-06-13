/*
 * NPCDialogue.cs
 * NPC 대화창 공용 UI 제어 (마을 NPC 전원이 하나의 대화창을 공유)
 * 디자인 시안: 중앙 창 + 좌측 초상화 + 우측 선택지 + 하단 대사바
 * 타이핑 효과 / 선택지 / ESC 닫기 / AudioManager 연동
 * 담당: 김보민
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class NPCDialogue : MonoBehaviour, IPointerClickHandler
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
        // 대화창은 마을 전용이라 씬마다 새로 생긴다.
        // 씬을 다시 로드하면 새 인스턴스가 자신을 등록한다.
        // 이전 씬의 인스턴스는 함께 파괴되므로 자기 자신을 파괴하지 않는다.
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
            choiceButtons[i].onClick.AddListener(delegate ()
            {
                OnChoiceClicked(index);
            });
        }
    }

    private void Update()
    {
        // 대화창 열린 상태에서만 키 입력 처리
        if (IsOpen == false)
        {
            return;
        }
        if (Keyboard.current == null)
        {
            return;
        }

        // ESC 누르면 닫기
        if (Keyboard.current.escapeKey.wasPressedThisFrame == true)
        {
            EndDialogue();
            return;
        }

        // 스페이스바로 다음 대사 진행 (선택지가 안 떠 있을 때만)
        if (Keyboard.current.spaceKey.wasPressedThisFrame == true)
        {
            TryAdvance();
        }
    }

    // 대화 패널을 마우스로 클릭하면 다음 대사 진행 (선택지가 안 떠 있을 때만)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsOpen == false)
        {
            return;
        }
        TryAdvance();
    }

    // 다음 대사로 넘길 수 있는 상황인지 확인 후 진행
    // 선택지가 떠 있으면(메뉴형 대화) 스페이스/클릭을 무시해 선택을 방해하지 않음
    private void TryAdvance()
    {
        if (choicePanel != null && choicePanel.activeSelf == true)
        {
            return;
        }
        DisplayNextSentence();
    }

    public void StartDialogue(DialogueData dialogue)
    {
        currentDialogue = dialogue;

        // 패널 참조가 파괴되었으면 대화를 시작하지 않는다 (씬 전환 직후 안전장치)
        if (dialoguePanel == null)
        {
            Debug.LogWarning("[NPCDialogue] dialoguePanel 참조가 없어 대화를 시작할 수 없습니다.");
            return;
        }

        dialoguePanel.SetActive(true);

        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }

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
        if (currentDialogue == null)
        {
            return;
        }

        // 시안 반영: endOnChoice 가 꺼져 있으면 창과 선택지를 유지한 채 콜백만 실행
        // (대화하기 처럼 하단 대사만 바뀌는 선택지에 사용)
        bool endAfterChoice = currentDialogue.endOnChoice;

        // 콜백을 미리 로컬에 보관한다.
        // 콜백 안에서 EndDialogue 가 호출되면 currentDialogue 가 정리될 수 있기 때문이다.
        System.Action<int> callback = currentDialogue.onChoiceSelected;

        if (endAfterChoice == true)
        {
            if (choicePanel != null)
            {
                choicePanel.SetActive(false);
            }
        }

        // 클릭 사운드 먼저 재생
        AudioManager.Instance?.PlaySFX(SFXClip.UIClick);

        // 선택지 콜백 실행 (이 안에서 대화창이 닫히거나 다음 대사로 바뀔 수 있음)
        if (callback != null)
        {
            callback.Invoke(choiceIndex);
        }

        // endAfterChoice 가 true 이고 콜백이 아직 대화를 닫지 않았다면 여기서 닫는다.
        if (endAfterChoice == true)
        {
            if (IsOpen == true)
            {
                EndDialogue();
            }
        }
    }

    public void EndDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }
        dialogueQueue.Clear();
        isTyping = false;

        // 종료 콜백을 로컬에 담아 실행한다 (실행 중 currentDialogue 가 바뀌어도 안전).
        DialogueData ended = currentDialogue;
        currentDialogue = null;

        if (ended != null)
        {
            if (ended.onDialogueEnd != null)
            {
                ended.onDialogueEnd.Invoke();
            }
        }

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