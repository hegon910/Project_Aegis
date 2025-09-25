using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleReplayPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject replayCanvas;
    [SerializeField] private GameObject replayPanel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private ScrollRect eventScrollRect;
    [SerializeField] private Transform eventListParent;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button clearButton;

    private SimpleEventHistoryManager historyManager;

    private void Start()
    {
        historyManager = SimpleEventHistoryManager.Instance;

        if (replayCanvas != null)
            replayCanvas.SetActive(false);

        if (replayPanel != null)
            replayPanel.SetActive(false);

        if (openButton != null)
            openButton.onClick.AddListener(OpenReplayPanel);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseReplayPanel);

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearHistory);

        UpdateStatusText();
    }

    public void OpenReplayPanel()
    {
        if (replayCanvas != null)
        {
            replayCanvas.SetActive(true);
            RefreshPlaythroughList();
        }
    }

    public void CloseReplayPanel()
    {
        if (replayCanvas != null)
        {
            replayCanvas.SetActive(false);
        }
    }

    private void RefreshPlaythroughList()
    {
        if (historyManager == null || eventListParent == null) return;

        foreach (Transform child in eventListParent)
        {
            Destroy(child.gameObject);
        }

        List<GamePlaythroughRecord> records = historyManager.GetPlaythroughHistory();

        // *** 추가된 코드: 로그를 통해 리스트의 개수 확인 ***
        Debug.Log($"[SimpleReplayPanel] 불러온 플레이 기록 수: {records.Count}");

        if (statusText != null)
        {
            statusText.text = $"총 {records.Count}개의 플레이 기록";
        }

        for (int i = records.Count - 1; i >= 0; i--)
        {
            CreatePlaythroughItem(records[i]);
        }
    }

    private void CreatePlaythroughItem(GamePlaythroughRecord record)
    {
        var recordItem = new GameObject($"RecordItem_{record.playDate}");
        recordItem.transform.SetParent(eventListParent);

        var rectTransform = recordItem.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.offsetMin = new Vector2(10, -50);
        rectTransform.offsetMax = new Vector2(-10, 0);
        rectTransform.anchoredPosition = new Vector2(0, -60 * (eventListParent.childCount - 1));

        var image = recordItem.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        string buttonText = $"{record.playDate} | {record.playDuration} | {record.outcome}";
        var recordText = CreateText("RecordText", recordItem.transform, buttonText, 14, Color.white, new Vector2(0, 0), new Vector2(1, 1));

        var button = recordItem.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => Debug.Log($"플레이 기록 클릭: {record.playDate}"));
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, int fontSize, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        var textObj = new GameObject(name);
        textObj.transform.SetParent(parent);

        var rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = new Vector2(10, 5);
        rectTransform.offsetMax = new Vector2(-10, -5);

        var textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = color;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.overflowMode = TextOverflowModes.Ellipsis;

        return textComponent;
    }

    private void OnEventClicked(SimpleEventRecord eventRecord) { }

    private void ClearHistory()
    {
        if (historyManager != null)
        {
            historyManager.ClearHistory();
            RefreshPlaythroughList();
        }
    }

    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            if (historyManager != null)
            {
                var records = historyManager.GetPlaythroughHistory();
                statusText.text = $"총 {records.Count}개의 플레이 기록";
            }
            else
            {
                statusText.text = "이벤트 기록 시스템을 찾을 수 없습니다.";
            }
        }
    }

    private void Update()
    {
        UpdateStatusText();
    }
}