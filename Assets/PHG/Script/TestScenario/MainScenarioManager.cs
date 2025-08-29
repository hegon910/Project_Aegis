using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MainStoryUI
{
    [Header("���� �г� ������Ʈ")]
    public GameObject panelRoot;
    [Header("ĳ���� ����")]
    public Image characterImage;
    public TextMeshProUGUI characterNameText;
    [Header("���")]
    public TextMeshProUGUI dialogueText;
    [Header("������ �̸�����")]
    public Image choicePreviewImage;
    public TextMeshProUGUI choicePreviewText;
}

public class MainScenarioManager : MonoBehaviour, IChoiceHandler
{
    public event Action OnScenarioEnded;
    public bool IsScenarioRunning { get; private set; }

    [Header("UI ��Ʈ�ѷ� ����")]
    [SerializeField] private CardController cardController;
    [SerializeField] private ParameterUIController parameterUIController;
    [SerializeField] private Image dimmerPanel;

    [Header("���� ���丮 ���� UI")]
    [SerializeField] private MainStoryUI mainStoryUI;
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("�ó����� ������")]
    [SerializeField] private StoryNode startingNode;

    private StoryNode currentNode;
    private Coroutine typingCoroutine;
    private bool IsTyping => typingCoroutine != null;
    public bool CanMakeChoice => !IsTyping;
    private float lastClickTime = 0f;
    private const float DOUBLE_CLICK_TIME = 0.3f;

    void Start()
    {
       // if (cardController != null) cardController.choiceHandler = this;
        if (dimmerPanel != null) dimmerPanel.color = Color.clear;
        if (mainStoryUI.choicePreviewText != null)
        {
            mainStoryUI.choicePreviewText.text = "";
            mainStoryUI.choicePreviewText.color = Color.clear;
        }

        // �� [���� 1] GameManager���� �浹�� ���� ����, ���⼭ �ó������� �ڵ����� �����ϴ� �κ��� ��Ȱ��ȭ�մϴ�.
        /*
        if (startingNode != null)
        {
            StartScenario(startingNode);
        }
        else
        {
            Debug.LogError("���� ���(startingNode)�� �ν����Ϳ� �Ҵ���� �ʾҽ��ϴ�!");
            gameObject.SetActive(false);
        }
        */
    }

    public void ResetScenarioState()
    {
        StopAllCoroutines();
        typingCoroutine = null;
        currentNode = startingNode;

        IsScenarioRunning = false;

        if (mainStoryUI.dialogueText != null) mainStoryUI.dialogueText.text = "";
        if (mainStoryUI.characterNameText != null) mainStoryUI.characterNameText.text = "";
        if (mainStoryUI.characterImage != null) mainStoryUI.characterImage.color = Color.clear;
        if (cardController != null) cardController.gameObject.SetActive(false);
        Debug.Log("MainScenarioManager ���°� �ʱ�ȭ�Ǿ����ϴ�.");
    }

    public void BeginScenarioFromStart()
    {
        IsScenarioRunning = true; // ���� �÷��� ����
        currentNode = startingNode;
        if (currentNode != null)
        {
            mainStoryUI.panelRoot.SetActive(true);
            DisplayNode(currentNode);
        }
        else
        {
            Debug.LogError("���� ��尡 �����ϴ�!");
        }
    }

    private void EndScenario()
    {
        IsScenarioRunning = false; // ���� �÷��� ����
        mainStoryUI.panelRoot.SetActive(false);
        GameManager.instance.GoToBattlePanel();
    }


  

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time - lastClickTime < DOUBLE_CLICK_TIME)
            {
                if (IsTyping)
                {
                    StopCoroutine(typingCoroutine);
                    mainStoryUI.dialogueText.text = currentNode.storyText;
                    typingCoroutine = null;
                }
            }
            lastClickTime = Time.time;
        }
    }

    public void UpdateChoicePreview(string text, Color color)
    {
        if (mainStoryUI.choicePreviewImage != null)
        {
            Color imageColor = mainStoryUI.choicePreviewImage.color;
            imageColor.a = color.a;
            mainStoryUI.choicePreviewImage.color = imageColor;
        }
        if (mainStoryUI.choicePreviewText != null)
        {
            mainStoryUI.choicePreviewText.text = text;
            mainStoryUI.choicePreviewText.color = color;
        }
    }

    public void StartScenario(StoryNode startNode)
    {
        mainStoryUI.panelRoot.SetActive(true);
        DisplayNode(startNode);
    }

    private void DisplayNode(StoryNode node)
    {
        if (node == null)
        {
            EndScenario();
            return;
        }
        currentNode = node;
        mainStoryUI.characterNameText.text = currentNode.characterName;
        if (currentNode.characterSprite != null)
        {
            mainStoryUI.characterImage.sprite = currentNode.characterSprite;
            mainStoryUI.characterImage.color = Color.white;
        }
        else
        {
            mainStoryUI.characterImage.color = Color.clear;
        }
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(currentNode.storyText));
        // ������ ǥ�� ������ �� ������� �ʴ��� Ȯ���ϱ� ���� �� �α׸� �߰��մϴ�.
        Debug.Log($"--- [����] DisplayNode ����: ���� ��� '{currentNode.name}' ---");
        if (currentNode.choices == null)
        {
            Debug.LogError("[����] currentNode.choices ����Ʈ�� NULL�Դϴ�!");
        }
        else
        {
            Debug.Log($"[����] currentNode.choices ����Ʈ�� ���Ե� ������ ����: {currentNode.choices.Count}");
        }
        if (currentNode.choices != null && currentNode.choices.Count >= 2)
        {
            if (cardController != null) cardController.choiceHandler = this;
            Debug.Log("[����] ������ ǥ�� ���� ����. ī�� UI�� Ȱ��ȭ�մϴ�.");
            cardController.gameObject.SetActive(true);
            cardController.SetChoiceTexts(currentNode.choices[0].choiceText, currentNode.choices[1].choiceText);
            cardController.ResetCardState();
        }
        else
        {
            Debug.Log("[����] �������� ���ų� �����Ͽ� �ڵ� ���� �������� �Ѿ�ϴ�.");
            cardController.gameObject.SetActive(false);
            StartCoroutine(AutoTransitionToNext(
                (currentNode.choices != null && currentNode.choices.Count == 1) ? currentNode.choices[0].nextNode : null
            ));
        }
    }

    public void HandleChoice(bool isRightChoice)
    {
        if (IsTyping) return;
        int choiceIndex = isRightChoice ? 1 : 0;
        Choice selectedChoice = currentNode.choices[choiceIndex];
        if (selectedChoice.parameterChanges != null && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ApplyChanges(selectedChoice.parameterChanges);
        }
        StartCoroutine(TransitionToNextNode(selectedChoice.nextNode));
    }

    private IEnumerator TransitionToNextNode(StoryNode nextNode)
    {
        cardController.gameObject.SetActive(false);
        yield return new WaitForSeconds(1.0f);
        DisplayNode(nextNode);
    }

    private IEnumerator AutoTransitionToNext(StoryNode nextNode)
    {
        yield return new WaitUntil(() => !IsTyping);
        yield return new WaitForSeconds(1.5f);
        DisplayNode(nextNode);
    }

    private IEnumerator TypeText(string text)
    {
        mainStoryUI.dialogueText.text = "";
        foreach (char letter in text.ToCharArray())
        {
            mainStoryUI.dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        typingCoroutine = null;
    }

    public void PreviewAffectedParameters(bool isRightChoice)
    {
        if (currentNode.choices.Count < 2) return;
        Choice choiceToPreview = currentNode.choices[isRightChoice ? 1 : 0];
        if (parameterUIController != null && choiceToPreview.parameterChanges != null)
        {
            var previewChanges = choiceToPreview.parameterChanges.Where(c => c.valueChange != 0).ToList();
            parameterUIController.UpdateAffectedToggles(previewChanges);
        }
    }

    public void ClearParameterPreview()
    {
        if (parameterUIController != null)
        {
            parameterUIController.ClearAllToggles();
        }
    }

    public void UpdateDimmer(float alpha)
    {
        if (dimmerPanel != null)
        {
            dimmerPanel.color = new Color(0, 0, 0, alpha);
        }
    }
}