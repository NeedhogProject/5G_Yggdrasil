using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class NPCDialogue : MonoBehaviour
{
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
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIOpen);
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
        choicePanel.SetActive(false);
        
        // 선택지 콜백 실행
        currentDialogue.onChoiceSelected?.Invoke(choiceIndex);
        
        AudioManager.Instance?.PlaySFX(SFXClip.UIClick);
        
        EndDialogue();
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
}