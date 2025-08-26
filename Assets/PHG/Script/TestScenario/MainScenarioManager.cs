// MainScenarioManager.cs

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

// [추가] MainScenarioManager가 제어할 UI 요소들을 묶어서 관리하는 전용 클래스
// 이렇게 하면 인스펙터 창이 깔끔해지고 관리가 용이해집니다.
[System.Serializable]
public class MainStoryUI
{
    [Header("메인 패널 오브젝트")]
    public GameObject panelRoot; // storyPanel 자체의 GameObject

    [Header("캐릭터 관련")]
    public Image characterImage;
    public TextMeshProUGUI characterNameText;

    [Header("대사")]
    public TextMeshProUGUI dialogueText;
}

public class MainScenarioManager : MonoBehaviour, IChoiceHandler
{
    [Header("UI 컨트롤러 참조")]
    // [수정] SituationCardController 참조를 제거하고, 필요한 UI만 남깁니다.
    [SerializeField] private UIPanelController uiPanelController;
    [SerializeField] private CardController cardController;
    [SerializeField] private ParameterUIController parameterUIController;
    [SerializeField] private Image dimmerPanel;

    [Header("메인 스토리 UI 요소")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("시나리오 시작점")]
    // [복원] 인스펙터에서 시작 노드를 직접 지정하는 방식을 사용합니다.
    [SerializeField] private StoryNode startingNode;

    private StoryNode currentNode;
    private Coroutine typingCoroutine;
    private bool IsTyping => typingCoroutine != null;

    // [복원] GameManager의 제어 없이, Start()에서 자동으로 시나리오를 시작합니다.
    void Start()
    {
        if (cardController != null) cardController.choiceHandler = this;
        if (dimmerPanel != null) dimmerPanel.color = Color.clear;

        if (startingNode != null)
        {
            // StartScenario 함수를 직접 호출합니다.
            StartScenario(startingNode);
        }
        else
        {
            Debug.LogError("시작 노드(startingNode)가 인스펙터에 할당되지 않았습니다!");
            // 시작할 스토리가 없으면 바로 비활성화하거나 다른 처리를 할 수 있습니다.
            gameObject.SetActive(false);
        }
    }

    public void StartScenario(StoryNode startNode)
    {
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

        // 1. UIPanelController로 초상화와 이름 표시
        uiPanelController.Show(currentNode.characterSprite, currentNode.characterName);

        // 2. 이 스크립트가 직접 타이핑 효과 시작
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(currentNode.storyText));

        // 3. 선택지 유무에 따라 카드 표시/숨김
        if (currentNode.choices != null && currentNode.choices.Count >= 2)
        {
            cardController.gameObject.SetActive(true);
            cardController.SetChoiceTexts(currentNode.choices[0].choiceText, currentNode.choices[1].choiceText);
            cardController.ResetCardState();
        }
        else
        {
            cardController.gameObject.SetActive(false);
            // 선택지가 하나이거나 없을 경우 자동 진행
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
        dialogueText.text = "";
        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        typingCoroutine = null;
    }

    private void EndScenario()
    {
        gameObject.SetActive(false);
        // 시나리오가 끝나면 GameManager의 다음 단계 함수를 호출 (이 부분은 유지)
        GameManager.instance.GoToBattlePanel();
    }

    // --- 나머지 IChoiceHandler 인터페이스 구현부 ---

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

    public void CycleChoice() { }

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