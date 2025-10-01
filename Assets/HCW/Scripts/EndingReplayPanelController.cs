using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class EndingReplayPanelController : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private GameObject endingListContainer;
    [SerializeField] private GameObject endingItemPrefab;
    [SerializeField] private Button closeButton;

    private CutsceneManager cutsceneManager;
    private Queue<CutsceneData> cutsceneSequenceQueue;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }
        cutsceneSequenceQueue = new Queue<CutsceneData>();
        gameObject.SetActive(false);
    }

    public void ShowPanel()
    {
        if (cutsceneManager == null)
        {
            cutsceneManager = FindObjectOfType<CutsceneManager>();
            if (cutsceneManager == null)
            {
                Debug.LogError("[EndingReplayPanel] CutsceneManager를 씬에서 찾을 수 없습니다!");
                return;
            }
        }

        gameObject.SetActive(true);
        PopulateEndingList();
    }

    public void HidePanel()
    {
        gameObject.SetActive(false);
    }

    private void PopulateEndingList()
    {
        foreach (Transform child in endingListContainer.transform)
        {
            Destroy(child.gameObject);
        }

        if (DataManager.Instance == null || DataManager.Instance.PlayerData == null)
        {
            Debug.LogError("[EndingReplayPanel] DataManager 또는 PlayerData가 없습니다.");
            return;
        }

        List<int> completedEndings = DataManager.Instance.PlayerData.completedEndingIds;

        if (completedEndings.Count == 0)
        {
            GameObject noEndingText = new GameObject("NoEndingText");
            noEndingText.transform.SetParent(endingListContainer.transform);
            TextMeshProUGUI textComponent = noEndingText.AddComponent<TextMeshProUGUI>();
            textComponent.text = "아직 본 엔딩이 없습니다.";
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontSize = 24;
            textComponent.color = Color.white;
            return;
        }

        var groupedEndings = completedEndings
            .Select(id => DataManager.Instance.GetEndingData(id))
            .Where(data => data != null)
            .GroupBy(data => data.EndingName);

        foreach (var endingGroup in groupedEndings)
        {
            int endingNameKey = endingGroup.Key;
            var firstEndingOfGroup = endingGroup.FirstOrDefault();
            string endingTitle = firstEndingOfGroup?.EndingTitle ?? $"엔딩 그룹 {endingNameKey}";

            GameObject itemGO = Instantiate(endingItemPrefab, endingListContainer.transform);
            TextMeshProUGUI buttonText = itemGO.GetComponentInChildren<TextMeshProUGUI>();
            Button endingButton = itemGO.GetComponent<Button>();

            if (buttonText != null)
            {
                buttonText.text = endingTitle;
            }

            if (endingButton != null)
            {
                endingButton.onClick.AddListener(() => StartEndingSequence(endingNameKey));
            }
        }
    }

    private void StartEndingSequence(int endingNameKey)
    {
        if (MultiEndingSystem.Instance == null)
        {
            Debug.LogError("[EndingReplayPanel] MultiEndingSystem 참조가 없습니다!");
            return;
        }

        List<int> completedEndings = DataManager.Instance.PlayerData.completedEndingIds;

        var endingSequenceData = completedEndings
            .Select(id => DataManager.Instance.GetEndingData(id))
            .Where(data => data != null && data.EndingName == endingNameKey)
            .OrderBy(data => data.ID)
            .ToList();

        if (endingSequenceData.Count == 0)
        {
            Debug.LogError($"[EndingReplayPanel] EndingNameKey {endingNameKey}에 해당하는 엔딩 시퀀스를 찾을 수 없습니다.");
            return;
        }

        cutsceneSequenceQueue.Clear();
        foreach (var endingStepData in endingSequenceData)
        {
            CutsceneData stepCutscene = MultiEndingSystem.Instance.ConvertToCutsceneData(endingStepData);
            if (stepCutscene != null)
            {
                cutsceneSequenceQueue.Enqueue(stepCutscene);
            }
            else
            {
                Debug.LogWarning($"[EndingReplayPanel] 엔딩 스텝 ID {endingStepData.ID}를 CutsceneData로 변환하는 데 실패했습니다.");
            }
        }

        if (cutsceneSequenceQueue.Count > 0)
        {
            Debug.Log($"[EndingReplayPanel] {endingNameKey} 그룹의 순차 재생을 시작합니다. (총 {cutsceneSequenceQueue.Count}개의 스텝)");
            HidePanel();
            CutsceneManager.OnCutsceneFinished += PlayNextInSequence;
            PlayNextInSequence();
        }
    }

    private void PlayNextInSequence()
    {
        if (cutsceneSequenceQueue.Count > 0)
        {
            CutsceneData nextCutscene = cutsceneSequenceQueue.Dequeue();
            cutsceneManager.StartCutscene(nextCutscene);
        }
        else
        {
            Debug.Log("[EndingReplayPanel] 엔딩 시퀀스 재생 완료.");
            CutsceneManager.OnCutsceneFinished -= PlayNextInSequence;
        }
    }
}